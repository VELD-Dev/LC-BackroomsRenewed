using System.Diagnostics;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using GameNetcodeStuff;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Logger = UnityEngine.Logger;

namespace VELDDev.BackroomsRenewed.BackroomsManagement;

public class Backrooms : NetworkBehaviour
{
    const float CELL_SIZE = 16f; // may be modified depending on how big I make the cells in blender

    public static Backrooms Instance;

    public List<BackroomThemeInfo> themes;          // Assign in inspector, available backroom themes
    public Transform CellsHolder;                   // Assign in inspector, parent transform for all cells
    public NavMeshSurface BackroomsNavMesh;          // Assign in inspector, the NavMeshSurface component to build the navmesh
    public GameObject BackroomsLightCover;          // Assign in inspector, light cover prefab to place above the backrooms to prevent light from leaking
    public BackroomsGenerator generator;            // Assign in inspector, the maze generator component
    public AnimationCurve lightTwinkleLightCurve;
    public AudioSource ambientMusicSource;
    public AudioSource ambientNoiseSource;

    [HideInInspector]
    public NetworkVariable<bool> IsGenerated = new(false);

    [HideInInspector] public NetworkList<ulong> PlayersInBackrooms;

    [HideInInspector] public CellBehaviour[,] Cells;

    public BackroomThemeInfo CurrentTheme { get; private set; }

    private readonly Dictionary<CellVariantInfo, int> _variantUsageCount = [];
    private float _timeSinceLastTwinkleCheck = 0f;
    private float _nextTwinkleCheckTime = 0f;
    
    private ManualLogSource Logger => Plugin.Instance.logger;

    void Awake()
    {
        PlayersInBackrooms = new NetworkList<ulong>();
        
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
        generator.exitCount = LocalConfig.Singleton.ExitCount.Value;
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
        if(IsServer && IsGenerated.Value)
        {
            if(_timeSinceLastTwinkleCheck < _nextTwinkleCheckTime)
            {
                _timeSinceLastTwinkleCheck += Time.deltaTime;
                return;
            }

            TwinkleRandomLightsClientRpc();
            _timeSinceLastTwinkleCheck = 0f;
            _nextTwinkleCheckTime = Random.Range(3f, 15f);
            Logger.LogInfo($"Twinkle check completed, next check in {_nextTwinkleCheckTime} seconds");
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
        targetPlayer.ResetFallGravity();
        targetPlayer.isInsideFactory = true;
        StartPlayingAmbientAudios();
        PlayersInBackrooms.Add(playerClientId);
    }

    private Vector3 GetFallbackPosition()
    {
        return new Vector3(
            transform.position.x,
            transform.position.y + 2,
            transform.position.z
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
        if (!CurrentTheme)
        {
            Logger.LogError($"Couldn't generate the backrooms: Selected theme is null !");
            yield break;
        }

        // Reset usage counter for new generation
        _variantUsageCount.Clear();
        foreach(var variant in CurrentTheme.CellsVariants)
        {
            _variantUsageCount[variant] = 0;
        }

        Logger.LogInfo("Starting generation...");
        yield return generator.Generate();
        Cells = new CellBehaviour[generator.width, generator.height];

        // Decide every cell's variant up front in two passes
        var variantLayout = BuildVariantLayout();
        
        // Set navmesh location and size
        /*
        var backroomsCenter = new Vector3((generator.width * CELL_SIZE) / 2f, 0, (generator.height * CELL_SIZE) / 2f);
        BackroomsNavMesh.center = backroomsCenter;
        BackroomsNavMesh.size = new Vector3(generator.width * CELL_SIZE, 1f, generator.height * CELL_SIZE);
        BackroomsNavMesh.navMeshData = new NavMeshData();
        BackroomsNavMesh.AddData();
        */

        Logger.LogInfo("Placing the cells in the world...");
        var sw = Stopwatch.StartNew();
        var globalSw = Stopwatch.StartNew();
        // Instantiate cells for all clients, should make a rectangle.
        for(int x = 0; x < generator.width; x++)
        {
            for(int y = 0; y < generator.height; y++)
            {
                var cell = generator.cells[x, y];
                var selectedVariant = variantLayout[x, y];
                var cellgo = Instantiate(selectedVariant.variantPrefab, CellsHolder);
                cellgo.transform.localPosition = new Vector3(CELL_SIZE * x, 0, CELL_SIZE * y);
                cellgo.GetComponent<NetworkObject>().Spawn(true);
                var cellmono = cellgo.GetComponent<CellBehaviour>();

                // It's hardcoded, it's not clean, but it's optimized and effective at least.
                if(y == 0)
                {
                    if(x == 0)
                    {
                        cell.walls |= WallFlags.West | WallFlags.South;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.walls |= WallFlags.East | WallFlags.South;
                    }
                    else
                    {
                        cell.walls |= WallFlags.South;
                    }
                }
                else if(y == generator.height - 1)
                {
                    if(x == 0)
                    {
                        cell.walls |= WallFlags.West | WallFlags.North;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.walls |= WallFlags.East | WallFlags.North;
                    }
                    else
                    {
                        cell.walls |= WallFlags.North;
                    }
                }
                else
                {
                    if(x == 0)
                    {
                        cell.walls |= WallFlags.West;
                    }
                    else if(x == generator.width - 1)
                    {
                        cell.walls |= WallFlags.East;
                    }
                    else
                    {
                        // This is my ingenious *cough* way to remove duplicate walls
                        // I remove these walls as the previous cells already have it !
                        // The next cell doesn't need to remove its walls !
                        // I call this easy optimization.
                        cell.walls &= ~(WallFlags.South | WallFlags.West);
                    }
                }

                const int LIGHT_GO_CHANCE_PERCENT = 60;
                const int LIGHT_ON_CHANCE_PERCENT = 90;
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
                
                //cellmono.gameObject.transform.SetParent(CellsHolder);
                
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
                    Logger.LogDebug($"Yielding frame; {sw.ElapsedMilliseconds:N0}ms elapsed since last frame.");
                    yield return null;
                    sw.Restart();
                }
            }
        }

        sw.Stop();
        globalSw.Stop();
        Logger.LogInfo($"Generated backrooms in {globalSw.ElapsedMilliseconds:N0}ms");

        Logger.LogInfo($"Spawning {generator.exitPositions.Count} exit door(s)...");
        foreach (var exit in generator.exitPositions)
        {
            var cellBehaviour = Cells[exit.position.x, exit.position.y];
            var socket = cellBehaviour.GetExitDoorSocket(exit.direction);
            if (socket == null)
            {
                Logger.LogWarning($"No exit socket found for cell {exit.position} facing {exit.direction}, skipping.");
                continue;
            }
            var exitGo = Instantiate(CurrentTheme.ExitPrefab, socket.position, socket.rotation);
            exitGo.GetComponent<NetworkObject>().Spawn(true);
            Logger.LogDebug($"Spawned exit door at cell {exit.position} facing {exit.direction}");
        }

        // Final update in case the generation of
        // yield return BackroomsNavMesh.UpdateNavMesh(BackroomsNavMesh.navMeshData);

        Logger.LogInfo("Finished backrooms generation");
        SetupBackroomsClientRpc(generator.width, generator.height);
    }

    [ClientRpc]
    private void SetupBackroomsClientRpc(int width, int length)
    {
        // Set anti-light leak cover location and size
        var backroomsCenter = new Vector3((width * CELL_SIZE) / 2f, 10f, (length * CELL_SIZE) / 2f);
        BackroomsLightCover.transform.localPosition = backroomsCenter;
        BackroomsLightCover.transform.localScale = new Vector3(width, 1f, length) * 1.5f;
        Logger.LogInfo("Set backrooms light cover");
        
        // Navmesh may be baked in the future by making one navmesh surface for each cell
        // and adding navmesh links between cells
        if (SyncedConfig.Instance.LegacyNavMeshGen)
        {
            var sw = Stopwatch.StartNew();
            BackroomsNavMesh.BuildNavMesh();
            sw.Stop();
            Logger.LogInfo($"Built navmesh in {sw.ElapsedMilliseconds:N0}ms");
        }
        else
        {
            Logger.LogInfo("Refreshing Navmesh...");
            var sw = Stopwatch.StartNew();
            StartCoroutine(RefreshNavmeshesAsync());
            sw.Stop();
            Logger.LogInfo($"Navmesh refreshed in {sw.ElapsedMilliseconds:N0}ms");
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
            if(twinkleChance < 40f)
            {
                cell.TwinkleLight(lightTwinkleLightCurve, Random.Range(1f, lightTwinkleLightCurve.keys[^1].time));
            }
        }
    }

    private CellVariantInfo[,] BuildVariantLayout()
    {
        int width = generator.width;
        int height = generator.height;
        var layout = new CellVariantInfo[width, height];

        var basicVariants = CurrentTheme.CellsVariants
            .Where(v => !v.mustSpawnAtLeastOnce)
            .ToList();

        // First pass: fill cells with random weighted basic variant
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                var available = basicVariants
                    .Where(v => v.maxAmount == -1 || _variantUsageCount[v] < v.maxAmount)
                    .ToList();

                if(available.Count > 0)
                {
                    layout[x, y] = SelectWeightedRandom(available);
                }
                else
                {
                    // No basic variant available (none defined or all maxed out).
                    var fallback = CurrentTheme.CellsVariants[0];
                    _variantUsageCount[fallback]++;
                    layout[x, y] = fallback;
                }
            }
        }

