using System;
using System.Collections.Generic;
using System.Linq;

namespace QOL;

// WIP: Dynamic auto-parameter generator for command /execute
public class DynamicAutoParams
{
    public delegate List<string> ParamGenerator(string[] previousArgs, Command currentCommand);

    private readonly ParamGenerator _generator;

    public DynamicAutoParams(ParamGenerator generator)
    {
        _generator = generator;
    }

    public List<string> GetCandidates(string[] args, Command command)
    {
        return _generator(args, command);
    }
}

public static class DynamicParamGenerators
{
    public static readonly DynamicAutoParams ExecuteParams = new DynamicAutoParams((args, command) =>
    {
        switch (args.Length)
        {
            case 0:
                return PlayerUtils.PlayerColorsParams;

            case 1:
                return ChatCommands.CmdNames.Select(cmd => cmd.Substring(1)).ToList();

            default:
                var targetCommand = args[1];
                if (ChatCommands.CmdDict.TryGetValue(targetCommand, out var targetCmd))
                {
                    var remainingArgs = args.Skip(2).ToArray();
                    return targetCmd.GetAutoParamCandidates(remainingArgs);
                }
                return new List<string>();
        }
    });

    // ?
    public static DynamicAutoParams CreateFilteredCommandParams(Func<Command, bool> filter)
    {
        return new DynamicAutoParams((args, cmd) =>
        {
            return ChatCommands.CmdDict.Values
                .Where(filter)
                .Select(command => command.Name.Substring(1))
                .ToList();
        });
    }

    // ?
    public static DynamicAutoParams CreateConditionalParams(
        Func<string[], bool> condition,
        List<string> trueParams,
        List<string> falseParams)
    {
        return new DynamicAutoParams((args, cmd) =>
        {
            return condition(args) ? trueParams : falseParams;
        });
    }
}
