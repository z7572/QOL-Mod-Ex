public class DynamicAutoParams(DynamicAutoParams.ParamType type)
{
    public enum ParamType
    {
        ChainCommand,
        ConfigKey
    }

    public ParamType Type { get; } = type;
}