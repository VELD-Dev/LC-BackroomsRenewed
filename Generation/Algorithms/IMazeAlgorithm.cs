namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public interface IMazeAlgorithm
{
    void Generate(Cell[,] maze, int width, int height);
}
