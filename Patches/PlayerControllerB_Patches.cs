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
        if (!__instance.IsOwner)
            return true;
        
        if (!SyncedConfig.Instance.TeleportOnDeath)
            return true;

        bool sendToTheBackrooms;
        if (SyncedConfig.Instance.UseFairRandomizer)
        {
            if (!__instance.TryGetComponent<FairRandomizer>(out var randomizer))
            {
                randomizer = __instance.gameObject.AddComponent<FairRandomizer>();
            }
            
            sendToTheBackrooms = randomizer.CheckChance(FairRandomizer.DEATH_EVENT,
                SyncedConfig.Instance.TeleportationOddsOnDeath / 100f);
        }
        else
        {
            sendToTheBackrooms = Random.Range(0, 100) < SyncedConfig.Instance.TeleportationOddsOnDeath;
        }
        
        if (__instance.AllowPlayerDeath() && sendToTheBackrooms)
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