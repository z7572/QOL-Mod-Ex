using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using System.Collections;

namespace QOL.Patches;

class FightingPatches
{
    public static void Patch(Harmony harmonyInstance)
    {
        var pickUpWeaponMethod = AccessTools.Method(typeof(Fighting), "PickUpWeapon");
        var pickUpWeaponMethodPostfix = new HarmonyMethod(typeof(FightingPatches).GetMethod(nameof(PickUpWeaponMethodPostfix)));
        harmonyInstance.Patch(pickUpWeaponMethod, postfix: pickUpWeaponMethodPostfix);

        var attackMethod = AccessTools.Method(typeof(Fighting), "Attack");
        var attackMethodPrefix = new HarmonyMethod(typeof(FightingPatches).GetMethod(nameof(AttackMethodPrefix)));
        harmonyInstance.Patch(attackMethod, prefix: attackMethodPrefix);

        var grabWeaponMethod = AccessTools.Method(typeof(Fighting), "GrabWeapon");
        var grabWeaponPrefix = new HarmonyMethod(typeof(FightingPatches).GetMethod(nameof(GrabWeaponMethodPrefix)));
        harmonyInstance.Patch(grabWeaponMethod, prefix: grabWeaponPrefix);

        var attachHandMethod = AccessTools.Method(typeof(Fighting), "AttachHand");
        var attachHandPrefix = new HarmonyMethod(typeof(FightingPatches).GetMethod(nameof(AttachHandMethodPrefix)));
        harmonyInstance.Patch(attachHandMethod, prefix: attachHandPrefix);
    }

    public static void PickUpWeaponMethodPostfix(Fighting __instance, ref Weapon ___weapon)
    {
        if (__instance.gameObject.GetComponent<CheatManager>() == null) return;

        CheatHelper.SwitcherWeaponIndex = __instance.CurrentWeaponIndex;
        if (ChatCommands.CmdDict["fullauto"].IsEnabled)
        {
            __instance.fullAuto = true;
        }
        if (ChatCommands.CmdDict["switchweapon"].IsEnabled || ChatCommands.CmdDict["infiniteammo"].IsEnabled)
        {
            bool losesWeaponAfterTime = Traverse.Create(___weapon).Field("losesWeaponAfterTime").GetValue<bool>();
            if (losesWeaponAfterTime)
            {
                ___weapon.loseWeaponCurrentTime = float.MinValue; // Boss weapons
            }
            if (___weapon.isActive)
            {
                ___weapon.secondsOfUseLeft = float.MaxValue; // Fan
            }
        }
    }

    public static void AttackMethodPrefix(Fighting __instance, ref Weapon ___weapon, ref int ___bulletsLeft)
    {
        if (__instance.gameObject.GetComponent<CheatManager>() == null) return;

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
            ___bulletsLeft++;
        }

    }

    public static void GrabWeaponMethodPrefix(Fighting __instance)
    {
        if (__instance.gameObject.GetComponent<CheatManager>() == null) return;

        if (ChatCommands.CmdDict["quickdraw"].IsEnabled)
        {
            originalFullAuto = __instance.weapon.fullAuto;
            __instance.fullAuto = true;
        }
    }

    public static void AttachHandMethodPrefix(Fighting __instance)
    {
        if (__instance.gameObject.GetComponent<CheatManager>() == null) return;

        if (ChatCommands.CmdDict["quickdraw"].IsEnabled)
        {
            __instance.fullAuto = originalFullAuto;
        }
        if (ChatCommands.CmdDict["fullauto"].IsEnabled)
        {
            __instance.fullAuto = true;
        }
    }

    private static bool originalFullAuto;
}
