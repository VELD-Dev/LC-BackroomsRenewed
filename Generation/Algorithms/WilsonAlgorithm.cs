using System.Diagnostics;

namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public class WilsonAlgorithm : IMazeAlgorithm
{
    public IEnumerator Generate(Cell[,] maze, int width, int height)
    {
        var inMaze = new bool[width, height];
        var path = new Vector2Int[width * height];
        var perfSw = Stopwatch.StartNew();

        // random starting cell as theres no real entrance in the Backrooms
        int startX = Random.Range(0, width);
        int startY = Random.Range(0, height);
        inMaze[startX, startY] = true;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (inMaze[x, y]) continue;

                int pathLength = RandomWalk(x, y, inMaze, path, width, height);

                for (int i = 0; i < pathLength - 1; i++)
                {
                    var current = path[i];
                    var next = path[i + 1];

                    RemoveWallBetween(maze, current, next);
                    inMaze[current.x, current.y] = true;
                }

                if (perfSw.ElapsedMilliseconds > 16)
                {
                    yield return null;
                    perfSw.Restart();
                }
            }
        }

        perfSw.Stop();
    }

    private int RandomWalk(int startX, int startY, bool[,] inMaze,
                          Vector2Int[] path, int width, int height)
    {
        var visited = new Dictionary<Vector2Int, int>();
        int pathLength = 0;

        var current = new Vector2Int(startX, startY);
        path[pathLength++] = current;
        visited[current] = 0;

        while (!inMaze[current.x, current.y])
        {
            var neighbors = GetNeighbors(current, width, height);
            current = neighbors[Random.Range(0, neighbors.Count)];

            if (visited.ContainsKey(current))
            {
                pathLength = visited[current] + 1;
            }
            else
            {
                visited[current] = pathLength;
                path[pathLength++] = current;
            }
        }

        return pathLength;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos, int width, int height)
    {
        var neighbors = new List<Vector2Int>();

        if (pos.x > 0) neighbors.Add(new Vector2Int(pos.x - 1, pos.y));
        if (pos.x < width - 1) neighbors.Add(new Vector2Int(pos.x + 1, pos.y));
        if (pos.y > 0) neighbors.Add(new Vector2Int(pos.x, pos.y - 1));
        if (pos.y < height - 1) neighbors.Add(new Vector2Int(pos.x, pos.y + 1));

        return neighbors;
    }

    private void RemoveWallBetween(Cell[,] maze, Vector2Int a, Vector2Int b)
    {
        int dx = b.x - a.x;
        int dy = b.y - a.y;

        if (dx == 1)
        {
            maze[a.x, a.y].Walls &= ~WallFlags.East;
            maze[b.x, b.y].Walls &= ~WallFlags.West;
        }
        else if (dx == -1)
        {
            maze[a.x, a.y].Walls &= ~WallFlags.West;
            maze[b.x, b.y].Walls &= ~WallFlags.East;
        }
        else if (dy == 1)
        {
            maze[a.x, a.y].Walls &= ~WallFlags.North;
            maze[b.x, b.y].Walls &= ~WallFlags.South;
        }
        else if (dy == -1)
        {
            maze[a.x, a.y].Walls &= ~WallFlags.South;
            maze[b.x, b.y].Walls &= ~WallFlags.North;
        }
    }
}