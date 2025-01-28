using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace QOL
{
    internal static class ApplyBotDeathFix
    {
        // Transpiler 实现，修改原 Die 方法的逻辑
        [HarmonyPatch(typeof(HealthHandler), "Die")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            // 获取 controller.isAI 和 controller.playerID 的字段
            var controllerIsAiFieldDef = AccessTools.Field(typeof(Controller), "isAI");
            var controllerPlayerIdFieldDef = AccessTools.Field(typeof(Controller), "playerID");

            // 寻找控制器 isAI 检查的指令
            for (int i = 0; i < instructionList.Count; i++)
            {
                // 找到原始的 `ldfld isAI` 和 `brfalse` 指令
                if (instructionList[i].opcode == OpCodes.Ldfld && instructionList[i].operand == controllerIsAiFieldDef)
                {
                    // 插入检查 playerID 是否大于 -1 的指令
                    // 加载 controller 对象到栈
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HealthHandler), "controller"));

                    // 加载 playerID 字段
                    yield return new CodeInstruction(OpCodes.Ldfld, controllerPlayerIdFieldDef);

                    // 将 -1 加载到栈上
                    yield return new CodeInstruction(OpCodes.Ldc_I4_M1);

                    // 比较 playerID 是否大于 -1
                    yield return new CodeInstruction(OpCodes.Bgt_S, instructionList[i + 1].operand); // 跳转到原来的目标

                    // 跳过原来的 isAI 检查
                    i += 4;  // 因为 isAI 检查一共占用了5条指令
                }

                // 保持原始指令
                yield return instructionList[i];
            }
        }
    }
}
