using System.Diagnostics;
using GameNetcodeStuff;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Logger = UnityEngine.Logger;

namespace VELDDev.BackroomsRenewed.BackroomsManagement;

public class Backrooms : NetworkBehaviour
{
    const float CELL_SIZE = 8f; // may be modified depending on how big I make the cells in blender

    public static Backrooms Instance;

    public List<BackroomThemeInfo> themes;          // Assign in inspector, available backroom themes
    public GameObject exitPrefab;                   // Assign in inspector, exit prefab
    public Transform CellsHolder;                   // Assign in inspector, parent transform for all cells
    public NavMeshSurface BackroomsNavMesh;          // Assign in inspector, the NavMeshSurface component to build the navmesh
    public GameObject BackroomsLightCover;          // Assign in inspector, light cover prefab to place above the backrooms to prevent light from leaking
    public BackroomsGenerator generator;            // Assign in inspector, the maze generator component
    public AnimationCurve lightTwinkleLightCurve;

    [HideInInspector]
    public NetworkVariable<bool> IsGenerated = new(false);

    [HideInInspector] public CellBehaviour[,] Cells;

    public BackroomThemeInfo CurrentTheme { get; private set; }

    private readonly Dictionary<CellVariantInfo, int> _variantUsageCount = [];
    private readonly HashSet<CellVariantInfo> _requiredVariantsNotYetSpawned = [];
    private float _timeSinceLastTwinkleCheck = 0f;
    private float _nextTwinkleCheckTime = 0f;
    
    private ManualLogSource Logger => Plugin.Instance.logger;

    void Awake()
    {
        if(!Instance)
        {
            Instance = this;
        }
    }

    void Start()
    {
        generator.algorithm = LocalConfig.Singleton.GenerationAlgorithm.Value;
        var size = Random.Range(LocalConfig.Singleton.MinBackroomsSize.Value, LocalConfig.Singleton.MaxBackroomsSize.Value + 1);
        generator.width = size;
        generator.height = size;
        if(IsServer)
        {
            StartCoroutine(GenerateBackrooms());
        }
    }

    private void SelectTheme()
    {
        if (themes == null || themes.Count == 0)
        {
            Logger.LogError("No themes configured! Cannot generate backrooms.");
            return;
        }

        // Single theme - no need for weighted selection
        if (themes.Count == 1)
        {
            CurrentTheme = themes[0];
            Logger.LogInfo($"Theme selected: {CurrentTheme.themeName}");
            return;
        }

        // Weighted random selection
        float totalWeight = themes.Sum(t => t.weight);
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var theme in themes)
        {
            cumulativeWeight += theme.weight;
            if (randomValue <= cumulativeWeight)
            {
                CurrentTheme = theme;
                Logger.LogInfo($"Theme selected: {CurrentTheme.themeName}");
                return;
            }
        }

