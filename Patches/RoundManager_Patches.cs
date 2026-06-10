namespace VELDDev.BackroomsRenewed.Patches;

[HarmonyPatch(typeof(RoundManager))]
public static class RoundManager_Patches
{
    [HarmonyPostfix, HarmonyPatch(nameof(RoundManager.DespawnPropsAtEndOfRound))]
    public static void DespawnBackroomsAtEndOfRound(RoundManager __instance)
    {
        if (!__instance.IsServer)
            return;

        if (!Backrooms.Instance)
            return;
        
        Backrooms.Instance.GetComponent<NetworkObject>().Despawn(true);
        GameObject.Destroy(Backrooms.Instance.gameObject);
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

        var dungenRootY = 50f;
        if (__instance.dungeonGenerator != null && __instance.dungeonGenerator.Root != null)
        {
            dungenRootY = __instance.dungeonGenerator.Root.transform.position.y;
        }

        var backroomsGo = GameObject.Instantiate(Plugin.Instance.BackroomsPrefab, new Vector3(5000, dungenRootY, 0), Quaternion.identity);
        backroomsGo.GetComponent<NetworkObject>().Spawn(true);
        
    }
}
