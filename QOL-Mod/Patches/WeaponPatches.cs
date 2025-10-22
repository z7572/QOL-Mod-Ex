using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace QOL.Patches;

class WeaponPatches
{
    public static void Patch(Harmony harmonyInstance)
    {
        var actuallyShootMethod = AccessTools.Method(typeof(Weapon), "ActuallyShoot");
        var actuallyShootMethodPrefix = new HarmonyMethod(typeof(WeaponPatches).GetMethod(nameof(ActuallyShootPrefix)));
        var actualyShootMethodTranspiler = new HarmonyMethod(typeof(WeaponPatches).GetMethod(nameof(ActuallyShootTranspiler)));
        var actuallyShootMethodPostfix = new HarmonyMethod(typeof(WeaponPatches).GetMethod(nameof(ActuallyShootPostfix)));
        harmonyInstance.Patch(actuallyShootMethod, transpiler: actualyShootMethodTranspiler);
        harmonyInstance.Patch(actuallyShootMethod, prefix: actuallyShootMethodPrefix, postfix: actuallyShootMethodPostfix);
    }

    internal struct ActuallyShootState
    {
        internal float recoil;
        internal float spread;
        internal float torsoRecoil;
        internal bool losesWeaponAfterTime;
        internal bool loseWeaponOnReload;
    }

    public static void ActuallyShootPrefix(Weapon __instance, out ActuallyShootState __state, ref bool ___losesWeaponAfterTime)
    {
        __state = new ActuallyShootState
        {
            recoil = __instance.recoil,
            spread = __instance.spread,
            torsoRecoil = __instance.torsoRecoil,
            losesWeaponAfterTime = ___losesWeaponAfterTime,
            loseWeaponOnReload = __instance.loseWeaponOnReload
        };

        if (ChatCommands.CmdDict["norecoil"].IsEnabled)
        {
            __instance.spread = 0;
            __instance.recoil = 0;
            if (ChatCommands.CmdDict["norecoil"].Option != "notorso")
            {
                __instance.torsoRecoil = 0;
            }
        }

        if (ChatCommands.CmdDict["nospread"].IsEnabled)
        {
            __instance.spread = 0;
        }

        if (ChatCommands.CmdDict["switchweapon"].IsEnabled)
        {
            ___losesWeaponAfterTime = false;
            __instance.loseWeaponOnReload = false;

        }
    }

    public static void ActuallyShootPostfix(Weapon __instance, ActuallyShootState __state, ref bool ___losesWeaponAfterTime)
    {
        if (ChatCommands.CmdDict["norecoil"].IsEnabled)
        {
            __instance.recoil = __state.recoil;
            __instance.spread = __state.spread;
            __instance.torsoRecoil = __state.torsoRecoil;
        }

        if (ChatCommands.CmdDict["switchweapon"].IsEnabled)
        {
            ___losesWeaponAfterTime = __state.losesWeaponAfterTime;
            __instance.loseWeaponOnReload = __state.loseWeaponOnReload;
        }
    }

