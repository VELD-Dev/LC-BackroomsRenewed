using System.Diagnostics;

namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public class PrimAlgorithm : IMazeAlgorithm
{
    private class WallInfo
    {
        public Vector2Int Cell;
        public WallFlags Wall;
        public Vector2Int Neighbor;
    }

    public IEnumerator Generate(Cell[,] maze, int width, int height)
    {
        var inMaze = new bool[width, height];
        var walls = new List<WallInfo>();
        var perfSw = Stopwatch.StartNew();

        int startX = Random.Range(0, width);
        int startY = Random.Range(0, height);
        inMaze[startX, startY] = true;

        AddWalls(maze, walls, inMaze, startX, startY, width, height);

        while (walls.Count > 0)
        {
            int index = Random.Range(0, walls.Count);
            var wall = walls[index];
            walls.RemoveAt(index);

            var nx = wall.Neighbor.x;
            var ny = wall.Neighbor.y;

            if (!inMaze[nx, ny])
            {
                maze[wall.Cell.x, wall.Cell.y].Walls &= ~wall.Wall;
                maze[nx, ny].Walls &= ~GetOppositeWall(wall.Wall);

                inMaze[nx, ny] = true;
                AddWalls(maze, walls, inMaze, nx, ny, width, height);
            }

            if (perfSw.ElapsedMilliseconds > 16)
            {
                yield return null;
                perfSw.Restart();
            }
        }

        perfSw.Stop();
    }

    private void AddWalls(Cell[,] maze, List<WallInfo> walls, bool[,] inMaze,
                          int x, int y, int width, int height)
    {
        //north
        if (y < height - 1 && !inMaze[x, y + 1])
        {
            walls.Add(new WallInfo
            {
                Cell = new Vector2Int(x, y),
                Wall = WallFlags.North,
                Neighbor = new Vector2Int(x, y + 1)
            });
        }

        // east
        if (x < width - 1 && !inMaze[x + 1, y])
        {
            walls.Add(new WallInfo
            {
                Cell = new Vector2Int(x, y),
                Wall = WallFlags.East,
                Neighbor = new Vector2Int(x + 1, y)
            });
        }

        // south
        if (y > 0 && !inMaze[x, y - 1])
        {
            walls.Add(new WallInfo
            {
                Cell = new Vector2Int(x, y),
                Wall = WallFlags.South,
                Neighbor = new Vector2Int(x, y - 1)
            });
        }

        // west
        if (x > 0 && !inMaze[x - 1, y])
        {
            walls.Add(new WallInfo
            {
                Cell = new Vector2Int(x, y),
                Wall = WallFlags.West,
                Neighbor = new Vector2Int(x - 1, y)
            });
        }
    }

    private WallFlags GetOppositeWall(WallFlags wall)
    {
        return wall switch
        {
            WallFlags.North => WallFlags.South,
            WallFlags.South => WallFlags.North,
            WallFlags.East => WallFlags.West,
            WallFlags.West => WallFlags.East,
            _ => WallFlags.None
        };
    }
}