using HarmonyLib;
using UnityEngine;

// Fix for AFK players not taking damage due to missing AI component in HealthHandler
[HarmonyPatch(typeof(HealthHandler), "TakeDamage", [typeof(float), typeof(Controller), typeof(DamageType), typeof(bool), typeof(Vector3), typeof(Vector3)])]
public static class AFKDamageFix
{
    [HarmonyPrefix]
    public static void Prefix(HealthHandler __instance)
    {
        var aiField = AccessTools.Field(typeof(HealthHandler), "ai");
        var controller = __instance.GetComponent<Controller>();

        if (controller.isAI && aiField.GetValue(__instance) == null)
        {
            aiField.SetValue(__instance, __instance.GetComponent<AI>());
        }
    }
}