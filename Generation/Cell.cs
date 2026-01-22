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

[Serializable]
public class Cell : INetworkSerializable, IEquatable<Cell>
{
    public WallFlags Walls;
    public Vector2Int position;
    
    public Cell()
    {
        Walls = WallFlags.North | WallFlags.East | WallFlags.South | WallFlags.West;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Walls);
        serializer.SerializeValue(ref position);
    }

    public bool Equals(Cell? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Walls == other.Walls && position.Equals(other.position);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Cell)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Walls, position);
    }
}
