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

            sendToTheBackrooms = randomizer.CheckChance(FairRandomizerEvent.Death,
                SyncedConfig.Instance.TeleportationOddsOnDeath / 100f);
        }
        else
        {
            sendToTheBackrooms = Random.Range(0f, 101f) < SyncedConfig.Instance.TeleportationOddsOnDeath;
        }

        if (__instance.AllowPlayerDeath() && sendToTheBackrooms)
        {
            // PROXY DEATH - teleport to backrooms instead of dying
            Backrooms.Instance.TeleportPlayerToBackrooms(__instance, SyncedConfig.Instance.DropHeldItemsOnTeleport);
            return false;
        }

        return true;
    }

    [HarmonyPrefix, HarmonyPatch(nameof(PlayerControllerB.DamagePlayer))]
    public static void DamagePlayer(PlayerControllerB __instance, ref int damageNumber)
    {
        if (!__instance.IsOwner)
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

            sendToTheBackrooms = randomizer.CheckChance(FairRandomizerEvent.Damage,
                SyncedConfig.Instance.TeleportationOddsOnDamage / 100f);
        }
        else
        {
            sendToTheBackrooms = Random.Range(0f, 101f) < SyncedConfig.Instance.TeleportationOddsOnDamage;
        }

        if (sendToTheBackrooms)
        {
            Backrooms.Instance.TeleportPlayerToBackrooms(__instance, SyncedConfig.Instance.DropHeldItemsOnTeleport);
            // Prevent lethal damage when teleporting to backrooms
            if (__instance.health - damageNumber < 0)
            {
                damageNumber = __instance.health - 1;
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