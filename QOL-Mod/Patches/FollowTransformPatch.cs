using HarmonyLib;
using UnityEngine;

namespace QOL.Patches
{
    [HarmonyPatch(typeof(FollowTransform))]
    class FollowTransformPatch
    {
        // Since we removed loading bars, we can use this to adjust chat bubble caps based on screen aspect ratio
        // Useful for non-16:9 screens to prevent chat bubbles from being too narrow or too short
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix(FollowTransform __instance)
        {
            if (__instance.cap == Vector2.zero || __instance.capTop == Vector2.zero) return;

            float widthScale = Helper.ScreenWidthScaleFactor;
            float heightScale = Helper.ScreenHeightScaleFactor;

            __instance.cap = new Vector2(
                __instance.cap.x * widthScale,
                __instance.cap.y * heightScale
            );

            __instance.capTop = new Vector2(
                __instance.capTop.x * widthScale,
                __instance.capTop.y * heightScale
            );
        }
    }
}
