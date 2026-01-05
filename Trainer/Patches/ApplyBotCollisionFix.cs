using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace QOL.Trainer.Patches;

// Make bots collide with each other. Normally AIs do not collide with each other as they are set on the same GameObject layer index.
[HarmonyPatch(typeof(Controller), "SetCollision")]
public static class ApplyBotCollisionFix
{
    // Transpiler to modify the SetCollision method to enable AI collision based on playerID.
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixBotCollision(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = new List<CodeInstruction>(instructions);

        // Fetch target field defs
        var isAiField = AccessTools.Field(typeof(Controller), "isAI");
        var playerIdField = AccessTools.Field(typeof(Controller), "playerID");

        int targetIndex = -1;
        for (int i = 0; i < instructionList.Count - 2; i++)
        {
            if (instructionList[i].opcode == OpCodes.Ldarg_0 &&
                instructionList[i + 1].opcode == OpCodes.Ldfld && instructionList[i + 1].operand == isAiField &&
                (instructionList[i + 2].opcode == OpCodes.Brfalse || instructionList[i + 2].opcode == OpCodes.Brfalse_S))
            {
                targetIndex = i + 1; // 定位到 ldfld isAI 之后
                break;
            }
        }

        if (targetIndex >= 0)
        {
            var originalBrFalse = instructionList[targetIndex + 1];
            var newInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, playerIdField),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Clt),
                new CodeInstruction(OpCodes.And),
                originalBrFalse.Clone()
            };

            // Remove brfalse 
            instructionList.RemoveAt(targetIndex + 1);
            instructionList.InsertRange(targetIndex + 1, newInstructions);
        }

        return instructionList;
    }
}