namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(ShipTeleporter))]
public class ShipTeleporter_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(ShipTeleporter.beamUpPlayer))]
    public static void BeamUpPlayerToBackrooms(ShipTeleporter __instance)
    {
        // Only handle regular teleporter, not inverse
        if (__instance.isInverseTeleporter)
            return;

        if (!SyncedConfig.Instance.TeleportOnShipTP)
            return;

        // Get the player that was just teleported
        // beamUpPlayer targets the player that was being tracked by the radar
        var teleportedPlayer = StartOfRound.Instance.mapScreen.targetedPlayer;
        if (teleportedPlayer == null)
            return;

        bool sendToTheBackrooms = CheckTeleportChance(
            teleportedPlayer,
            FairRandomizerEvent.ShipTP,
            SyncedConfig.Instance.TeleportationOddsOnShipTP
        );

        if (sendToTheBackrooms)
        {
            Backrooms.Instance.TeleportPlayerToBackrooms(teleportedPlayer, SyncedConfig.Instance.DropHeldItemsOnTeleport);
        }
    }

    [HarmonyPostfix, HarmonyPatch(nameof(ShipTeleporter.TeleportPlayerOutWithInverseTeleporter))]
    public static void InverseTeleporterToBackrooms(ShipTeleporter __instance, int playerObj)
    {
        if (!SyncedConfig.Instance.TeleportOnShipRevTP)
            return;

        // Get the player that was teleported by the inverse teleporter
        if (playerObj < 0 || playerObj >= StartOfRound.Instance.allPlayerScripts.Length)
            return;

        var teleportedPlayer = StartOfRound.Instance.allPlayerScripts[playerObj];
        if (teleportedPlayer == null)
            return;

        bool sendToTheBackrooms = CheckTeleportChance(
            teleportedPlayer,
            FairRandomizerEvent.ShipRevTP,
            SyncedConfig.Instance.TeleportationOddsOnShipRevTP
        );

        if (sendToTheBackrooms)
        {
            Backrooms.Instance.TeleportPlayerToBackrooms(teleportedPlayer, SyncedConfig.Instance.DropHeldItemsOnTeleport);
        }
    }

    private static bool CheckTeleportChance(PlayerControllerB player, FairRandomizerEvent eventType, float odds)
    {
        if (SyncedConfig.Instance.UseFairRandomizer)
        {
            if (!player.TryGetComponent<FairRandomizer>(out var randomizer))
            {
                randomizer = player.gameObject.AddComponent<FairRandomizer>();
            }

            return randomizer.CheckChance(eventType, odds / 100f);
        }
        else
        {
            return Random.Range(0f, 101f) < odds;
        }
    }
}