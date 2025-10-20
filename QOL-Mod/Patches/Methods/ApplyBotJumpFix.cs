using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace QOL
{
    /// <summary>
    /// 修复“无限跳跃”问题和修正音效的索引越界错误。
    /// </summary>
    //[HarmonyPatch]
    internal static class ApplyBotJumpFix
    {
        // 修复无限跳跃问题的补丁
        //[HarmonyPatch(typeof(Controller), "Jump")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixInfiniteJump(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>(instructions);

            // 定位需要移除的目标指令序列（伪代码）
            var indexToRemove = instructionList.FindIndex(instr =>
                instr.opcode == OpCodes.Ldarg_0 &&
                instructionList[instructionList.IndexOf(instr) + 1].opcode == OpCodes.Ldloc_0 &&
                instructionList[instructionList.IndexOf(instr) + 2].opcode == OpCodes.Brfalse);

            if (indexToRemove >= 0)
            {
                // 将匹配的目标指令序列替换为 NOP
                for (int i = 0; i < 7; i++) // 对应伪代码中 7 条指令
                {
                    instructionList[indexToRemove + i].opcode = OpCodes.Nop;
                }
            }

            return instructionList;
        }

        // 修复音效索引越界问题的补丁
        //[HarmonyPatch(typeof(Movement), "Jump")]
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixAudioClipBounds(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var instructionList = new List<CodeInstruction>(instructions);
            var returnLabel = il.DefineLabel(); // 创建返回点标签

            // 插入音效数组检查代码
            var jumpClipsField = AccessTools.Field(typeof(Movement), "jumpClips");
            var auField = AccessTools.Field(typeof(Movement), "au");
            var doJumpMethod = AccessTools.Method(typeof(Movement), "DoJump", new[] { typeof(bool), typeof(bool) });
            var playOneShotMethod = AccessTools.Method(typeof(UnityEngine.AudioSource), "PlayOneShot", new[] { typeof(UnityEngine.AudioClip) });
            var randomRangeMethod = AccessTools.Method(typeof(UnityEngine.Random), "Range", new[] { typeof(int), typeof(int) });

            var newInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0), // this
                new CodeInstruction(OpCodes.Ldfld, jumpClipsField), // this.jumpClips
                new CodeInstruction(OpCodes.Ldlen), // jumpClips.Length
                new CodeInstruction(OpCodes.Brfalse_S, returnLabel), // if length == 0, return

                // 播放音效的修复代码
                new CodeInstruction(OpCodes.Ldarg_0), // this
                new CodeInstruction(OpCodes.Ldfld, auField), // this.au
                new CodeInstruction(OpCodes.Ldarg_0), // this
                new CodeInstruction(OpCodes.Ldfld, jumpClipsField), // this.jumpClips
                new CodeInstruction(OpCodes.Ldc_I4_0), // 最小索引 0
                new CodeInstruction(OpCodes.Ldarg_0), // this
                new CodeInstruction(OpCodes.Ldfld, jumpClipsField), // this.jumpClips
                new CodeInstruction(OpCodes.Ldlen), // 获取长度
                new CodeInstruction(OpCodes.Conv_I4), // 转换为 int
                new CodeInstruction(OpCodes.Ldc_I4_1), // -1
                new CodeInstruction(OpCodes.Sub), // 长度 - 1
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(System.Math), "Max", new[] { typeof(int), typeof(int) })), // System.Math.Max
                new CodeInstruction(OpCodes.Call, randomRangeMethod), // UnityEngine.Random.Range
                new CodeInstruction(OpCodes.Ldelem_Ref), // 取数组元素
                new CodeInstruction(OpCodes.Callvirt, playOneShotMethod), // PlayOneShot(jumpClip)
            };

            // 插入新的指令到方法结尾之前
            instructionList.InsertRange(instructionList.Count - 1, newInstructions);

            return instructionList;
        }
    }
}
