namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(RoundManager))]
public class RoundManager_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(RoundManager.DespawnPropsAtEndOfRound))]
    public static void DespawnBackroomsAt(RoundManager __instance)
    {
        if (!__instance.IsServer)
            return;

        if (!Backrooms.Instance)
            return;
        
        Backrooms.Instance.GetComponent<NetworkObject>().Despawn(true);
    }

    [HarmonyPostfix, HarmonyPatch(nameof(RoundManager.SpawnSyncedProps))]
    public static void SpawnBackroomsAtStartOfRound(RoundManager __instance)
    {
        if (!__instance.IsServer)
            return;

        if (Backrooms.Instance)
        {
            Backrooms.Instance.GetComponent<NetworkObject>().Despawn(true);
            Backrooms.Instance = null;
        }

        var backroomsGo = GameObject.Instantiate(Plugin.Instance.BackroomsPrefab, new Vector3(0, -1000, 0), Quaternion.identity);
        backroomsGo.GetComponent<NetworkObject>().Spawn(true);
        
    }
}