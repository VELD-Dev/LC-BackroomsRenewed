namespace VELDDev.BackroomsRenewed.Utils;

public class FairRandomizer : NetworkBehaviour
{
    public const string DEATH_EVENT = "OnDeath";
    public const string OPEN_DOOR_EVENT = "OnEnterFacility";
    public const string DAMAGE_EVENT = "OnDamage";
    public const string CLIPPING_EVENT = "OnClipping";
    public const string SHIP_TP = "OnShipTP";
    public const string SHIP_REV_TP = "OnShipRevTP";

    public readonly Dictionary<string, float> LuckDictionary = [];

    void Awake()
    {
        LuckDictionary.Add(DEATH_EVENT, 0f);
        LuckDictionary.Add(OPEN_DOOR_EVENT, 0f);
        LuckDictionary.Add(DAMAGE_EVENT, 0f);
        LuckDictionary.Add(CLIPPING_EVENT, 0f);
        LuckDictionary.Add(SHIP_TP, 0f);
        LuckDictionary.Add(SHIP_REV_TP, 0f);
    }

    /// <summary>
    /// Checks if the event should trigger based on accumulated luck. It's a pity system.
    /// On success: resets luck to 0. On failure: increases luck by chance.
    /// </summary>
    /// <param name="eventName">The event type (use constants like DEATH_EVENT)</param>
    /// <param name="chance">Chance (percentage * 100)</param>
    /// <returns>True if the event triggers (player gets teleported)</returns>
    public bool CheckChance(string eventName, float chance)
    {
        if (!LuckDictionary.TryGetValue(eventName, out float currentLuck))
        {
            Plugin.Instance.logger.LogWarning($"FairRandomizer: Unknown event '{eventName}', defaulting to base threshold.");
            currentLuck = 0f;
        }

        float roll = Random.Range(0f, 1f);

        if (roll < currentLuck)
        {
            LuckDictionary[eventName] = 0f;
            return true;
        }
        
        // Failure - increase luck for next time
        LuckDictionary[eventName] += chance;
        return false;
    }

    /// <summary>
    /// Resets luck for a specific event back to 0.
    /// </summary>
    public void ResetLuck(string eventName)
    {
        if (LuckDictionary.ContainsKey(eventName))
        {
            LuckDictionary[eventName] = 0f;
        }
    }

    /// <summary>
    /// Resets all luck values back to 0.
    /// </summary>
    public void ResetAllLuck()
    {
        foreach (var key in LuckDictionary.Keys.ToList())
        {
            LuckDictionary[key] = 0f;
        }
    }
}