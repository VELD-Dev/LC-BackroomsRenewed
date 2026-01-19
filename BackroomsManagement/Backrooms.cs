namespace VELDDev.BackroomsRenewed.BackroomsManagement;

public class Backrooms : NetworkBehaviour
{
    public static Backrooms Instance;

    public GameObject baseCellPrefab;  // Assign in inspector, the base cell prefab with cellBehaviour
    public List<GameObject> cellsVariants; // Assign in inspector, different cell variants to randomize appearance
    public GameObject exitPrefab;      // Assign in inspector, exit prefab
    public BackroomsGenerator generator; // Assign in inspector, the maze generator component
    public AnimationCurve lightTwinkleLightCurve;

    [HideInInspector] public CellBehaviour[,] Cells;

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
        GenerateBackroomsServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    private void GenerateBackroomsServerRpc()
    {
        if(!NetworkManager.Singleton.IsHost)
            return;

        generator.Generate();

        // Instatiate cells for all clients, should make a rectangle.
        for(int x = 0; x < generator.width; x++)
        {
            for(int y = 0; y < generator.height; y++)
            {
                const float CELL_SIZE = 4f; // may be modified depending on how big I make the cells in blender
                var cell =generator.cells[x, y];
                var cellgo = Instantiate(baseCellPrefab, new Vector3(x * CELL_SIZE, -1000, y * CELL_SIZE), Quaternion.identity);
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

                const int LIGHT_GO_CHANCE_PERCENT = 30;
                const int LIGHT_ON_CHANCE_PERCENT = 60;
                var putLightFlag = Random.RandomRangeInt(0, 101) < LIGHT_GO_CHANCE_PERCENT;
                if(putLightFlag)
                {
                    var lightOnFlag = Random.RandomRangeInt(0, 101) < LIGHT_ON_CHANCE_PERCENT;
                    cellmono.Initialize(cell, true, lightOnFlag);
                }
                else
                {
                    cellmono.Initialize(cell, false, false);
                }
            }
        }
    }

    public override void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    private void TwinkleRandomLights()
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
}
