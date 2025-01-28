using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QOL
{

    public class P2PPackageHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var checkMessageTypeMethod = AccessTools.Method(typeof(P2PPackageHandler), "CheckMessageType");
            var checkMessageTypeMethodTranspiler = new HarmonyMethod(typeof(P2PPackageHandlerPatch)
                .GetMethod(nameof(CheckMessageTypeMethodTranspiler)));
            harmonyInstance.Patch(checkMessageTypeMethod, transpiler: checkMessageTypeMethodTranspiler);
        }

        public static IEnumerable<CodeInstruction> CheckMessageTypeMethodTranspiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var onKickedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnKicked");
            var onMapsRecievedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnNewWorkshopMapsRecieved");
            var instructionList = instructions.ToList();

            for (var i = 0; i < instructionList.Count; i++)
            {
                if (!instructionList[i].Calls(onKickedMethod)) continue;

                instructionList.InsertRange(i + 1, new[]
                {
                    // P2PPackageHandlerPatch.PreventKick(steamIdRemote)
                    new CodeInstruction(OpCodes.Ldarg_3),
                    CodeInstruction.Call(typeof(P2PPackageHandlerPatch), nameof(PreventKick))
                });

                Debug.Log("Found and patched CheckMessageType method for OnKicked!!");
                break;
            }

            for (var i = 0; i < instructionList.Count; i++)
            {
                if(!instructionList[i].Calls(onMapsRecievedMethod)) continue;

                instructionList.InsertRange(i + 1, new[]
                {
                    // P2PPackageHandlerPatch.PreventKick(steamIdRemote, true)
                    new CodeInstruction(OpCodes.Ldarg_3),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    CodeInstruction.Call(typeof(P2PPackageHandlerPatch), nameof(PreventKick))
                });

                Debug.Log("Found and patched CheckMessageType method for");
                break;
            }

            return instructionList.AsEnumerable();
        }

        [HarmonyPatch(typeof(P2PPackageHandler), "GetChannelForMsgType")]
        [HarmonyReversePatch]
        public static int GetChannelForMsgType(object instance, P2PPackageHandler.MsgType msgType) => 0;

        // SteamID's are Monky and Rexi and z7572
        private static void PreventKick(CSteamID kickPacketSender, bool useBlacklist = false)
        {
            var senderPlayerColor = Helper.GetColorFromID(Helper.ClientData
                .First(data => data.ClientID == kickPacketSender)
                .PlayerObject.GetComponent<NetworkPlayer>()
                .NetworkSpawnID);

            if (useBlacklist)
            {
                if (ConfigHandler.BlacklistedPlayers.Contains(senderPlayerColor))
                {
                    Helper.TrustedKicker = false;
                    Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor}", Command.LogType.Warning, false);
                    Debug.LogWarning($"Blocked kick sent by: {senderPlayerColor}, SteamID: {kickPacketSender.m_SteamID} (Blacklisted)");
                    return;
                }
            }

            if (kickPacketSender.m_SteamID is not (76561198202108442 or 76561198870040513 or 76561198840554147))
            {
                Helper.TrustedKicker = false;
                Helper.SendModOutput($"Blocked kick sent by: {senderPlayerColor}", Command.LogType.Warning, false);
                Debug.LogWarning($"Blocked kick sent by: {senderPlayerColor}, SteamID: {kickPacketSender.m_SteamID}");
                // Auto blacklist
                if (!ConfigHandler.BlacklistedPlayers.Contains(senderPlayerColor))
                {
                    ConfigHandler.BlacklistedPlayers.AddToArray(senderPlayerColor);
                    ConfigHandler.ModifyEntry("Blacklist", string.Join(",", ConfigHandler.BlacklistedPlayers));
                }
                return;
            }

            Helper.TrustedKicker = true;
        }
    }
}