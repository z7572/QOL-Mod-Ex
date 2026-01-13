using UnityEngine;

namespace QOL;

public class AFKManager : MonoBehaviour
{
    private void Update()
    {
        if (ChatManager.isTyping || PauseManager.isPaused) return;

        if (CharacterActions.IsAnyKeybindPressed())
        {
            ExitAFK();
            return;
        }
    }

    private void ExitAFK()
    {
        var controller = GetComponent<Controller>();
        if (controller != null)
        {
            var info = controller.GetComponent<CharacterInformation>();
            if (info != null)
            {
                info.paceState = 0;
                info.sinceFallen = 0f;
            }
        }

        ChatCommands.CmdDict["afk"].Execute();
        Destroy(this);
    }
}
