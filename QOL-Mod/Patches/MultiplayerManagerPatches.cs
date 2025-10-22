using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Steamworks;
using Object = UnityEngine.Object;


namespace QOL.Patches;

class MultiplayerManagerPatches
{
    public static void Patches(Harmony harmonyInstance) // Multiplayer methods to patch with the harmony __instance
    {
        var onServerJoinedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerJoined");
        var onServerJoinedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnServerJoinedMethodPostfix)));
        harmonyInstance.Patch(onServerJoinedMethod, postfix: onServerJoinedMethodPostfix);

        var onServerCreatedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerCreated");
        var onServerCreatedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnServerCreatedMethodPostfix)));
        harmonyInstance.Patch(onServerCreatedMethod, postfix: onServerCreatedMethodPostfix);

        var onPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnPlayerSpawned");
        // var onPlayerSpawnedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
        //     .GetMethod(nameof(OnPlayerSpawnedMethodPrefix)));
        // harmonyInstance.Patch(onPlayerSpawnedMethod, prefix: onPlayerSpawnedMethodPrefix);
        var onPlayerSpawnedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnPlayerSpawnedMethodPostfix)));
        harmonyInstance.Patch(onPlayerSpawnedMethod, postfix: onPlayerSpawnedMethodPostfix);

        var onKickedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnKicked");
        var onKickedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnKickedMethodPrefix)));
        harmonyInstance.Patch(onKickedMethod, prefix: onKickedMethodPrefix);

        var onMapChangedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnMapChanged");
        var onMapChangedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnMapChangedMethodPrefix)));
        harmonyInstance.Patch(onMapChangedMethod, prefix: onMapChangedMethodPrefix);

        var onNewWorkshopMapsRecievedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnNewWorkshopMapsRecieved");
        var onNewWorkshopMapsRecievedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
            .GetMethod(nameof(OnNewWorkshopMapsRecievedMethodPrefix)));
        harmonyInstance.Patch(onNewWorkshopMapsRecievedMethod, prefix: onNewWorkshopMapsRecievedMethodPrefix);
    }

    public static void OnServerJoinedMethodPostfix()
    {
        InitGUI();
        GameManagerPatches.LobbiesJoined += 1;
    }

    public static void OnServerCreatedMethodPostfix()
    {
        InitGUI();
        GameManagerPatches.LobbiesJoined += 1;
    }

    // Refuse player in the blacklist to join
    // https://server.monblog.top/alist/%E6%8F%92%E4%BB%B6/%E9%BB%91%E5%90%8D%E5%8D%95/stick.plugins.blacklist-1.0.0.dll
    [HarmonyPatch(typeof(MultiplayerManager), "AddClientToList")]
    [HarmonyPrefix]
    public static bool AddClientToListPrefix(P2PPackageHandler ___mPacketHandler, ref CSteamID newClient)
    {
        if (Blacklist.IsPlayerBlacklisted(newClient.ToString()))
        {
            Helper.ShowLoadText($"Refused blacklisted player to join:\n{Helper.GetPlayerName(newClient)}");
            //Helper.SendModOutput($"Blacklisted: {Helper.GetPlayerName(newClient)}", Command.LogType.Warning, false);
            Debug.LogWarning($"Refused blacklisted player to join: {newClient} {Helper.GetPlayerName(newClient)}");
            byte[] data = new byte[1];
            ___mPacketHandler.SendP2PPacketToUser(newClient, data, P2PPackageHandler.MsgType.ClientInit, EP2PSend.k_EP2PSendReliable, 0);
            return false;
        }
        return true;
    }

    // Guards against Built-in kick attempts made towards the user by skipping the method, if not Monky or Rexi or z7572
    public static bool OnKickedMethodPrefix() => Helper.TrustedKicker;

    public static bool OnMapChangedMethodPrefix(byte[] data)
    {
        if (Helper.TrustedKicker) return true;

        //Debug.Log("Map changed! Recieved data: " + data.ToByteString());

        // Guards against Invalid_Map
        if (data[1] == 0 && data[2] == 103) // LevelEditor
        {
            P2PPackageHandlerPatch.CheckPacket(Helper.LastPacketSender, true);
            return false;
        }

        if (data[2] == 86) // Lava6
        {
            CoroutineRunner.Run(Size());
            static IEnumerator Size()
            {
                yield return new WaitForSeconds(1f);
                var onMapSizeChangedMethod = AccessTools.Method(typeof(GameManager), "OnMapSizeChanged");
                onMapSizeChangedMethod.Invoke(GameManager.Instance, [12.5f]);
            }
        }

        return true;
    }

    // Guards against Workshop_Crash
    public static bool OnNewWorkshopMapsRecievedMethodPrefix(byte[] mapData)
    {
        if (Helper.TrustedKicker) return true;

        var senderPlayerColor = Helper.GetColorFromID(Helper.ClientData
            .First(data => data.ClientID == Helper.LastPacketSender)
            .PlayerObject.GetComponent<NetworkPlayer>()
            .NetworkSpawnID); ;
        var senderPlayerID = Helper.LastPacketSender.m_SteamID.ToString();

        if (mapData.Length > 802 /* 100 * 8 + 2 */ ) // 100 maps
        {
            Debug.LogWarning($"Invalid map count(>100), blocking...");
            P2PPackageHandlerPatch.CheckPacket(Helper.LastPacketSender, true);
            return false;
        }

        //Debug.Log("Recieved mapData: " + mapData.ToByteString());
        return true;
    }

    /*public static void OnPlayerSpawnedMethodPrefix(ref GameObject ___m_PlayerPrefab)
    {
        Debug.Log("RUNNING OnPlayerSpawned NOW!!!!!");

        foreach (var hoard in Resources.FindObjectsOfTypeAll<HoardHandler>())
        {
            if (hoard.name == "AI spawner") Helper.Hoards[0] = hoard; // Player
            if (hoard.name == "AI spawner (1)") Helper.Hoards[1] = hoard; // Bolt
            if (hoard.name == "AI spawner (2)") Helper.Hoards[2] = hoard; // Zombie
        }

        Debug.Log("Trying to find child objs!!");

        var predictionSyncCubeTest =  ___m_PlayerPrefab.transform.GetChild(7).gameObject;
        var chat =  ___m_PlayerPrefab.transform.GetChild(8).gameObject;
        var gameCanvas =  ___m_PlayerPrefab.transform.GetChild(9).gameObject;
        var damageParticleObj = ___m_PlayerPrefab.GetComponentInChildren<BlockParticle>().transform.GetChild(0)
            .GetComponent<ParticleSystem>();

        Debug.Log("All objs found");

        foreach (var hoard in Helper.Hoards)
        {
            var newCubeTest = Object.Instantiate(predictionSyncCubeTest, hoard.character.transform, 
                true);
            Object.Instantiate(chat, hoard.character.transform, false);
            Object.Instantiate(gameCanvas, hoard.character.transform, true);
            Object.Instantiate(damageParticleObj, hoard.character.GetComponentInChildren<BlockParticle>().transform, 
                true);

            Traverse.Create(hoard.character.FetchComponent<NetworkPlayer>())
                .Field("mHelpPredictionSphere")
                .SetValue(newCubeTest.transform);

            hoard.character.GetComponent<Movement>().jumpClips =
                ___m_PlayerPrefab.GetComponent<Movement>().jumpClips;
        }

        ___m_PlayerPrefab = Helper.Hoards[0].character.gameObject;
        Debug.Log("Changed player prefab!!");
    }*/

    public static void OnPlayerSpawnedMethodPostfix(MultiplayerManager __instance)
    {
        Debug.Log("#1");
        var customPlayerColor = ConfigHandler.GetEntry<Color>("CustomColor");

        foreach (var player in Object.FindObjectsOfType<NetworkPlayer>())
        {
            if (player.NetworkSpawnID != __instance.LocalPlayerIndex)
            {
                var otherCharacter = player.transform.root.gameObject;
                var otherColor = ConfigHandler.DefaultColors[player.NetworkSpawnID];

                ChangeAllCharacterColors(otherColor, otherCharacter);
            }
            else
            {
                var character = player.transform.root.gameObject;

                ChangeAllCharacterColors(!ConfigHandler.IsCustomPlayerColor
                        ? ConfigHandler.DefaultColors[player.NetworkSpawnID]
                        : customPlayerColor,
                    character);
            }
        }
    }

    public static void ChangeSpriteRendColor(Color colorWanted, GameObject character)
    {
        foreach (var spriteRenderer in character.GetComponentsInChildren<SpriteRenderer>())
        {
            spriteRenderer.color = colorWanted;
            spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = colorWanted;
        }
    }

    public static void ChangeLineRendColor(Color colorWanted, GameObject character)
    {
        foreach (var t in character.GetComponentsInChildren<LineRenderer>())
            t.sharedMaterial.color = colorWanted;
    }

    public static void ChangeParticleColor(Color colorWanted, GameObject character)
    {
        var unchangedEffects = new[]
        {
        "BlockParticle",
        "punchPartilce",
        "JumpParticle",
        "landParticle (1)",
        "footParticle",
        "footParticle (1)"
    };

        var applyParticleColor = ConfigHandler.GetEntry<bool>("CustomColorOnParticle");

        foreach (var partSys in character.GetComponentsInChildren<ParticleSystem>())
        {
            if (unchangedEffects.Contains(partSys.name) && !applyParticleColor)
                continue;

            var main = partSys.main;
            main.startColor = colorWanted;
        }
    }

    public static void ChangeWinTextColor(Color colorWanted, int playerID)
    {
        var winTexts = Traverse.Create(Object.FindObjectOfType<WinCounterUI>()).Field("mPlayerWinTexts")
            .GetValue<TextMeshProUGUI[]>();

        winTexts[playerID].color = colorWanted;
    }

    public static void ChangeAllCharacterColors(Color colorWanted, GameObject character)
    {
        var customPlayerColor = ConfigHandler.GetEntry<Color>("CustomColor");
        var isCustomPlayerColor = customPlayerColor != ConfigHandler.GetEntry<Color>("CustomColor", true);
        if (!isCustomPlayerColor) return;

        var playerID = 0;
        if (MatchmakingHandler.Instance.IsInsideLobby)
            playerID = character.GetComponent<NetworkPlayer>().NetworkSpawnID;

        ChangeLineRendColor(colorWanted, character);
        ChangeSpriteRendColor(colorWanted, character);
        ChangeParticleColor(colorWanted, character);
        ChangeWinTextColor(colorWanted, playerID);

        Traverse.Create(character.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(colorWanted);
        var playerNames = Traverse.Create(Object.FindObjectOfType<OnlinePlayerUI>())
            .Field("mPlayerTexts").GetValue<TextMeshProUGUI[]>();

        playerNames[playerID].color = colorWanted;
    }


    private static void InitGUI()
    {
        try
        {
            new GameObject("GUIHandler").AddComponent<GUIManager>();
            Debug.Log("Added GUIManager!");
        }
        catch (Exception ex)
        {
            Debug.Log("Exception on starting GUIManager: " + ex.Message);
        }
    }
}