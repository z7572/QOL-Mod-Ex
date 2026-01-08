using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace QOL;

public static class Extensions
{
    public static string Replace(this string origStr, char replaceableChar, string replacedWith)
        => origStr.Replace(replaceableChar.ToString(), replacedWith);

    public static bool StartsWith(this string str, char charToFind)
        => str.StartsWith(charToFind.ToString());

    public static string ToDecString(this byte[] bytes)
        => bytes == null ? "null" : string.Join(" ", bytes.Select(b => b.ToString().PadLeft(3)).ToArray());

    public static string ToHexString(this byte[] bytes)
        => bytes == null ? "null" : string.Join(" ", bytes.Select(b => b.ToString("X2").PadLeft(2)).ToArray());

    public static Dictionary<string, object> FromListAndValue(List<string> keys, object value)
    {
        var dict = new Dictionary<string, object>();
        foreach (var key in keys)
        {
            dict[key] = value;
        }
        return dict;
    }

    public static bool IsAI(this Controller controller) => controller.isAI && controller.GetComponent<AFKManager>() == null;
}