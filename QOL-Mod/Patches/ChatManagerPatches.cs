using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using LevelEditor;
using Object = UnityEngine.Object;

namespace QOL.Patches;

[HarmonyPatch]
public static class ChatManagerPatches
{
    public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony __instance
    {
        var awakeMethod = AccessTools.Method(typeof(ChatManager), "Awake");
        var awakeMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
             .GetMethod(nameof(AwakeMethodPrefix)));
        harmonyInstance.Patch(awakeMethod, prefix: awakeMethodPrefix);

        var startMethod = AccessTools.Method(typeof(ChatManager), "Start");
        var startMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
            .GetMethod(nameof(StartMethodPostfix))); // Patches Start() with prefix method
        harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);

        var updateMethod = AccessTools.Method(typeof(ChatManager), "Update");
        var updateMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(UpdateMethodPrefix)));
        harmonyInstance.Patch(updateMethod, prefix: updateMethodPrefix);

        var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
        var stopTypingMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
            .GetMethod(nameof(StopTypingMethodPostfix))); // Patches StopTyping() with postfix method
        harmonyInstance.Patch(stopTypingMethod, postfix: stopTypingMethodPostfix);

        var sendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
        var sendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
            .GetMethod(nameof(SendChatMessageMethodPrefix)));
        harmonyInstance.Patch(sendChatMessageMethod, prefix: sendChatMessageMethodPrefix);

        var replaceUnacceptableWordsMethod = AccessTools.Method(typeof(ChatManager), "ReplaceUnacceptableWords");
        var replaceUnacceptableWordsMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
            .GetMethod(nameof(ReplaceUnacceptableWordsMethodPrefix)));
        harmonyInstance.Patch(replaceUnacceptableWordsMethod, prefix: replaceUnacceptableWordsMethodPrefix);

        var talkMethod = AccessTools.Method(typeof(ChatManager), "Talk");
        var talkMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(TalkMethodPostfix)));
        harmonyInstance.Patch(talkMethod, postfix: talkMethodPostfix);
    }

    // Enable chat field in LevelEditor
    [HarmonyPatch(typeof(ChatManager), "Start")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ChatManagerStartTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        for (int i = 1; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("get_Instance"))
            {
                codes.RemoveRange(i - 1, 4); // this.chatField = GameManager.Instance.chatInputField;
                codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ChatManagerPatches), nameof(SetChatField))));
            }
            if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand.ToString().Contains("set_richText"))
            {
                codes.RemoveRange(i - 3, 4); // this.text.richText = false;
                codes.Insert(i - 3, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ChatManagerPatches), nameof(FixRichText))));
            }
        }
        return codes;
    }

    public static void SetChatField(ChatManager __instance)
    {
        try
        {
            if (GameManager.Instance == null)
            {
                // Avoiding null reference exception causing HarmonyPostfix not working (In LevelEditor)
                Traverse.Create(__instance).Field("chatField").SetValue(__instance.transform.root.GetComponentInChildren<TMP_InputField>());
            }
            else
            {
                Traverse.Create(__instance).Field("chatField").SetValue(GameManager.Instance.chatInputField);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to set chatfield: " + e);
        }
    }

    public static void FixRichText(ChatManager __instance)
    {
        var text = Traverse.Create(__instance).Field("text").GetValue<TMP_Text>();
        text.richText = ConfigHandler.GetEntry<bool>("EnableChatFieldInLevelEditor") && WorkshopStateHandler.IsPlayTestingMode;
    }

    [HarmonyPatch(typeof(ChatManager), "Start")]
    [HarmonyPostfix]
    private static void ChatManagerStartPostfix(ChatManager __instance)
    {
        if (!ConfigHandler.GetEntry<bool>("EnableChatFieldInLevelEditor")) return;

        // Cache chatfield object
        if (Helper.ChatField == null)
        {
            Helper.ChatField = Object.Instantiate(Object.FindObjectOfType<TMP_InputField>().gameObject);
            Helper.ChatField.SetActive(false);
            Object.DontDestroyOnLoad(Helper.ChatField);
            Debug.Log("Cached ChatField GameObject");
        }
    }

    static GameObject _chatField;
    [HarmonyPatch(typeof(WorkshopStateHandler), "StartPlayMode")]
    [HarmonyPrefix]
    private static void StartPlayModePrefix()
    {
        if (!ConfigHandler.GetEntry<bool>("EnableChatFieldInLevelEditor")) return;

        var canvas = ResourcesManager.Instance.CharacterObject.transform.Find("GameCanvas");
        if (_chatField == null)
        {
            _chatField = Object.Instantiate(Helper.ChatField);
        }
        _chatField.transform.SetParent(canvas, false);
        _chatField.SetActive(true);
        Debug.Log("Added ChatField GameObject to the canvas");
    }

    // Enable chat bubble in all lobbies
    public static bool AwakeMethodPrefix(ChatManager __instance)
    {
        return false;
    }

    public static void StartMethodPostfix(ChatManager __instance, ref TMP_InputField ___chatField, NetworkPlayer ___m_NetworkPlayer)
    {
        var playerID = ushort.MaxValue;

        try
        {
            playerID = ___m_NetworkPlayer.NetworkSpawnID;
        }
        catch
        {
            Helper.LocalChat = __instance;
        }
        finally
        {
            // Assigns m_NetworkPlayer value to Helper.localNetworkPlayer if networkPlayer is ours
            Helper.InitValues(__instance, playerID);
        }
        ___chatField.restoreOriginalTextOnEscape = false; // Manually clear text on escape using Update()
        SetChatFieldProportion(ConfigHandler.GetEntry<float>("ChatFieldProportion"));
    }

    public static void SetChatFieldProportion(float proportion)
    {
        var chatField = Traverse.Create(Helper.LocalChat).Field("chatField").GetValue<TMP_InputField>();
        var chatFieldRect = chatField.GetComponent<RectTransform>();

        chatFieldRect.anchorMax = new Vector2(proportion, 0.5f);
        chatFieldRect.anchorMin = new Vector2(1 - proportion, 0.5f);
    }

    public static bool UpdateMethodPrefix(ChatManager __instance)
    {
        var chatFieldInfo = AccessTools.Field(typeof(ChatManager), "chatField");
        TMP_InputField chatField = (TMP_InputField)chatFieldInfo.GetValue(__instance);

        var startTypingMethod = AccessTools.Method(typeof(ChatManager), "StartTyping");
        var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");

        // Enable local chat and commands
        if (MatchmakingHandler.IsNetworkMatch)
        {
            var m_NetworkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue<NetworkPlayer>();
            if (m_NetworkPlayer.HasLocalControl)
            {
                UpdateKeybinds();
            }
        }
        else
        {
            var controller = __instance.transform.root.gameObject.GetComponent<Controller>();
            if (controller.HasControl && !controller.isAI)
            {
                UpdateKeybinds();
            }
        }
        float disableChatIn = Traverse.Create(__instance).Field("disableChatIn").GetValue<float>();
        disableChatIn -= Time.deltaTime;
        Traverse.Create(__instance).Field("disableChatIn").SetValue(disableChatIn);

        var chatBubbleAnim = Traverse.Create(__instance).Field("chatBubbleAnim").GetValue<object>();
        if (disableChatIn < 0f && Traverse.Create(chatBubbleAnim).Field("state1").GetValue<bool>())
        {
            Traverse.Create(chatBubbleAnim).Field("state1").SetValue(false);
        }
        return false;

        void UpdateKeybinds()
        {
            if (ChatManager.isTyping)
            {
                if (EventSystem.current.currentSelectedGameObject != chatField)
                {
                    chatField.Select();
                }
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    chatField.text = string.Empty;
                    stopTypingMethod.Invoke(__instance, null);
                }
                CheckForArrowKeysAndAutoComplete(chatField);
            }
            if (Input.anyKeyDown && ChatManager.isTyping)
            {
                ScreenshakeHandler.Instance.AddShake(UnityEngine.Random.insideUnitSphere * 0.01f);
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (ChatManager.isTyping)
                {
                    stopTypingMethod.Invoke(__instance, null);
                }
                else if (!PauseManager.isPaused)
                {
                    startTypingMethod.Invoke(__instance, null);
                }
            }

            // Input command
            if (!ChatManager.isTyping && !PauseManager.isPaused)
            {
                if (Input.GetKeyDown(KeyCode.Slash))
                {
                    startTypingMethod.Invoke(__instance, null);
                    chatField.DeactivateInputField();
                    chatField.text = Command.CmdPrefix.ToString();
                    chatField.stringPosition = chatField.text.Length;
                    chatField.ActivateInputField();
                }
            }

            // Switch command
            if (ChatManager.isTyping && _canSwitchCmd)
            {
                if (!Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Tab))
                {
                    _keyHoldTime += Time.deltaTime;
                    if (Input.GetKeyDown(KeyCode.Tab) || _keyHoldTime > 0.5f)
                    {
                        HandleCommandNavigation(true);
                    }
                }
                else if (Input.GetKeyUp(KeyCode.Tab))
                {
                    _keyHoldTime = 0f;
                }
                if (Input.GetAxis("Mouse ScrollWheel") < 0)
                {
                    HandleCommandNavigation(true);
                }

                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Tab))
                {
                    _keyHoldTime += Time.deltaTime;
                    if (Input.GetKeyDown(KeyCode.Tab) || _keyHoldTime > 0.5f)
                    {
                        HandleCommandNavigation(false);
                    }
                }
                else if (Input.GetKeyUp(KeyCode.Tab))
                {
                    _keyHoldTime = 0f;
                }
                if (Input.GetAxis("Mouse ScrollWheel") > 0)
                {
                    HandleCommandNavigation(false);
                }
            }
        }

        void HandleCommandNavigation(bool forward)
        {
            var txt = chatField.text;
            if (!txt.StartsWith(Command.CmdPrefix)) return;

            var allCmds = ChatCommands.CmdNames;
            var matchedCmd = allCmds.FirstOrDefault(cmd => txt.ToLower() == cmd);

            if (matchedCmd != null)
            {
                var currentParamIndex = allCmds.FindIndex(cmd => cmd.StartsWith(matchedCmd));
                string newCmd = forward
                    ? (currentParamIndex >= 0 && currentParamIndex < allCmds.Count - 1 ? allCmds[currentParamIndex + 1] : allCmds.First())
                    : (currentParamIndex > 0 ? allCmds[currentParamIndex - 1] : allCmds.Last());

                chatField.DeactivateInputField();
                chatField.text = newCmd;
                chatField.stringPosition = chatField.text.Length;
                chatField.ActivateInputField();
            }
            else
            {
                Command cmd = null;
                var cmdName = txt.Replace(Command.CmdPrefix, "").Split(' ')[0].ToLower();

                if (ChatCommands.CmdDict.ContainsKey(cmdName))
                    cmd = ChatCommands.CmdDict[cmdName];

                if (cmd == null || cmd.AutoParams == null) return;

                var plainText = txt;
                const string rTxtFmt = "<#000000BB><u>";
                var richTextPos = plainText.IndexOf(rTxtFmt);
                if (richTextPos != -1)
                {
                    plainText = plainText.Remove(richTextPos);
                }

                var args = plainText.Replace(Command.CmdPrefix, "").Split(' ');
                if (args.Length <= 1) return;

                var currentParamIndex = args.Length - 2;
                var currentParam = args[args.Length - 1];

                var previousArgs = args.Skip(1).Take(currentParamIndex).ToArray();
                var candidates = cmd.GetAutoParamCandidates(previousArgs);

                if (candidates.Count == 0) return;

                var matchedParam = candidates.FirstOrDefault(p => p.StartsWith(currentParam, StringComparison.InvariantCultureIgnoreCase));

                if (matchedParam == null)
                {
                    matchedParam = candidates.First();
                }

                var currentParamIndexInList = candidates.IndexOf(matchedParam);
                string newParam;

                if (forward)
                {
                    newParam = currentParamIndexInList >= 0 && currentParamIndexInList < candidates.Count - 1
                        ? candidates[currentParamIndexInList + 1]
                        : candidates.First();
                }
                else
                {
                    newParam = currentParamIndexInList > 0
                        ? candidates[currentParamIndexInList - 1]
                        : candidates.Last();
                }

                chatField.DeactivateInputField();

                var lastSpaceIndex = plainText.LastIndexOf(' ');
                if (lastSpaceIndex == -1) return;

                var baseText = plainText.Substring(0, lastSpaceIndex + 1); // Include space
                chatField.text = baseText + newParam;
                chatField.stringPosition = chatField.text.Length;
                chatField.ActivateInputField();
            }
        }
    }

    public static void StopTypingMethodPostfix()
    {
        Debug.Log("ChatManagerPatches.upArrowCounter : " + _upArrowCounter);
        _upArrowCounter = 0; // When player is finished typing, reset the counter for # of up-arrow presses
    }

    public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance)
    {
        if (_backupTextList[0] != message && message.Length <= 350) SaveForUpArrow(message);

        if (message.StartsWith(Command.CmdPrefix))
        {
            FindAndRunCommand(message);
            return false;
        }

        if (!MatchmakingHandler.IsNetworkMatch)
        {
            Helper.SendPublicOutput(message);
            return false;
        }

        if (ChatCommands.CmdDict["uwu"].IsEnabled && !string.IsNullOrEmpty(message) && Helper.networkPlayer.HasLocalControl)
        {
            if (ChatCommands.CmdDict["nuky"].IsEnabled)
            {
                message = UwUify(message);
                Helper.RoutineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(Helper.RoutineUsed);
                return false;
            }

            Helper.SendPublicOutput(UwUify(message));
            return false;
        }

        if (ChatCommands.CmdDict["nuky"].IsEnabled)
        {
            if (ChatCommands.CmdDict["lowercase"].IsEnabled)
                message = message.ToLower();

            Helper.RoutineUsed = WaitCoroutine(message);
            __instance.StartCoroutine(Helper.RoutineUsed);
            return false;
        }

        if (ChatCommands.CmdDict["lowercase"].IsEnabled)
        {
            Helper.networkPlayer.OnTalked(message.ToLower());
            return false;
        }

        return true;
    }

    public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result)
    {
        if (ChatCommands.CmdDict["uncensor"].IsEnabled)
        {
            Debug.Log("skipping censorship");
            __result = message;
            return false;
        }

        Debug.Log("censoring message");
        return true;
    }

    // Method which increases duration of a chat message by set amount in config
    public static void TalkMethodPostfix(ref float ___disableChatIn)
    {
        var extraTime = ConfigHandler.GetEntry<float>("MsgDuration");
        if (extraTime > 0) ___disableChatIn += extraTime;
    }

    public static void FindAndRunCommand(string message)
    {
        Debug.Log("User is trying to run command: " + message);
        var args = message.TrimStart(Command.CmdPrefix).Trim().Split(' '); // Sanitising input

        var targetCommandTyped = args[0];

        if (!ChatCommands.CmdDict.ContainsKey(targetCommandTyped)) // If command is not found
        {
            Helper.SendModOutput("Specified command or it's alias not found. See /help for full list of commands.",
                Command.LogType.Warning, false);
            return;
        }

        ChatCommands.CmdDict[targetCommandTyped].Execute(args.Skip(1).ToArray()); // Skip first element (original cmd)
    }

    // Checks if the up-arrow or down-arrow key is pressed, if so then
    // set the chatField.text to whichever message the user stops on
    public static void CheckForArrowKeysAndAutoComplete(TMP_InputField chatField)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && _upArrowCounter < _backupTextList.Count)
        {
            chatField.text = _backupTextList[_upArrowCounter];
            _upArrowCounter++;

            chatField.DeactivateInputField(); // Necessary to properly update carat pos
            chatField.stringPosition = chatField.text.Length;
            chatField.ActivateInputField();

            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && _upArrowCounter > 0)
        {
            _upArrowCounter--;
            chatField.text = _backupTextList[_upArrowCounter];

            chatField.DeactivateInputField(); // Necessary to properly update carat pos
            chatField.stringPosition = chatField.text.Length;
            chatField.ActivateInputField();

            return;
        }

        const string rTxtFmt = "<#000000BB><u>";
        var txt = chatField.text;
        var txtLen = txt.Length;
        var parsedTxt = chatField.textComponent.GetParsedText();
        // Remove last char of non-richtext str since a random space is added from GetParsedText() 
        parsedTxt = parsedTxt.Remove(parsedTxt.Length - 1);

        if (txtLen > 0 && txt[0] == Command.CmdPrefix)
        {
            // Credit for this easy way of getting the closest matching string from a list
            //https://forum.unity.com/threads/auto-complete-text-field.142181/#post-1741569
            var cmdsMatched = ChatCommands.CmdNames.FindAll(
                word => word.StartsWith(parsedTxt, StringComparison.InvariantCultureIgnoreCase));

            if (cmdsMatched.Count > 0)
            {
                var cmdMatch = cmdsMatched[0];
                var cmdMatchLen = cmdMatch.Length;

                if (chatField.richText && parsedTxt.Length == cmdMatchLen)
                {
                    // Check if cmd has been manually fully typed, if so remove its rich text
                    var richTxtStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                    if (richTxtStartPos != -1 && txt.Substring(0, richTxtStartPos) == cmdMatch)
                    {
                        chatField.text = cmdMatch;
                        return;
                    }

                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        chatField.DeactivateInputField(); // Necessary to properly update carat pos
                        chatField.text = cmdMatch;
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                        _canSwitchCmd = txt == parsedTxt; // Prevent switching cmd when have rich text(to complete current cmd)
                    }

                    return;
                }

                chatField.richText = true;
                chatField.text += txtLen <= cmdMatchLen ? rTxtFmt + cmdMatch.Substring(txtLen) : Command.CmdPrefix;
            }
            else if (chatField.richText)
            { // Already a cmd typed
                var cmdAndParam = parsedTxt.Split(' ');
                var cmdDetectedIndex = ChatCommands.CmdNames.IndexOf(cmdAndParam[0]);

                if (cmdDetectedIndex == -1)
                {
                    var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                    if (effectStartPos == -1)
                        // This will only occur if a cmd is fully typed and then more chars are added after
                        return;

                    chatField.text = txt.Remove(effectStartPos);
                    return;
                }

                var cmdMatch = ChatCommands.CmdNames[cmdDetectedIndex];
                var targetCmd = ChatCommands.CmdDict[cmdMatch.Substring(1)];
                var targetCmdParams = targetCmd.AutoParams;

                if (targetCmdParams == null) return; // Cmd may not take any params
                if (cmdAndParam.Length <= 1 || cmdAndParam[0].Length != cmdMatch.Length) return;

                // Focusing on auto-completing the parameter now
                var paramTxt = cmdAndParam[cmdAndParam.Length - 1];
                var paramTxtLen = paramTxt.Length;

                // TODO: Implement auto-completing multiple parameters
                var currentParamIndex = cmdAndParam.Length - 2; // skip [0] (cmd)

                if (targetCmdParams is List<string> simpleAutoParams)
                {
                    //Debug.Log("paramTxt: \"" + paramTxt + "\"");
                    var paramsMatched = simpleAutoParams.FindAll(
                    word => word.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));

                    // Len check is band-aid so spaces don't break it, this will affect dev on nest parameters if it happens
                    if (paramsMatched.Count > 0 && cmdAndParam.Length < 3)
                    {
                        var paramMatch = paramsMatched[0];
                        var paramMatchLen = paramMatch.Length;

                        if (paramTxtLen == paramMatchLen)
                        {
                            var paramRichTxtStartPos = paramTxt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                            if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == paramMatch)
                            {
                                chatField.text = chatField.text.Remove(txtLen - paramMatchLen - rTxtFmt.Length + 1, rTxtFmt.Length);
                                return;
                            }

                            if (Input.GetKeyDown(KeyCode.Tab))
                            {   // Auto-completes the suggested parameter. Input field is made immutable so str pos is set correctly
                                chatField.DeactivateInputField();

                                if (ReferenceEquals(simpleAutoParams, PlayerUtils.PlayerColorsParams))
                                {   // Change player color to 1 letter variant to encourage shorthand alternative
                                    // Multiply by 2 to get correct shorthand index for color
                                    var colorIndex = Helper.GetIDFromColor(paramMatch) * 2;
                                    paramMatch = PlayerUtils.PlayerColorsParams[colorIndex];
                                }

                                // string.Remove() so we don't rely on the update loop to remove the rich txt leftovers
                                if (txtLen - paramMatchLen - rTxtFmt.Length > 0) // Actually fixs the bug: the second switch don't work
                                {
                                    chatField.text = txt.Remove(txtLen - paramMatchLen - rTxtFmt.Length) + paramMatch;
                                }
                                chatField.stringPosition = chatField.text.Length;
                                chatField.ActivateInputField();
                                _canSwitchCmd = txt == parsedTxt;
                            }

                            return;
                        }

                        chatField.text += rTxtFmt + paramMatch.Substring(paramTxtLen);
                        chatField.richText = true;
                    }
                    else if (chatField.richText) // ~TODO: Implement support for rich text as argument input~ --Why?
                    {
                        var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                        if (effectStartPos == -1) return;

                        chatField.text = txt.Remove(effectStartPos);
                    }
                }

                // Implement auto-completing multiple parameters
                else if (targetCmdParams is List<List<string>> AutoParamsByIndex)
                {
                    var previousArgs = cmdAndParam.Skip(1).Take(cmdAndParam.Length - 2).ToArray();
                    var candidates = targetCmd.GetAutoParamCandidates(previousArgs);

                    if (candidates != null && candidates.Count > 0)
                    {
                        var match = candidates.FirstOrDefault(p => p.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));

                        if (match != null)
                        {
                            var matchLen = match.Length;

                            if (paramTxtLen == matchLen)
                            {
                                var paramRichTxtStartPos = paramTxt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                                if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == match)
                                {
                                    chatField.text = chatField.text.Remove(txtLen - matchLen - rTxtFmt.Length + 1, rTxtFmt.Length);
                                    return;
                                }

                                if (Input.GetKeyDown(KeyCode.Tab))
                                {
                                    chatField.DeactivateInputField();

                                    var currentIndexInList = candidates.IndexOf(match);
                                    var newParam = candidates[currentIndexInList];

                                    var lastSpaceIndex = parsedTxt.LastIndexOf(' ');
                                    if (lastSpaceIndex == -1) return;

                                    var baseText = parsedTxt.Substring(0, lastSpaceIndex + 1);
                                    chatField.text = baseText + newParam;

                                    chatField.stringPosition = chatField.text.Length;
                                    chatField.ActivateInputField();
                                    _canSwitchCmd = txt == parsedTxt;
                                }
                                return;
                            }

                            chatField.richText = true;
                            chatField.text += rTxtFmt + match.Substring(paramTxtLen);
                        }
                        else if (chatField.richText)
                        {
                            RemoveRichTextAndTryRematch(chatField, txt, targetCmd, previousArgs, paramTxt);
                        }
                    }
                    else if (chatField.richText)
                    {
                        RemoveRichTextAndTryRematch(chatField, txt, targetCmd, previousArgs, paramTxt);
                    }
                }

                else if (targetCmdParams is Dictionary<string, object> AutoParamsTree)
                {
                    var previousArgs = cmdAndParam.Skip(1).Take(cmdAndParam.Length - 2).ToArray();
                    var candidates = targetCmd.GetAutoParamCandidates(previousArgs);

                    if (candidates != null && candidates.Count > 0)
                    {
                        var match = candidates.FirstOrDefault(p => p.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));

                        if (match != null)
                        {
                            var matchLen = match.Length;

                            if (paramTxtLen == matchLen)
                            {
                                var paramRichTxtStartPos = paramTxt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                                if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == match)
                                {
                                    chatField.text = chatField.text.Remove(txtLen - matchLen - rTxtFmt.Length + 1, 14);
                                    return;
                                }

                                if (Input.GetKeyDown(KeyCode.Tab))
                                {
                                    chatField.DeactivateInputField();

                                    var currentIndexInList = candidates.IndexOf(match);
                                    var newParam = candidates[currentIndexInList];

                                    var lastSpaceIndex = parsedTxt.LastIndexOf(' ');
                                    if (lastSpaceIndex == -1) return;

                                    var baseText = parsedTxt.Substring(0, lastSpaceIndex + 1);
                                    chatField.text = baseText + newParam;

                                    chatField.stringPosition = chatField.text.Length;
                                    chatField.ActivateInputField();
                                    _canSwitchCmd = txt == parsedTxt;
                                }
                                return;
                            }

                            chatField.richText = true;
                            chatField.text += rTxtFmt + match.Substring(paramTxtLen);
                        }
                        else if (chatField.richText)
                        {
                            RemoveRichTextAndTryRematch(chatField, txt, targetCmd, previousArgs, paramTxt);
                        }
                    }
                    else if (chatField.richText)
                    {
                        RemoveRichTextAndTryRematch(chatField, txt, targetCmd, previousArgs, paramTxt);
                    }
                }
            }
        }
        else if (chatField.richText)
        {
            var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
            if (effectStartPos == -1)
            {
                // Occurs when a cmd is sent, richtext needs to be reset
                chatField.richText = false;
                return;
            }
            chatField.text = txt.Remove(effectStartPos);
            chatField.richText = false;
        }
    }

    private static void RemoveRichTextAndTryRematch(TMP_InputField chatField, string txt, Command targetCmd, string[] previousArgs, string currentParam)
    {
        const string rTxtFmt = "<#000000BB><u>";
        var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
        if (effectStartPos == -1) return;

        var plainText = txt.Remove(effectStartPos);
        chatField.text = plainText;
        chatField.richText = false;

        var currentArgs = plainText.Replace(Command.CmdPrefix, "").Split(' ');
        if (currentArgs.Length > 1)
        {
            currentParam = currentArgs.Last();
            previousArgs = currentArgs.Skip(1).Take(currentArgs.Length - 2).ToArray();
            var newCandidates = targetCmd.GetAutoParamCandidates(previousArgs);

            if (newCandidates != null && newCandidates.Count > 0)
            {
                var newMatch = newCandidates.FirstOrDefault(p => p.StartsWith(currentParam, StringComparison.InvariantCultureIgnoreCase));
                if (newMatch != null)
                {
                    chatField.richText = true;
                    chatField.text += rTxtFmt + newMatch.Substring(currentParam.Length);
                }
            }
        }
    }

    // Checks if the message should be inserted then inserts it into the 0th index of backup list
    private static void SaveForUpArrow(string backupThisText)
    {
        if (_backupTextList.Count <= 20)
        {
            _backupTextList.Insert(0, backupThisText);
            return;
        }

        _backupTextList.RemoveAt(19);
        _backupTextList.Insert(0, backupThisText);
    }

    private static IEnumerator WaitCoroutine(string msg)
    {
        var msgParts = msg.Split(' ');

        foreach (var text in msgParts)
        {
            Helper.SendPublicOutput(text);
            yield return new WaitForSeconds(0.45f);
        }
    }

    // UwUifies a message if possible, not perfect
    public static string UwUify(string targetText)
    {
        var i = 0;
        var newMessage = new StringBuilder(targetText);
        while (i < newMessage.Length)
        {
            if (!char.IsLetter(newMessage[i]))
            {
                i++;
                continue;
            }

            var c = char.ToLower(newMessage[i]);
            var nextC = i < newMessage.Length - 1 ? char.ToLower(newMessage[i + 1]) : '\0';

            switch (c)
            {
                case 'r' or 'l':
                    newMessage[i] = char.IsUpper(newMessage[i]) ? 'W' : 'w';
                    break;
                case 't' when nextC == 'h':
                    newMessage[i] = char.IsUpper(newMessage[i]) ? 'D' : 'd';
                    newMessage.Remove(i + 1, 1);
                    break;
                case 'n' when nextC != ' ' && nextC != 'g' && nextC != 't' && nextC != 'd':
                    newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'Y' : 'y');
                    break;
                default:
                    if (Helper.IsVowel(c) && nextC == 't')
                        newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'W' : 'w');
                    break;
            }
            i++;
        }

        return newMessage.ToString();
    }

    private static bool _canSwitchCmd = true;

    private static float _keyHoldTime = 0f;

    private static int _upArrowCounter; // Holds how many times the up-arrow key is pressed while typing
                                        //private static bool _startedTypingParam;

    // List to contain previous messages sent by us (up to 20)
    private static List<string> _backupTextList = new(21)
    {
        "" // has an empty string so that the list isn't null when attempting to perform on it
    };
}