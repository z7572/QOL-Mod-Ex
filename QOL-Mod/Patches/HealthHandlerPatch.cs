using HarmonyLib;
using UnityEngine;

namespace QOL.Patches;

[HarmonyPatch]
class HealthHandlerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BodyPart), "TakeDamage")]
    [HarmonyPatch(typeof(BodyPart), "TakeDamageWithParticle", [typeof(float), typeof(Vector3), typeof(Vector3), typeof(Controller), typeof(DamageType)])]
    [HarmonyPatch(typeof(BodyPart), "TakeDamageWithParticle", [typeof(float), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(Controller)])]
    [HarmonyPatch(typeof(HealthHandler), "TakeDamage", [typeof(float), typeof(byte), typeof(bool)])]
    [HarmonyPatch(typeof(HealthHandler), "TakeDamage", [typeof(float), typeof(Controller), typeof(DamageType), typeof(bool), typeof(Vector3), typeof(Vector3)])]
    public static bool Prefix(object __instance)
    {
        var controller = Traverse.Create(__instance).Field("controller").GetValue<Controller>();
        if (controller == Helper.controller)
        {
            return !ChatCommands.CmdDict["god"].IsEnabled;
        }
        return true;
    }
}