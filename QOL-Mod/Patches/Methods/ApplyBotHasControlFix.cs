using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace QOL
{
    /// <summary>
    /// Enable bots to be considered as in control and to go for guns. 
    /// </summary>
    [HarmonyPatch(typeof(Controller), "Start")]
    public static class ControllerStartPatch
    {
        // Transpiler 方法，用于修改 IL 指令
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // 找到目标指令的位置（通过特定指令模式匹配）
            int insertionIndex = -1;
            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    codes[i + 1].opcode == OpCodes.Call && // GetComponent<AI>() 调用
                    codes[i + 2].opcode == OpCodes.Ldc_I4_1 &&
                    codes[i + 3].opcode == OpCodes.Callvirt) // set_enabled 调用
                {
                    insertionIndex = i + 4; // 在目标指令之后插入
                    break;
                }
            }

            if (insertionIndex == -1)
            {
                Debug.LogError("Target instructions not found in Controller.Start method.");
                return instructions;
            }

            // 创建新的指令
            var newInstructions = new List<CodeInstruction>
            {
                // this.GetComponent<AI>().goForGuns = true;
                new CodeInstruction(OpCodes.Ldarg_0), // 加载当前对象 (this)
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Component), "GetComponent").MakeGenericMethod(typeof(AI))),
                new CodeInstruction(OpCodes.Ldc_I4_1), // 加载布尔值 true
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(AI), "goForGuns")),

                // this.mHasControl = true;
                new CodeInstruction(OpCodes.Ldarg_0), // 加载当前对象 (this)
                new CodeInstruction(OpCodes.Ldc_I4_1), // 加载布尔值 true
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Controller), "mHasControl")),

                // this.mPlayerActions = CharacterActions.CreateWithControllerBindings();
                new CodeInstruction(OpCodes.Ldarg_0), // 加载当前对象 (this)
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterActions), "CreateWithControllerBindings")),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Controller), "mPlayerActions")),
            };

            // 插入新指令
            codes.InsertRange(insertionIndex, newInstructions);

            return codes;
        }
    }
}
