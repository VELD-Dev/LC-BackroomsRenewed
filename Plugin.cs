using System;

namespace VELDDev.BackroomsRenewed;

[BepInPlugin(PluginInfo.GUID, PluginInfo.Version)]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony = new Harmony(PluginInfo.GUID);
    private ManualLogSource logger = this.Logger;

    void Start()
    {
        logger.Log("""/// B##CKR##MS R#N#W#D \\\""");
        harmony.PatchAll();
        logger.Log(""">>> [x] P#TCH#S #PPL##D""");
    }

}
