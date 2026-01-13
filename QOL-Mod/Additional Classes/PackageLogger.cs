#if DEBUG
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QOL;

class PackageLogger
{
    public static void ProcessPacketLog(CSteamID clientID, byte[] data, P2PPackageHandler.MsgType messageType, int channel)
    {
        var cmd = ChatCommands.CmdDict["logpkg"];
        if (!cmd.IsEnabled) return;

        var filterStr = cmd.Option;
        if (string.IsNullOrEmpty(filterStr))
        {
            LogPacket(clientID, data, messageType, channel);
            return;
        }

        var msgTypeEnum = typeof(P2PPackageHandler.MsgType);
        var allowedTypes = new HashSet<P2PPackageHandler.MsgType>();

        foreach (var typeName in filterStr.Split([' '], StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var parsedObj = Enum.Parse(msgTypeEnum, typeName, true);
                var parsedType = (P2PPackageHandler.MsgType)parsedObj;
                allowedTypes.Add(parsedType);
            }
            catch (ArgumentException)
            {
                Debug.LogWarning($"Invalid MsgType: {typeName} (ignored)");
            }
        }

        if (allowedTypes.Contains(messageType))
        {
            LogPacket(clientID, data, messageType, channel);
        }
    }

    //[HarmonyPostfix]
    //[HarmonyPatch(typeof(NetworkPlayer), "SyncClientState")]
    //private static void SyncClientStatePostfix(byte[] data)
    //{
    //    var cmd = ChatCommands.CmdDict["logpkg"];
    //    if (!cmd.IsEnabled) return;

    //    var filterStr = cmd.Option;
    //    if (string.IsNullOrEmpty(filterStr))
    //    {
    //        LogPacket(clientID, data, messageType, channel);
    //        return;
    //    }

    //    var msgTypeEnum = typeof(P2PPackageHandler.MsgType);
    //    var allowedTypes = new HashSet<P2PPackageHandler.MsgType>();

    //    foreach (var typeName in filterStr.Split([' '], StringSplitOptions.RemoveEmptyEntries))
    //    {
    //        try
    //        {
    //            var parsedObj = Enum.Parse(msgTypeEnum, typeName, true);
    //            var parsedType = (P2PPackageHandler.MsgType)parsedObj;
    //            allowedTypes.Add(parsedType);
    //        }
    //        catch (ArgumentException)
    //        {
    //            Debug.LogWarning($"Invalid MsgType: {typeName} (ignored)");
    //        }
    //    }

    //    if (allowedTypes.Contains(messageType))
    //    {
    //        LogPacket(clientID, data, messageType, channel);
    //    }

    //}

    private static void LogPacket(CSteamID clientID, byte[] data, P2PPackageHandler.MsgType type, int channel)
    {
        if (type == P2PPackageHandler.MsgType.PlayerUpdate && data.Length == 18)
        {
            return; // Ignore player moves
        }

        Debug.Log($"[PkgLogger] Sent message to client: {clientID}\n" +
                  $"type: {type}\n" +
                  $"data: {data.ToDecString()}\n" +
                  $"channel: {channel}");
    }
}
#endif