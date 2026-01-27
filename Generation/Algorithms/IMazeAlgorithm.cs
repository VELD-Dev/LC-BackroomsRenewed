namespace VELDDev.BackroomsRenewed.Generation.Algorithms;

public interface IMazeAlgorithm
{
    IEnumerator Generate(Cell[,] maze, int width, int height);
}
