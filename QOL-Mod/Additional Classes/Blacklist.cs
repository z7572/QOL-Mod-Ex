using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace QOL;
public class Blacklist
{
    private static JSONObject blacklistData;

    public static void LoadBlacklist()
    {
        try
        {
            if (File.Exists(Plugin.BlacklistPath))
            {
                string jsonContent = File.ReadAllText(Plugin.BlacklistPath);
                blacklistData = JSON.Parse(jsonContent).AsObject;
            }
            else
            {
                blacklistData = new JSONObject();
                blacklistData.Add("76561199219504453", "<u><i><#FF7>Stick<#77F>BugFight"); // This guy sucks
                SaveBlacklist();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during loading blacklist: {ex.Message}");
        }
    }

    public static void SaveBlacklist()
    {
        try
        {
            string jsonContent = blacklistData.ToString();
            File.WriteAllText(Plugin.BlacklistPath, jsonContent);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during saving blacklist: {ex.Message}");
        }
    }

    public static bool AddToBlacklist(string steamID, string playerName)
    {
        if (blacklistData.HasKey(steamID))
        {
            return false;
        }
        blacklistData.Add(steamID, playerName);
        SaveBlacklist();
        return true;
    }

    public static bool RemoveFromBlacklist(string steamID)
    {
        if (blacklistData.HasKey(steamID))
        {
            blacklistData.Remove(steamID);
        }
        else
        {
            return false;
        }
        SaveBlacklist();
        return true;
    }

    public static bool RemoveFromBlacklist(int index)
    {
        if (index < 1 || index > blacklistData.Count)
        {
            return false;
        }

        int currentIndex = 0;
        string steamIDToRemove = null;
        foreach (var item in blacklistData.Linq)
        {
            currentIndex++;
            if (currentIndex == index)
            {
                steamIDToRemove = item.Key;
                break;
            }
        }
        if (steamIDToRemove != null)
        {
            string playerID = blacklistData[steamIDToRemove];
            blacklistData.Remove(steamIDToRemove);
            SaveBlacklist();
            Helper.SendModOutput($"Removed {playerID} from blacklist", Command.LogType.Success, false);
        }
        return true;
    }

    public static bool IsPlayerBlacklisted(string steamID)
    {
        return blacklistData.HasKey(steamID);
    }

    public static void ClearBlacklist()
    {
        blacklistData.Clear();
        SaveBlacklist();
    }

    public static string ListBlacklist()
    {
        if (blacklistData.Count == 0)
        {
            return "Blacklist is empty.";
        }
        var result = new StringBuilder();
        var index = 1;
        foreach (var item in blacklistData.Linq)
        {
            result.AppendLine($"{index}. {item.Key} {item.Value}");
            index++;
        }
        return result.ToString().TrimEnd();
    }
}
