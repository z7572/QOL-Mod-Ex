using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace QOL.Trainer.Patches;

[HarmonyPatch]
public static class ApplyBotJumpFix
{
    [HarmonyPatch(typeof(Controller), "Jump")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> FixInfiniteJumpTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = new List<CodeInstruction>(instructions);

        var indexToRemove = instructionList.FindIndex(instr =>
            instr.opcode == OpCodes.Ldarg_0 &&
            instructionList[instructionList.IndexOf(instr) + 1].opcode == OpCodes.Ldloc_0 &&
            instructionList[instructionList.IndexOf(instr) + 2].opcode == OpCodes.Brfalse);

        if (indexToRemove >= 0)
        {
            // Remove codes
            for (int i = 0; i < 7; i++)
            {
                // this.m_MovementState = ((!flag) ? Controller.MovementStateEnum.GroundJump : Controller.MovementStateEnum.WallJump);
                instructionList[indexToRemove + i].opcode = OpCodes.Nop;
            }
        }

        return instructionList;
    }

    [HarmonyPatch(typeof(Movement), "Jump")]
    [HarmonyPrefix]
    public static bool FixAudioClipBoundsPrefix(Movement __instance, ref bool __result, bool force, bool forceWallJump, AudioClip[] ___jumpClips, AudioSource ___au)
    {
        var doJumpMethod = AccessTools.Method(typeof(Movement), "DoJump", [typeof(bool), typeof(bool)]);
        __result = (bool)doJumpMethod.Invoke(__instance, [force, forceWallJump]);

        if (___jumpClips.Length != 0)
        {
            ___au.PlayOneShot(___jumpClips[UnityEngine.Random.Range(0, Math.Max(___jumpClips.Length - 1, 0))]);
        }

        return false;
    }
}
