namespace VELDDev.BackroomsRenewed.Utils;

/// <summary>
/// Enum representing the different event types for the FairRandomizer pity system.
/// </summary>
public enum FairRandomizerEvent : byte
{
    Death = 0,
    OpenDoor = 1,
    Damage = 2,
    Clipping = 3,
    ShipTP = 4,
    ShipRevTP = 5
}

public class FairRandomizer : NetworkBehaviour
{
    // Legacy string constants for backwards compatibility
    public const string DEATH_EVENT = "OnDeath";
    public const string OPEN_DOOR_EVENT = "OnEnterFacility";
    public const string DAMAGE_EVENT = "OnDamage";
    public const string CLIPPING_EVENT = "OnClipping";
    public const string SHIP_TP = "OnShipTP";
    public const string SHIP_REV_TP = "OnShipRevTP";

    private static readonly Dictionary<string, FairRandomizerEvent> StringToEventMap = new()
    {
        { DEATH_EVENT, FairRandomizerEvent.Death },
        { OPEN_DOOR_EVENT, FairRandomizerEvent.OpenDoor },
        { DAMAGE_EVENT, FairRandomizerEvent.Damage },
        { CLIPPING_EVENT, FairRandomizerEvent.Clipping },
        { SHIP_TP, FairRandomizerEvent.ShipTP },
        { SHIP_REV_TP, FairRandomizerEvent.ShipRevTP }
    };

    // Network-synchronized parallel lists for luck values
    private NetworkList<byte> _luckKeys = null!;
    private NetworkList<float> _luckValues = null!;

    // Legacy dictionary for backwards compatibility (read-only access)
    public Dictionary<string, float> LuckDictionary
    {
        get
        {
            var dict = new Dictionary<string, float>();
            foreach (var kvp in StringToEventMap)
            {
                if (TryGetLuckIndex(kvp.Value, out int index))
                {
                    dict[kvp.Key] = _luckValues[index];
                }
            }
            return dict;
        }
    }

