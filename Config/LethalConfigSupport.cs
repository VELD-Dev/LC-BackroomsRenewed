using System.Runtime.CompilerServices;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace VELDDev.BackroomsRenewed.Config;

internal static class LethalConfigSupport
{
    private static bool? lethalConfigLoaded;
    public static bool LethalConfigLoaded
    {
        get
        {
            lethalConfigLoaded ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig");

            return (bool)lethalConfigLoaded;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void RegisterLethalConfig(LocalConfig config)
    {
        LethalConfigManager.SetModDescription(LocalConfig.ModDescription);
        CanModifyResult NetworkAllowModifyCb() => (SyncedConfig.Synced && SyncedConfig.IsHost && NetworkManager.Singleton.IsServer) || !SyncedConfig.Synced;

        var streamerMode = new BoolCheckBoxConfigItem(
            config.StreamerMode,
            new BoolCheckBoxOptions
            {
                Name = "Streamer Mode",
                Description =
                    "Enable streamer mode to disable copyrighted musics and replace with copyright-free alternatives.",
                Section = "General",
                RequiresRestart = false,
            });
        var fairRandomizer = new BoolCheckBoxConfigItem(
            config.UseFairRandomizer,
            new BoolCheckBoxOptions 
            {
                Name = "Use Fair Randomization",
                Description = "Whether to use fair randomization (a.k.a. Pity System) or normal randomization for random events (e.g. TP)",
                Section = "General",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });
        
        
        var tpOnDeath = new BoolCheckBoxConfigItem(
            config.TeleportOnDeath,
            new BoolCheckBoxOptions
            {
                Name = "Teleport on Death",
                Description = "Allows teleportation on death of player. See also 'Teleportation Odds on Death'.",
                Section = "Teleportation",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });
        var tpOnClipping = new BoolCheckBoxConfigItem(
            config.TeleportOnClipping,
            new BoolCheckBoxOptions
            {
                Name = "Teleport on Clipping",
                Description = "Allows teleportation on clipping through walls. See also 'Teleportation Odds on Clipping'.",
                Section = "Teleportation",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });
        var tpOnDamage = new BoolCheckBoxConfigItem(
            config.TeleportOnDamage,
            new BoolCheckBoxOptions
            {
                Name = "Teleport on Damage",
                Description = "Allows teleportation on taking damage. See also 'Teleportation Odds on Damage'.",
                Section = "Teleportation",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });
        var tpOnDoorInteract = new BoolCheckBoxConfigItem(
            config.TeleportOnInteractDoor,
            new BoolCheckBoxOptions
            {
                Name = "Teleport on Entrance/Exit",
                Description =
                    "Allows teleportation when entering/exiting the Facility. See also 'Teleportation Odds on Entrance/Exit'.",
                Section = "Teleportation",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });


        var tpOddsOnDeath = new FloatSliderConfigItem(
            config.TeleportationOddsOnDeath,
            new FloatSliderOptions
            {
                Name = "Teleportation Odds on Death",
                Description = "The percentage chance of teleportation occurring on death.",
                Section = "Teleportation",
                Min = 0f,
                Max = 100f,
                RequiresRestart = false,
                CanModifyCallback = () => config.TeleportOnDeath.Value && NetworkAllowModifyCb()
            });
        var tpOddsOnClipping = new FloatSliderConfigItem(
            config.TeleportationOddsOnClipping,
            new FloatSliderOptions
            {
                Name = "Teleportation Odds on Clipping",
                Description = "The percentage chance of teleportation occurring on clipping through walls or ground.",
                Section = "Teleportation",
                Min = 0f,
                Max = 100f,
                RequiresRestart = false,
                CanModifyCallback = () => config.TeleportOnClipping.Value && NetworkAllowModifyCb()
            });
        var tpOddsOnDamage = new FloatSliderConfigItem(
            config.TeleportationOddsOnDamage,
            new FloatSliderOptions
            {
                Name = "Teleportation Odds on Damage",
                Description = "The percentage chance of teleportation occurring on taking damage.",
                Section = "Teleportation",
                Min = 0f,
                Max = 100f,
                RequiresRestart = false,
                CanModifyCallback = () => config.TeleportOnDamage.Value && NetworkAllowModifyCb()
            });
        var tpOddsOnDoorInteract = new FloatSliderConfigItem(
            config.TeleportationOddsOnInteractDoor,
            new FloatSliderOptions
            {
                Name = "Teleportation Odds on Entrance/Exit",
                Description = "The chance percentage of teleportation ocurring on entering/exiting the Facility.",
                Min = 0f,
                Max = 100f,
                RequiresRestart = false,
                CanModifyCallback = () => config.TeleportOnInteractDoor.Value && NetworkAllowModifyCb()
            });

        var dropItemsOnTp = new BoolCheckBoxConfigItem(
            config.DropHeldItemsOnTeleport,
            new BoolCheckBoxOptions()
            {
                Name = "Drop Items on Teleport",
                Description = "If enabled, will drop all the items when teleporting to the backrooms.",
                Section = "Teleportation",
                RequiresRestart = false,
                CanModifyCallback = NetworkAllowModifyCb
            });
        
        var genAlgorithm = new EnumDropDownConfigItem<BackroomsGenerator.MazeAlgorithm>(
            config.GenerationAlgorithm,
            new EnumDropDownOptions
            {
                Name = "Backrooms Generation Algorithm",
                Description = "The maze generation algorithm used for Backrooms levels. (Blob is recommended for optimal aesthetics)",
                Section = "Generation",
                RequiresRestart = true,
            });

        var genMinSize = new IntSliderConfigItem(
            config.MinBackroomsSize,
            new IntSliderOptions
            {
                Name = "Minimum Backrooms Size",
                Description = "The minimum size in cells (width and height) of generated Backrooms levels.",
                Section = "Generation",
                Min = 10,
                Max = 50,
                RequiresRestart = false,
            });
        var genMaxSize = new IntSliderConfigItem(
            config.MaxBackroomsSize,
            new IntSliderOptions
            {
                Name = "Maximum Backrooms Size",
                Description = "The maximum size in cells (width and height) of generated Backrooms levels.",
                Section = "Generation",
                Min = 10,
                Max = 50,
                RequiresRestart = false,
            });
        var genFakeExitMax = new IntInputFieldConfigItem(
            config.MaxFakeExitCount,
            new IntInputFieldOptions
            {
                Name = "Maximum Fake Exits Count",
                Description = "The maximum number of fake exits that will appear in the Backrooms. They do not kill, just TP to somewhere else in the backrooms.",
                Section = "Generation",
                RequiresRestart = false,
                Min = 0,
                Max = 10,
            }
        );
        
        // Advanced

        var legacyNavGen = new BoolCheckBoxConfigItem(
            config.LegacyNavMeshGeneration,
            false
        );

        LethalConfigManager.AddConfigItem(streamerMode);
        LethalConfigManager.AddConfigItem(fairRandomizer);

        LethalConfigManager.AddConfigItem(dropItemsOnTp);
        LethalConfigManager.AddConfigItem(tpOnDeath);
        LethalConfigManager.AddConfigItem(tpOddsOnDeath);
        LethalConfigManager.AddConfigItem(tpOnClipping);
        LethalConfigManager.AddConfigItem(tpOddsOnClipping);
        LethalConfigManager.AddConfigItem(tpOnDamage);
        LethalConfigManager.AddConfigItem(tpOddsOnDamage);

        LethalConfigManager.AddConfigItem(genAlgorithm);
        LethalConfigManager.AddConfigItem(genMinSize);
        LethalConfigManager.AddConfigItem(genMaxSize);
        LethalConfigManager.AddConfigItem(genFakeExitMax);
        
        LethalConfigManager.AddConfigItem(legacyNavGen);
    }
}
