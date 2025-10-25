using HarmonyLib;
using LevelEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace QOL.Patches
{
    [HarmonyPatch]
    class LevelCreatorPatches
    {
        [HarmonyPatch(typeof(LevelCreator) , "Start")]
        [HarmonyPostfix]
        private static void StartPostfix(LevelCreator __instance)
        {
            // Below are MainScene's render settings, apply them to LevelEditor so bullet color(goto WeaponPatches) works properly
            RenderSettings.ambientEquatorColor = Color.white;
            RenderSettings.ambientGroundColor = Color.white;
            RenderSettings.ambientLight = Color.white;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientProbe = new SphericalHarmonicsL2();
            RenderSettings.ambientSkyColor = Color.white;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom; // Different from MainScene
            RenderSettings.skybox = Helper.skyBoxMat;
            RenderSettings.sun.enabled = false;
        }
    }
}
