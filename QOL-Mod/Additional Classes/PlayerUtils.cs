using System.Collections.Generic;

namespace QOL; 

public static class PlayerUtils
{
    public static readonly List<string> PlayerColorsParams = new() { "y", "yellow", "b", "blue", "r", "red", "g", "green" };

    public static readonly List<string> PlayerColorsParamsWithAll = new() { "all", "y", "yellow", "b", "blue", "r", "red", "g", "green" };

    public static bool IsPlayerInLobby(int targetID)
    {
        if (MatchmakingHandler.IsNetworkMatch)
        {
            var connectedClients = GameManager.Instance.mMultiplayerManager.ConnectedClients;
            return connectedClients[targetID] != null && connectedClients[targetID].PlayerObject;
        }

        else if (ControllerHandler.Instance != null)
        {
            var players = ControllerHandler.Instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                var controller = players[i];
                if (controller != null && controller.playerID == targetID)
                {
                    return true;
                }
            }
        }

        return false;
    }
}