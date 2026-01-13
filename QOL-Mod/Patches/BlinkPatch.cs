using System.Collections;
using System.Reflection.Emit;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace QOL.Patches;

// Fixes the wings not disappearing when you leave the blink
[HarmonyPatch(typeof(Blink))]
class BlinkPatch
{
    private static GameObject _newParticles;
    private static List<ParticleSystem.Particle[]> _particleData = [];

    [HarmonyPatch("LeaveTrail")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LeaveTrailTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        for (int i = 0; i < codes.Count; i++)
        {
            // Find Instantiate<GameObject>()
            if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("Instantiate"))
            {
                // SomeMethod(gameObject)
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Dup)); // Copy gameObject
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BlinkPatch), nameof(ClearWings))));
                break;
            }
        }
        return codes;
    }

    public static void ClearWings(GameObject gameObject)
    {
        _particleData.Clear();
        _newParticles = gameObject;
    }

    [HarmonyPatch("LeaveTrail")]
    [HarmonyPostfix]
    private static void LeaveTrailPostfix(Blink __instance, ref Renderers ___rends)
    {
        foreach (var part in ___rends.GetComponentsInChildren<ParticleSystem>())
        {
            var particles = new ParticleSystem.Particle[part.main.maxParticles];
            part.GetParticles(particles);
            _particleData.Add(particles);
            __instance.StartCoroutine(DisableAccel(part));
        }
        for (int i = 0; i < _newParticles.GetComponentsInChildren<ParticleSystem>().Length; i++)
        {
            var particles = _particleData[i];
            var part = _newParticles.GetComponentsInChildren<ParticleSystem>()[i];
            for (int j = 0; j < particles.Length; j++)
            {
                particles[j].startLifetime *= 4f;
                particles[j].remainingLifetime *= 4f;
                particles[j].velocity = Vector3.zero;
            }
            var main = part.main;
            main.loop = false;
            var emission = part.emission;
            emission.enabled = false;
            part.SetParticles(particles, particles.Length);
        }
        _newParticles.AddComponent<DestroyWhenAllChildrenDisabled>();

        // Working at 60 FPS
        static IEnumerator DisableAccel(ParticleSystem part)
        {
            part.Clear();
            var emission = part.emission;
            emission.enabled = false;
            yield return new WaitForSeconds(0.05f);
            emission.enabled = true;
        }
    }

    class DestroyWhenAllChildrenDisabled : MonoBehaviour
    {
        void Update()
        {
            bool isBusy = false;
            var particles = GetComponentsInChildren<ParticleSystem>();
            foreach (var part in particles)
            {
                if (part.particleCount == 0 && !part.IsAlive())
                {
                    part.gameObject.SetActive(false);
                }
                else
                {
                    isBusy = true;
                }
            }

            if (isBusy) return;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!r.enabled) continue;
                if (r is LineRenderer || r is SpriteRenderer)
                {
                    isBusy = true;
                    break;
                }
            }
            if (!isBusy)
            {
                Destroy(gameObject);
            }
        }
    }
}