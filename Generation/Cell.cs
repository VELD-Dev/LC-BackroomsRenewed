namespace VELDDev.BackroomsRenewed.Generation;

[System.Flags]
public enum WallFlags : byte
{
    None = 0,
    North = 1 << 0,  // +Z
    East  = 1 << 1,  // +X
    South = 1 << 2,  // -Z
    West  = 1 << 3   // -X
}

public class Cell
{
    public WallFlags Walls;
    public Vector2Int Position;
    
    public Cell(int x, int y)
    {
        Position = new Vector2Int(x, y);
        Walls = WallFlags.North | WallFlags.East | WallFlags.South | WallFlags.West;
    }
}