        // Second pass: scatter required variants into random cells.
        var requiredVariants = CurrentTheme.CellsVariants
            .Where(v => v.mustSpawnAtLeastOnce && v.maxAmount != 0)
            .ToList();

        if(requiredVariants.Count > 0)
        {
            var usedCells = new HashSet<(int x, int y)>();
            int totalCells = width * height;

            foreach(var required in requiredVariants)
            {
                if(usedCells.Count >= totalCells)
                {
                    Logger.LogWarning($"No free cells left to place required variant '{required.name}'.");
                    break;
                }

                (int x, int y) cell;
                do
                {
                    cell = (Random.Range(0, width), Random.Range(0, height));
                } while(usedCells.Contains(cell));

                usedCells.Add(cell);

                // We're overwriting a basic variant, so reclaim its usage count.
                var replaced = layout[cell.x, cell.y];
                _variantUsageCount[replaced]--;

                layout[cell.x, cell.y] = required;
                _variantUsageCount[required]++;
            }
        }

        return layout;
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

    public void StartPlayingAmbientAudios()
    {
        if (ambientNoiseSource && CurrentTheme.AmbientNoise)
        {
            ambientNoiseSource.clip = CurrentTheme.AmbientNoise;
            ambientNoiseSource.Play();
            ambientNoiseSource.loop = true;
        }

        if (ambientMusicSource && CurrentTheme.AmbientMusics.Count > 0)
        {
            var randomMusic = CurrentTheme.AmbientMusics.GetWeightedRandom(md => (!md.isStreamSafe && LocalConfig.Singleton.StreamerMode.Value) ? 0.0f : 1.0f);
            ambientMusicSource.clip = randomMusic.soundtrack;
            ambientMusicSource.Play();
        }
    }

    public void StopPlayingAmbientAudios()
    {
        if (ambientNoiseSource)
        {
            ambientNoiseSource.Stop();
        }

        if (ambientMusicSource)
        {
            ambientMusicSource.Stop();
        }
    }
}
