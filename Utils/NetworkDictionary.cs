namespace VELDDev.BackroomsRenewed.Utils;

/// <summary>
/// Serializable key-value pair struct for luck values using byte key (enum) and float value.
/// </summary>
public struct LuckKeyValuePair : INetworkSerializable, IEquatable<LuckKeyValuePair>
{
    public byte Key;
    public float Value;

    public LuckKeyValuePair(byte key, float value)
    {
        Key = key;
        Value = value;
    }

    public LuckKeyValuePair(FairRandomizerEvent eventType, float value)
    {
        Key = (byte)eventType;
        Value = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Key);
        serializer.SerializeValue(ref Value);
    }

    public bool Equals(LuckKeyValuePair other)
    {
        return Key == other.Key && Math.Abs(Value - other.Value) < 0.0001f;
    }

    public override bool Equals(object obj)
    {
        return obj is LuckKeyValuePair other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value);
    }
}

/// <summary>
/// A network-synchronized dictionary for FairRandomizer luck values.
/// Uses byte keys (for FairRandomizerEvent enum) and float values.
/// Built on top of NetworkList for automatic synchronization.
/// </summary>
public class NetworkLuckDictionary : IDisposable, IEnumerable<KeyValuePair<FairRandomizerEvent, float>>
{
    private readonly NetworkList<LuckKeyValuePair> _networkList;

    public event Action<FairRandomizerEvent, float>? OnValueChanged;
    public event Action<FairRandomizerEvent, float>? OnKeyAdded;
    public event Action<FairRandomizerEvent, float>? OnKeyRemoved;

    public int Count => _networkList.Count;

    public NetworkLuckDictionary()
    {
        _networkList = new NetworkList<LuckKeyValuePair>();
        _networkList.OnListChanged += HandleListChanged;
    }

    public NetworkLuckDictionary(NetworkVariableReadPermission readPermission, NetworkVariableWritePermission writePermission)
    {
        _networkList = new NetworkList<LuckKeyValuePair>(readPerm: readPermission, writePerm: writePermission);
        _networkList.OnListChanged += HandleListChanged;
    }

    private void HandleListChanged(NetworkListEvent<LuckKeyValuePair> changeEvent)
    {
        var eventType = (FairRandomizerEvent)changeEvent.Value.Key;
        switch (changeEvent.Type)
        {
            case NetworkListEvent<LuckKeyValuePair>.EventType.Add:
                OnKeyAdded?.Invoke(eventType, changeEvent.Value.Value);
                break;
            case NetworkListEvent<LuckKeyValuePair>.EventType.Remove:
            case NetworkListEvent<LuckKeyValuePair>.EventType.RemoveAt:
                OnKeyRemoved?.Invoke(eventType, changeEvent.Value.Value);
                break;
            case NetworkListEvent<LuckKeyValuePair>.EventType.Value:
                OnValueChanged?.Invoke(eventType, changeEvent.Value.Value);
                break;
            case NetworkListEvent<LuckKeyValuePair>.EventType.Clear:
                break;
        }
    }

    /// <summary>
    /// Gets or sets the luck value associated with the specified event type.
    /// </summary>
    public float this[FairRandomizerEvent eventType]
    {
        get
        {
            byte key = (byte)eventType;
            for (int i = 0; i < _networkList.Count; i++)
            {
                if (_networkList[i].Key == key)
                {
                    return _networkList[i].Value;
                }
            }
            throw new KeyNotFoundException($"Event '{eventType}' not found in NetworkLuckDictionary.");
        }
        set
        {
            byte key = (byte)eventType;
            for (int i = 0; i < _networkList.Count; i++)
            {
                if (_networkList[i].Key == key)
                {
                    _networkList[i] = new LuckKeyValuePair(key, value);
                    return;
                }
            }
            _networkList.Add(new LuckKeyValuePair(key, value));
        }
    }

    /// <summary>
    /// Adds a luck value for an event type.
    /// </summary>
    public void Add(FairRandomizerEvent eventType, float value)
    {
        if (ContainsKey(eventType))
        {
            throw new ArgumentException($"Event '{eventType}' already exists in NetworkLuckDictionary.");
        }
        _networkList.Add(new LuckKeyValuePair((byte)eventType, value));
    }

    /// <summary>
    /// Tries to add a luck value. Returns false if event already exists.
    /// </summary>
    public bool TryAdd(FairRandomizerEvent eventType, float value)
    {
        if (ContainsKey(eventType))
        {
            return false;
        }
        _networkList.Add(new LuckKeyValuePair((byte)eventType, value));
        return true;
    }

    /// <summary>
    /// Removes the luck value for the specified event type.
    /// </summary>
    public bool Remove(FairRandomizerEvent eventType)
    {
        byte key = (byte)eventType;
        for (int i = 0; i < _networkList.Count; i++)
        {
            if (_networkList[i].Key == key)
            {
                _networkList.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified event type.
    /// </summary>
    public bool ContainsKey(FairRandomizerEvent eventType)
    {
        byte key = (byte)eventType;
        for (int i = 0; i < _networkList.Count; i++)
        {
            if (_networkList[i].Key == key)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tries to get the luck value associated with the specified event type.
    /// </summary>
    public bool TryGetValue(FairRandomizerEvent eventType, out float value)
    {
        byte key = (byte)eventType;
        for (int i = 0; i < _networkList.Count; i++)
        {
            if (_networkList[i].Key == key)
            {
                value = _networkList[i].Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Clears all luck values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _networkList.Clear();
    }

    /// <summary>
    /// Gets all event types in the dictionary.
    /// </summary>
    public IEnumerable<FairRandomizerEvent> Keys
    {
        get
        {
            for (int i = 0; i < _networkList.Count; i++)
            {
                yield return (FairRandomizerEvent)_networkList[i].Key;
            }
        }
    }

    /// <summary>
    /// Gets all luck values in the dictionary.
    /// </summary>
    public IEnumerable<float> Values
    {
        get
        {
            for (int i = 0; i < _networkList.Count; i++)
            {
                yield return _networkList[i].Value;
            }
        }
    }

    /// <summary>
    /// Gets the underlying NetworkList for initialization with NetworkBehaviour.
    /// </summary>
    public NetworkList<LuckKeyValuePair> GetNetworkList() => _networkList;

    public void Dispose()
    {
        _networkList.OnListChanged -= HandleListChanged;
        _networkList.Dispose();
    }

    public IEnumerator<KeyValuePair<FairRandomizerEvent, float>> GetEnumerator()
    {
        for (int i = 0; i < _networkList.Count; i++)
        {
            var kvp = _networkList[i];
            yield return new KeyValuePair<FairRandomizerEvent, float>((FairRandomizerEvent)kvp.Key, kvp.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}