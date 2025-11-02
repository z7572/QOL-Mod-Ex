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
                CheckPacket(steamIdRemote, true);
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

    // Check and block kick packets and auto blacklist
    public static void CheckPacket(CSteamID kickPacketSender, bool isKickPacket)
    {
        var senderPlayerColor = Helper.GetColorFromID(Helper.ClientData
            .First(data => data.ClientID == kickPacketSender)
            .PlayerObject.GetComponent<NetworkPlayer>()
            .NetworkSpawnID);
        var senderPlayerID = kickPacketSender.ToString();
        var senderPlayerName = Helper.GetPlayerName(kickPacketSender);

        if (Blacklist.IsPlayerBlacklisted(senderPlayerID)) // In case non-host lobby
        {
            Helper.TrustedKicker = false;
            Helper.LastPacketSender = kickPacketSender;
            Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor} (Blacklisted)", Command.LogType.Warning, false);
            Debug.LogWarning($"Blocked kick sent by: {senderPlayerName}, SteamID: {senderPlayerID} (Blacklisted)");
            return;
        }

        // SteamID's are Monky and Rexi and z7572
        if (isKickPacket && kickPacketSender.m_SteamID is not
            (76561198202108442 or 76561198870040513 or 76561198840554147))
        {
            Helper.TrustedKicker = false;
            Helper.LastPacketSender = kickPacketSender;
            Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor}, blacklisted!", Command.LogType.Warning, false);
            Debug.LogWarning($"Blocked kick sent by: {senderPlayerName}, SteamID: {senderPlayerID}");

            // Auto blacklist
            if (!Blacklist.IsPlayerBlacklisted(senderPlayerID))
            {
                Blacklist.AddToBlacklist(senderPlayerID, senderPlayerName);
                Debug.LogWarning($"Added {senderPlayerID}({senderPlayerName}) to blacklist!");
            }
            else
            {
                Debug.LogWarning($"{senderPlayerID}({senderPlayerName}) is already blacklisted!");
            }
            return;
        }

        Helper.TrustedKicker = true;
    }
}