    public static IEnumerable<CodeInstruction> ActuallyShootTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            // Find Instantiate<GameObject>()
            if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("Instantiate"))
            {
                // gameObject = Instantiate(...)
                // SomeMethod(gameObject, this.controller.playerID)
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Dup)); // Copy gameObject
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldarg_0)); // this
                codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Weapon), "controller")));
                codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Controller), "playerID")));
                codes.Insert(i + 5, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WeaponPatches), nameof(ChangeProjectileColor))));
                break;
            }
        }
        return codes;
    }

    private static Color GetTeamColor(int playerID)
    {
        return playerID switch
        {
            // Based on _EmissionColor (PropertyID = 58) == Color(1f, 0.7059f, 0.1765f)
            0 => new Color(0.8f, 0.8f, 0f), // Yellow
            1 => new Color(-0.5f, 0.5f, 2f), // Blue
            2 => new Color(1.4f, 0f, 0.4f), // Red (light)
            3 => new Color(-0.3f, 1f, 0f), // Green
            _ => new Color(0.5f, 1f, 1.7f) // White

            //2 => new Color(1.4f, -0.1f, 0.3f), // Red (pure)
            //2 => new Color(1.5f, 0.24f, 0.786f), // Red (white)
        };
    }

    private static void SetEmissionColor(GameObject projectile, Color color, string targetMat = "yellow")
    {
        foreach (RayCastForward rayCastForward in projectile.GetComponentsInChildren<RayCastForward>())
        {
            foreach (TrailRenderer trailRenderer in rayCastForward.GetComponentsInChildren<TrailRenderer>())
            {
                if (trailRenderer.sharedMaterial == null) return;


                if (trailRenderer.sharedMaterial.name.StartsWith(targetMat))
                {
                    trailRenderer.sharedMaterial.SetColor("_EmissionColor", color);
                }

            }
        }
    }

    private static void ChangeProjectileColor(GameObject projectile, int playerID)
    {
        var newColor = new Color(1f, 0.4759f, 0f);
        var newPartColor = new Color(1f, 0.5992f, 0.2353f);
        var newEmitColor = new Color(1f, 0.7059f, 0.1765f);

        if (!ChatCommands.CmdDict["bulletcolor"].IsEnabled)
        {
            foreach (RayCastForward ray in projectile.GetComponentsInChildren<RayCastForward>())
            {
                ResetColor<TrailRenderer>("yellow", newColor, newEmitColor);
                ResetColor<ParticleSystemRenderer>("yellowBall", newPartColor, newEmitColor);

                void ResetColor<T>(string matName, Color color, Color emitColor) where T : Renderer
                {
                    foreach (var renderer in ray.GetComponentsInChildren<T>())
                    {
                        if (renderer.sharedMaterial == null) continue;
                        if (!renderer.sharedMaterial.name.StartsWith(matName)) continue;
                        if (renderer.sharedMaterial.color == color &&
                            renderer.sharedMaterial.GetColor(58) == emitColor) continue;

                        renderer.sharedMaterial.color = color;
                        renderer.sharedMaterial.SetColor("_EmissionColor", emitColor);
                    }
                }
            }
            return;
        }

        foreach (RayCastForward ray in projectile.GetComponentsInChildren<RayCastForward>())
        {
            SetColor<TrailRenderer>("yellow");
            SetColor<ParticleSystemRenderer>("yellowBall");

            void SetColor<T>(string matName) where T : Renderer
            {
                foreach (var renderer in ray.GetComponentsInChildren<T>())
                {
                    if (renderer.sharedMaterial == null) continue;

                    if (!renderer.sharedMaterial.name.StartsWith(matName)) // In case of " (Instance)"
                    {
                        //Debug.Log($"Projectile material: {trailRenderer.sharedMaterial.name}");
                        continue;
                    }

                    switch (ChatCommands.CmdDict["bulletcolor"].Option)
                    {
                        case "team":
                            newColor = GetTeamColor(playerID);
                            break;

                        case "random":
                            newColor = GetTeamColor(Random.Range(0, 5));
                            break;

                        case "battery":
                            var controller = Helper.controllerHandler.ActivePlayers[playerID];
                            var fighting = Traverse.Create(controller).Field("fighting").GetValue<Fighting>();
                            var weapon = Traverse.Create(fighting).Field("weapon").GetValue<Weapon>();

                            var startBullets = weapon.startBullets;
                            var bulletsLeft = Traverse.Create(fighting).Field("bulletsLeft").GetValue<int>();

                            float ratio = (float)bulletsLeft / startBullets;

                            var green = new HSBColor(100f / 360f, 1f, 1f);
                            var yellow = new HSBColor(50f / 360f, 1f, 1f);
                            var red = new HSBColor(0f / 360f, 0.8f, 1f);

                            if (ratio > 0.5f)
                            {
                                newColor = HSBColor.Lerp(green, yellow, (1f - ratio) * 2f).ToColor();
                            }
                            else
                            {
                                newColor = HSBColor.Lerp(yellow, red, (1f - 2f * ratio)).ToColor();
                            }

                            newEmitColor = newColor;
                            break;

                        case "yellow":
                            newColor = GetTeamColor(0);
                            break;
                        case "blue":
                            newColor = GetTeamColor(1);
                            break;
                        case "red":
                            newColor = GetTeamColor(2);
                            break;
                        case "green":
                            newColor = GetTeamColor(3);
                            break;
                        case "white":
                            newColor = GetTeamColor(4);
                            break;
                    }

                    renderer.sharedMaterial.color = newColor;
                    renderer.sharedMaterial.SetColor("_EmissionColor", newEmitColor);
                    var newMat = new Material(renderer.sharedMaterial) { color = newColor };
                    newMat.SetColor("_EmissionColor", newEmitColor);
                    renderer.material = newMat;
                    //Debug.Log($"Modifying projectile: {lastProjectile.name}");
                    // bullet: yellow, rocket: rocket, laser/god: golden, bounce: purple

                    var cleanup = projectile.gameObject.AddComponent<MaterialCleanup>();
                    cleanup.SetMaterial(newMat);
                }
            }
        }
    }

    class MaterialCleanup : MonoBehaviour
    {
        private Material Material;

        public void SetMaterial(Material mat)
        {
            Material = mat;
        }

        private void OnDestroy()
        {
            if (Material != null)
            {
                Destroy(Material);
            }
        }
    }
}
