namespace VELDDev.BackroomsRenewed.BackroomsManagement;

public class Backrooms : NetworkBehaviour
{
    public static Backrooms Instance;

    public GameObject baseCellPrefab;  // Assign in inspector, the base cell prefab with cellBehaviour
    public GameObject wallPrefab;     // Assign in inspector, wall prefab (for exterior walls)
    public GameObject exitPrefab;      // Assign in inspector, exit prefab
    public BackroomsGenerator generator; // Assign in inspector, the maze generator component

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
        GenerateBackrooms();
    }

    [ServerRpc(RequireOwnership = true)]
    private void GenerateBackroomsServerRpc()
    {
        if(!NetworkManager.Singleton.IsHost)
            return;

        generator.Generate();

        for(int x = 0; x < generator.width; x++)
        {
            for(int y = 0; y < generator.height; y++)
            {
                var cell =generator.cells[x, y];
                var cellgo = Instantiate(baseCellPrefab, new Vector3(0, -1000, 0), Quaternion.identity);
                cellgo.GetComponent<NetworkObject>().Spawn(true);
                var cellmono = cellgo.GetComponent<CellBehaviour>();

                // It's hardcoded, it's not clean, but it's optimized and effective at least.
                if(y == 0)
                {
                    if(x == 0)
                    {
                        cellmono.representation.Walls |= WallFlags.West | WallFlags.South;
                    }
                    else if(x == generator.width - 1)
                    {
                        cellmono.representation.Walls |= WallFlags.East | WallFlags.South;
                    }
                    else
                    {
                        cellmono.representation.Walls |= WallFlags.South;
                    }
                }
                else if(y == generator.height - 1)
                {
                    if(x == 0)
                    {
                        cellmono.representation.Walls |= WallFlags.West | WallFlags.North;
                    }
                    else if(x == generator.width - 1)
                    {
                        cellmono.representation.Walls |= WallFlags.East | WallFlags.North;
                    }
                    else
                    {
                        cellmono.representation.Walls |= WallFlags.North;
                    }
                }
                else
                {
                    if(x == 0)
                    {
                        cellmono.representation.Walls |= WallFlags.West;
                    }
                    else if(x == generator.width - 1)
                    {
                        cellmono.representation.Walls |= WallFlags.East;
                    }
                }
                cellmono.Initialize(cell);
            }
        }
    }

    public override void OnDestroy() {
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
