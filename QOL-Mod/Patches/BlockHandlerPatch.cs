using HarmonyLib;

namespace QOL.Patches;

[HarmonyPatch(typeof(BlockHandler))]
public static class BlockHandlerPatch
{
    [HarmonyPatch("Update")]
    [HarmonyPostfix]
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
