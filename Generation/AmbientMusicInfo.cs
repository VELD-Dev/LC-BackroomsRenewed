namespace VELDDev.BackroomsRenewed.Generation;

[CreateAssetMenu(fileName = "AmbientMusicInfo", menuName = "Ambient Music Info")]
public class AmbientMusicInfo : ScriptableObject
{
    public AudioClip soundtrack;
    public bool isStreamSafe = true;
    [Tooltip("If not stream safe, fallback on this track as copyright-safe.")]
    public AudioClip fallbackSoundtrack;
}