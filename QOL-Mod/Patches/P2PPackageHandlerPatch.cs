using System.Linq;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QOL.Patches;

[HarmonyPatch(typeof(P2PPackageHandler))]
public class P2PPackageHandlerPatch
{
    [HarmonyPatch("CheckMessageType")]
    [HarmonyPrefix]
    private static void CheckMessageTypeMethodPrefix(ref byte[] data, ref P2PPackageHandler.MsgType type, ref CSteamID steamIdRemote)
    {
        Helper.LastPacketSender = steamIdRemote;
        switch (type)
        {
            case P2PPackageHandler.MsgType.KickPlayer:
                CheatHelper.CheckPacket(steamIdRemote, true);
                break;
            case P2PPackageHandler.MsgType.WorkshopMapsLoaded:
                Helper.LastKickPacketSender = steamIdRemote;
                break;
            case P2PPackageHandler.MsgType.MapChange:
                Helper.LastKickPacketSender = steamIdRemote;
                break;
            default:
                break;
        }
    }

#if DEBUG
    [HarmonyPostfix]
    [HarmonyPatch(typeof(P2PPackageHandler), "SendP2PPacketToUser", [typeof(CSteamID), typeof(byte[]), typeof(P2PPackageHandler.MsgType), typeof(EP2PSend), typeof(int)])]
    private static void SendP2PPacketToUserPostfix(CSteamID clientID, byte[] data, P2PPackageHandler.MsgType messageType, EP2PSend sendMethod, int channel)
    {
        PackageLogger.ProcessPacketLog(clientID, data, messageType, channel);
    }
#endif
}