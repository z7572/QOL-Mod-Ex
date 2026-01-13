using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace QOL.Trainer.Patches
{
    [HarmonyPatch(typeof(Weapon), "ActuallyShoot")]
    public static class BulletTrackerPatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AttachTrackerTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var attachMethod = AccessTools.Method(typeof(BulletTrackerPatch), nameof(AttachTracker));

            for (int i = 0; i < codes.Count; i++)
            {
                // Find: Instantiate<GameObject>(...)
                if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("Instantiate"))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Dup));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, attachMethod));
                    break;
                }
            }
            return codes;
        }

        public static void AttachTracker(GameObject bulletObj)
        {
            if (bulletObj != null && bulletObj.GetComponent<BulletTracker>() == null)
            {
                bulletObj.AddComponent<BulletTracker>();
            }
        }
    }
}