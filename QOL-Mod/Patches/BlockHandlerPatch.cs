using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace QOL.Patches;

class BlockHandlerPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var updateMethod = AccessTools.Method(typeof(BlockHandler), "Update");
        var updateMethodPostfix = new HarmonyMethod(typeof(BlockHandlerPatch)
            .GetMethod(nameof(UpdateMethodPostfix)));
        harmonyInstance.Patch(updateMethod, postfix: updateMethodPostfix);
    }

    public static void UpdateMethodPostfix(BlockHandler __instance, ref BlockAnimation ___anim)
    {
        if (ChatCommands.CmdDict["blockall"].IsEnabled)
        {
            var fighting = Traverse.Create(__instance).Field("fighting").GetValue<Fighting>();
            var controller = Traverse.Create(fighting).Field("controller").GetValue<Controller>();
            if (controller != Helper.controller) return;
            ___anim.blockPower = 99999f;
            __instance.blockForce = 99999f;
            __instance.sinceBlockStart = 0f;
        }
    }
}
