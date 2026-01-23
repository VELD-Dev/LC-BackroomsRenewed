using GameNetcodeStuff;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace VELDDev.BackroomsRenewed.BackroomsManagement;

public class Backrooms : NetworkBehaviour
{
    const float CELL_SIZE = 8f; // may be modified depending on how big I make the cells in blender
    
    public static Backrooms Instance;

    public List<CellVariantInfo> cellsVariants;     // Assign in inspector, different cell variants to randomize appearance
    public GameObject exitPrefab;                   // Assign in inspector, exit prefab
    public Transform CellsHolder;                   // Assign in inspector, parent transform for all cells
    public NavMeshSurface BackroomsNavMesh;          // Assign in inspector, the NavMeshSurface component to build the navmesh
    public GameObject BackroomsLightCover;          // Assign in inspector, light cover prefab to place above the backrooms to prevent light from leaking
    public BackroomsGenerator generator;            // Assign in inspector, the maze generator component
    public AnimationCurve lightTwinkleLightCurve;

    [HideInInspector]
    public NetworkVariable<bool> IsGenerated = new(false);

    [HideInInspector] public CellBehaviour[,] Cells;

    private readonly Dictionary<CellVariantInfo, int> _variantUsageCount = [];
    private readonly HashSet<CellVariantInfo> _requiredVariantsNotYetSpawned = [];
    private float _timeSinceLastTwinkleCheck = 0f;
    private float _nextTwinkleCheckTime = 0f;

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
            GenerateBackrooms();
        }
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
                Plugin.Instance.logger.LogWarning("Failed to find valid NavMesh position, using fallback center position");
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
            Plugin.Instance.logger.LogWarning("Failed to find valid NavMesh position, using fallback center position");
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
            Plugin.Instance.logger.LogWarning($"TeleportPlayerClientRpc: Could not find player with clientId {playerClientId}");
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
    
    private void GenerateBackrooms()
    {
        if(!NetworkManager.Singleton.IsHost && !IsServer)
            return;

        // Reset usage counter and required variants tracking for new generation
        _variantUsageCount.Clear();
        _requiredVariantsNotYetSpawned.Clear();
        foreach(var variant in cellsVariants)
        {
            _variantUsageCount[variant] = 0;
            if(variant.mustSpawnAtLeastOnce)
            {
                _requiredVariantsNotYetSpawned.Add(variant);
            }
        }

        generator.Generate();
        Cells = new CellBehaviour[generator.width, generator.height];


        // Instatiate cells for all clients, should make a rectangle.
        for(int x = 0; x < generator.width; x++)
        {
            for(int y = 0; y < generator.height; y++)
            {
                var cell = generator.cells[x, y];
                var selectedVariant = GetWeightedRandomVariant();
                var cellgo = Instantiate(selectedVariant.variantPrefab, new Vector3(x * CELL_SIZE, -1000, y * CELL_SIZE), Quaternion.identity);
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
            }
        }
        
        SetupBackroomsClientRpc(generator.width, generator.height);
    }

    [ClientRpc]
    private void SetupBackroomsClientRpc(int width, int length)
    {
        // Set anti-light leak cover location and size
        var backroomsCenter = new Vector3((width * CELL_SIZE) / 2f, -995f, (length * CELL_SIZE) / 2f);
        BackroomsLightCover.transform.position = backroomsCenter;
        BackroomsLightCover.transform.localScale = new Vector3(width * CELL_SIZE, 1f, length * CELL_SIZE) * 1.1f;
        
        // Set navmesh location and size
        BackroomsNavMesh.center = backroomsCenter + new Vector3(0, -5f, 0);
        BackroomsNavMesh.size = new Vector3(width * CELL_SIZE, 1f, length * CELL_SIZE);
        // Navmesh may be baked in the future by making one navmesh surface for each cell
        // and adding navmesh links between cells
        BackroomsNavMesh.BuildNavMesh();
        // More research to be done for this smh
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
        var availableVariants = cellsVariants.Where(v =>
            v.maxAmount == -1 || _variantUsageCount[v] < v.maxAmount
        ).ToList();

        // If no variants available (all maxed out), return the first variant as fallback
        if(availableVariants.Count == 0)
        {
            return cellsVariants[0];
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
