namespace VELDDev.BackroomsRenewed.Generation;

[RequireComponent(typeof(NetworkObject))]
public class CellBehaviour : NetworkBehaviour {
    public Cell representation;

    public GameObject NorthWall;
    public GameObject EastWall;
    public GameObject SouthWall;
    public GameObject WestWall;

    public void Initialize(Cell cell)
    {
        representation = cell;
        UpdateWalls();
    }

    public void UpdateWalls()
    {
        NorthWall.SetActive((representation.Walls & WallFlags.North) != 0);
        EastWall.SetActive((representation.Walls & WallFlags.East) != 0);
        SouthWall.SetActive((representation.Walls & WallFlags.South) != 0);
        WestWall.SetActive((representation.Walls & WallFlags.West) != 0);
    }
}