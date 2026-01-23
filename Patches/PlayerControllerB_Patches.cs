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
            sendToTheBackrooms = Random.Range(0f, 101f) < SyncedConfig.Instance.TeleportationOddsOnDeath;
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

    [HarmonyPrefix, HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
    public static void DamagePlayer(PlayerControllerB __instance, ref int ___damageNumber)
    {
        if(!__instance.IsOwner)
            return;

        if (!SyncedConfig.Instance.TeleportOnDamage)
            return;

        bool sendToTheBackrooms;
        if (SyncedConfig.Instance.UseFairRandomizer)
        {
            if (!__instance.TryGetComponent<FairRandomizer>(out var randomizer))
            {
                randomizer = __instance.gameObject.AddComponent<FairRandomizer>();
            }

            sendToTheBackrooms = randomizer.CheckChance(FairRandomizer.DAMAGE_EVENT,
                SyncedConfig.Instance.TeleportationOddsOnDamage / 100f);
        }
        else
        {
            sendToTheBackrooms = Random.Range(0f, 101f) < SyncedConfig.Instance.TeleportationOddsOnDamage;
        }

        if (sendToTheBackrooms)
        {
            if (SyncedConfig.Instance.DropHeldItemsOnTeleport)
            {
                __instance.DropAllHeldItems();
                __instance.DisableJetpackControlsLocally();
            }
            
            Backrooms.Instance.TeleportLocalPlayerSomewhereInBackrooms();
            if (__instance.health - ___damageNumber < 0)
            {
                ___damageNumber = __instance.health - 1;
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    public static void InitializeLocalPlayer()
    {
        if (SyncedConfig.IsHost)
        {
            SyncedConfig.MessagingManager.RegisterNamedMessageHandler("BackroomsRenewed_OnRequestConfigSync", SyncedConfig.OnRequestSync);
            SyncedConfig.Synced = true;

            return;
        }

        SyncedConfig.Synced = false;
        SyncedConfig.MessagingManager.RegisterNamedMessageHandler("BackroomsRenewed_OnReceiveConfigSync", SyncedConfig.OnReceiveSync);
        SyncedConfig.RequestSync();
    }
}