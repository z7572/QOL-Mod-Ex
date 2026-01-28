using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QOL;

public class Command
{

    public bool IsPublic
    {
        get => _isPublic;
        set
        {
            if (AlwaysPublic || AlwaysPrivate)
            {
                Debug.LogWarning("Cannot modify cmd visibility once it has been set always public/private!");
                return;
            }

            _isPublic = value;
        }
    }

    public string Name { get; }
    public string Option { get; private set; }
    public List<string> Aliases { get; } = new();
    public object AutoParams { get; internal set; }
    public bool IsToggle { get; private set; }
    public bool IsCheat { get; private set; }
    public bool IsEnabled { get; set; }

    public static char CmdPrefix = ConfigHandler.GetEntry<string>("CommandPrefix").Length == 1
        ? ConfigHandler.GetEntry<string>("CommandPrefix")[0]
        : '/';

    private readonly Action<string[], Command> _runCmdAction; // Use Action as method will never return anything
    private readonly int _minExpectedArgs; // Minimal # of args required for cmd to function
    private bool _isPublic;
    private bool _isLastParamInfinite; // Just a mark for enumerable params
    public bool AlwaysPublic;
    public bool AlwaysPrivate;

    private static string _currentOutputMsg;
    private static LogType _currentLogType; // Any mod msg will be of type "success" by default

    public Command(string name, Action<string[], Command> cmdMethod, int minNumExpectedArgs, bool defaultPrivate,
        object autoParameters = null)
    {
        Name = CmdPrefix + name;
        _runCmdAction = cmdMethod;
        _minExpectedArgs = minNumExpectedArgs;

        // Compatible with old structure List<string> autocompletion
        if (autoParameters is List<string> simpleAutoParams)
        {
            AutoParams = simpleAutoParams;
        }
        else if (autoParameters is List<List<string>> AutoParamsByIndex)
        {
            AutoParams = AutoParamsByIndex;
        }
        else if (autoParameters is List<string>[] AutoParamsByIndexEnumerable)
        {
            AutoParams = AutoParamsByIndexEnumerable.ToList();
            _isLastParamInfinite = true;
        }
        else if (autoParameters is Dictionary<string, object> AutoParamsTree)
        {
            AutoParams = AutoParamsTree;
        }
        else if (autoParameters is HybridAutoParams hybridAutoParams)
        {
            AutoParams = hybridAutoParams;
        }
        else if (autoParameters is List<object> mixedAutoParams)
        {
            AutoParams = mixedAutoParams;
        }
        else
        {
            AutoParams = null;
        }

        IsPublic = !defaultPrivate;
    }

    public List<string> GetAutoParamCandidates(string[] args)
    {
        if (AutoParams == null) return [];

        if (AutoParams is DynamicAutoParams dynamicParams)
        {
            // Usually DynamicAutoParams won't be the root, but if it is, handle it via recursive method
            return GetCandidatesRecursive(dynamicParams, args, 0);
        }

        return GetCandidatesRecursive(AutoParams, args, 0);
    }

