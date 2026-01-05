using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace QOL.Trainer.Patches;

[HarmonyPatch(typeof(HealthHandler), "Die")]
public static class ApplyBotDeathFix
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DieTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeList = new List<CodeInstruction>(instructions);
        bool foundAICheckBrfalse = false;
        int targetBrfalseIndex = -1;

        // Find the second Brfalse instruction which corresponds to the jump when "this.controller.isAI" is false
        for (int i = 0; i < codeList.Count; i++)
        {
            if (i + 2 < codeList.Count
                && codeList[i].opcode == OpCodes.Ldarg_0
                && codeList[i + 1].opcode == OpCodes.Ldfld
                && codeList[i + 1].OperandIs(AccessTools.Field(typeof(HealthHandler), "controller"))
                && codeList[i + 2].opcode == OpCodes.Ldfld
                && codeList[i + 2].OperandIs(AccessTools.Field(typeof(Controller), "isAI")))
            {
                for (int j = i + 3; j < codeList.Count; j++)
                {
                    if (codeList[j].opcode == OpCodes.Brfalse || codeList[j].opcode == OpCodes.Brfalse_S)
                    {
                        foundAICheckBrfalse = true;
                        targetBrfalseIndex = j;
                        break;
                    }
                }
                if (foundAICheckBrfalse)
                    break;
            }
        }

        // Insert instructions to check "this.controller.playerID <= -1" before the Brfalse, forming the composite condition: this.controller && this.controller.isAI && this.controller.playerID <= -1
        if (foundAICheckBrfalse && targetBrfalseIndex != -1)
        {
            var playerIDCheckInstructions = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HealthHandler), "controller")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Controller), "playerID")), // this.controller.playerID
                new CodeInstruction(OpCodes.Ldc_I4_M1), // -1
                new CodeInstruction(OpCodes.Clt), // this.controller.playerID < -1
                new CodeInstruction(OpCodes.And) // (this.controller.isAI) && (this.controller.playerID <= -1)
            };

            codeList.InsertRange(targetBrfalseIndex, playerIDCheckInstructions);
        }

        foreach (var instruction in codeList)
        {
            yield return instruction;
        }
    }
}