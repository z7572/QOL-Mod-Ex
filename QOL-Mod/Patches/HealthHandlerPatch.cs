using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class HealthHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var takeDamageMethod = AccessTools.Method(typeof(HealthHandler), "TakeDamage",
                new System.Type[] { typeof(float), typeof(Controller), typeof(DamageType), typeof(bool), typeof(Vector3), typeof(Vector3) });
            var takeDamageByPlayerMethod = AccessTools.Method(typeof(HealthHandler), "TakeDamage",
                new System.Type[] { typeof(float), typeof(byte), typeof(bool) });
            var takeDamageMethodPrefix = new HarmonyMethod(typeof(HealthHandlerPatch).GetMethod(nameof(TakeDamageMethodPrefix)));

            harmonyInstance.Patch(takeDamageMethod, prefix: takeDamageMethodPrefix);
            harmonyInstance.Patch(takeDamageByPlayerMethod, prefix: takeDamageMethodPrefix);
        }

        public static bool TakeDamageMethodPrefix(Fighting __instance)
        {
            return !ChatCommands.CmdDict["god"].IsEnabled;
        }
    }
}
