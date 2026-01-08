using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QOL;

public class Helper
{
    // From stick.plugins.playermanager by Moncak and Kruziiloksu
    public static void JoinSpecificServer(CSteamID lobby)
    {
        Traverse.Create(MatchmakingHandler.Instance).Field("mCurrentConnectionType").SetValue(ConnectionType.Specific);
        MatchmakingHandler.Instance.JoinServer(lobby, new CallResult<LobbyEnter_t>.APIDispatchDelegate(GameManager.Instance.mMultiplayerManager.OnServerJoined));
    }

    // Returns the steamID of the specified spawnID
    public static CSteamID GetSteamID(ushort targetID) => ClientData[targetID].ClientID;

    // Returns the corresponding spawnID from the specified color
    public static ushort GetIDFromColor(string targetSpawnColor)
    {
        return targetSpawnColor.ToLower() switch
        {
            "yellow" or "y" => 0,
            "blue" or "b" => 1,
            "red" or "r" => 2,
            "green" or "g" => 3,
            _ => ushort.MaxValue
        };
    }

    // Returns the corresponding color from the specified spawnID
    public static string GetColorFromID(ushort x) => x switch { 1 => "Blue", 2 => "Red", 3 => "Green", 65535 => "All", _ => "Yellow" };

    // Returns the targeted player based on the specified spawnID
    public static NetworkPlayer GetNetworkPlayer(ushort targetID) => ClientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();

    public static string GetPlayerHp(ushort targetID) =>
        GetNetworkPlayer(targetID)
            .GetComponentInChildren<HealthHandler>()
            .health + "%";

    // Gets the steam profile name of the specified steamID
    public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

    // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
    public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

    public static Controller GetLocalController()
    {
        if (controllerHandler != null && controllerHandler.ActivePlayers != null)
        {
            foreach (Controller controller in controllerHandler.ActivePlayers)
            {
                if (controller.HasControl && !controller.IsAI())
                {
                    return controller;
                }
            }
        }
        return null;
    }

    public static Controller GetControllerFromID(ushort playerID)
    {
        try
        {
            var controller = controllerHandler.ActivePlayers[playerID];
            return controller;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get controller of player " + playerID + "! Exception: " + ex);
            return null;
        }
    }

    // Assigns some commonly accessed values as well as runs anything that needs to be everytime a lobby is joined
    public static void InitValues(ChatManager __instance, ushort playerID)
    {
        controllerHandler = GameObject.Find("GameManagement").GetComponent<ControllerHandler>();
        gameManager = GameObject.Find("GameManagement").GetComponent<GameManager>();
        Fighting fighting = Traverse.Create(controller).Field("fighting").GetValue<Fighting>();
        networkPlayer = Traverse.Create(fighting).Field("mNetworkPlayer").GetValue<NetworkPlayer>();

        MutedPlayers.Clear();

        if (playerID == GameManager.Instance.mMultiplayerManager.LocalPlayerIndex)
        {
            ClientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;

            var localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            //networkPlayer = ClientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
            LocalChat = ClientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

            WeaponSelectHandler = UnityEngine.Object.FindObjectOfType<WeaponSelectionHandler>();

            if (networkPlayer == null)
                Debug.LogError("Failed to get networkPlayer!");
            else
                Debug.Log("Assigned the localNetworkPlayer!: " + networkPlayer.NetworkSpawnID);
        }

        TMPText = Traverse.Create(__instance).Field("text").GetValue<TextMeshPro>();
        TMPText.richText = ChatCommands.CmdDict["rich"].IsEnabled;
        // Increase caret width so caret won't disappear at certain times
        Traverse.Create(__instance).Field("chatField").GetValue<TMP_InputField>().caretWidth = 3;

        var customCrownColor = ConfigHandler.GetEntry<Color>("CustomCrownColor");

        if (ConfigHandler.GetEntry<bool>("FixCrownTxt"))
        {
            var counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
            foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                crownCount.enableAutoSizing = true;
                crownCount.GetComponentInChildren<Image>().color = customCrownColor;
            }
        }

        if (ConfigHandler.GetEntry<bool>("ResizeName"))
        {
            var playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>())
                .Field("mPlayerTexts")
                .GetValue<TextMeshProUGUI[]>();

