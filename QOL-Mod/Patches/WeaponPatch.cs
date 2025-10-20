using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class WeaponPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var actuallyShootMethod = AccessTools.Method(typeof(Weapon), "ActuallyShoot");
            var actuallyShootMethodPrefix = new HarmonyMethod(typeof(WeaponPatch).GetMethod(nameof(ActuallyShootPrefix)));
            var actualyShootMethodTranspiler = new HarmonyMethod(typeof(WeaponPatch).GetMethod(nameof(ActuallyShootTranspiler)));
            var actuallyShootMethodPostfix = new HarmonyMethod(typeof(WeaponPatch).GetMethod(nameof(ActuallyShootPostfix)));
            harmonyInstance.Patch(actuallyShootMethod, transpiler: actualyShootMethodTranspiler);
            harmonyInstance.Patch(actuallyShootMethod, prefix: actuallyShootMethodPrefix, postfix: actuallyShootMethodPostfix);
        }

        public static void ActuallyShootPrefix(Weapon __instance)
        {
            if (ChatCommands.CmdDict["norecoil"].IsEnabled)
            {
                __instance.spread = 0;
                __instance.recoil = 0;
                //__instance.torsoRecoil = 0;
            }

        }

        public static void ActuallyShootPostfix(Weapon __instance)
        {
            if (__instance.isBossOnly)
            {
                foreach (Controller controller in GameManager.Instance.playersAlive)
                {
                    if (controller.canFly && !controller.GetComponent<CharacterInformation>().isDead)
                    {
                        return;
                    }
                }
            }

            TeamHolder teamHolder = lastProjectile.GetComponent<TeamHolder>();
            Color newColor = GetTeamColor(teamHolder.team);

            foreach (RayCastForward rayCastForward in lastProjectile.GetComponentsInChildren<RayCastForward>())
            {
                foreach (TrailRenderer trailRenderer in rayCastForward.GetComponentsInChildren<TrailRenderer>())
                {
                    if (trailRenderer.sharedMaterial == null) return;

                    if (trailRenderer.sharedMaterial.name == "yellow")
                    {
                        trailRenderer.sharedMaterial.color = newColor;
                        //Debug.Log($"Modifying projectile: {lastProjectile.name}");
                    }
                    else
                    {
                        //Debug.Log($"Projectile material: {trailRenderer.sharedMaterial.name}");
                    }
                }
            }

            lastProjectile = null;
        }

        public static IEnumerable<CodeInstruction> ActuallyShootTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var instList = new List<CodeInstruction>(instructions);
            MethodInfo instantiateMethod = AccessTools.Method(typeof(UnityEngine.Object), "Instantiate", new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion) });

            // Store last projectile
            for (int i = 0; i < instList.Count; i++)
            {
                //if (instList[i].Calls(instantiateMethod)) // Find Instantiate<GameObject>()
                if (i == 100) // TODO: Use Calls() instead of index
                {
                    // 100: gameObject = Instantiate(...)
                    instList.Insert(i + 1, new CodeInstruction(OpCodes.Dup)); // Copy gameObject
                    instList.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WeaponPatch), nameof(StoreProjectile))));
                    break;
                }
            }
            return instList;
        }

        private static Color GetTeamColor(int team)
        {
            if (!ChatCommands.CmdDict["bulletcolor"].IsEnabled)
            {
                return new Color(1f, 0.4759f, 0f); // Default color
            }

            return team switch
            {
                0 => new Color(0.8f, 0.8f, 0f), // Yellow
                1 => new Color(-0.5f, 0.5f, 2f), // Blue
                //2 => new Color(1.4f, -0.1f, 0.3f), // Red (dark) 
                2 => new Color(1.4f, 0f, 0.4f), // Red (light)
                3 => new Color(-0.3f, 1f, 0f), // Green
                _ => new Color(0.5f, 1f, 1.7f) // White
            };
        }

        private static void StoreProjectile(GameObject projectile)
        {
            lastProjectile = projectile;
        }

        private static GameObject lastProjectile;
    }
}
