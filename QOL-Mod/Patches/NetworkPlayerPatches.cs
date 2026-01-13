using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace QOL.Patches;

[HarmonyPatch(typeof(NetworkPlayer))]
class NetworkPlayerPatch
{
    public static void Patch(Harmony harmonyInstance) // NetworkPlayer methods to patch with the harmony instance
    {
        var syncClientChatMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat");
        var syncClientChatMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatch)
            .GetMethod(nameof(SyncClientChatMethodPrefix)));
        harmonyInstance.Patch(syncClientChatMethod, prefix: syncClientChatMethodPrefix);
    }

    [HarmonyPatch("CreateNetworkPositionPackage")]
    [HarmonyPostfix]
    private static void CreateNetworkPositionPackagePostfix(ref NetworkPlayer.NetworkPositionPackage __result)
    {
        if (ChatCommands.CmdDict["invisible"].IsEnabled)
        {
            __result.Position = new ShortVector2(-2000, 0);
            __result.Rotation = new ByteVector2(0, 0);
        }
    }

    [HarmonyPatch("ListenForPositionPackages")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ListenForPositionPackagesTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var matcher = new CodeMatcher(codes);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Failed to read P2P Package!"));
        int insertIndex = matcher.Pos + 3;
        var newCodes = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1), // byte[] array (index 1)
                new CodeInstruction(OpCodes.Ldloc_S, 3), // CSteamID csteamID (index 3)
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetworkPlayerPatch), nameof(IsProjectilePacketOverLimit)))
            };

        codes.InsertRange(insertIndex, newCodes);
        return codes;
    }

    public static void IsProjectilePacketOverLimit(byte[] array, CSteamID sender)
    {
        AllowProjectilePacket = true;
        int bulletCount = -1;
        if (array.Length >= 12 && (array.Length - 12) % 8 == 0)
        {
            bulletCount = (array.Length - 12) / 8;
        }
        else
        {
            Helper.ShowLoadText($"Unexpected projectile packet length: {array.Length}, sender: {sender}");
            Debug.LogWarning($"Unexpected projectile packet length: {array.Length}, sender: {sender}");
        }

        if (bulletCount >= 10)
        {
            Helper.LastKickPacketSender = sender;
            Helper.GetColorFromID(Helper.ClientData
            .First(data => data.ClientID == Helper.LastKickPacketSender)
            .PlayerObject.GetComponent<NetworkPlayer>()
            .NetworkSpawnID);
            AllowProjectilePacket = false;
            CheatHelper.CheckPacket(sender, true);
            Helper.ShowLoadText($"Blocked {bulletCount} projectiles packets sent from player: {Helper.GetPlayerName(sender)} Blacklisted!");
            return;
        }
    }

    private static bool AllowProjectilePacket = true;

    // Guards against projectile packets spamming
    [HarmonyPatch("SyncClientState")]
    [HarmonyPrefix]
    private static bool SyncClientStatePrefix() => AllowProjectilePacket;

    public static bool SyncClientChatMethodPrefix(ref byte[] data, NetworkPlayer __instance)
    {
        if (Helper.MutedPlayers.Contains(__instance.NetworkSpawnID))
        {
            return false;
        }

        if (!ChatCommands.CmdDict["translate"].IsEnabled) return true;

        TranslateMessage(data, __instance);
        return false;
    }

    // TODO: Refactor and expand upon this
    // Checks if auto-translation is enabled, if so then translate it
    private static void TranslateMessage(byte[] data, NetworkPlayer __instance)
    {
        var textToTranslate = Encoding.UTF8.GetString(data);
        Debug.Log("Got message: " + textToTranslate);

        var authKey = ConfigHandler.GetEntry<string>("AutoAuthTranslationsAPIKey");
        var usingKey = !string.IsNullOrEmpty(authKey);

        var mHasLocalControl = Traverse.Create(__instance).Field("mHasLocalControl").GetValue<bool>();
        var mLocalChatManager = AccessTools.StaticFieldRefAccess<ChatManager>(typeof(NetworkPlayer),
            "mLocalChatManager");
        Debug.Log("mLocalChatManager : " + mLocalChatManager);
        Debug.Log("mHasLocalControl : " + mHasLocalControl);

        if (mHasLocalControl)
        {
            if (usingKey)
            {
                __instance.StartCoroutine(AuthTranslate.TranslateText("auto",
                    "en",
                    textToTranslate,
                    s => mLocalChatManager.Talk(s)));

                return;
            }

            __instance.StartCoroutine(Translate.Process("en",
                textToTranslate,
                s => mLocalChatManager.Talk(s)));

            return;
        }

        var mChatManager = Traverse.Create(__instance).Field("mChatManager").GetValue<ChatManager>();

        if (!usingKey)
        {
            __instance.StartCoroutine(Translate.Process("en",
                textToTranslate,
                s => mChatManager.Talk(s)));

            return;
        }

        __instance.StartCoroutine(AuthTranslate.TranslateText("auto",
            "en",
            textToTranslate,
            s => mChatManager.Talk(s)));
    }
}