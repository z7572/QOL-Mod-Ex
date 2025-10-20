using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace QOL
{
    /*
    /// <summary>
    /// Add bot chat messages when bots take damage. Bots will randomly select pre-defined messages such as "Ouch!" or "That's monk-y business!".
    /// </summary>
    [HarmonyPatch(typeof(Controller), "OnTakeDamage")]
    internal static class AddBotChatMessages
    {
        /// <summary>
        /// Transpiler method to modify the OnTakeDamage method to include bot chat messages.
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> InjectBotChatMessages(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionList = new List<CodeInstruction>(instructions);

            // 获取所需的字段和方法信息
            var isAiField = AccessTools.Field(typeof(Controller), "isAI");
            var fightingField = AccessTools.Field(typeof(Controller), "fighting");
            var networkPlayerField = AccessTools.Field(typeof(Fighting), "mNetworkPlayer");
            var chatManagerField = AccessTools.Field(typeof(NetworkPlayer), "mChatManager");
            var talkMethod = AccessTools.Method(typeof(ChatManager), "Talk");
            var randomRangeMethod = AccessTools.Method(typeof(UnityEngine.Random), "Range", new[] { typeof(int), typeof(int) });

            // 定义局部变量 num
            var localNum = generator.DeclareLocal(typeof(int));

            /*
            
                // Instructions to inject

                0	0000	ldarg.0
                1	0001	ldfld	bool Controller::isAI
                2	0006	brfalse.s	40 (0085) ldarg.0 
                3	0008	ldarg.0
                4	0009	ldfld	class Fighting Controller::fighting
                5	000E	ldfld	class NetworkPlayer Fighting::mNetworkPlayer
                6	0013	brfalse.s	40 (0085) ldarg.0 
                7	0015	ldc.i4.0
                8	0016	ldc.i4	10
                9	001B	call	int32 [UnityEngine]UnityEngine.Random::Range(int32, int32)
                10	0020	stloc.0
                11	0021	ldloc.0
                12	0022	ldc.i4.s	1
                13	0024	bne.un.s	21 (0042) ldloc.0 
                14	0026	ldarg.0
                15	0027	ldfld	class Fighting Controller::fighting
                16	002C	ldfld	class NetworkPlayer Fighting::mNetworkPlayer
                17	0031	ldfld	class ChatManager NetworkPlayer::mChatManager
                18	0036	ldstr	"Ouch!"
                19	003B	callvirt	instance void ChatManager::Talk(string)
                20	0040	br.s	40 (0085) ldarg.0 
                21	0042	ldloc.0
                22	0043	ldc.i4	0x1D1
                23	0048	bne.un.s	31 (0066) ldloc.0 
                24	004A	ldarg.0
                25	004B	ldfld	class Fighting Controller::fighting
                26	0050	ldfld	class NetworkPlayer Fighting::mNetworkPlayer
                27	0055	ldfld	class ChatManager NetworkPlayer::mChatManager
                28	005A	ldstr	"Ow!"
                29	005F	callvirt	instance void ChatManager::Talk(string)
                30	0064	br.s	40 (0085) ldarg.0 
                31	0066	ldloc.0
                32	0067	ldc.i4.s	10
                33	0069	bne.un.s	40 (0085) ldarg.0 
                34	006B	ldarg.0
                35	006C	ldfld	class Fighting Controller::fighting
                36	0071	ldfld	class NetworkPlayer Fighting::mNetworkPlayer
                37	0076	ldfld	class ChatManager NetworkPlayer::mChatManager
                38	007B	ldstr	"That's monk-y business!"
                39	0080	callvirt	instance void ChatManager::Talk(string)

            1/

            // 插入的 IL 指令
            var newInstructions = new List<CodeInstruction>
            {
                // 检查 isAI
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, isAiField),
                new CodeInstruction(OpCodes.Brfalse_S, instructionList[0].labels[0]),

                // 检查 Fighting->NetworkPlayer 是否存在
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, fightingField),
                new CodeInstruction(OpCodes.Ldfld, networkPlayerField),
                new CodeInstruction(OpCodes.Brfalse_S, instructionList[0].labels[0]),

                // 生成随机数
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 800),
                new CodeInstruction(OpCodes.Call, randomRangeMethod),
                new CodeInstruction(OpCodes.Stloc, localNum.LocalIndex),

                // 条件1：随机数 == 0
                new CodeInstruction(OpCodes.Ldloc, localNum.LocalIndex),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Bne_Un_S, instructionList[0].labels[0]),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, fightingField),
                new CodeInstruction(OpCodes.Ldfld, networkPlayerField),
                new CodeInstruction(OpCodes.Ldfld, chatManagerField),
                new CodeInstruction(OpCodes.Ldstr, "Ouch!"),
                new CodeInstruction(OpCodes.Callvirt, talkMethod),

                // 条件2：随机数 == 500
                new CodeInstruction(OpCodes.Ldloc, localNum.LocalIndex),
                new CodeInstruction(OpCodes.Ldc_I4, 500),
                new CodeInstruction(OpCodes.Bne_Un_S, instructionList[0].labels[0]),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, fightingField),
                new CodeInstruction(OpCodes.Ldfld, networkPlayerField),
                new CodeInstruction(OpCodes.Ldfld, chatManagerField),
                new CodeInstruction(OpCodes.Ldstr, "Existence is pain!"),
                new CodeInstruction(OpCodes.Callvirt, talkMethod),

                // 条件3：随机数 == 100
                new CodeInstruction(OpCodes.Ldloc, localNum.LocalIndex),
                new CodeInstruction(OpCodes.Ldc_I4_S, 100),
                new CodeInstruction(OpCodes.Bne_Un_S, instructionList[0].labels[0]),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, fightingField),
                new CodeInstruction(OpCodes.Ldfld, networkPlayerField),
                new CodeInstruction(OpCodes.Ldfld, chatManagerField),
                new CodeInstruction(OpCodes.Ldstr, "That's monk-y business!"),
                new CodeInstruction(OpCodes.Callvirt, talkMethod)
            };

            // 在适当位置插入新指令
            instructionList.InsertRange(0, newInstructions);

            return instructionList;
        }
    }
    */
}
