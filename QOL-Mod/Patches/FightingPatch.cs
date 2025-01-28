using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class FightingPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var pickUpWeaponMethod = AccessTools.Method(typeof(Fighting), "PickUpWeapon");
            var pickUpWeaponMethodPostfix = new HarmonyMethod(typeof(FightingPatch).GetMethod(nameof(PickUpWeaponMethodPostfix)));
            harmonyInstance.Patch(pickUpWeaponMethod, postfix: pickUpWeaponMethodPostfix);
        }

        public static void PickUpWeaponMethodPostfix(Fighting __instance)
        {
            Helper.CurrentWeaponIndex = __instance.CurrentWeaponIndex;
        }
    }
}
