using HarmonyLib;
using UnityEngine;

namespace QOL.Trainer.Patches
{
    [HarmonyPatch(typeof(AI))]
    public static class AIPatch
    {
        public class AILogicTracker : MonoBehaviour
        {
            public AILogic Logic;
        }

        // Disable the random nerf to movement force multiplier
        [HarmonyPatch("SetStats")]
        [HarmonyPrefix]
        public static bool SetStatsPrefix(AI __instance) => false;

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool UpdateMethodPrefix(AI __instance,
            ref float ___reactionTime,
            ref float ___velocitySmoothnes,
            ref float ___preferredRange,
            ref float ___heightRange,
            ref bool ___canAttack,
            ref float ___range,
            ref float ___reactionHitReset,
            ref float ___jumpOffset,
            ref float ___targetingSmoothing,
            ref bool ___goForGuns,
            ref bool ___attacking,
            ref bool ___dontAimWhenAttacking,
            ref float ___startAttackDelay,
            ref Transform ___behaviourTarget,
            ref Rigidbody ___target,
            ref float ___velocity,
            ref ControllerHandler ___controllerHandler,
            ref Controller ___controller,
            ref Transform ___aimer,
            ref Fighting ___fighting,
            ref float ___reactionCounter,
            ref Movement ___movement,
            ref Transform ___head,
            ref CharacterInformation ___info,
            ref CharacterInformation ___targetInformation,
            ref float ___counter)
        {
            if (___info.isDead) return false;

            var tracker = __instance.GetComponent<AILogicTracker>();

            if (tracker == null)
            {
                tracker = __instance.gameObject.AddComponent<AILogicTracker>();
                tracker.Logic = new AILogic(__instance);
            }
            else if (tracker.Logic == null)
            {
                tracker.Logic = new AILogic(__instance);
            }

            return tracker.Logic.Update(
                ref ___reactionTime,
                ref ___velocitySmoothnes,
                ref ___preferredRange,
                ref ___heightRange,
                ref ___canAttack,
                ref ___range,
                ref ___reactionHitReset,
                ref ___jumpOffset,
                ref ___targetingSmoothing,
                ref ___goForGuns,
                ref ___attacking,
                ref ___dontAimWhenAttacking,
                ref ___startAttackDelay,
                ref ___behaviourTarget,
                ref ___target,
                ref ___velocity,
                ref ___controllerHandler,
                ref ___controller,
                ref ___aimer,
                ref ___fighting,
                ref ___reactionCounter,
                ref ___movement,
                ref ___head,
                ref ___info,
                ref ___targetInformation,
                ref ___counter
            );
        }
    }
}