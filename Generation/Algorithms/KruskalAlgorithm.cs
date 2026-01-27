using System.Diagnostics;

namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public class KruskalAlgorithm : IMazeAlgorithm
{
    private class Edge
    {
        public Vector2Int CellA;
        public Vector2Int CellB;
        public WallFlags WallA;
        public WallFlags WallB;
    }

    private class UnionFind
    {
        private int[] parent;
        private int[] rank;

        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];
            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        public bool Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return false;

            if (rank[rootX] < rank[rootY])
                parent[rootX] = rootY;
            else if (rank[rootX] > rank[rootY])
                parent[rootY] = rootX;
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }

            return true;
        }
    }

    public IEnumerator Generate(Cell[,] maze, int width, int height)
    {
        var edges = new List<Edge>();
        var perfSw = Stopwatch.StartNew();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < width - 1)
                {
                    edges.Add(new Edge
                    {
                        CellA = new Vector2Int(x, y),
                        CellB = new Vector2Int(x + 1, y),
                        WallA = WallFlags.East,
                        WallB = WallFlags.West
                    });
                }

                if (y < height - 1)
                {
                    edges.Add(new Edge
                    {
                        CellA = new Vector2Int(x, y),
                        CellB = new Vector2Int(x, y + 1),
                        WallA = WallFlags.North,
                        WallB = WallFlags.South
                    });
                }

                if (perfSw.ElapsedMilliseconds > 16)
                {
                    yield return null;
                    perfSw.Restart();
                }
            }
        }

        yield return Shuffle(edges);

        var uf = new UnionFind(width * height);

        foreach (var edge in edges)
        {
            int idA = edge.CellA.y * width + edge.CellA.x;
            int idB = edge.CellB.y * width + edge.CellB.x;

            if (uf.Union(idA, idB))
            {
                maze[edge.CellA.x, edge.CellA.y].Walls &= ~edge.WallA;
                maze[edge.CellB.x, edge.CellB.y].Walls &= ~edge.WallB;
            }

            if (perfSw.ElapsedMilliseconds > 16)
            {
                yield return null;
                perfSw.Restart();
            }
        }

        perfSw.Stop();
    }

    private IEnumerator Shuffle<T>(List<T> list)
    {
        var perfSw = Stopwatch.StartNew();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
            if (perfSw.ElapsedMilliseconds > 16)
            {
                yield return null;
                perfSw.Restart();
            }
        }
        perfSw.Stop();
    }
}