    void Awake()
    {
        // Initialize with server-write permission so only server/host can modify luck values
        _luckKeys = new NetworkList<byte>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );
        _luckValues = new NetworkList<float>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the server initializes the luck values
        if (IsServer)
        {
            InitializeLuckValues();
        }
    }

    private void InitializeLuckValues()
    {
        TryAddLuck(FairRandomizerEvent.Death, 0f);
        TryAddLuck(FairRandomizerEvent.OpenDoor, 0f);
        TryAddLuck(FairRandomizerEvent.Damage, 0f);
        TryAddLuck(FairRandomizerEvent.Clipping, 0f);
        TryAddLuck(FairRandomizerEvent.ShipTP, 0f);
        TryAddLuck(FairRandomizerEvent.ShipRevTP, 0f);
    }

    private bool TryGetLuckIndex(FairRandomizerEvent eventType, out int index)
    {
        byte key = (byte)eventType;
        for (int i = 0; i < _luckKeys.Count; i++)
        {
            if (_luckKeys[i] == key)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    private bool TryAddLuck(FairRandomizerEvent eventType, float value)
    {
        if (TryGetLuckIndex(eventType, out _))
        {
            return false;
        }
        _luckKeys.Add((byte)eventType);
        _luckValues.Add(value);
        return true;
    }

    private void SetLuck(FairRandomizerEvent eventType, float value)
    {
        if (TryGetLuckIndex(eventType, out int index))
        {
            _luckValues[index] = value;
        }
        else
        {
            _luckKeys.Add((byte)eventType);
            _luckValues.Add(value);
        }
    }

    /// <summary>
    /// Checks if the event should trigger based on accumulated luck (server-authoritative).
    /// On success: resets luck to 0. On failure: increases luck by chance.
    /// </summary>
    /// <param name="eventType">The event type enum</param>
    /// <param name="chance">Chance (percentage as 0-1 float, e.g., 0.1 for 10%)</param>
    /// <returns>True if the event triggers (player gets teleported)</returns>
    public bool CheckChance(FairRandomizerEvent eventType, float chance)
    {
        float currentLuck;
        if (!TryGetLuckIndex(eventType, out int index))
        {
            Plugin.Instance.logger.LogWarning($"FairRandomizer: Event '{eventType}' not initialized, defaulting to base threshold.");
            currentLuck = 0f;
            if (IsServer)
            {
                TryAddLuck(eventType, 0f);
            }
        }
        else
        {
            currentLuck = _luckValues[index];
        }

        float roll = Random.Range(0f, 1f);

        if (roll < currentLuck)
        {
            if (IsServer)
            {
                SetLuck(eventType, 0f);
            }
            else
            {
                // Request server to reset luck
                ResetLuckServerRpc(eventType);
            }
            return true;
        }

        // Failure - increase luck for next time
        if (IsServer)
        {
            SetLuck(eventType, currentLuck + chance);
        }
        else
        {
            // Request server to increase luck
            IncreaseLuckServerRpc(eventType, chance);
        }
        return false;
    }

    /// <summary>
    /// Checks if the event should trigger based on accumulated luck (server-authoritative).
    /// Overload that accepts string event names for backwards compatibility.
    /// </summary>
    /// <param name="eventName">The event type string (use constants like DEATH_EVENT)</param>
    /// <param name="chance">Chance (percentage as 0-1 float)</param>
    /// <returns>True if the event triggers (player gets teleported)</returns>
    public bool CheckChance(string eventName, float chance)
    {
        if (!StringToEventMap.TryGetValue(eventName, out FairRandomizerEvent eventType))
        {
            Plugin.Instance.logger.LogWarning($"FairRandomizer: Unknown event '{eventName}'");
            return false;
        }
        return CheckChance(eventType, chance);
    }

    /// <summary>
    /// Gets the current luck value for an event (read-only, for both client and server).
    /// </summary>
    public float GetLuck(FairRandomizerEvent eventType)
    {
        return TryGetLuckIndex(eventType, out int index) ? _luckValues[index] : 0f;
    }

    /// <summary>
    /// Gets the current luck value for an event using string name.
    /// </summary>
    public float GetLuck(string eventName)
    {
        if (!StringToEventMap.TryGetValue(eventName, out FairRandomizerEvent eventType))
        {
            return 0f;
        }
        return GetLuck(eventType);
    }

    /// <summary>
    /// Resets luck for a specific event back to 0.
    /// </summary>
    public void ResetLuck(FairRandomizerEvent eventType)
    {
        if (IsServer)
        {
            if (TryGetLuckIndex(eventType, out int index))
            {
                _luckValues[index] = 0f;
            }
        }
        else
        {
            ResetLuckServerRpc(eventType);
        }
    }

    /// <summary>
    /// Resets luck for a specific event back to 0 (string overload for backwards compatibility).
    /// </summary>
    public void ResetLuck(string eventName)
    {
        if (StringToEventMap.TryGetValue(eventName, out FairRandomizerEvent eventType))
        {
            ResetLuck(eventType);
        }
    }

    /// <summary>
    /// Resets all luck values back to 0.
    /// </summary>
    public void ResetAllLuck()
    {
        if (IsServer)
        {
            for (int i = 0; i < _luckValues.Count; i++)
            {
                _luckValues[i] = 0f;
            }
        }
        else
        {
            ResetAllLuckServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetLuckServerRpc(FairRandomizerEvent eventType)
    {
        if (TryGetLuckIndex(eventType, out int index))
        {
            _luckValues[index] = 0f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncreaseLuckServerRpc(FairRandomizerEvent eventType, float amount)
    {
        if (TryGetLuckIndex(eventType, out int index))
        {
            _luckValues[index] = _luckValues[index] + amount;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetAllLuckServerRpc()
    {
        for (int i = 0; i < _luckValues.Count; i++)
        {
            _luckValues[i] = 0f;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _luckKeys?.Dispose();
        _luckValues?.Dispose();
    }
}