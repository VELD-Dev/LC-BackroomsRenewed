namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(ShipTeleporter))]
public class ShipTeleporter_Patches
{
    // TODO: Implement the patches
    // NB: This will require a few tweaks of the Backrooms class to TP from other clients (it seems)
    [HarmonyPostfix, HarmonyPatch(nameof(ShipTeleporter.beamUpPlayer))]
    public static void TeleportToBackroomsInstead(ShipTeleporter __instance)
    {
        if (!SyncedConfig.Instance.TeleportOnShipTP)
            return;
        
        if (SyncedConfig.Instance.UseFairRandomizer)
        {
            //if()
        }
    }
}