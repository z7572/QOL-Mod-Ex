using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace QOL.Trainer.Patches;

[HarmonyPatch(typeof(Controller), "Start")]
public static class ApplyBotHasControlFix
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeList = new List<CodeInstruction>(instructions);
        bool foundAICondition = false;
        int insertIndex = -1;

        // Find the IL sequence: ldarg.0 → ldfld Controller::isAI → brfalse/brfalse.s (corresponds to "if (!isAI) jump" in C#)
        for (int i = 0; i < codeList.Count - 2; i++)
        {
            if (codeList[i].opcode != OpCodes.Ldarg_0)
                continue;

            if (codeList[i + 1].opcode != OpCodes.Ldfld ||
                !codeList[i + 1].OperandIs(AccessTools.Field(typeof(Controller), "isAI")))
                continue;

            if (codeList[i + 2].opcode != OpCodes.Brfalse && codeList[i + 2].opcode != OpCodes.Brfalse_S)
                continue;

            // Mark the insertion point right after the brfalse instruction (start of the AI true branch)
            foundAICondition = true;
            insertIndex = i + 3;
            break;
        }

        // Insert IL instructions to call the custom AI setup method if the target sequence is found
        if (foundAICondition && insertIndex != -1)
        {
            var callInstructions = new List<CodeInstruction>()
            {
                // ApplyBotHasControlFix.CustomAISetup(__instance)
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ApplyBotHasControlFix), nameof(CustomAISetup)))
            };

            codeList.InsertRange(insertIndex, callInstructions);
        }

        return codeList;
    }

    // Custom setup method to initialize AI properties and controller control states
    public static void CustomAISetup(Controller controllerInstance)
    {
        var aiComponent = controllerInstance.GetComponent<AI>();
        if (aiComponent != null)
        {
            aiComponent.goForGuns = true;
        }

        Traverse.Create(controllerInstance).Field("mHasControl").SetValue(true);

        var playerActions = CharacterActions.CreateWithControllerBindings();
        Traverse.Create(controllerInstance).Field("mPlayerActions").SetValue(playerActions);
    }
}