using VELDDev.BackroomsRenewed.Utils;

namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
public class PlayerControllerB_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerControllerB.Awake))]
    public static void AddFairRandomizerToPlayer(PlayerControllerB __instance)
    {
        if (__instance.GetComponent<FairRandomizer>())
            return;
        
        __instance.gameObject.AddComponent<FairRandomizer>();
    }
    
    [HarmonyPrefix, HarmonyPatch(nameof(PlayerControllerB.KillPlayer))]
    public static bool KillPlayer(PlayerControllerB __instance)
    {
        if (!SyncedConfig.Instance.TeleportOnDeath)
            return true;
        
        if (!__instance.TryGetComponent<FairRandomizer>(out var randomizer))
        {
            randomizer = __instance.gameObject.AddComponent<FairRandomizer>();
        }

        var sendToTheBackrooms = randomizer.CheckChance(FairRandomizer.DEATH_EVENT, SyncedConfig.Instance.TeleportationOddsOnDeath);
        if (__instance.IsOwner && __instance.AllowPlayerDeath() && sendToTheBackrooms)
        {
            // PROXY DEATH HAHAHAHA
            if (SyncedConfig.Instance.DropHeldItemsOnTeleport)
            {
                __instance.DropAllHeldItems();
                __instance.DisableJetpackControlsLocally();
            }
            
            Backrooms.Instance.TeleportLocalPlayerSomewhereInBackrooms();
            return false;
        }

        return true;
    }
}