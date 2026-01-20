namespace VELDDev.BackroomsRenewed;

[BepInPlugin(PluginInfo.GUID, PluginInfo.PluginName, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony = new Harmony(PluginInfo.GUID);
    private ManualLogSource logger;
    public static Plugin Instance { get; private set; }

    internal AssetBundle assetBundle;
    internal GameObject BackroomsPrefab;

    void Start()
    {
        NetcodePatch();
        
        var backroomsModExists = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Neekhaulas.Backrooms");
        Instance = this;
        logger = Logger;
        logger.LogInfo("""/// B##CKR##MS R#N#W#D \\\""");
        if (backroomsModExists)
        {
            logger.LogError(""">>> IMP#SSIBL# T# L##D <<BACKROOMS RENEWED>>: The other backroom mod is already here !""");
            return;
        }
        harmony.PatchAll();
        logger.LogInfo(""">>> [x] P#TCH#S #PPL##D""");

        // I understood the lesson, Thunderstore, no more embedded assets !
        assetBundle = AssetBundle.LoadFromFile("./backroomsrenewed.assets");
        logger.LogInfo(""">>> [x] #SS#T B#NDL# L##D#D""");
        RegisterPrefabs();
        logger.LogInfo("""\\\ /!\ #RR#R: #NT#GR#T# CH#CK F##L#R# -- PR#C##D C#R#F#LL# /!\ ///""");
        logger.LogInfo(""">>> Entering the b--#c__ro#^m:s~""");
    }

    void RegisterPrefabs()
    {
        BackroomsPrefab = assetBundle.LoadAsset<GameObject>("Backrooms");
        var defaultCellDefaultVariant = assetBundle.LoadAsset<GameObject>("DefaultBackroomCellPrefab");
        
        // TODO: REGISTRATION WITH LETHALLIB OR DAWNLIB
    }
    
    private void NetcodePatch()
    {
            var types = Assembly.GetExecutingAssembly().GetTypes(); 
            foreach (var type in types)
            {
                // Avoids errors when not having LethalConfig (it's a soft dep)
                if (type == typeof(LethalConfigSupport))
                    continue;
                
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
    }
}
