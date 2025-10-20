using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace QOL
{

    public static class ChatCommands
    {
        private static readonly List<Command> Cmds = new()
        {
            //new Command("socket", SocketCmd, 0, true),
            //new Command("bulletcolor", BulletColorCmd, 1, true, new List<string>(3) {"all", }),
            new Command("adv", AdvCmd, 0, false).SetAlwaysPublic(),
            new Command("alias", AliasCmd, 1, true, CmdNames),
            new Command("config", ConfigCmd, 1, true, ConfigHandler.GetConfigKeys().ToList()),
            new Command("deathmsg", DeathMsgCmd, 0, false).MarkAsToggle(),
            new Command("dm", DmCmd, 1, false, PlayerUtils.PlayerColorsParams),
            new Command("fov", FovCmd, 1, true),
            new Command("fps", FpsCmd, 1, true),
            new Command("friend", FriendCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("gg", GgCmd, 0, true).MarkAsToggle(),
            new Command("help", HelpCmd, 0, true),
            new Command("hp", HpCmd, 0, false, PlayerUtils.PlayerColorsParams).SetAlwaysPublic(),
            new Command("id", IdCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("invite", InviteCmd, 0, true),
            new Command("lobhp", LobHpCmd, 0, false).SetAlwaysPublic(),
            new Command("lobregen", LobRegenCmd, 0, false).SetAlwaysPublic(),
            new Command("logprivate", LogPrivateCmd, 1, true, CmdNames),
            new Command("logpublic", LogPublicCmd, 1, true, CmdNames),
            new Command("lowercase", LowercaseCmd, 0, true).MarkAsToggle(),
            new Command("nuky", NukyCmd, 0, true).MarkAsToggle(),
            new Command("maps", MapsCmd, 1, true, MapPresetHandler.MapPresetNames),
            new Command("mute", MuteCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("music", MusicCmd, 1, true, new List<string>(3) { "loop", "play", "skip" }).MarkAsToggle(),
            new Command("ouchmsg", OuchCmd, 0, true).MarkAsToggle(),
            new Command("ping", PingCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("private", PrivateCmd, 0, true),
            new Command("profile", ProfileCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("public", PublicCmd, 0, true),
        new Command("pumpkin", PumpkinCmd, 0, true).MarkAsToggle(),
            new Command("rainbow", RainbowCmd, 0, true).MarkAsToggle(),
            new Command("resolution", ResolutionCmd, 2, true),
            new Command("rich", RichCmd, 0, true).MarkAsToggle(),
            new Command("shrug", ShrugCmd, 0, false).SetAlwaysPublic(),
            new Command("stat", StatCmd, 1, true),
            new Command("suicide", SuicideCmd, 0, false),
            new Command("translate", TranslateCmd, 0, true).MarkAsToggle(),
            new Command("uncensor", UncensorCmd, 0, true).MarkAsToggle(),
            new Command("uwu", UwuCmd, 0, true).MarkAsToggle(),
            new Command("ver", VerCmd, 0, true),
            new Command("weapons", WeaponsCmd, 1, true, GunPresetHandler.GunPresetNames),
            new Command("winnerhp", WinnerHpCmd, 0, false).MarkAsToggle(),
            new Command("winstreak", WinstreakCmd, 0, true).MarkAsToggle(),

            // Cheat cmds below
            new Command("afk", AfkCmd, 0, true).MarkAsToggle(), // TODO: Assign AI to player and auto turn off when anyKeyDown
            new Command("pkg", PkgCmd, 0, true, PlayerUtils.PlayerColorsParams),
            new Command("bullethell", BulletHellCmd, 0, true, PlayerUtils.PlayerColorsParamsWithAll),
            new Command("bulletring", BulletRingCmd, 0, true),
            new Command("execute", ExecuteCmd, 2, true, PlayerUtils.PlayerColorsParams),
            /* TODO: Implement multiple auto-completion
            new List<List<string>>
            {
                PlayerUtils.PlayerColorsParams,
                CmdNames,
                new List<string> { } // Parameters of the command
            }),*/
            new Command("god", GodCmd, 0, true).MarkAsToggle(),
            new Command("fullauto", FullAutoCmd, 0, true).MarkAsToggle(),
            new Command("norecoil", NoRecoilCmd, 0, true).MarkAsToggle(),
            new Command("infiniteammo", InfiniteAmmoCmd, 0, true).MarkAsToggle(),
            new Command("fastfire", FastFireCmd, 0, true).MarkAsToggle(),
            new Command("fastpunch", FastPunchCmd, 0, true).MarkAsToggle(),
            new Command("fly", FlyCmd, 0, true).MarkAsToggle(),
            new Command("gun", GunCmd, 0, true),
            new Command("kick", KickCmd, 1, true, PlayerUtils.PlayerColorsParams),
            new Command("kill", KillCmd, 0, true, PlayerUtils.PlayerColorsParams),
            new Command("revive", ReviveCmd, 0, true),
            new Command("sudo", SudoCmd, 2, true, PlayerUtils.PlayerColorsParamsWithAll),
            new Command("summon", SummonCmd, 1, true, new List<string>(3) { "player", "bolt", "zombie" }),
            new Command("switchweapon",SwitchWeaponCmd, 0, true).MarkAsToggle(),
            new Command("win", WinCmd, 0, true, PlayerUtils.PlayerColorsParams),

        };

        public static readonly Dictionary<string, Command> CmdDict = Cmds.ToDictionary(cmd => cmd.Name.Substring(1),
            cmd => cmd,
            StringComparer.InvariantCultureIgnoreCase);

        public static readonly List<string> CmdNames = Cmds.Select(cmd => cmd.Name).ToList();

        public static void InitializeCmds()
        {
            if (File.Exists(Plugin.CmdVisibilityStatesPath))
                LoadCmdVisibilityStates();

            if (File.Exists(Plugin.CmdAliasesPath))
                LoadCmdAliases();

            CmdNames.Sort();

            // Reflection hackery so that auto-params for the alias, log, maps, weapons cmds work
            const string autoParamsBackingField = $"<{nameof(Command.AutoParams)}>k__BackingField";
            Traverse.Create(CmdDict["alias"]).Field(autoParamsBackingField).SetValue(CmdNames);
            Traverse.Create(CmdDict["logprivate"]).Field(autoParamsBackingField).SetValue(CmdNames);
            Traverse.Create(CmdDict["logpublic"]).Field(autoParamsBackingField).SetValue(CmdNames);
            Traverse.Create(CmdDict["maps"]).Field(autoParamsBackingField).SetValue(MapPresetHandler.MapPresetNames);
            Traverse.Create(CmdDict["weapons"]).Field(autoParamsBackingField).SetValue(GunPresetHandler.GunPresetNames);

            // On-startup options need to map their values to respective cmds
            CmdDict["gg"].IsEnabled = ConfigHandler.GetEntry<bool>("ggstartup");
            CmdDict["uncensor"].IsEnabled = ConfigHandler.GetEntry<bool>("uncensorstartup");
            CmdDict["rich"].IsEnabled = ConfigHandler.GetEntry<bool>("richtextstartup");
            CmdDict["translate"].IsEnabled = ConfigHandler.GetEntry<bool>("translatestartup");
            CmdDict["winnerhp"].IsEnabled = ConfigHandler.GetEntry<bool>("winnerhpstartup");
            CmdDict["winstreak"].IsEnabled = ConfigHandler.GetEntry<bool>("winstreakstartup");
            CmdDict["rainbow"].IsEnabled = ConfigHandler.GetEntry<bool>("rainbowstartup");
        }

        private static void LoadCmdAliases()
        {
            try
            {
                Debug.Log("Setting saved aliases of cmds");

                foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdAliasesPath)))
                {
                    foreach (var alias in pair.Value.AsArray)
                    {
                        var cmdName = pair.Key;
                        if (!CmdDict.ContainsKey(cmdName))
                        {
                            Debug.LogWarning("Command: " + cmdName + " DNE!! Assuming it no longer exists and skipping...");
                            continue;
                        }

                        var cmd = CmdDict[cmdName];
                        var aliasStr = ((string)alias.Value).Substring(1); // substring so no prefix
                        cmd.Aliases.Add(Command.CmdPrefix + aliasStr);
                    }
                }

                Debug.Log("Adding aliases of cmds to cmd dict and list");

                foreach (var cmd in Cmds)
                {
                    CmdNames.AddRange(cmd.Aliases);
                    foreach (var alias in cmd.Aliases)
                        CmdDict[alias.Substring(1)] = cmd;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to change cmd alias, something went wrong: " + e);
                Debug.Log("Resetting cmd alias json to prevent corruption...");
                SaveCmdAliases();
            }
        }

        private static void SaveCmdAliases()
        {
            var cmdAliasesJson = new JSONObject();

            foreach (var cmd in Cmds)
            {
                var aliasHolder = new JSONArray();

                foreach (var alias in cmd.Aliases)
                    aliasHolder.Add(alias);

                cmdAliasesJson.Add(cmd.Name.Substring(1), aliasHolder);
            }

            File.WriteAllText(Plugin.CmdAliasesPath, cmdAliasesJson.ToString());
        }

        private static void LoadCmdVisibilityStates()
        {
            try
            {
                foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdVisibilityStatesPath)))
                {
                    var cmdName = pair.Key;
                    if (!CmdDict.ContainsKey(cmdName))
                    {
                        Debug.LogWarning("Command: " + cmdName + " DNE!! Assuming it no longer exists and skipping...");
                        continue;
                    }

                    Debug.Log("Setting saved visibility of cmd: " + cmdName);
                    CmdDict[cmdName].IsPublic = pair.Value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to change cmd state, something went wrong: " + e);
                Debug.Log("Resetting cmd visibility states json to prevent corruption...");
                SaveCmdVisibilityStates();
            }
        }

        private static void SaveCmdVisibilityStates()
        {
            var cmdStatesJson = new JSONObject();

            foreach (var cmd in Cmds)
                cmdStatesJson.Add(cmd.Name.Remove(0, 1), cmd.IsPublic);

            File.WriteAllText(Plugin.CmdVisibilityStatesPath, cmdStatesJson.ToString());
        }

        // ****************************************************************************************************
        //                                    All chat command methods below                                      
        // ****************************************************************************************************

        // private static void SocketCmd(string[] args, Command cmd)
        // {
        //     Debug.Log("Trying to connect to socket server!!!");
        //     var multiplayerStuff = Object.FindObjectOfType<MatchmakingHandler>().gameObject;
        //     
        //     if (!multiplayerStuff.GetComponent<MatchMakingHandlerSockets>())
        //         multiplayerStuff.AddComponent<MatchMakingHandlerSockets>().JoinServer();
        // }

        // Outputs player-specified msg from config to chat, blank by default
        private static void AdvCmd(string[] args, Command cmd)
            => cmd.SetOutputMsg(ConfigHandler.GetEntry<string>("AdvertiseMsg"));

        private static void AliasCmd(string[] args, Command cmd)
        {
            var resetAlias = args.Length == 1; // Should be true even if cmd has a space char after it 
            var targetCmdName = args[0].Replace("\"", "").Replace(Command.CmdPrefix, "").ToLower();
            Command targetCmd = null;

            if (CmdDict.ContainsKey(targetCmdName))
                targetCmd = CmdDict[targetCmdName];

            if (targetCmd == null)
            {
                cmd.SetOutputMsg("Specified command or alias not found.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            if (resetAlias)
            {
                cmd.SetOutputMsg("Removed aliases for " + targetCmd.Name + ".");

                foreach (var alias in targetCmd.Aliases)
                {
                    CmdDict.Remove(alias);
                    CmdNames.Remove(alias);
                }

                targetCmd.Aliases.Clear();
                CmdNames.Sort();
                SaveCmdAliases();
                return;
            }

            var newAlias = Command.CmdPrefix + args[1].Replace("\"", "").Replace(Command.CmdPrefix, "").ToLower();

            if (CmdNames.Contains(newAlias))
            {
                if (cmd.Name == newAlias)
                {
                    cmd.SetOutputMsg("Invalid alias: already exists as name of a command.");
                    cmd.SetLogType(Command.LogType.Warning);
                    return;
                }

                cmd.SetOutputMsg("Invalid alias: already exists.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            CmdDict[newAlias.Substring(1)] = targetCmd;
            targetCmd.Aliases.Add(newAlias);
            CmdNames.Add(newAlias);

            cmd.SetOutputMsg("Added alias " + newAlias + " for " + targetCmd.Name + ".");
            CmdNames.Sort();
            SaveCmdAliases();
        }

        private static void ConfigCmd(string[] args, Command cmd)
        {
            var entryKey = args[0].Replace('"', "").ToLower(); // Sanitize

            var newEntryValue = args.Length switch
            {
                > 2 => string.Join(" ", args, 1, args.Length - 1).Replace('"', ""),
                > 1 => args[1].Replace('"', ""),
                _ => ""
            };

            if (!ConfigHandler.EntryExists(entryKey))
            {
                cmd.SetOutputMsg("Invalid key. Try fixing any spelling mistakes.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(newEntryValue))
            {
                ConfigHandler.ResetEntry(entryKey);
                cmd.SetOutputMsg("Config option has been reset to default.");
                return;
            }

            ConfigHandler.ModifyEntry(entryKey, newEntryValue);
            cmd.SetOutputMsg("Config option has been updated.");
        }

        private static void DeathMsgCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled AutoDeathMsg.");
        }

        private static void DmCmd(string[] args, Command cmd)
        {
            var targetID = Helper.GetIDFromColor(args[0]);
            var targetSteamID = Helper.GetSteamID(targetID);
            var msg = string.Join(" ", args, 1, args.Length - 1);
            var msgBytes = Encoding.UTF8.GetBytes(msg);

            var channel = Helper.localNetworkPlayer.NetworkSpawnID switch
            {
                0 => 3, // Yellow
                1 => 5, // Red
                2 => 7, // Green
                3 => 9, // Blue
                _ => throw new ArgumentOutOfRangeException()
            };

            P2PPackageHandler.Instance.SendP2PPacketToUser(targetSteamID,
                msgBytes,
                P2PPackageHandler.MsgType.PlayerTalked,
                EP2PSend.k_EP2PSendReliable,
                channel);
        }

        private static void FovCmd(string[] args, Command cmd) // TODO: Do tryparse instead to provide better error handling
        {
            var success = int.TryParse(args[0], out var newFov);
            if (!success)
            {
                cmd.SetOutputMsg("Error parsing FOV value.");
                cmd.SetLogType(Command.LogType.Warning);
            }


            Camera.main!.fieldOfView = newFov;
            cmd.SetOutputMsg("Set FOV to: " + newFov);
        }

        private static void FpsCmd(string[] args, Command cmd)
        {
            var success = int.TryParse(args[0], out var targetFPS);
            if (!success)
            {
                cmd.SetOutputMsg("Error parsing FPS value.");
                cmd.SetLogType(Command.LogType.Warning);
            }

            if (targetFPS < 60)
            {
                cmd.SetOutputMsg("FPS cannot be set below 60.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            Application.targetFrameRate = targetFPS;
            cmd.SetOutputMsg("Target framerate is now: " + targetFPS);
        }

        private static void FriendCmd(string[] args, Command cmd)
        {
            var steamID = Helper.GetSteamID(Helper.GetIDFromColor(args[0]));
            SteamFriends.ActivateGameOverlayToUser("friendadd", steamID);
        }

        // Enables or disables automatic "gg" upon death
        private static void GgCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled AutoGG.");
        }

        private static void WeaponsCmd(string[] args, Command cmd)
        {
            var arg = args[0];
            switch (arg)
            {
                case "remove" or "save" when args.Length < 2:
                    cmd.SetLogType(Command.LogType.Warning);
                    cmd.SetOutputMsg("No preset name given, please specify one.");
                    return;
                case "save":
                    {
                        var presetName = args[1].ToLower();
                        if (GunPresetHandler.GunPresetNames.Contains(presetName))
                        {
                            cmd.SetLogType(Command.LogType.Warning);
                            cmd.SetOutputMsg("Preset with specified name already exists.");
                            return;
                        }

                        var activeWeapons = GunPresetHandler.GetAllActiveWeapons();
                        var presetToSave = new SaveableGunPreset(activeWeapons, presetName);
                        GunPresetHandler.AddNewPreset(presetToSave);

                        cmd.SetOutputMsg("Saved preset: \"" + presetName + "\".");
                        return;
                    }
                case "remove":
                    {
                        var presetName = args[1];
                        var presetWantedIndex = GunPresetHandler.FindIndexOfPreset(presetName);

                        if (presetWantedIndex == -1)
                        {
                            cmd.SetLogType(Command.LogType.Warning);
                            cmd.SetOutputMsg("Specified preset not found.");
                            return;
                        }

                        GunPresetHandler.DeletePreset(presetWantedIndex, presetName);
                        cmd.SetOutputMsg("Removed preset: \"" + presetName + "\".");
                        return;
                    }
            }

            // Must want to load preset instead
            var presetWanted = GunPresetHandler.FindPreset(arg);
            if (presetWanted is null)
            {
                cmd.SetLogType(Command.LogType.Warning);
                cmd.SetOutputMsg("Specified preset not found.");
                return;
            }

            Debug.Log("Trying to load a weapon preset: " + presetWanted.PresetName);
            GunPresetHandler.LoadPreset(presetWanted);
            cmd.SetOutputMsg("Enabled preset: \"" + arg + "\".");
        }

        // Opens up the steam overlay to the GitHub readme, specifically the Chat Commands section
        private static void HelpCmd(string[] args, Command cmd)
            => SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");

        // Outputs HP of targeted color to chat
        private static void HpCmd(string[] args, Command cmd)
        {
            if (args.Length == 0)
            {
                cmd.SetOutputMsg("My HP: " + Helper.GetPlayerHp(Helper.localNetworkPlayer.NetworkSpawnID));
                return;
            }

            // Assuming user wants another player's hp
            var targetID = Helper.GetIDFromColor(args[0]);

            if (!PlayerUtils.IsPlayerInLobby(targetID))
            {
                cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " is not in the lobby.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " HP: " + Helper.GetPlayerHp(targetID));
        }

        // Outputs the specified player's SteamID
        private static void IdCmd(string[] args, Command cmd)
        {
            var targetColor = Helper.GetIDFromColor(args[0]);
            GUIUtility.systemCopyBuffer = Helper.GetSteamID(targetColor).m_SteamID.ToString();

            cmd.SetOutputMsg(Helper.GetColorFromID(targetColor) + "'s steamID copied to clipboard!");
        }

        // Builds a "join game" link (same one you'd find on a steam profile) for the lobby and copies it to clipboard
        private static void InviteCmd(string[] args, Command cmd)
        {
            GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
            cmd.SetOutputMsg("Join link copied to clipboard!");
        }

        // Outputs the HP setting for the lobby to chat
        private static void LobHpCmd(string[] args, Command cmd)
            => cmd.SetOutputMsg("Lobby HP: " + OptionsHolder.HP);

        // Outputs whether regen is enabled (true) or disabled (false) for the lobby to chat
        private static void LobRegenCmd(string[] args, Command cmd)
            => cmd.SetOutputMsg("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen));

        private static void LogPrivateCmd(string[] args, Command cmd)
        {
            var targetCmdName = args[0].Replace("\"", "").Replace("/", "")
                .TrimStart(Command.CmdPrefix)
                .ToLower(); // Even though CmdDict is case-insensitive we need to compare it to "all"

            if (targetCmdName == "all")
            {
                cmd.SetOutputMsg("Toggled private logging for all applicable commands.");
                SaveCmdVisibilityStates();
                return;
            }

            if (!CmdDict.ContainsKey(targetCmdName))
            {
                Debug.Log("targetCmdName: " + targetCmdName);
                cmd.SetOutputMsg("Specified command not found.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var targetCmd = CmdDict[targetCmdName];

            if (targetCmd.AlwaysPublic)
            {
                cmd.SetOutputMsg(targetCmd.Name + " is restricted to public logging only.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            targetCmd.IsPublic = false;
            cmd.SetOutputMsg("Toggled private logging for " + targetCmd.Name + ".");
            SaveCmdVisibilityStates();
        }

        private static void LogPublicCmd(string[] args, Command cmd)
        {
            var targetCmdName = args[0].Replace("\"", "").Replace("/", "")
                .TrimStart(Command.CmdPrefix)
                .ToLower();

            if (targetCmdName == "all")
            {
                cmd.SetOutputMsg("Toggled public logging for all applicable commands.");
                ConfigHandler.AllOutputPublic = true;
                SaveCmdVisibilityStates();
                return;
            }

            if (!CmdDict.ContainsKey(targetCmdName))
            {
                cmd.SetOutputMsg("Specified command not found.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var targetCmd = CmdDict[targetCmdName];

            if (targetCmd.AlwaysPrivate)
            {
                cmd.SetOutputMsg(targetCmd.Name + " is restricted to private logging only.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            targetCmd.IsPublic = true;
            cmd.SetOutputMsg("Toggled public logging for " + targetCmd.Name + ".");
            SaveCmdVisibilityStates();
        }

        // Enables/Disables chat messages always being sent in lowercase
        private static void LowercaseCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled LowercaseOnly.");
        }

        // Enables/disables Nuky chat mode
        private static void NukyCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            if (Helper.RoutineUsed != null) Helper.LocalChat.StopCoroutine(Helper.RoutineUsed);
            cmd.SetOutputMsg("Toggled NukyChat.");
        }

        // Saves/Removes/Loads map presets
        private static void MapsCmd(string[] args, Command cmd)
        {
            var arg = args[0];
            switch (arg)
            {
                case "remove" or "save" when args.Length < 2:
                    cmd.SetLogType(Command.LogType.Warning);
                    cmd.SetOutputMsg("No preset name given, please specify one.");
                    return;
                case "save":
                    {
                        var presetName = args[1].ToLower();
                        if (MapPresetHandler.MapPresetNames.Contains(presetName))
                        {
                            cmd.SetLogType(Command.LogType.Warning);
                            cmd.SetOutputMsg("Preset with specified name already exists.");
                            return;
                        }

                        var activeMaps = MapPresetHandler.GetAllStockMapIndexes(true);
                        var presetToSave = new SaveableMapPreset(activeMaps, presetName);
                        MapPresetHandler.AddNewPreset(presetToSave);

                        cmd.SetOutputMsg("Saved preset: \"" + presetName + "\".");
                        return;
                    }
                case "remove":
                    {
                        var presetName = args[1];
                        var presetWantedIndex = MapPresetHandler.FindIndexOfPreset(presetName);

                        if (presetWantedIndex == -1)
                        {
                            cmd.SetLogType(Command.LogType.Warning);
                            cmd.SetOutputMsg("Specified preset not found.");
                            return;
                        }

                        MapPresetHandler.DeletePreset(presetWantedIndex, presetName);
                        cmd.SetOutputMsg("Removed preset: \"" + presetName + "\".");
                        return;
                    }
            }

            // Must want to load preset instead
            var presetWanted = MapPresetHandler.FindPreset(arg);
            if (presetWanted is null)
            {
                cmd.SetLogType(Command.LogType.Warning);
                cmd.SetOutputMsg("Specified preset not found.");
                return;
            }

            Debug.Log("Trying to load a preset: " + presetWanted.PresetName);
            MapPresetHandler.LoadPreset(presetWanted);
            cmd.SetOutputMsg("Enabled preset: \"" + arg + "\".");
        }

        // Mutes the specified player (Only for the current lobby and only client-side)
        private static void MuteCmd(string[] args, Command cmd)
        {
            var targetID = Helper.GetIDFromColor(args[0]);

            if (!Helper.MutedPlayers.Contains(targetID))
            {
                Helper.MutedPlayers.Add(targetID);
                cmd.IsEnabled = true;

                cmd.SetOutputMsg("Muted: " + Helper.GetColorFromID(targetID));
                return;
            }

            Helper.MutedPlayers.Remove(targetID);
            cmd.IsEnabled = false;
            cmd.SetOutputMsg("Unmuted: " + Helper.GetColorFromID(targetID));
        }

        // Music commands
        private static void MusicCmd(string[] args, Command cmd)
        {
            var musicHandler = MusicHandler.Instance;
            var currentSong = Traverse.Create(musicHandler).Field("currntSong").GetValue<int>();
            var songNames = musicHandler.myMusic.Select(x => x.clip.name).ToArray();

            switch (args[0].ToLower())
            {
                case "skip": // Skips to the next song or if all have been played, a random one

                    Helper.SongLoop = false;
                    var nextSongMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
                    nextSongMethod.Invoke(MusicHandler.Instance, null);

                    Helper.SongLoop = false;
                    cmd.IsEnabled = true;

                    cmd.SetOutputMsg($"Skipped to: { songNames[currentSong]} ({currentSong}/{musicHandler.myMusic.Length - 1})");
                    return;
                case "play": // Plays song that corresponds to the specified index (0 to # of songs - 1)
                    var songIndex = int.Parse(args[1]);

                    if (songIndex > musicHandler.myMusic.Length - 1 || songIndex < 0)
                    {
                        cmd.SetOutputMsg($"Invalid index: input must be between 0 and {musicHandler.myMusic.Length - 1}.");
                        cmd.SetLogType(Command.LogType.Warning);
                        return;
                    }
                    Traverse.Create(musicHandler).Field("currntSong").SetValue(songIndex);
                    var audioSource = musicHandler.GetComponent<AudioSource>();
                    audioSource.clip = musicHandler.myMusic[songIndex].clip;
                    audioSource.volume = musicHandler.myMusic[songIndex].volume;
                    audioSource.Play();

                    Helper.SongLoop = false;
                    cmd.IsEnabled = true;
                    cmd.SetOutputMsg($"Now playing: {songNames[songIndex]} ({songIndex}/{musicHandler.myMusic.Length - 1})");
                    return;
                case "loop": // Loops the current or specified song
                    if (args.Length == 1)
                    {
                        Helper.SongLoop = !Helper.SongLoop;
                        cmd.IsEnabled = Helper.SongLoop;
                        cmd.SetOutputMsg("Toggled SongLoop.");
                    }
                    else if (args.Length == 2)
                    {
                        songIndex = int.Parse(args[1]);
                        musicHandler = MusicHandler.Instance;

                        if (songIndex > musicHandler.myMusic.Length - 1 || songIndex < 0)
                        {
                            cmd.SetOutputMsg($"Invalid index: input must be between 0 and {musicHandler.myMusic.Length - 1}.");
                            cmd.SetLogType(Command.LogType.Warning);
                            return;
                        }
                        Traverse.Create(musicHandler).Field("currntSong").SetValue(songIndex);
                        audioSource = musicHandler.GetComponent<AudioSource>();
                        audioSource.clip = musicHandler.myMusic[songIndex].clip;
                        audioSource.volume = musicHandler.myMusic[songIndex].volume;
                        audioSource.Play();

                        Helper.SongLoop = true;
                        cmd.IsEnabled = true;
                        cmd.SetOutputMsg($"Now looping: {songNames[songIndex]} ({songIndex}/{musicHandler.myMusic.Length - 1})");
                        return;
                    }
                    return;
                default:
                    return;
            }
        }

        private static void OuchCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled OuchMode.");
        }

        // Outputs the specified player's ping
        private static void PingCmd(string[] args, Command cmd)
        {
            var targetID = Helper.GetIDFromColor(args[0]);

            if (targetID == Helper.localNetworkPlayer.NetworkSpawnID)
            {
                cmd.SetOutputMsg("Can't ping yourself.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var targetPing = Helper.ClientData[targetID].Ping;

            cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + targetPing);
        }

        // Privates the lobby (no player can publicly join unless invited)
        private static void PrivateCmd(string[] args, Command cmd)
        {
            if (!MatchmakingHandler.Instance.IsHost)
            {
                cmd.SetOutputMsg("Need to be host!");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var changeLobbyTypeMethod = AccessTools.Method(typeof(MatchmakingHandler), "ChangeLobbyType");
            changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance, new object[]
            {
            ELobbyType.k_ELobbyTypePrivate
            });

            cmd.SetOutputMsg("Lobby made private!");
        }

        private static void ProfileCmd(string[] args, Command cmd)
            => SteamFriends.ActivateGameOverlayToUser("steamid", Helper.GetSteamID(Helper.GetIDFromColor(args[0])));

        // Publicizes the lobby (any player can join through quick match)
        private static void PublicCmd(string[] args, Command cmd)
        {
            if (!MatchmakingHandler.Instance.IsHost)
            {
                cmd.SetOutputMsg("Need to be host!");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var changeLobbyTypeMethod = AccessTools.Method(typeof(MatchmakingHandler), "ChangeLobbyType");
            changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance, new object[]
            {
            ELobbyType.k_ELobbyTypePublic
            });

            cmd.SetOutputMsg("Lobby made public!");
        }

        // Enables/disables the rainbow system
        private static void RainbowCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("Rainbow", cmd.IsEnabled);
            Object.FindObjectOfType<RainbowManager>().enabled = cmd.IsEnabled;

            cmd.SetOutputMsg("Toggled PlayerRainbow.");
        }

        private static void ResolutionCmd(string[] args, Command cmd)
        {
            var width = int.Parse(args[0]);
            var height = int.Parse(args[1]);

            Screen.SetResolution(width, height, Convert.ToBoolean(OptionsHolder.fullscreen));
            cmd.SetOutputMsg("Set new resolution of: " + width + "x" + height);
        }

        // Enables/disables rich text for chat messages
        private static void RichCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            var chatPopup = Traverse.Create(Helper.LocalChat).Field("text").GetValue<TextMeshPro>();
            chatPopup.richText = cmd.IsEnabled;

            cmd.SetOutputMsg("Toggled Richtext.");
        }

        // Appends shrug emoticon to the end of the msg just sent
        private static void ShrugCmd(string[] args, Command cmd)
        {
            var msg = string.Join(" ", args, 0, args.Length) + " \u00af\\_" +
                      ConfigHandler.GetEntry<string>("ShrugEmoji") + "_/\u00af";

            cmd.SetOutputMsg(msg);
        }

        // Outputs a stat of the target user (WeaponsThrown, Falls, BulletShot, and etc.)
        private static void StatCmd(string[] args, Command cmd)
        {
            if (args.Length == 1)
            {
                var targetStat = args[0].ToLower();
                var myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();

                cmd.SetOutputMsg("My " + targetStat + ": " + Helper.GetTargetStatValue(myStats, targetStat));
                return;
            }

            if (args[0] == "all")
            {
                var targetStat = args[1].ToLower();

                var statMsg = "";
                foreach (var player in GameManager.Instance.mMultiplayerManager.PlayerControllers)
                {
                    if (player != null)
                        statMsg = statMsg +
                                  Helper.GetColorFromID((ushort)player.playerID) + ", " +
                                  targetStat + ": " +
                                  Helper.GetTargetStatValue(player.GetComponent<CharacterStats>(), targetStat) +
                                  "\n";
                }

                cmd.SetOutputMsg(statMsg);
                return;
            }

            var targetID = Helper.GetIDFromColor(args[0]);
            var targetStats = Helper.GetNetworkPlayer(targetID).GetComponentInParent<CharacterStats>();
            var targetPlayerStat = args[1].ToLower();

            cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + ", " + targetPlayerStat + ": " +
                             Helper.GetTargetStatValue(targetStats, targetPlayerStat));
        }

        // Kills user
        private static void SuicideCmd(string[] args, Command cmd)
        {
            Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);

            var phrases = ConfigHandler.DeathPhrases;
            var randMsg = phrases[Random.Range(0, phrases.Length)];
            cmd.SetOutputMsg(randMsg);
        }

        // Enables/disables the autargetStatto-translate system for chat
        private static void TranslateCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled Auto-Translate.");
        }

        // Enables/disables chat censorship
        private static void UncensorCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled Chat De-censoring.");
        }

        // Enables UwUifier for chat messages
        private static void UwuCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled UwUifier.");
        }

        // Outputs the mod version number to chat
        private static void VerCmd(string[] args, Command cmd) => cmd.SetOutputMsg("QOL Mod: " + Plugin.VersionNumber);

        // Enables/Disables system for outputting the HP of the winner after each round to chat
        private static void WinnerHpCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            cmd.SetOutputMsg("Toggled WinnerHP Announcer.");
        }

        // Enables/disables winstreak system
        private static void WinstreakCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            if (cmd.IsEnabled)
            {
                cmd.IsEnabled = true;
                GameManager.Instance.winText.fontSize = ConfigHandler.GetEntry<int>("WinstreakFontsize");
            }

            cmd.SetOutputMsg("Toggled Winstreak system.");
        }

        // ****************************************************************************************************
        //                                    Cheat command methods below                                      
        // ****************************************************************************************************

        // Sending fire packages from specified player
        private static void PkgCmd(string[] args, Command cmd)
        {
            var help = "args: target, x, y, Vx, Vy, (weaponIndex), (isLocalDisplay)";
            if (args.Length < 5)
            {
                cmd.SetLogType(Command.LogType.Warning);
                cmd.SetOutputMsg(help);
                return;
            }

            var playerID = ushort.MaxValue;
            var targetID = ushort.MaxValue;
            var x = short.Parse(args[1]);
            var y = short.Parse(args[2]);
            var Vx = sbyte.Parse(args[3]);
            var Vy = sbyte.Parse(args[4]);
            var weaponIndex = -1;
            var isLocalDisplay = true;

            if (MatchmakingHandler.IsNetworkMatch)
            {
                playerID = Helper.localNetworkPlayer.NetworkSpawnID;
                targetID = args[1] switch
                {
                    "all" => ushort.MaxValue,
                    _ => Helper.GetIDFromColor(args[0]),
                };
            }

            if (args.Length > 6)
            {
                if (!int.TryParse(args[5], out weaponIndex))
                {
                    weaponIndex = -1;
                    isLocalDisplay = bool.Parse(args[5]);
                }
                if (args.Length > 7) isLocalDisplay = bool.Parse(args[6]);
            }

            CheatHelper.FirePackage(x, y, Vx, Vy, playerID, targetID, weaponIndex, isLocalDisplay);

        }

        public static void BulletHellCmd(string[] args, Command cmd)
        {
            // args: targetColor, (isLocalDisplay)
            var playerID = ushort.MaxValue;
            var targetID = ushort.MaxValue;
            var isLocalDisplay = false;
            if (args.Length > 1) isLocalDisplay = bool.Parse(args[1]);

            if (MatchmakingHandler.IsNetworkMatch)
            {
                playerID = Helper.localNetworkPlayer.NetworkSpawnID;
                if (args.Length > 0)
                    targetID = args[0] == "all" ? ushort.MaxValue : Helper.GetIDFromColor(args[0]);
            }

            cmd.SetOutputMsg("Beaming: " + Helper.GetColorFromID(targetID));
            GameManager.Instance.StartCoroutine(CheatHelper.BulletHell(playerID, targetID, isLocalDisplay));
        }

        public static void BulletRingCmd(string[] args, Command cmd)
        {
            // args: weaponIndex, radius
            var playerID = MatchmakingHandler.IsNetworkMatch ? Helper.localNetworkPlayer.NetworkSpawnID : ushort.MaxValue;
            var targetID = ushort.MaxValue;
            var weaponIndex = args.Length > 0 ? int.Parse(args[0]) : -1;
            var radius = args.Length > 1 ? int.Parse(args[1]) : 200;

            cmd.SetOutputMsg("Bullet Ring!");
            GameManager.Instance.StartCoroutine(CheatHelper.BulletRing(playerID, targetID, (short)radius, weaponIndex));
        }

        public static void AfkCmd(string[] args, Command cmd)
        {
            var controller = Helper.controller;
            var AI = controller.GetComponent<AI>();
            if (AI.enabled)
            {
                AI.enabled = false;
                cmd.IsEnabled = false;
            }
            else
            {
                AI.enabled = true;
                cmd.IsEnabled = true;
            }

            cmd.SetOutputMsg("Toggled AFK.");
        }

        // Execute commands as specified player
        private static void ExecuteCmd(string[] args, Command cmd)
        {
            if (args[0] == "all")
            {
                // TODO
                return;
            }
            var targetID = Helper.GetIDFromColor(args[0]);
            var targetCommand = args[1].TrimStart(Command.CmdPrefix).ToLower();
            var argsToExecute = args.Skip(2).ToArray(); // Skip first 2 args (player, command)
            if (!PlayerUtils.IsPlayerInLobby(targetID))
            {
                cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " is not in the lobby.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
            if (targetCommand == "say")
            {
                var argsToSay = new[] { args[0] }.Concat(args.Skip(2)).ToArray(); // (player, args)
                SudoCmd(argsToSay, cmd);
                return;
            }
            if (!CmdDict.ContainsKey(targetCommand))
            {
                cmd.SetOutputMsg("Specified command or it's alias not found. See /help for full list of commands.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            var localNetworkPlayer = Helper.localNetworkPlayer;
            try
            {
                Helper.localNetworkPlayer = Helper.GetNetworkPlayer(targetID);
                CmdDict[targetCommand].Execute(argsToExecute);
            }
            finally // Skip return, like postfix
            {
                Helper.localNetworkPlayer = localNetworkPlayer;
            }
        }

        public static void FlyCmd(string[] args, Command cmd)
        {
            var controller = Helper.controller;
            cmd.IsEnabled = controller.canFly;
            cmd.Toggle();
            CheatTextManager.ToggleFeature("Fly", cmd.IsEnabled);
            controller.canFly = cmd.IsEnabled;
            cmd.SetOutputMsg("Toggled Fly.");
        }

        private static void GodCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("GodMode", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled GodMode.");
        }

        private static void FullAutoCmd(string[] args, Command cmd)
        {
            var fighting = Traverse.Create(Helper.controller).Field("fighting").GetValue<Fighting>();
            var weapon = Traverse.Create(fighting).Field("weapon").GetValue<Weapon>();

            cmd.Toggle();
            CheatTextManager.ToggleFeature("FullAuto", cmd.IsEnabled);
            if (cmd.IsEnabled && weapon != null) // Punch shouldn't be fullauto
            {
                fighting.fullAuto = true;
            }
            else
            {
                fighting.fullAuto = weapon != null ? weapon.fullAuto : false;
            }
            cmd.SetOutputMsg("Toggled FullAuto.");
        }

        private static void NoRecoilCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("NoRecoil", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled NoRecoil.");
        }

        private static void InfiniteAmmoCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("InfiniteAmmo", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled InfiniteAmmo.");
        }

        private static void FastFireCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("FastFire", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled FastFire.");
        }

        private static void FastPunchCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("FastPunch", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled FastPunch.");
        }

        private static void GunCmd(string[] args, Command cmd)
        {
            if (args[0] == "-1")
            {
                var spawnRandomWeaponMethod = AccessTools.Method(typeof(GameManager), "SpawnRandomWeapon");
                spawnRandomWeaponMethod.Invoke(GameManager.Instance, null);
                //GameManager.Instance.SpawnRandomWeapon();
                return;
            }
            var fighting = Traverse.Create(Helper.controller).Field("fighting").GetValue<Fighting>();

            var weaponWanted = byte.Parse(args[0]);
            fighting.Dissarm();
            fighting.NetworkPickUpWeapon(weaponWanted);

            var weapon = Traverse.Create(fighting).Field("weapon").GetValue<Weapon>();
            if (weapon != null || !string.IsNullOrEmpty(weapon.name))
            {
                var weaponName = weapon.name.TrimStart(' ');
                cmd.SetOutputMsg("Gave gun: " + weaponName);
            }
            else
            {
                cmd.SetOutputMsg("Cleared gun.");
            }
        }

        private static void KickCmd(string[] args, Command cmd)
        {
            if (args.Length < 1)
            {
                cmd.SetOutputMsg("Must select a player!");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
            var method = args.Length > 1 ? args[1] : "1"; // Default: Client_Init
            var targetID = Helper.GetIDFromColor(args[0]);
            var payload = new byte[] { 0x00 };
            var msgType = P2PPackageHandler.MsgType.KickPlayer;
            if (!PlayerUtils.IsPlayerInLobby(targetID))
            {
                cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " is not in the lobby.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
            switch (method)
            {
                case "0": // Built-in
                    msgType = P2PPackageHandler.MsgType.KickPlayer;
                    break;
                case "1": // Client_Init
                    msgType = P2PPackageHandler.MsgType.ClientInit;
                    break;
                case "2": // Workshop_Corruption_Kick
                    msgType = P2PPackageHandler.MsgType.WorkshopMapsLoaded;
                    payload = new byte[] { 0x01, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    break;
                case "3": // Workshop_Crash
                    msgType = P2PPackageHandler.MsgType.WorkshopMapsLoaded;
                    payload = new byte[524282 /* 0xFFFF * 8 + 2 */];
                    new System.Random().NextBytes(payload);
                    payload[0] = 0xFF;
                    payload[1] = 0xFF;
                    break;
                case "4": // Invalid_Map
                    msgType = P2PPackageHandler.MsgType.MapChange;
                    payload = new byte[] { (byte)Helper.localNetworkPlayer.NetworkSpawnID, 0, 0 }; // Map ID: 0
                    break;
                default:
                    cmd.SetLogType(Command.LogType.Warning);
                    cmd.SetOutputMsg("Invalid method! 0:Built-in 1:Client_Init 2:Workshop_Corruption_Kick 3:Workshop_Crash 4:Invalid_Map");
                    return;
            }
            P2PPackageHandler.Instance.SendP2PPacketToUser(Helper.GetSteamID(targetID), payload, msgType,
            channel: P2PPackageHandlerPatch.GetChannelForMsgType(P2PPackageHandler.Instance, msgType));
            cmd.SetOutputMsg("Kick player: " + Helper.GetColorFromID(targetID));
        }

        private static void KillCmd(string[] args, Command cmd)
        {
            if (args.Length == 0)
            {
                SuicideCmd(args, cmd);
                return;
            }
            var targetID = Helper.GetIDFromColor(args[0]);
            if (!PlayerUtils.IsPlayerInLobby(targetID))
            {
                cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " is not in the lobby.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
            Helper.GetNetworkPlayer(targetID).UnitWasDamaged(0, true);
            cmd.SetOutputMsg("Killed player " + Helper.GetColorFromID(targetID));
        }

        public static void ReviveCmd(string[] args, Command cmd)
        {
            var reviveMethod = AccessTools.Method(typeof(GameManager), "RevivePlayer");
            var target = MatchmakingHandler.IsNetworkMatch ?
                Helper.localNetworkPlayer.gameObject.GetComponent<Controller>() : Helper.controller;
            reviveMethod.Invoke(GameManager.Instance, new object[] { target, false });

            cmd.SetOutputMsg("Me alive <:");
        }

        // Say as specified player
        private static void SudoCmd(string[] args, Command cmd)
        {
            var colorWanted = args[0] != "all" ? Helper.GetIDFromColor(args[0]) : ushort.MaxValue;
            var txt = string.Join(" ", args, 1, args.Length - 1);
            var bytes = Encoding.UTF8.GetBytes(txt);
            var syncClientChatMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat");

            if (colorWanted != ushort.MaxValue)
            {
                var channel = colorWanted switch
                {
                    0 => 3, // Yellow
                    1 => 5, // Red
                    2 => 7, // Green
                    3 => 9, // Blue
                    _ => throw new ArgumentOutOfRangeException()
                };
                GameManager.Instance.mMultiplayerManager.OnPlayerTalked(bytes, channel, colorWanted);
                syncClientChatMethod.Invoke(Helper.GetNetworkPlayer(colorWanted), new object[] { bytes });
                return;
            }
            else
            {
                foreach (var color in new ushort[] { 0, 1, 2, 3 })
                {
                    var channel = (color + 1) * 2 + 1;
                    try
                    {
                        GameManager.Instance.mMultiplayerManager.OnPlayerTalked(bytes, channel, color);
                        syncClientChatMethod.Invoke(Helper.GetNetworkPlayer(color), new object[] { bytes });
                    }
                    catch (Exception) { continue; }
                }
            }
        }

        private static void SummonCmd(string[] args, Command cmd)
        {
            if (MatchmakingHandler.IsNetworkMatch)
            {
                cmd.SetLogType(Command.LogType.Warning);
                cmd.SetOutputMsg("Can't summon in network match!");
                return;
            }
            var spawnPcEnabled = true;
            if (args.Length > 1) spawnPcEnabled = bool.Parse(args[1]);

            switch (args[0])
            {
                case "player":
                    BotHandler.Instance.SpawnBotEnemyPlayer(spawnPcEnabled);
                    cmd.SetOutputMsg("Spawned player");
                    break;
                case "bolt":
                    BotHandler.Instance.SpawnBotEnemyBolt(spawnPcEnabled);
                    cmd.SetOutputMsg("Spawned bolt");
                    break;
                case "zombie":
                    BotHandler.Instance.SpawnBotEnemyZombie(spawnPcEnabled);
                    cmd.SetOutputMsg("Spawned zombie");
                    break;
                default:
                    cmd.SetOutputMsg("Invalid PlayerPrefab!");
                    break;
            }
        }

        private static void SwitchWeaponCmd(string[] args, Command cmd)
        {
            cmd.Toggle();
            CheatTextManager.ToggleFeature("WeaponSwitch", cmd.IsEnabled);
            cmd.SetOutputMsg("Toggled WeaponSwitch.");
        }

        // Set the selected player win and switch to selected map
        private static void WinCmd(string[] args, Command cmd)
        {
            var unReadyAllPlayersMethod = AccessTools.Method(typeof(MultiplayerManager), "UnReadyAllPlayers");
            var sendPacketToAllMethod = AccessTools.Method(typeof(MultiplayerManager), "SendMessageToAllClients");

            var levelSelector = Traverse.Create(GameManager.Instance).Field("levelSelector").GetValue<LevelSelection>();
            var nextLevel = levelSelector.GetNextLevel();

            var targetID = Helper.localNetworkPlayer.NetworkSpawnID;

            // TODO: If not host, mapIndex is always 0
            var mapType = nextLevel.MapType; // 0: Landfall, 1: CustomLocal, 2: CustomOnline
            var mapData = nextLevel.MapData; // [mapIndex, 0, 0, 0]

            if (args.Length == 1)
            {
                if (int.TryParse(args[0], out int mapIndex)) // Only index
                {
                    mapIndex = mapIndex == -1 ? Random.Range(1, 125) : mapIndex;
                    mapData = BitConverter.GetBytes(mapIndex);
                }
                else if (PlayerUtils.PlayerColorsParams.Contains(args[0])) // Only color
                {
                    targetID = Helper.GetIDFromColor(args[0]);
                }
            }
            else if (args.Length > 1) // Color and index
            {
                mapType = 0;
                int mapIndex = int.Parse(args[1]);
                mapIndex = mapIndex == -1 ? Random.Range(1, 125) : mapIndex;
                mapData = BitConverter.GetBytes(mapIndex);
            }

            unReadyAllPlayersMethod.Invoke(GameManager.Instance.mMultiplayerManager, null);
            sendPacketToAllMethod.Invoke(GameManager.Instance.mMultiplayerManager, new object[]
            {
                    new byte[] {(byte) targetID, mapType}.Concat(mapData).ToArray(),
                    P2PPackageHandler.MsgType.MapChange,
                    false,
                    0UL,
                    EP2PSend.k_EP2PSendReliable,
                    0
            });
        }
    }
}