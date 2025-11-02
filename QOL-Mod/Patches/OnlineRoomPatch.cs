using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QOL.Patches;

[HarmonyPatch(typeof(OnlineRoom))]
class OnlineRoomPatch
{
    [HarmonyPatch("CheckDoor")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var setInputFieldMethod = AccessTools.Method(typeof(OnlineRoomPatch), nameof(SetChatFieldWorldPosition));

        // Find: playersInOnlineRoom = true (right)
        // After IL 128 stfld bool CodeStateAnimation::state1
        int firstBranchIndex = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            // Match: cameraAnim.state1 = false
            if (codes[i].opcode == OpCodes.Stfld &&
                codes[i].operand as System.Reflection.FieldInfo == AccessTools.Field(typeof(CodeStateAnimation), "state1") &&
                i > 0 && codes[i - 1].opcode == OpCodes.Ldc_I4_0 &&
                i > 2 && codes[i - 2].opcode == OpCodes.Ldfld &&
                codes[i - 2].operand as System.Reflection.FieldInfo == AccessTools.Field(typeof(OnlineRoom), "cameraAnim"))
            {
                firstBranchIndex = i + 1;
                break;
            }
        }

        // SetInputFieldWorldPosition(-37f)
        codes.InsertRange(firstBranchIndex, new[] {
            new CodeInstruction(OpCodes.Ldc_R4, -37f),
            new CodeInstruction(OpCodes.Call, setInputFieldMethod)
        });

        // Find: playersInOnlineRoom = false (left)
        // After IL 220 stfld bool CodeStateAnimation::state1
        int secondBranchIndex = -1;
        for (int i = 0; i < codes.Count; i++)
        {
            // Match: cameraAnim.state1 = true
            if (codes[i].opcode == OpCodes.Stfld &&
                codes[i].operand as System.Reflection.FieldInfo == AccessTools.Field(typeof(CodeStateAnimation), "state1") &&
                i > 0 && codes[i - 1].opcode == OpCodes.Ldc_I4_1 &&
                i > 2 && codes[i - 2].opcode == OpCodes.Ldfld &&
                codes[i - 2].operand as System.Reflection.FieldInfo == AccessTools.Field(typeof(OnlineRoom), "cameraAnim"))
            {
                secondBranchIndex = i + 1; // 插入点在该指令后
                break;
            }
        }

        // SetInputFieldWorldPosition(0f)
        codes.InsertRange(secondBranchIndex, new[] {
            new CodeInstruction(OpCodes.Ldc_R4, 0f),
            new CodeInstruction(OpCodes.Call, setInputFieldMethod)
        });

        return codes;
    }

    public static void SetChatFieldWorldPosition(float zPosition)
    {
        var inputField = GameObject.Find("GameManagement/GameCanvas/TextMeshPro - InputField");
        if (inputField != null)
        {
            var newPos = inputField.transform.position;
            newPos.z = zPosition;
            inputField.transform.position = newPos;
        }
        SetChatBubbleCap(zPosition != 0f);
    }

    public static void SetChatBubbleCap(bool leftToRight)
    {
        var chatManagers = Object.FindObjectsOfType<ChatManager>();

        if (chatManagers.Length == 0) return;

        var scaledOffset = 37f * Helper.ScreenWidthScaleFactor * (leftToRight ? -1 : 1);

        foreach (var chatManager in chatManagers)
        {
            if (!chatManager.transform.root.gameObject.activeInHierarchy) return;
            var followTransform = chatManager.GetComponent<FollowTransform>();

            if (followTransform != null)
            {
                followTransform.cap = new Vector2(
                    followTransform.cap.x + scaledOffset,
                    followTransform.cap.y
                );
                followTransform.capTop = new Vector2(
                    followTransform.capTop.x + scaledOffset,
                    followTransform.capTop.y
                );
            }
        }
    }
    
}
