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
    // TODO: Implement auto-suggested parameters property
    //public List<string> AutoParams { get; }
    public object AutoParams { get; private set; }
    public bool IsToggle { get; private set; }
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
            return dynamicParams.GetCandidates(args, this);
        }

        return GetCandidatesRecursive(AutoParams, args, 0);
    }

    private List<string> GetCandidatesRecursive(object currentLevel, string[] args, int depth)
    {
        if (depth >= args.Length)
        {
            return currentLevel switch
            {
                Dictionary<string, object> dict => dict.Keys.ToList(),
                List<string> list => list,
                List<List<string>> listOfLists when depth < listOfLists.Count => listOfLists[depth],
                List<List<string>> listOfLists when _isLastParamInfinite => listOfLists.Count > 0 ? listOfLists[listOfLists.Count - 1] : [],
                HybridAutoParams hybrid when depth < hybrid.IndexedParams.Count => hybrid.IndexedParams[depth],
                HybridAutoParams hybrid when hybrid.TreeParams != null => hybrid.TreeParams.Keys.ToList(),
                _ => []
            };
        }

        var currentArg = args[depth];

        switch (currentLevel)
        {
            case Dictionary<string, object> dict:
                if (dict.ContainsKey(currentArg))
                {
                    return GetCandidatesRecursive(dict[currentArg], args, depth + 1);
                }
                else
                {
                    return dict.Keys.ToList();
                }

            case List<List<string>> listOfLists:
                if (depth < listOfLists.Count)
                {
                    var currentList = listOfLists[depth];
                    if (currentList.Contains(currentArg))
                    {
                        return GetCandidatesRecursive(listOfLists, args, depth + 1);
                    }
                    else
                    {
                        return currentList;
                    }
                }
                else if (_isLastParamInfinite)
                {
                    return listOfLists.Count > 0 ? listOfLists[listOfLists.Count - 1] : [];
                }
                else
                {
                    return [];
                }

            case List<string> list:
                if (list.Contains(currentArg))
                {
                    return [];
                }
                else
                {
                    return list;
                }

            case HybridAutoParams hybrid:
                if (depth < hybrid.IndexedParams.Count)
                {
                    var currentList = hybrid.IndexedParams[depth];
                    if (currentList.Contains(currentArg))
                    {
                        return GetCandidatesRecursive(hybrid, args, depth + 1);
                    }
                    else
                    {
                        return currentList;
                    }
                }
                else if (hybrid.TreeParams != null)
                {
                    if (hybrid.TreeParams.ContainsKey(currentArg))
                    {
                        return GetCandidatesRecursive(hybrid.TreeParams[currentArg], args, depth + 1);
                    }
                    else
                    {
                        return hybrid.TreeParams.Keys.ToList();
                    }
                }
                else
                {
                    return [];
                }

            default:
                return [];
        }
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
            Debug.LogError("Exception occured when running command: " + e);

            // _currentOutputMsg = "Something went wrong! DM Monky#4600 if bug.";
            _currentOutputMsg = e.Message;
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