        // Fallback
        CurrentTheme = themes[^1];
        Logger.LogInfo($"Theme selected: {CurrentTheme.themeName}");
    }

    void FixedUpdate()
    {
        // Not denesting in case I'm adding more stuff
        if(NetworkManager.Singleton.IsHost && IsServer && IsGenerated.Value)
        {
            if(_timeSinceLastTwinkleCheck < _nextTwinkleCheckTime)
            {
                _timeSinceLastTwinkleCheck += Time.deltaTime;
                return;
            }
            else
            {
                TwinkleRandomLightsClientRpc();
                _timeSinceLastTwinkleCheck = 0f;
                _nextTwinkleCheckTime = Random.Range(3f, 15f);
            }
        }
    }

    /// <summary>
    /// Teleports a player to a random position in the backrooms.
    /// This method is server-authoritative - it determines the position and broadcasts to all clients.
    /// </summary>
    /// <param name="targetPlayer">The player to teleport</param>
    /// <param name="dropItems">Whether to drop held items before teleporting</param>
    public void TeleportPlayerToBackrooms(PlayerControllerB targetPlayer, bool dropItems = false)
    {
        if (IsServer)
        {
            // Server picks the position and broadcasts to all clients
            var randomPos = PickRandomPosOnNavmesh();
            var targetPos = randomPos ?? GetFallbackPosition();

            if (!randomPos.HasValue)
            {
                Logger.LogWarning("Failed to find valid NavMesh position, using fallback center position");
            }

            TeleportPlayerClientRpc(targetPlayer.playerClientId, targetPos, dropItems);
        }
        else
        {
            // Client requests teleportation from server
            RequestTeleportServerRpc(targetPlayer.playerClientId, dropItems);
        }
    }

    /// <summary>
    /// Legacy method for backwards compatibility. Calls TeleportPlayerToBackrooms.
    /// </summary>
    [Obsolete("Use TeleportPlayerToBackrooms instead for server-authoritative teleportation")]
    public void TeleportLocalPlayerSomewhereInBackrooms(PlayerControllerB targetPlayer)
    {
        TeleportPlayerToBackrooms(targetPlayer, SyncedConfig.Instance.DropHeldItemsOnTeleport);
    }

    /// <summary>
    /// Request the server to teleport a player to the backrooms.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestTeleportServerRpc(ulong playerClientId, bool dropItems)
    {
        var randomPos = PickRandomPosOnNavmesh();
        var targetPos = randomPos ?? GetFallbackPosition();

        if (!randomPos.HasValue)
        {
            Logger.LogWarning("Failed to find valid NavMesh position, using fallback center position");
        }

        TeleportPlayerClientRpc(playerClientId, targetPos, dropItems);
    }

    /// <summary>
    /// Server broadcasts teleportation to all clients. Each client executes teleport for the target player.
    /// </summary>
    [ClientRpc]
    private void TeleportPlayerClientRpc(ulong playerClientId, Vector3 position, bool dropItems)
    {
        var targetPlayer = GetPlayerByClientId(playerClientId);
        if (targetPlayer == null)
        {
            Logger.LogWarning($"TeleportPlayerClientRpc: Could not find player with clientId {playerClientId}");
            return;
        }

        if (dropItems && targetPlayer.IsOwner)
        {
            targetPlayer.DropAllHeldItems();
            targetPlayer.DisableJetpackControlsLocally();
        }

        targetPlayer.TeleportPlayer(position, true);
    }

    private Vector3 GetFallbackPosition()
    {
        return new Vector3(
            (generator.width * CELL_SIZE) / 2f,
            -1000f,
            (generator.height * CELL_SIZE) / 2f
        );
    }

    private PlayerControllerB GetPlayerByClientId(ulong clientId)
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.playerClientId == clientId)
            {
                return player;
            }
        }
        return null;
    }

    private Vector3? PickRandomPosOnNavmesh(int maxAttempts = 30)
    {
        // Calculate the bounds of the backrooms
        float minX = 0f;
        float maxX = generator.width * CELL_SIZE;
        float minZ = 0f;
        float maxZ = generator.height * CELL_SIZE;
        float y = -1000f; // Backrooms floor level

        for(int i = 0; i < maxAttempts; i++)
        {
            // Pick a random point within the backrooms bounds
            var randomPoint = new Vector3(
                Random.Range(minX, maxX),
                y,
                Random.Range(minZ, maxZ)
            );

            // Try to find the nearest point on the NavMesh
            if(NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, CELL_SIZE, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return null;
    }
    
    private IEnumerator GenerateBackrooms()
    {
        if(!NetworkManager.Singleton.IsHost && !IsServer)
            yield break;

        // Select theme for this generation
        SelectTheme();
        if (CurrentTheme == null)
            yield break;

        // Reset usage counter and required variants tracking for new generation
        _variantUsageCount.Clear();
        _requiredVariantsNotYetSpawned.Clear();
        foreach(var variant in CurrentTheme.CellsVariants)
        {
            _variantUsageCount[variant] = 0;
            if(variant.mustSpawnAtLeastOnce)
            {
                _requiredVariantsNotYetSpawned.Add(variant);
            }
        }

        yield return generator.Generate();
        Cells = new CellBehaviour[generator.width, generator.height];
        
        // Set navmesh location and size
        /*
        var backroomsCenter = new Vector3((generator.width * CELL_SIZE) / 2f, 0, (generator.height * CELL_SIZE) / 2f);
        BackroomsNavMesh.center = backroomsCenter;
        BackroomsNavMesh.size = new Vector3(generator.width * CELL_SIZE, 1f, generator.height * CELL_SIZE);
        BackroomsNavMesh.navMeshData = new NavMeshData();
        BackroomsNavMesh.AddData();
        */

        var sw = Stopwatch.StartNew();
        var globalSw = Stopwatch.StartNew();
        // Instantiate cells for all clients, should make a rectangle.
        for(int x = 0; x < generator.width; x++)
        {
            for(int y = 0; y < generator.height; y++)
            {
                var cell = generator.cells[x, y];
                var selectedVariant = GetWeightedRandomVariant();
                var cellgo = Instantiate(selectedVariant.variantPrefab, CellsHolder);
                cellgo.transform.localPosition = new Vector3(CELL_SIZE * x, 0, CELL_SIZE * y);
                cellgo.GetComponent<NetworkObject>().Spawn(true);
                var cellmono = cellgo.GetComponent<CellBehaviour>();

                // It's hardcoded, it's not clean, but it's optimized and effective at least.
                if(y == 0)
                {
                    if(x == 0)
                    {
                        cell.Walls |= WallFlags.West | WallFlags.South;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.Walls |= WallFlags.East | WallFlags.South;
                    }
                    else
                    {
                        cell.Walls |= WallFlags.South;
                    }
                }
                else if(y == generator.height - 1)
                {
                    if(x == 0)
                    {
                        cell.Walls |= WallFlags.West | WallFlags.North;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.Walls |= WallFlags.East | WallFlags.North;
                    }
                    else
                    {
                        cell.Walls |= WallFlags.North;
                    }
                }
                else
                {
                    if(x == 0)
                    {
                        cell.Walls |= WallFlags.West;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.Walls |= WallFlags.East;
                    }
                    else
                    {
                        // This is my ingenious *cough* way to remove duplicate walls
                        // I remove these walls as the previous cells already have it !
                        // The next cell doesn't need to remove its walls !
                        // I call this easy optimization.
                        cell.Walls &= ~(WallFlags.South | WallFlags.West);
                    }
                }

                const int LIGHT_GO_CHANCE_PERCENT = 30;
                const int LIGHT_ON_CHANCE_PERCENT = 60;
                var putLightFlag = Random.Range(0, 101) < LIGHT_GO_CHANCE_PERCENT;
                if(putLightFlag)
                {
                    var lightOnFlag = Random.Range(0, 101) < LIGHT_ON_CHANCE_PERCENT;
                    cellmono.InitializeClientRpc(cell, true, lightOnFlag);
                }
                else
                {
                    cellmono.InitializeClientRpc(cell, false, false);
                }
                Cells[x, y] = cellmono;
                
                // Update navmesh periodically
                /*
                if (!SyncedConfig.Instance.LegacyNavMeshGen && y % 5 == 0)
                {
                    sw.Restart();
                    yield return BackroomsNavMesh.UpdateNavMesh(BackroomsNavMesh.navMeshData);
                    sw.Stop();
                    Logger.LogInfo($"Navmesh refreshed in {sw.ElapsedMilliseconds:N3}ms");
                }
                */
                if (sw.ElapsedMilliseconds > 16)
                {
                    yield return null;
                    sw.Restart();
                }
            }
        }

        sw.Stop();
        globalSw.Stop();
        Logger.LogInfo($"Generated backrooms in {globalSw.ElapsedMilliseconds:N3}ms");

        // Final update in case the generation of 
        // yield return BackroomsNavMesh.UpdateNavMesh(BackroomsNavMesh.navMeshData);
        
        SetupBackroomsClientRpc(generator.width, generator.height);
    }

    [ClientRpc]
    private void SetupBackroomsClientRpc(int width, int length)
    {
        // Set anti-light leak cover location and size
        var backroomsCenter = new Vector3((width * CELL_SIZE) / 2f, 5f, (length * CELL_SIZE) / 2f);
        BackroomsLightCover.transform.localPosition = backroomsCenter;
        BackroomsLightCover.transform.localScale = new Vector3(width * CELL_SIZE, 1f, length * CELL_SIZE) * 1.1f;
        
        // Navmesh may be baked in the future by making one navmesh surface for each cell
        // and adding navmesh links between cells
        if (SyncedConfig.Instance.LegacyNavMeshGen)
        {
            var sw = Stopwatch.StartNew();
            BackroomsNavMesh.BuildNavMesh();
            sw.Stop();
            Logger.LogInfo($"Built navmesh in {sw.ElapsedMilliseconds:N3}ms");
        }
        else
        {
            Logger.LogInfo("Refreshing Navmesh...");
            var sw = Stopwatch.StartNew();
            StartCoroutine(RefreshNavmeshesAsync());
            sw.Stop();
            Logger.LogInfo($"Navmesh refreshed in {sw.ElapsedMilliseconds:N3}ms");
        }
        // More research to be done for this smh
    }

    private IEnumerator RefreshNavmeshesAsync()
    {
        var backroomsCenter = new Vector3((generator.width * CELL_SIZE) / 2f, 0, (generator.height * CELL_SIZE) / 2f);
        BackroomsNavMesh.center = backroomsCenter;
        BackroomsNavMesh.size = new Vector3(generator.width * CELL_SIZE, 1f, generator.height * CELL_SIZE);
        BackroomsNavMesh.navMeshData = new NavMeshData();
        BackroomsNavMesh.AddData();
        yield return BackroomsNavMesh.UpdateNavMesh(BackroomsNavMesh.navMeshData);
    }
    
    public override void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    [ClientRpc]
    private void TwinkleRandomLightsClientRpc()
    {
        foreach(var cell in Cells)
        {
            if(!cell.hasLightSource)
                continue;
            var twinkleChance = Random.Range(0f, 100f);
            if(twinkleChance < 10f)
            {
                cell.TwinkleLight(lightTwinkleLightCurve, Random.Range(1f, lightTwinkleLightCurve.keys[^1].time));
            }
        }
    }

    private CellVariantInfo GetWeightedRandomVariant()
    {
        // If there are required variants that haven't spawned yet, prioritize them
        // It's not optimal as they may all spawn in a corner of the backrooms but
        // It will do the trick for now.
        if(_requiredVariantsNotYetSpawned.Count > 0)
        {
            // Get available required variants (respecting max amount)
            var availableRequiredVariants = _requiredVariantsNotYetSpawned
                .Where(v => v.maxAmount == -1 || _variantUsageCount[v] < v.maxAmount)
                .ToList();

            if(availableRequiredVariants.Count > 0)
            {
                var selectedVariant = SelectWeightedRandom(availableRequiredVariants);
                _requiredVariantsNotYetSpawned.Remove(selectedVariant);
                return selectedVariant;
            }
        }

        // Normal weighted random selection for all available variants
        var availableVariants = CurrentTheme.CellsVariants.Where(v =>
            v.maxAmount == -1 || _variantUsageCount[v] < v.maxAmount
        ).ToList();

        // If no variants available (all maxed out), return the first variant as fallback
        if(availableVariants.Count == 0)
        {
            return CurrentTheme.CellsVariants[0];
        }

        return SelectWeightedRandom(availableVariants);
    }

    private CellVariantInfo SelectWeightedRandom(List<CellVariantInfo> variants)
    {
        float totalWeight = variants.Sum(v => v.weight);
        float randomValue = Random.Range(0f, totalWeight);

        // Select variant based on cumulative weights
        float cumulativeWeight = 0f;
        foreach(var variant in variants)
        {
            cumulativeWeight += variant.weight;
            if(randomValue <= cumulativeWeight)
            {
                _variantUsageCount[variant]++;
                return variant;
            }
        }

        // Fallback (should rarely happen)
        var selected = variants[^1];
        _variantUsageCount[selected]++;
        return selected;
    }
}
