using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL
{
    public class CheatTextManager : MonoBehaviour
    {
        private static List<string> activeFeatures = new List<string>();
        private static TextMeshProUGUI cheatTextTMP;
        private static TextMeshProUGUI cheatTextTMP2;

        private void Start()
        {
            CreateTextObject("CheatText", out cheatTextTMP);
            CreateTextObject("CheatText2", out cheatTextTMP2);
            UpdateCheatText();
        }

        private void Update()
        {
            var rbColor = HSBColor.ToColor(new HSBColor(Mathf.PingPong(Time.time * RainbowManager.Speed, 1), 1, 1));
            cheatTextTMP.color = rbColor;
            cheatTextTMP2.color = rbColor;
        }

        private void CreateTextObject(string name, out TextMeshProUGUI tmpComponent)
        {
            var cheatText = new GameObject(name);
            var canvas = cheatText.AddComponent<Canvas>();
            var canvasScaler = cheatText.AddComponent<CanvasScaler>();
            tmpComponent = cheatText.AddComponent<TextMeshProUGUI>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            tmpComponent.fontSizeMax = 25;
            tmpComponent.fontSize = 25;
            tmpComponent.enableAutoSizing = true;
            tmpComponent.color = Color.green;
            tmpComponent.fontStyle = FontStyles.Bold;
            tmpComponent.alignment = TextAlignmentOptions.TopRight;
            tmpComponent.richText = true;
        }

        public static void ToggleFeature(string featureName, bool isEnabled)
        {
            if (isEnabled)
            {
                if (!activeFeatures.Contains(featureName))
                    activeFeatures.Add(featureName);
            }
            else
            {
                activeFeatures.Remove(featureName);
            }

            UpdateCheatText();
        }

        private static void UpdateCheatText()
        {
            activeFeatures = activeFeatures
            .OrderByDescending(GetTextWidth)
            .ThenBy(s => s) // If width is equal, sort by name
            .ToList();
            activeFeatures.Insert(0, "");
            cheatTextTMP.text = string.Join("\n",activeFeatures.ToArray());
            cheatTextTMP2.text = string.Join("\n",activeFeatures.ToArray());
        }
        private static float GetTextWidth(string text)
        {
            if (cheatTextTMP == null) return text.Length;
            return cheatTextTMP.GetPreferredValues(text).x;
        }
    }
}