    private List<string> GetCandidatesRecursive(object currentLevel, string[] args, int depth)
    {
        if (currentLevel is DynamicAutoParams dynamicParams)
        {
            if (depth > 0 && depth <= args.Length)
            {
                var prevParam = args[depth - 1];

                switch (dynamicParams.Type)
                {
                    case DynamicAutoParams.ParamType.ChainCommand:
                        if (ChatCommands.CmdDict.TryGetValue(prevParam.ToLower(), out var targetCmd))
                        {
                            var remainingArgs = args.Skip(depth).ToArray();
                            return targetCmd.GetAutoParamCandidates(remainingArgs);
                        }
                        break;

                    case DynamicAutoParams.ParamType.ConfigKey:
                        return ConfigHandler.GetConfigCandidates(prevParam);
                }
            }
            return [];
        }

        if (depth >= args.Length)
        {
            return currentLevel switch
            {
                null => [],
                Dictionary<string, object> dict => dict.Keys.ToList(),
                List<string> list => list,
                List<List<string>> listOfLists when depth < listOfLists.Count => listOfLists[depth] ?? [],
                List<List<string>> listOfLists when _isLastParamInfinite => listOfLists.Count > 0 ? listOfLists[listOfLists.Count - 1] ?? [] : [],
                HybridAutoParams hybrid when depth < hybrid.IndexedParams.Count => hybrid.IndexedParams[depth] ?? [],
                HybridAutoParams hybrid when hybrid.TreeParams != null => hybrid.TreeParams.Keys.ToList(),

                List<object> mixedList when depth < mixedList.Count =>
                    mixedList[depth] is DynamicAutoParams
                        ? GetCandidatesRecursive(mixedList[depth], args, depth)
                        : GetCandidatesForMixedLevel(mixedList[depth]),

                _ => []
            };
        }

        var currentArg = args[depth];

        if (currentLevel == null)
        {
            return GetCandidatesRecursive(null, args, depth + 1);
        }

        switch (currentLevel)
        {
            case Dictionary<string, object> dict:
                var dictKey = dict.Keys.FirstOrDefault(k => k.Equals(currentArg, StringComparison.InvariantCultureIgnoreCase));
                if (dictKey != null)
                {
                    var nextLevel = dict[dictKey];
                    return GetCandidatesRecursive(nextLevel, args, depth + 1);
                }
                else
                {
                    return dict.Keys.ToList();
                }

            case List<List<string>> listOfLists:
                if (depth < listOfLists.Count)
                {
                    var currentList = listOfLists[depth];
                    if (currentList == null) return GetCandidatesRecursive(listOfLists, args, depth + 1);

                    if (currentList.Any(s => s.Equals(currentArg, StringComparison.InvariantCultureIgnoreCase)))
                        return GetCandidatesRecursive(listOfLists, args, depth + 1);
                    else
                        return currentList;
                }
                else if (_isLastParamInfinite)
                {
                    return listOfLists.Count > 0 ? listOfLists[listOfLists.Count - 1] ?? [] : [];
                }
                return [];

            case List<string> list:
                if (list.Any(s => s.Equals(currentArg, StringComparison.InvariantCultureIgnoreCase))) return [];
                else return list;

            case List<object> mixedList:
                if (depth < mixedList.Count)
                {
                    var item = mixedList[depth];

                    if (item is List<string> strList)
                    {
                        if (strList.Contains(currentArg)) return GetCandidatesRecursive(mixedList, args, depth + 1);
                        else return strList;
                    }
                    else if (item is DynamicAutoParams)
                    {
                        return GetCandidatesRecursive(item, args, depth);
                    }
                    else if (item is Dictionary<string, object> dictItem)
                    {
                        if (dictItem.ContainsKey(currentArg))
                        {
                            var nextLevel = dictItem[currentArg];
                            return GetCandidatesRecursive(nextLevel, args, depth + 1);
                        }
                        return dictItem.Keys.ToList();
                    }
                    else if (item is HybridAutoParams hybridItem) // NOT TESTED YET
                    {
                        return GetCandidatesRecursive(hybridItem, args, depth);
                    }
                    else if (item == null)
                    {
                        return GetCandidatesRecursive(mixedList, args, depth + 1);
                    }
                }
                return [];

            case HybridAutoParams hybrid:
                if (depth < hybrid.IndexedParams.Count)
                {
                    var currentList = hybrid.IndexedParams[depth];
                    if (currentList == null) return GetCandidatesRecursive(hybrid, args, depth + 1);
                    if (currentList.Any(s => s.Equals(currentArg, StringComparison.InvariantCultureIgnoreCase)))
                        return GetCandidatesRecursive(hybrid, args, depth + 1);
                    else
                        return currentList;
                }
                else if (hybrid.TreeParams != null)
                {
                    var key = hybrid.TreeParams.Keys.FirstOrDefault(k => k.Equals(currentArg, StringComparison.InvariantCultureIgnoreCase));
                    if (key != null)
                    {
                        var nextLevel = hybrid.TreeParams[key];
                        return GetCandidatesRecursive(nextLevel, args, depth + 1);
                    }
                    else return hybrid.TreeParams.Keys.ToList();
                }
                return [];

            default:
                return [];
        }
    }

