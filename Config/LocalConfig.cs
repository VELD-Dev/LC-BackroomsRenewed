using BepInEx.Configuration;

namespace VELDDev.BackroomsRenewed.Config;

public class LocalConfig
{
    public static LocalConfig Singleton { get; private set; }

    internal const string ModDescription = "CR#T#CAL FA#LUR#: UNKN#WN #NTERV#ENTI#N D#T#CT#D -- #/1?.!1'(>>> JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME";

    public readonly ConfigEntry<bool> StreamerMode;
    public readonly ConfigEntry<bool> UseFairRandomizer;
    public readonly ConfigEntry<bool> TeleportOnDeath;
    public readonly ConfigEntry<bool> TeleportOnClipping;
    public readonly ConfigEntry<bool> TeleportOnDamage;
    public readonly ConfigEntry<bool> TeleportOnInteractDoor;
    public readonly ConfigEntry<bool> TeleportOnShipTeleport;
    public readonly ConfigEntry<bool> TeleportOnShipRevertTeleport;
    public readonly ConfigEntry<float> TeleportationOddsOnDeath; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnClipping; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnDamage; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnInteractDoor; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnShipTeleport; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnShipRevertTeleport; // Percentage
    public readonly ConfigEntry<bool> DropHeldItemsOnTeleport;
    public readonly ConfigEntry<BackroomsGenerator.MazeAlgorithm> GenerationAlgorithm;
    public readonly ConfigEntry<int> MinBackroomsSize;
    public readonly ConfigEntry<int> MaxBackroomsSize;
    public readonly ConfigEntry<int> MaxFakeExitCount;

    internal ConfigFile CfgFile;
    
    public LocalConfig(ConfigFile cfg)
    {
        Singleton = this;
        CfgFile = cfg;
        
        StreamerMode = cfg.Bind(
            "General",
            "Streamer Mode",
            false,
            "Enable streamer mode to disable copyrighted musics and replace with copyright-free alternatives."
        );
        UseFairRandomizer = cfg.Bind(
            "General",
            "Use Fair Randomization",
            false,
            "Whether to use Fair Randomization (a.k.a. Pity system) for random events (e.g. TP) or use normal randomization."
        );

        // FLAGS
        TeleportOnDeath = cfg.Bind(
            "Teleportation",
            "Teleport on Death",
            true,
            "Enable teleportation on death."
        );
        TeleportOnClipping = cfg.Bind(
            "Teleportation",
            "Teleport on Clipping",
            true,
            "Enable teleportation on clipping through walls."
        );
        TeleportOnDamage = cfg.Bind(
            "Teleportation",
            "Teleport on Damage",
            false,
            "Enable teleportation on taking damage."
        );
        TeleportOnInteractDoor = cfg.Bind(
            "Teleportation",
            "Teleport on Entrance/Exit",
            true,
            "Enable teleportation when entering/exiting the facility"
        );
        TeleportOnShipTeleport = cfg.Bind(
            "Teleportation",
            "Teleport on Ship Teleport",
            true,
            "Enable teleportation when using Ship Teleporter"
        );
        TeleportOnShipRevertTeleport = cfg.Bind(
            "Teleport",
            "Teleport on Ship Revert TP",
            true,
            "Enable teleportation when using the Ship Revert Teleporter"
        );

        // ODDS
        TeleportationOddsOnDeath = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Death",
            10f,
            "The percentage chance of teleportation occurring on death."
        );
        TeleportationOddsOnClipping = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Clipping",
            100f,
            "The chance percentage of teleportation occurring on clipping through walls."
        );
        TeleportationOddsOnDamage = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Damage",
            1f,
            "The percentage chance of teleportation occurring on taking damage."
        );
        TeleportationOddsOnInteractDoor = cfg.Bind(
            "Teleportation",
            "Telportation Odds On Entrance/Exit",
            0.1f,
            "The chance percentage of teleportation occurring when entering or exiting the facility"
        );
        TeleportationOddsOnShipTeleport = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Ship Teleport",
            0.1f,
            "The chance percentage of teleportation occurring when using the ship teleporter."
        );
        TeleportationOddsOnShipRevertTeleport = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Ship Revert TP",
            1f,
            "The chance percentage of teleportation occurring when using the ship revert teleporter"
        );
        DropHeldItemsOnTeleport = cfg.Bind(
            "Teleportation",
            "Drop All Held Items On TP",
            false,
            "If enabled, will drop all items on the ground when TP in the Backrooms."
        );

        // GENERATION
        GenerationAlgorithm = cfg.Bind(
            "Generation",
            "Backrooms Generation Algorithm",
            BackroomsGenerator.MazeAlgorithm.Blob,
            "The maze generation algorithm used for Backrooms levels. (Blob is recommended for optimal aesthetics)"
        );
        MinBackroomsSize = cfg.Bind(
            "Generation",
            "Minimum Backrooms Size",
            15,
            "The minimum size in cells (width and length) of generated Backrooms levels."
        );
        MaxBackroomsSize = cfg.Bind(
            "Generation",
            "Maximum Backrooms Size",
            30,
            "The maximum size in cells (width and length) of generated Backrooms levels."
        );
        MaxFakeExitCount = cfg.Bind(
            "Generation",
            "Maximum Fake Exits Count",
            2,
            "The maximum number of fake exits that will appear in the Backrooms. They do not kill, just TP to somewhere else in the backrooms."
        );

        if (LethalConfigSupport.LethalConfigLoaded)
        {
            LethalConfigSupport.RegisterLethalConfig(this);
        }
    }
}
