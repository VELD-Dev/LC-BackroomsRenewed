using VELDDev.BackroomsRenewed.Generation.Algorithms;

namespace VELDDev.BackroomsRenewed.Generation;

public class BackroomsGenerator : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public MazeAlgorithm algorithm = LocalConfig.Singleton.GenerationAlgorithm.Value;
    public Vector2Int exitPosition;
    
    [HideInInspector] public Cell[,] cells = new Cell[0, 0];
    private IMazeAlgorithm currentAlgorithm;
    
    public enum MazeAlgorithm
    {
        Blob,
        Kruskal,
        Prim,
        Wilson,
        RandomPathMerge,
        FractalTessellation
    }
    
    public void Generate()
    {
        InitializeMaze();
        
        currentAlgorithm = algorithm switch
        {
            MazeAlgorithm.Kruskal => new KruskalAlgorithm(),
            MazeAlgorithm.Prim => new PrimAlgorithm(),
            MazeAlgorithm.Wilson => new WilsonAlgorithm(),
            MazeAlgorithm.Blob => new BlobAlgorithm(),
            //MazeAlgorithm.RandomPathMerge => new RandomPathMergeAlgorithm(),
            //MazeAlgorithm.FractalTessellation => new FractalTessellationAlgorithm(),
            _ => throw new System.NotImplementedException()
        };
        
        currentAlgorithm.Generate(cells, width, height);
        PlaceExit();
    }
    
    private void InitializeMaze()
    {
        cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(x, y);
            }
        }
    }
    
    private void PlaceExit()
    {
        int side = Random.Range(0, 4);
        switch (side)
        {
            case 0:
                exitPosition = new Vector2Int(Random.Range(0, width), height - 1);
                cells[exitPosition.x, exitPosition.y].Walls &= ~WallFlags.North;
                break;
            case 1:
                exitPosition = new Vector2Int(width - 1, Random.Range(0, height));
                cells[exitPosition.x, exitPosition.y].Walls &= ~WallFlags.East;
                break;
            case 2:
                exitPosition = new Vector2Int(Random.Range(0, width), 0);
                cells[exitPosition.x, exitPosition.y].Walls &= ~WallFlags.South;
                break;
            case 3:
                exitPosition = new Vector2Int(0, Random.Range(0, height));
                cells[exitPosition.x, exitPosition.y].Walls &= ~WallFlags.West;
                break;
        }
    }
}