    private List<string> GetCandidatesForMixedLevel(object levelItem)
    {
        return levelItem switch
        {
            List<string> list => list,
            Dictionary<string, object> dict => dict.Keys.ToList(),
            DynamicAutoParams => [], // Normal to return empty here, as it should be intercepted and processed by the upper switch
            _ => []
        };
    }
    // Private as there has been no cases where this type of visibility was necessary and the cmd was not a toggle
    private void SetAlwaysPrivate()
    {
        if (AlwaysPublic)
        {
            Debug.LogWarning("Cmd is already always public, cannot modify this!");
            return;
        }

        AlwaysPrivate = true;
        IsPublic = false;
    }

    public Command SetAlwaysPublic()
    {
        if (AlwaysPrivate)
        {
            Debug.LogWarning("Cmd is already always private, cannot modify this!");
            return this;
        }

        AlwaysPublic = true;
        IsPublic = true;
        return this;
    }

    public Command MarkAsToggle()
    {
        IsToggle = true;
        return this;
    }

    public Command MarkAsCheat()
    {
        IsCheat = true;
        return this;
    }

    public Command MarkAsToggleCheat()
    {
        IsToggle = true;
        IsCheat = true;
        return this;
    }

    public void SetOutputMsg(string msg) => _currentOutputMsg = msg;
    public void SetLogType(LogType type) => _currentLogType = type;
    public void Toggle() => IsEnabled = !IsEnabled;
    public void Toggle(string option = null)
    {
        if (!string.IsNullOrEmpty(option))
        {
            if (Option != option)
            {
                IsEnabled = true;
                Option = option;
            }
            else
            {
                IsEnabled = !IsEnabled;
                Option = IsEnabled ? Option : null;
            }
        }
        else
        {
            IsEnabled = !IsEnabled;
            Option = null;
        }
    }

    public void Execute(params string[] args)
    {
        if (IsCheat && !CheatHelper.CheatEnabled)
        {
            _currentLogType = LogType.Warning;
            _currentOutputMsg = "Cheat is disabled!";
            Helper.SendModOutput(_currentOutputMsg, _currentLogType, false);
            return;
        }

        if (args.Length < _minExpectedArgs)
        {
            _currentLogType = LogType.Warning;
            _currentOutputMsg = "Invalid # of arguments specified. See /help for more info.";
            Helper.SendModOutput(_currentOutputMsg, _currentLogType, false);

            _currentLogType = LogType.Success;
            _currentOutputMsg = ""; // In case next cmd has no output 
            return;
        }

        try
        {
            _runCmdAction(args, this);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception occured when running command: " + e.GetType().Name + " : " + e.Message);

            // _currentOutputMsg = "Something went wrong! DM Monky#4600 if bug.";
            _currentOutputMsg = e.GetType().Name + " : " + e.Message;
            Helper.SendModOutput(_currentOutputMsg, LogType.Warning, false);
            _currentOutputMsg = "";
            throw;
        }

        if (string.IsNullOrEmpty(_currentOutputMsg)) // Some cmds may not have any output at all
            return;

        if (_currentLogType == LogType.Warning) // All warning msg's should be client-side
        {
            Helper.SendModOutput(_currentOutputMsg, LogType.Warning, false);
            _currentLogType = LogType.Success;
            _currentOutputMsg = "";
            return;
        }

        Helper.SendModOutput(_currentOutputMsg, LogType.Success, !IsToggle && IsPublic, !IsToggle || IsEnabled);
        _currentLogType = LogType.Success;
        _currentOutputMsg = "";
    }

    public enum LogType
    {
        Success,
        Warning
    }
}