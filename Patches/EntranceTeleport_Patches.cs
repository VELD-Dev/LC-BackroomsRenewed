namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(EntranceTeleport))]
public class EntranceTeleport_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(EntranceTeleport.TeleportPlayer))]
    public static void TeleportPlayer(EntranceTeleport __instance)
    {
        if (!SyncedConfig.Instance.TeleportOnInteractDoor)
            return;

        var locPlayer = GameNetworkManager.Instance.localPlayerController;
        bool sendToTheBackrooms;
        if (SyncedConfig.Instance.UseFairRandomizer)
        {
            if (!locPlayer.TryGetComponent<FairRandomizer>(out var randomizer))
            {
                randomizer = locPlayer.gameObject.AddComponent<FairRandomizer>();
            }

            sendToTheBackrooms = randomizer.CheckChance(FairRandomizer.OPEN_DOOR_EVENT,
                SyncedConfig.Instance.TeleportationOddsOnInteractDoor / 100f);
        }
        else
        {
            sendToTheBackrooms = Random.Range(0f, 101f) < SyncedConfig.Instance.TeleportationOddsOnInteractDoor;
        }

        if (sendToTheBackrooms)
        {
            if (SyncedConfig.Instance.DropHeldItemsOnTeleport)
            {
                locPlayer.DropAllHeldItems();
                locPlayer.DisableJetpackControlsLocally();
            }
            
            Backrooms.Instance.TeleportLocalPlayerSomewhereInBackrooms();
        }
    }
}