namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManager_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(GameNetworkManager.StartDisconnect))]
    public static void StartDisconnect()
    {
        SyncedConfig.RevertSync();
    }
}