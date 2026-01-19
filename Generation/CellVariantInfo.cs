namespace VELDDev.BackroomsRenewed.Generation;

[CreateAssetMenu(fileName = "CellVariantInfo", menuName = "CellVariantInfo", order = 0)]
public class CellVariantInfo : ScriptableObject
{
    public GameObject variantPrefab;
    public float weight = 1f;
    [Tooltip("Maximum amount of this variant that can be used in a single generation. Set to -1 for no limit, 1 for unique.")]
    public int maxAmount = -1;
    public bool mustSpawnAtLeastOnce = false;
}
