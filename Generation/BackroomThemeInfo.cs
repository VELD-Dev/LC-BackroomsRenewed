namespace VELDDev.BackroomsRenewed.Generation;

[CreateAssetMenu(fileName = "BackroomThemeInfo", menuName = "Backroom Theme Info", order = 1)]
public class BackroomThemeInfo : ScriptableObject
{
    public string themeName;
    public List<CellVariantInfo> CellsVariants;
    public AudioClip AmbientNoise;
    public List<AmbientMusicInfo> AmbientMusics;
    public float weight = 1f;
}