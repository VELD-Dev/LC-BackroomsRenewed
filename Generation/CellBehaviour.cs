using Unity.XR.CoreUtils;

namespace VELDDev.BackroomsRenewed.Generation;

[RequireComponent(typeof(NetworkObject))]
public class CellBehaviour : NetworkBehaviour {
    public GameObject NorthWall;
    public GameObject EastWall;
    public GameObject SouthWall;
    public GameObject WestWall;
    public GameObject LightObject;
    public MeshRenderer CeilingMR;
    public Light cellLightSource;
    public float DefaultLightIntensity = 750f;
    public bool hasLightSource = false;  // Only when initializing
    public bool defaultLightState = false;  // Anytime
    
    private Cell _representation;

    private Material lightOnMat;
    private Material lightOffMat;
    private Material ceilingMat;
    
    [ClientRpc]
    public void InitializeClientRpc(Cell cell, bool withLight, bool lightState)
    {
        _representation = cell;
        hasLightSource = withLight;
        defaultLightState = lightState;
        UpdateWalls();
        
        Plugin.Instance.logger.LogInfo($"Initializing cell ceiling has {CeilingMR.materials.Length} materials");
        lightOnMat = CeilingMR.materials[0]; 
        ceilingMat = CeilingMR.materials[1];
        lightOffMat = CeilingMR.materials[2];
        if(!hasLightSource)
        {
            Plugin.Instance.logger.LogWarning($"Cell {cell.position} initialized with light source disabled.");
            LightObject.SetActive(false);
            CeilingMR.SetMaterials([ceilingMat, ceilingMat]);
        }
        else
        {
            Plugin.Instance.logger.LogDebug($"Cell {cell.position} initialized with light source enabled.");
            LightObject.SetActive(true);
            cellLightSource.enabled = true;
            cellLightSource.intensity = defaultLightState ? DefaultLightIntensity : 0f;

            if (!defaultLightState)
            {
                CeilingMR.SetMaterials([lightOffMat, ceilingMat]);
            }
        }
    }
    
    private void UpdateWalls()
    {
        NorthWall.SetActive((_representation.walls & WallFlags.North) != 0);
        EastWall.SetActive((_representation.walls & WallFlags.East) != 0);
        SouthWall.SetActive((_representation.walls & WallFlags.South) != 0);
        WestWall.SetActive((_representation.walls & WallFlags.West) != 0);
    }

    public Transform GetExitDoorSocket(WallFlags dir)
    {
        switch (dir)
        {
            case WallFlags.North:
                return NorthWall.transform.GetChild(0);
                break;
            case WallFlags.East:
                return EastWall.transform.GetChild(0);
                break;
            case WallFlags.South:
                return SouthWall.transform.GetChild(0);
                break;
            case WallFlags.West:
                return WestWall.transform.GetChild(0);
                break;
            default:
                return null;
        }
    }

    private void SetLightState(bool state)
    {
        if (!hasLightSource) return;
        cellLightSource.intensity = state ? DefaultLightIntensity : 0f;
    }

    public void TwinkleLight(AnimationCurve intensityCurve, float duration)
    {
        if (!cellLightSource || !hasLightSource) return;
        StartCoroutine(TwinkleCoroutine(intensityCurve, duration));
        Plugin.Instance.logger.LogInfo($"Twinkle started for duration:{duration} seconds");
    }

    private IEnumerator TwinkleCoroutine(AnimationCurve intensityCurve, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = intensityCurve.Evaluate(elapsed / duration) * DefaultLightIntensity;
            cellLightSource.intensity = intensity;
            yield return null;
        }
        cellLightSource.intensity = defaultLightState ? DefaultLightIntensity : 0f;
    }
}