            foreach (var name in playerNames)
            {
                name.fontSizeMin = 45;
                name.fontSizeMax = 45;
                name.overflowMode = TextOverflowModes.Overflow;
                name.enableWordWrapping = false;
            }
        }

        if (customCrownColor != ConfigHandler.GetEntry<Color>("CustomCrownColor", true))
        {
            var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;

            foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true))
                sprite.color = customCrownColor;
        }

        if (GameObject.Find("RainbowHandler") == null)
        {
            GameObject rbHand = new("RainbowHandler");
            rbHand.AddComponent<RainbowManager>().enabled = false;
        }
        var rbCmd = ChatCommands.CmdDict["rainbow"];
        if (rbCmd.IsEnabled) rbCmd.Execute();

        if (_notifyUpdateCount < 3)
        {
            Debug.Log("Checking for new mod version...");
            __instance.StartCoroutine(CheckForModUpdate());
            _notifyUpdateCount++;
        }

        MapPresetHandler.RefreshMutables();
        GunPresetHandler.RefreshMutables();
        // Check if all default presets exist, if not generate them first.
        if (!MapPresetHandler.DefaultPresetsExist())
            MapPresetHandler.GenerateDefaultPresets();
        if (!GunPresetHandler.DefaultPresetsExist())
            GunPresetHandler.GenerateDefaultPresets();
    }

    public static string GetTargetStatValue(CharacterStats stats, string targetStat)
    {
        foreach (var stat in typeof(CharacterStats).GetFields())
            if (string.Equals(stat.Name, targetStat, StringComparison.InvariantCultureIgnoreCase))
                return stat.GetValue(stats).ToString();

        return "No value";
    }

    public static void SendPublicOutput(string msg)
    {
        currentOutputMsg = msg;
        if (MatchmakingHandler.IsNetworkMatch)
        {
            networkPlayer.OnTalked(msg);
        }
        else
        {
            LocalChat?.Talk(msg);
        }
    }

    public static void SendModOutput(string msg, Command.LogType logType, bool isPublic = true, bool toggleState = true)
    {
        if (isPublic || ConfigHandler.AllOutputPublic)
        {
            SendPublicOutput(msg);
            return;
        }

        var msgColor = logType switch
        {
            Command.LogType.Warning => "<color=#CC0000>",
            // Enabled => green, disabled => gray
            Command.LogType.Success => toggleState ? "<color=#006400>" : "<color=#56595C>",
            _ => ""
        };

        currentOutputMsg = msgColor + msg + "</color>";
        if (LocalChat == null) return;
        TMPText.richText = true;
        LocalChat.Talk(currentOutputMsg);
    }

    public static void ShowLoadText(string text, float time = 2f, bool wrap = true, bool rich = true)
    {
        CoroutineRunner.Run(ShowLoadTextCoroutine());

        IEnumerator ShowLoadTextCoroutine()
        {
            var errorText = Traverse.Create(LoadingScreenManager.Instance).Field("m_ErrorText").GetValue<GameObject>();
            if (!wrap) errorText.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
            if (!rich) errorText.GetComponent<TextMeshProUGUI>().richText = false;
            LoadingScreenManager.Instance.ChangeLoadingScreenText(text);
            yield return new WaitForSecondsRealtime(time);

            errorText.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
            errorText.GetComponent<TextMeshProUGUI>().richText = true;
            LoadingScreenManager.Instance.ChangeLoadingScreenText(string.Empty);
        }
    }

    public static void LoadAndExecute(string sceneName, Action<GameObject[]> loadedObjsCallback = null)
    {
        CoroutineRunner.Run(LoadCorotine());

        IEnumerator LoadCorotine()
        {
            if (LoadedScene.IsValid() && LoadedScene.isLoaded)
            {
                var unloadAsync = SceneManager.UnloadSceneAsync(LoadedScene);
                yield return new WaitUntil(() => unloadAsync.isDone);
            }

            var loadAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadAsync.isDone);
            var scene = SceneManager.GetSceneByName(sceneName);
            LoadedScene = scene;
            var rootObjs = scene.GetRootGameObjects();
            foreach (var obj in rootObjs)
            {
                obj.GetComponent<MonoBehaviour>().enabled = false;
             
                obj.gameObject.SetActive(false);
            }
            loadedObjsCallback?.Invoke(rootObjs);
        }
    }

    [HarmonyPatch(typeof(P2PPackageHandler), "GetChannelForMsgType")]
    [HarmonyReversePatch]
    private static int GetChannelForMsgType(object instance, P2PPackageHandler.MsgType msgType) => 0;

    private static void SendMessageToAllClients(object instance, byte[] data, P2PPackageHandler.MsgType type, bool ignoreServer = false,
        ulong ignoreUserID = 0, EP2PSend sendMethod = EP2PSend.k_EP2PSendReliable, int channel = 0)
    {
        var sendPacketToAllMethod = AccessTools.Method(typeof(MultiplayerManager), "SendMessageToAllClients");
        sendPacketToAllMethod.Invoke(instance, [data, type, ignoreServer, ignoreUserID, sendMethod, channel]);
    }

    public static void SendMessageToAllClients(byte[] data, P2PPackageHandler.MsgType type, bool ignoreServer = false,
        ulong ignoreUserID = 0, EP2PSend sendMethod = EP2PSend.k_EP2PSendReliable, int channel = -1)
    {
        if (ChatCommands.CmdDict["logpkg"].IsEnabled)
            Debug.Log(
                $"[QOL] Sent message to all clients: \n" +
                $"type: {type}\n" +
                $"data: {data.ToDecString()}\n" +
                $"channel: {channel}");
        SendMessageToAllClients(gameManager.mMultiplayerManager, data, type, ignoreServer, ignoreUserID, sendMethod,
                (channel != -1) ? channel : GetChannelForMsgType(P2PPackageHandler.Instance, type));
    }

    public static void SendP2PPacketToUser(CSteamID clientID, byte[] data, P2PPackageHandler.MsgType type,
        EP2PSend sendMethod = EP2PSend.k_EP2PSendReliable, int channel = -1)
    {
        if (ChatCommands.CmdDict["logpkg"].IsEnabled)
            Debug.Log(
                $"[QOL] Sent message to client: {clientID}\n" +
                $"type: {type}\n" +
                $"data: {data.ToDecString()}\n" +
                $"channel: {channel}");
        P2PPackageHandler.Instance.SendP2PPacketToUser(clientID, data, type, sendMethod,
                (channel != -1) ? channel : GetChannelForMsgType(P2PPackageHandler.Instance, type));
    }

    public static void InitMusic(GameManager __instance)
    {
        //if (MatchmakingHandler.IsNetworkMatch) return;

        var filePaths = Directory.GetFiles(Plugin.MusicPath)
            .Where(filePath => filePath.EndsWith(".ogg") || filePath.EndsWith(".wav"))
            .ToArray();

        if (filePaths.Length == 0) return;

        __instance.StartCoroutine(LoadMusicFilesAsync());

        IEnumerator LoadMusicFilesAsync()
        {
            var loadingCoroutines = new List<IEnumerator>();

            foreach (var filePath in filePaths)
            {
                loadingCoroutines.Add(ImportWav(filePath, audioClip =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var dotIndex = fileName.IndexOf('.');
                        if (dotIndex > 0 && fileName.Substring(0, dotIndex).All(char.IsDigit))
                        {
                            fileName = fileName.Substring(dotIndex + 1).TrimStart(); // Remove number prefix
                        }
                        audioClip.name = fileName;
                        MusicHandler.Instance.myMusic = MusicHandler.Instance.myMusic.AddToArray(new MusicClip { clip = audioClip });
                    }));
            }

            foreach (var coroutine in loadingCoroutines)
            {
                yield return __instance.StartCoroutine(coroutine);
            }
        }
    }

    public static IEnumerator ImportWav(string url, Action<AudioClip> callback)
    {
        url = "file:///" + url.Replace(" ", "%20");
        Debug.Log("Loading song: " + url);

        using var www = UnityWebRequest.GetAudioClip(url, AudioType.UNKNOWN);
        yield return www.Send();

        if (www.isError)
        {
            Debug.LogWarning("Audio error:" + www.error);
            yield break;
        }

        var audioClip = DownloadHandlerAudioClip.GetContent(www);
        callback(audioClip);
    }

    private static IEnumerator CheckForModUpdate()
    {
        if (!string.IsNullOrEmpty(Plugin.NewUpdateVerCode))
        {
            SendModOutput("A new mod update has been detected: <#006400>" + Plugin.NewUpdateVerCode,
                Command.LogType.Warning, false);
            yield break;
        }

        const string latestReleaseUri = "https://api.github.com/repos/Mn0ky/QOL-Mod/releases/latest";
        using var webRequest = UnityWebRequest.Get(latestReleaseUri);

        yield return webRequest.Send();

        if (webRequest.isError)
        {
            Debug.LogError(webRequest.error);
            Debug.Log("Error occured during fetch for latest qol mod version!");
            yield break;
        }

        string latestVer = JSONNode.Parse(webRequest.downloadHandler.text)["tag_name"];
        latestVer = latestVer.Remove(0, 1); // Remove the 'v', ex: v1.17.0 --> 1.17.0
        const string curVer = Plugin.VersionNumber;

        var latestMajorBuildNum = int.Parse(latestVer.Substring(2, 2)); // The '17' in 1.17.0
        var latestMinorBuildNum = int.Parse(latestVer.Substring(5, latestVer.Length == 6 ? 1 : 2));
        //Debug.Log("Current build: " + latestMajorBuildNum + " " + latestMinorBuildNum);

        var curMajorBuildNum = int.Parse(curVer.Substring(2, 2));
        var curMinorBuildNum = int.Parse(curVer.Substring(5, curVer.Length == 6 ? 1 : 2));

        if (latestVer == Plugin.VersionNumber || curMajorBuildNum > latestMajorBuildNum || curMinorBuildNum > latestMinorBuildNum)
            yield break;

        Plugin.NewUpdateVerCode = latestVer;
        SendModOutput("A new mod update has been detected: <#006400>" + Plugin.NewUpdateVerCode,
            Command.LogType.Warning, false);
    }

    // Fancy bit-manipulation of a char's ASCII values to check whether it's a vowel or not
    public static bool IsVowel(char c) => (0x208222 >> (c & 0x1f) & 1) != 0;


    public static CSteamID lobbyID; // The ID of the current lobby

    public static Controller controller; // The controller of the local user (ours)
    public static NetworkPlayer networkPlayer; // The networkPlayer of the local user (ours)
    public static ControllerHandler controllerHandler;
    public static GameManager gameManager;

    public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
    public static List<ushort> MutedPlayers = new(4);
    //public static readonly bool IsCustomPlayerColor = Plugin.ConfigCustomColor.Value != new Color(1, 1, 1);
    //public static readonly bool IsCustomName = !string.IsNullOrEmpty(Plugin.ConfigCustomName.Value);
    public static bool SongLoop;

    public static List<GameObject> SpawnedWings = [];
    public static GameObject[] HpBars = new GameObject[4];
    public static GameObject ChatField;
    public static Scene LoadedScene;
    public static Material skyBoxMat;

    // Cauculate the portion exceeding the screen ratio
    public static float ScreenWidthScaleFactor =>
        (Screen.width / (float)Screen.height) > 16f / 9f ? (Screen.width / (float)Screen.height) / (16f / 9f) : 1f;
    public static float ScreenHeightScaleFactor =>
        (Screen.width / (float)Screen.height) < 16f / 9f ? (16f / 9f) / (Screen.width / (float)Screen.height) : 1f;


    //public static readonly string[] OuchPhrases = Plugin.ConfigOuchPhrases.Value.Split(' ');
    //private static readonly bool NameResize = Plugin.ConfigNoResize.Value;
    private static int _notifyUpdateCount;
    public static IEnumerator RoutineUsed;
    public static string currentOutputMsg; // Save for mod MapEditorExtension

    public static ConnectedClientData[] ClientData;
    public static ChatManager LocalChat;
    public static WeaponSelectionHandler WeaponSelectHandler;

    public static TextMeshPro TMPText;
    public static int WinStreak = 0;

    //public static HoardHandler[] Hoards = new HoardHandler[3];

    public static bool TrustedKicker;
    public static CSteamID LastPacketSender;
    public static CSteamID LastKickPacketSender;
}