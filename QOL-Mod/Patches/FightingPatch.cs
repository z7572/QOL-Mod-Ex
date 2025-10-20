using HarmonyLib;

namespace QOL
{
    class FightingPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var pickUpWeaponMethod = AccessTools.Method(typeof(Fighting), "PickUpWeapon");
            var pickUpWeaponMethodPostfix = new HarmonyMethod(typeof(FightingPatch).GetMethod(nameof(PickUpWeaponMethodPostfix)));
            harmonyInstance.Patch(pickUpWeaponMethod, postfix: pickUpWeaponMethodPostfix);

            var attackMethod = AccessTools.Method(typeof(Fighting), "Attack");
            var attackMethodPrefix = new HarmonyMethod(typeof(FightingPatch).GetMethod(nameof(AttackMethodPrefix)));
            harmonyInstance.Patch(attackMethod, prefix: attackMethodPrefix);
        }

        public static void PickUpWeaponMethodPostfix(Fighting __instance)
        {
            Helper.SwitcherWeaponIndex = __instance.CurrentWeaponIndex;
            if (ChatCommands.CmdDict["fullauto"].IsEnabled)
            {
                __instance.fullAuto = true;
            }
        }

        public static void AttackMethodPrefix(Fighting __instance, ref Weapon ___weapon, ref int ___bulletsLeft)
        {
            if (___weapon != null)
            {
                if (ChatCommands.CmdDict["fastfire"].IsEnabled) __instance.counter = 10f;
            }
            else
            {
                if (ChatCommands.CmdDict["fastpunch"].IsEnabled) __instance.counter = 10f;
            }

            if (ChatCommands.CmdDict["infiniteammo"].IsEnabled)
            {
                ___bulletsLeft ++;
            }

        }
    }
}
