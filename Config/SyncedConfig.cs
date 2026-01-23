using BepInEx.Configuration;
using Unity.Collections;

namespace VELDDev.BackroomsRenewed.Config;

public class SyncedConfig : Synchronizable<SyncedConfig>
{
    public bool UseFairRandomizer;
    public bool TeleportOnDeath;
    public bool TeleportOnClipping;
    public bool TeleportOnDamage;
    public bool TeleportOnInteractDoor;
    public bool TeleportOnShipTP;
    public bool TeleportOnShipRevTP;
    public float TeleportationOddsOnDeath;
    public float TeleportationOddsOnClipping;
    public float TeleportationOddsOnDamage;
    public float TeleportationOddsOnInteractDoor;
    public float TeleportationOddsOnShipTP;
    public float TeleportationOddsOnShipRevTP;
    public bool DropHeldItemsOnTeleport;

    [NonSerialized] public LocalConfig config;

    public SyncedConfig(LocalConfig cfg)
    {
        InitInstance(this);
        config = cfg;
        
        // TODO: Add security to prevent Non-hosts from modifying the Instance when Synced. (Local function?)
        config.UseFairRandomizer.SettingChanged += (v, _) => UseFairRandomizer = (bool)v;
        config.TeleportOnDeath.SettingChanged += (v, _) => TeleportOnDeath = (bool)v;
        config.TeleportOnClipping.SettingChanged += (v, _) => TeleportOnClipping = (bool)v;
        config.TeleportOnDamage.SettingChanged += (v, _) => TeleportOnDamage = (bool)v;
        config.TeleportOnInteractDoor.SettingChanged += (v, _) => TeleportOnInteractDoor = (bool)v;
        config.TeleportOnShipTeleport.SettingChanged += (v, _) => TeleportOnShipTP = (bool)v;
        config.TeleportOnShipRevertTeleport.SettingChanged += (v, _) => TeleportOnShipRevTP = (bool)v;
        config.TeleportationOddsOnDeath.SettingChanged += (v, _) => TeleportationOddsOnDeath = (float)v;
        config.TeleportationOddsOnClipping.SettingChanged += (v, _) => TeleportationOddsOnClipping = (float)v;
        config.TeleportationOddsOnDamage.SettingChanged += (v, _) => TeleportationOddsOnDamage = (float)v;
        config.TeleportationOddsOnInteractDoor.SettingChanged += (v, _) => TeleportationOddsOnInteractDoor = (float)v;
        config.TeleportationOddsOnShipTeleport.SettingChanged += (v, _) => TeleportationOddsOnShipTP = (float)v;
        config.TeleportationOddsOnShipTeleport.SettingChanged += (v, _) => TeleportationOddsOnShipRevTP = (float)v;
        config.DropHeldItemsOnTeleport.SettingChanged += (v, _) => DropHeldItemsOnTeleport = (bool)v;

        // Force synchronization when host changes settings.
        cfg.CfgFile.SettingChanged += (_, __) => BroadcastSync();
        
        UseFairRandomizer = config.UseFairRandomizer.Value;
        TeleportOnDeath = config.TeleportOnDeath.Value;
        TeleportOnClipping = config.TeleportOnClipping.Value;
        TeleportOnDamage = config.TeleportOnDamage.Value;
        TeleportOnInteractDoor = config.TeleportOnInteractDoor.Value;
        TeleportOnShipTP = config.TeleportOnShipTeleport.Value;
        TeleportOnShipRevTP = config.TeleportOnShipRevertTeleport.Value;
        TeleportationOddsOnDeath = config.TeleportationOddsOnDeath.Value;
        TeleportationOddsOnClipping = config.TeleportationOddsOnClipping.Value;
        TeleportationOddsOnDamage = config.TeleportationOddsOnDamage.Value;
        TeleportationOddsOnInteractDoor = config.TeleportationOddsOnInteractDoor.Value;
        TeleportationOddsOnShipTP = config.TeleportationOddsOnShipTeleport.Value;
        TeleportationOddsOnShipRevTP = config.TeleportationOddsOnShipRevertTeleport.Value;
        DropHeldItemsOnTeleport = config.DropHeldItemsOnTeleport.Value;
    }

    public static void BroadcastSync()
    {
        if (!IsHost) return;
        
        Plugin.Instance.logger.LogDebug($"Host is broadcasting their config");

        byte[] data = Serialize(Instance);
        var trueLength = data.Length;
        var fbwLength = FastBufferWriter.GetWriteSize(data) + IntSize;
        
        using FastBufferWriter writer = new(fbwLength, Allocator.Temp);
        try
        {
            writer.WriteValueSafe(in trueLength, default);
            writer.WriteBytesSafe(data);

            MessagingManager.SendNamedMessageToAll("BackroomsRenewed_OnReceiveConfigSync", writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }
        catch (Exception ex)
        {
            Plugin.Instance.logger.LogError($"The config sync braodcast lamentably failed: {ex}");
        }
    }

    public static void RequestSync()
    {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessagingManager.SendNamedMessage("BackroomsRenewed_OnRequestConfigSync", 0uL, stream);
        Plugin.Instance.logger.LogInfo($"Requesting host's config...");
    }

    public static void OnRequestSync(ulong clientId, FastBufferReader _)
    {
        if (!IsHost) return;
        
        Plugin.Instance.logger.LogDebug($"Config request sync received from client {clientId}");

        byte[] data = Serialize(Instance);
        var trueLength = data.Length;
        var fbwLength = FastBufferWriter.GetWriteSize(data)  + IntSize;

        using FastBufferWriter stream = new FastBufferWriter(fbwLength, Allocator.Temp);

        try
        {
            stream.WriteValueSafe(in trueLength);
            stream.WriteBytesSafe(data);

            MessagingManager.SendNamedMessage("BackroomsRenewed_OnReceiveConfigSync", clientId, stream,
                NetworkDelivery.ReliableFragmentedSequenced);
        }
        catch (Exception ex)
        {
            Plugin.Instance.logger.LogError($"Couldn't sync configs between clients: {ex}");
        }
    }
    
    public static void OnReceiveSync(ulong _, FastBufferReader reader) 
    {
        if (!reader.TryBeginRead(IntSize))
        {
            Plugin.Instance.logger.LogError($"Config sync error: Could not begin reading buffer.");
            return;
        }
        
        reader.ReadValueSafe(out int length);
        if (!reader.TryBeginRead(length))
        {
            Plugin.Instance.logger.LogError($"Config sync error: Announced length and data length mismatch");
            return;
        }

        var data = new byte[length];
        reader.ReadBytesSafe(ref data, length);
        
        SyncInstance(data);
        
        Plugin.Instance.logger.LogInfo($"Config successfully synchronized with the host.");
    }
}