namespace VELDDev.BackroomsRenewed.Generation;

[RequireComponent(typeof(NetworkObject))]
public class CellBehaviour : NetworkBehaviour {
    public GameObject NorthWall;
    public GameObject EastWall;
    public GameObject SouthWall;
    public GameObject WestWall;
    public GameObject LightObject;
    public Light cellLightSource;
    public bool hasLightSource = false;  // Only when initializing
    public bool defaultLightState = false;  // Anytime

    private NetworkVariable<Cell> _representation = new();
    
    [ClientRpc]
    public void InitializeClientRpc(Cell cell, bool withLight, bool lightState)
    {
        _representation.Value = cell;
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
    
    private void UpdateWalls()
    {
        NorthWall.SetActive((_representation.Value.Walls & WallFlags.North) != 0);
        EastWall.SetActive((_representation.Value.Walls & WallFlags.East) != 0);
        SouthWall.SetActive((_representation.Value.Walls & WallFlags.South) != 0);
        WestWall.SetActive((_representation.Value.Walls & WallFlags.West) != 0);
    }

    private void SetLightState(bool state)
    {
        if (!hasLightSource) return;
        cellLightSource.intensity = state ? 1f : 0f;
    }

    public void TwinkleLight(AnimationCurve intensityCurve, float duration)
    {
        if (!cellLightSource || !hasLightSource) return;
        StartCoroutine(TwinkleCoroutine(intensityCurve, duration));
    }

    private IEnumerator TwinkleCoroutine(AnimationCurve intensityCurve, float duration)
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