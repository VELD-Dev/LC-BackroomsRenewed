using System.Diagnostics;

namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public class BlobAlgorithm : IMazeAlgorithm
{
    private class Blob
    {
        public int Id { get; set; }
        public List<Vector2Int> Cells { get; set; }
        
        public Blob(int id)
        {
            Id = id;
            Cells = new List<Vector2Int>();
        }
        
        public void Merge(Blob other)
        {
            Cells.AddRange(other.Cells);
        }
    }
    
    private class MergeCandidate
    {
        public Vector2Int CellA;
        public Vector2Int CellB;
        public WallFlags WallA;
        public WallFlags WallB;
        public int BlobIdA;
        public int BlobIdB;
    }
    
    public IEnumerator Generate(Cell[,] maze, int width, int height)
    {
        var cellToBlob = new Dictionary<Vector2Int, Blob>();
        var blobs = new List<Blob>();
        int blobIdCounter = 0;
        var perfSw = Stopwatch.StartNew();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector2Int(x, y);
                var blob = new Blob(blobIdCounter++);
                blob.Cells.Add(pos);
                blobs.Add(blob);
                cellToBlob[pos] = blob;
                
                // Dynamic refresh to avoid stutters (smort)
                if (perfSw.ElapsedMilliseconds > 16)
                {
                    yield return null;
                    perfSw.Restart();
                }
            }
        }
        
        var allCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allCells.Add(new Vector2Int(x, y));
                if (perfSw.ElapsedMilliseconds > 16)
                {
                    yield return null;
                    perfSw.Restart();
                }
            }
        }
        
        yield return ShuffleCells(allCells);
        
        while (blobs.Count > 1)
        {
            var currentCell = allCells[Random.Range(0, allCells.Count)];
            var currentBlob = cellToBlob[currentCell];
            
            var mergeCandidates = FindMergeCandidates(
                currentCell, 
                currentBlob, 
                cellToBlob, 
                width, 
                height
            );
            
            if (mergeCandidates.Count > 0)
            {
                var merge = mergeCandidates[Random.Range(0, mergeCandidates.Count)];
                
                maze[merge.CellA.x, merge.CellA.y].Walls &= ~merge.WallA;
                maze[merge.CellB.x, merge.CellB.y].Walls &= ~merge.WallB;
                
                var blobA = cellToBlob[merge.CellA];
                var blobB = cellToBlob[merge.CellB];
                
                blobA.Merge(blobB);
                
                foreach (var cell in blobB.Cells)
                {
                    cellToBlob[cell] = blobA;
                }
                
                blobs.Remove(blobB);
            }

            if (perfSw.ElapsedMilliseconds > 16)
            {
                yield return null;
                perfSw.Restart();
            }
        }

        perfSw.Stop();
    }
    
    private List<MergeCandidate> FindMergeCandidates(
        Vector2Int cell,
        Blob currentBlob,
        Dictionary<Vector2Int, Blob> cellToBlob,
        int width,
        int height)
    {
        var candidates = new List<MergeCandidate>();
        

        var directions = new[]
        {
            (new Vector2Int(0, 1), WallFlags.North, WallFlags.South),
            (new Vector2Int(1, 0), WallFlags.East, WallFlags.West),
            (new Vector2Int(0, -1), WallFlags.South, WallFlags.North),
            (new Vector2Int(-1, 0), WallFlags.West, WallFlags.East)
        };
        
        foreach (var (offset, wallA, wallB) in directions)
        {
            var neighbor = cell + offset;
            
            if (neighbor.x < 0 || neighbor.x >= width || 
                neighbor.y < 0 || neighbor.y >= height)
            {
                continue;
            }
            
            var neighborBlob = cellToBlob[neighbor];
            
            if (neighborBlob.Id != currentBlob.Id)
            {
                candidates.Add(new MergeCandidate
                {
                    CellA = cell,
                    CellB = neighbor,
                    WallA = wallA,
                    WallB = wallB,
                    BlobIdA = currentBlob.Id,
                    BlobIdB = neighborBlob.Id
                });
            }
        }
        
        return candidates;
    }

    private IEnumerator ShuffleCells(List<Vector2Int> cells)
    {
        var perfSw = Stopwatch.StartNew();
        
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
            if (perfSw.ElapsedMilliseconds > 16)
            {
                yield return null;
                perfSw.Restart();
            }
        }
        perfSw.Stop();
    }
}