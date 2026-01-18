namespace VELDDev.BackroomsRenewed.Generation;

[RequireComponent(typeof(NetworkObject))]
public class CellBehaviour : NetworkBehaviour {
    public Cell representation;

    public GameObject NorthWall;
    public GameObject EastWall;
    public GameObject SouthWall;
    public GameObject WestWall;
    public GameObject LightObject;
    public Light cellLightSource;
    public bool hasLightSource = false;  // Only when initializing
    public bool defaultLightState = false;  // Anytime

    public void Initialize(Cell cell, bool withLight, bool lightState)
    {
        representation = cell;
        hasLightSource = withLight;
        defaultLightState = lightState;
        UpdateWalls();
        if(!hasLightSource)
        {
            LightObject.SetActive(false);
        }
        else
        {
            cellLightSource.enabled = true;
            cellLightSource.intensity = defaultLightState ? 1f : 0f;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void UpdateWalls()
    {
        NorthWall.SetActive((representation.Walls & WallFlags.North) != 0);
        EastWall.SetActive((representation.Walls & WallFlags.East) != 0);
        SouthWall.SetActive((representation.Walls & WallFlags.South) != 0);
        WestWall.SetActive((representation.Walls & WallFlags.West) != 0);
    }

    public void setLightState(bool state)
    {
        if (!hasLightSource) return;
        cellLightSource.intensity = state ? 1f : 0f;
    }

    public void TwinkleLight(AnimationCurve intensityCurve, float duration)
    {
        if (cellLightSource == null || !hasLightSource) return;
        StartCoroutine(TwinkleCoroutine(intensityCurve, duration));
    }

    private System.Collections.IEnumerator TwinkleCoroutine(AnimationCurve intensityCurve, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = intensityCurve.Evaluate(elapsed / duration);
            cellLightSource.intensity = intensity;
            yield return null;
        }
        cellLightSource.intensity = defaultLightState ? 1f : 0f;
    }
}