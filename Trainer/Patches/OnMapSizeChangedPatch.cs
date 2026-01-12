using HarmonyLib;

namespace QOL.Trainer.Patches
{
    [HarmonyPatch(typeof(GameManager), "OnMapSizeChanged")]
    public static class GameManagerPatch
    {
        [HarmonyPostfix]
        public static void OnMapSizeChangedPostfix(float newSize)
        {
            AILogic.MapSizeMultiplier = newSize / 10f;
        }
    }
}