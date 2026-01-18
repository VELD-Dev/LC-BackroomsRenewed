namespace VELDDev.BackroomsRenewed;

[BepInPlugin(PluginInfo.GUID, PluginInfo.PluginName, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony = new Harmony(PluginInfo.GUID);
    private ManualLogSource logger;

    internal AssetBundle assetBundle;
    internal GameObject BackroomsGeneratorPrefab;

    void Start()
    {
        var backroomsModExists = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Neekhaulas.Backrooms");
        logger = Logger;
        logger.LogInfo("""/// B##CKR##MS R#N#W#D \\\""");
        harmony.PatchAll();
        logger.LogInfo(""">>> [x] P#TCH#S #PPL##D""");

        // I understood the lesson, Thunderstore, no more embedded assets !
        assetBundle = AssetBundle.LoadFromFile("./backroomsrenewed.assets");
        BackroomsGeneratorPrefab = assetBundle.LoadAsset<GameObject>("BackroomsGenerator");
        logger.LogInfo(""">>> [x] #SS#T B#NDL# L##D#D""");
        logger.LogInfo("""\\\ /!\ #RR#R: #NT#GR#T# CH#CK F##L#R# -- PR#C##D C#R#F#LL# /!\ ///""");
        logger.LogInfo(""">>> Entering the b--#c__ro#^m:s~""");
    }

}
