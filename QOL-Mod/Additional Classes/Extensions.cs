using HarmonyLib;
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
}