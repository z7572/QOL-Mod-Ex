using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace QOL.Patches;

class LoadingScreenManagerPatch
{
    public static void Patch(Harmony harmony)
    {
        var loadThenFailMethod = AccessTools.Method(typeof(LoadingScreenManager), "LoadThenFail");
        var loadThenFailMethodPrefix = new HarmonyMethod(typeof(LoadingScreenManagerPatch)
            .GetMethod(nameof(LoadThenFailMethodPrefix)));
        harmony.Patch(loadThenFailMethod, prefix: loadThenFailMethodPrefix);
        
    }

    // Guards against Workshop_Corruption_Kick
    public static bool LoadThenFailMethodPrefix(LoadingScreenManager __instance,ref ConnectionErrorType type)
    {
        if (type == ConnectionErrorType.DownloadFailure)
        {
            if (Helper.TrustedKicker) return true;

            __instance.isShowingLoadingScreen = false;
            __instance.part.Stop();
            Debug.LogWarning(type);

            P2PPackageHandlerPatch.CheckPacket(Helper.LastKickPacketSender, true);

            return false;
        }

        return true;
    }
}
