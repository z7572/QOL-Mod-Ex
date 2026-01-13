using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace QOL.Patches;

[HarmonyPatch(typeof(Controller))]
class ControllerPatches
{
    public static void Patch(Harmony harmonyInstance) // Controller methods to patch with the harmony instance
    {
        var onTakeDamageMethod = AccessTools.Method(typeof(Controller), "OnTakeDamage");
        var onTakeDamageMethodPostfix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(OnTakeDamageMethodPostfix)));
        harmonyInstance.Patch(onTakeDamageMethod, postfix: onTakeDamageMethodPostfix);

        var onDeathMethod = AccessTools.Method(typeof(Controller), "OnDeath");
        var onDeathMethodMethodPostfix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(OnDeathMethodMethodPostfix)));
        harmonyInstance.Patch(onDeathMethod, postfix: onDeathMethodMethodPostfix);

        var updateMethod = AccessTools.Method(typeof(Controller), "Update");
        var updateMethodPostfix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(UpdateMethodPostfix)));
        harmonyInstance.Patch(updateMethod, postfix: updateMethodPostfix);

        var lateUpdateMethod = AccessTools.Method(typeof(Controller), "LateUpdate");
        var lateUpdateMethodPrefix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(LateUpdateMethodPrefix)));
        harmonyInstance.Patch(lateUpdateMethod, prefix: lateUpdateMethodPrefix);
    }

    public static void OnTakeDamageMethodPostfix(Controller __instance) // Postfix method for OnTakeDamage()
    {
        if (!ChatCommands.CmdDict["ouchmsg"].IsEnabled ||
            !__instance.HasControl)
            return;

        // The max is exclusive, hence no len(OuchPhrases) - 1
        var ouchPhrases = ConfigHandler.OuchPhrases;
        var randWord = ouchPhrases[Random.Range(0, ouchPhrases.Length)];

        Helper.SendPublicOutput(randWord);
    }

    public static void OnDeathMethodMethodPostfix(Controller __instance) // Postfix method for OnDeath()
    {
        if (!ChatCommands.CmdDict["deathmsg"].IsEnabled || !__instance.HasControl)
            return;

        var randIndex = Random.Range(0, ConfigHandler.DeathPhrases.Length);
        Helper.SendPublicOutput(ConfigHandler.DeathPhrases[randIndex]);
    }

    public static void UpdateMethodPostfix(Controller __instance)
    {
        if (!CheatHelper.CheatEnabled) return;

        if (__instance.HasControl && !__instance.IsAI())
        {
            if (!ChatManager.isTyping)
            {
                var mPlayerActions = Traverse.Create(__instance).Field("mPlayerActions").GetValue<CharacterActions>();
                if (mPlayerActions.Throw.WasPressed && __instance.canFly && ChatCommands.CmdDict["fly"].IsEnabled)
                {
                    __instance.Throw();
                }
            }
        }
    }

    public static void LateUpdateMethodPrefix(Controller __instance, CharacterInformation ___info)
    {
        var localPlayerID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
        if (__instance.inactive || __instance.playerID != localPlayerID || !GameManager.inFight)
            return;

        if (___info.isDead)
        {
            GameManagerPatches.TimeDead += Time.deltaTime;
            return;
        }

        GameManagerPatches.TimeAlive += Time.deltaTime;
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostfix(Controller __instance)
    {
        if (/* !__instance.isAI && */!__instance.name.ToLower().Contains("snake"))
        {
            if (__instance.gameObject.GetComponent<EdgeArrowManager>() == null)
            {
                __instance.gameObject.AddComponent<EdgeArrowManager>();
                Debug.Log("Added EdgeArrowManager");
            }
            if (__instance.gameObject.GetComponent<HPBarManager>() == null)
            {
                __instance.gameObject.AddComponent<HPBarManager>();
                Debug.Log("Added HPBarManager");
            }
        }
        if (__instance.HasControl && !__instance.IsAI()) // me
        {
            Helper.controller = __instance;

            if (CheatHelper.CheatEnabled)
            {
                __instance.gameObject.AddComponent<CheatManager>();
                Debug.Log("Added CheatManager");
            }

            var barsHandler = Object.FindObjectOfType<BarsHandler>();
            if (barsHandler != null)
            {
                barsHandler.gameObject.SetActive(false);
                Debug.Log("Disabled Loading Bars!");
            }
        }
    }
}