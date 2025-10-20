using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace QOL
{
    /// <summary>
    /// Make bots collide with each other. Normally AIs do not collide with each other as they are set on the same GameObject layer index.
    /// </summary>
    //[HarmonyPatch(typeof(Controller), "SetCollision")]
    internal static class ApplyBotCollisionFix
    {
        /// <summary>
        /// Transpiler to modify the SetCollision method to enable AI collision based on playerID.
        /// </summary>
        //[HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixBotCollision(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = new List<CodeInstruction>(instructions);

            // Fetch target field defs
            // 获取 Controller 类中的字段 isAI 和 playerID
            var isAiField = AccessTools.Field(typeof(Controller), "isAI");
            var playerIdField = AccessTools.Field(typeof(Controller), "playerID");

            /*
            
                32	004C	ldarg.0
                33	004D	ldfld	    bool Controller::isAI
                34	0052	brfalse.s	40 (0063) ldloc.2 

             */

            // Signature of the condition to which the check will be added
            // 定位指令模式：目标为 `ldarg.0 -> ldfld isAI -> brfalse`
            int index = instructionList.FindIndex(instr =>
                instr.opcode == OpCodes.Ldfld && instr.operand == isAiField);

            if (index >= 0)
            {
                var branchInstruction = instructionList[index + 1]; // brfalse.s 的指令
                // Add new instructions after the matched signature
                // 在原始逻辑后插入新逻辑：检查 AI 是否有有效的 playerID
                var newInstructions = new List<CodeInstruction>
                {
                    // 加载 `this.playerID`
                    new CodeInstruction(OpCodes.Ldarg_0), // 加载 this
                    new CodeInstruction(OpCodes.Ldfld, playerIdField), // this.playerID
                    new CodeInstruction(OpCodes.Ldc_I4_0), // 加载常量 0
                    new CodeInstruction(OpCodes.Bge_S, branchInstruction.operand) // 如果 playerID >= 0，跳到 brfalse 的目标
                };

                // 插入新指令
                instructionList.InsertRange(index + 1, newInstructions);
            }

            return instructionList;
        }
    }
}
