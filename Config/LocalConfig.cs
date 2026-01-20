using BepInEx.Configuration;

namespace VELDDev.BackroomsRenewed.Config;

public class LocalConfig
{
    public static LocalConfig Singleton { get; private set; }

    internal const string ModDescription = "CR#T#CAL FA#LUR#: UNKN#WN #NTERV#ENTI#N D#T#CT#D -- #/1?.!1'(>>> JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME JOIN ME";

    public readonly ConfigEntry<bool> StreamerMode;
    public readonly ConfigEntry<bool> TeleportOnDeath;
    public readonly ConfigEntry<bool> TeleportOnClipping;
    public readonly ConfigEntry<bool> TeleportOnDamage;
    public readonly ConfigEntry<float> TeleportationOddsOnDeath; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnClipping; // Percentage
    public readonly ConfigEntry<float> TeleportationOddsOnDamage; // Percentage
    public readonly ConfigEntry<BackroomsGenerator.MazeAlgorithm> GenerationAlgorithm;
    public readonly ConfigEntry<int> MinBackroomsSize;
    public readonly ConfigEntry<int> MaxBackroomsSize;
    public readonly ConfigEntry<int> MaxFakeExitCount;

    public LocalConfig(ConfigFile cfg)
    {
        Singleton = this;

        StreamerMode = cfg.Bind(
            "General",
            "Streamer Mode",
            false,
            "Enable streamer mode to disable copyrighted musics and replace with copyright-free alternatives."
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
            10f,
            "The percentage chance of teleportation occurring on clipping through walls."
        );
        TeleportationOddsOnDamage = cfg.Bind(
            "Teleportation",
            "Teleportation Odds on Damage",
            1f,
            "The percentage chance of teleportation occurring on taking damage."
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
