using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

public class CheatTextManager : MonoBehaviour
{
    private static readonly Dictionary<string, string> activeFeatures = new();
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
        //var rbColor = new HSBColor(Mathf.PingPong(Time.time * RainbowManager.Speed, 1), 1, 1).ToColor();
        //cheatTextTMP.color = rbColor;
        //cheatTextTMP2.color = rbColor;
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

        //tmpComponent.overrideColorTags = true;
        cheatText.AddComponent<RainbowText>();
        //tmpComponent.font = Resources.Load<TMP_FontAsset>("fonts & materials/roboto-bold sdf");
        tmpComponent.fontSizeMax = 25;
        tmpComponent.fontSize = 25;
        tmpComponent.enableAutoSizing = true;
        tmpComponent.fontStyle = FontStyles.Bold;
        tmpComponent.alignment = TextAlignmentOptions.TopRight;
        tmpComponent.richText = true;
    }

    public static void ToggleFeature(string featureName, bool isEnabled, string featureInfo = "")
    {
        if (isEnabled)
        {
            if (activeFeatures.ContainsKey(featureName))
            {
                activeFeatures[featureName] = featureInfo;
            }
            else
            {
                activeFeatures.Add(featureName, featureInfo);
            }
        }
        else
        {
            activeFeatures.Remove(featureName);
        }

        UpdateCheatText();
    }

    private static void UpdateCheatText()
    {
        var featuresList = activeFeatures
            .Select(kv => FormatFeatureText(kv.Key, kv.Value))
            .OrderByDescending(GetTextWidth)
            .ThenBy(s => s)
            .ToList();

        featuresList.Insert(0, "");
        
        var text = string.Join("\n", featuresList.ToArray());
        cheatTextTMP.text = text;
        cheatTextTMP2.text = text;
    }

    private static string FormatFeatureText(string featureName, string featureInfo)
    {
        if (string.IsNullOrEmpty(featureInfo))
            return featureName;

        return $"{featureName}<color=#808080> {featureInfo}</color>";
    }

    private static float GetTextWidth(string text)
    {
        if (cheatTextTMP == null) return text.Length;
        return Mathf.RoundToInt(cheatTextTMP.GetPreferredValues(text).x);
    }
}

public class RainbowText : MonoBehaviour
{
    private TextMeshProUGUI m_TextComponent;
    private float m_hueOffset;
    private float colorSpacing = 0.01f;
    private float hueSpeed = 0.002f;

    private void Start()
    {
        m_TextComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {

        m_hueOffset += hueSpeed;
        m_hueOffset = Mathf.Repeat(m_hueOffset, 1f);

        for (var i = 0; i < m_TextComponent.textInfo.characterCount; ++i)
        {
            var charInfo = m_TextComponent.textInfo.characterInfo[i];
            if (charInfo.color != Color.white) continue;

            //var hue = (_hueOffset + i * colorSpacing) % 1f;
            var hue = Mathf.Repeat(m_hueOffset + (i * colorSpacing), 1f);
            var hsbColor = new HSBColor(hue, 0.5f, 1f);
            var myColor32 = hsbColor.ToColor();

            var meshIndex = m_TextComponent.textInfo.characterInfo[i].materialReferenceIndex;
            var vertexIndex = m_TextComponent.textInfo.characterInfo[i].vertexIndex;
            var vertexColors = m_TextComponent.textInfo.meshInfo[meshIndex].colors32;
            vertexColors[vertexIndex + 0] = myColor32;
            vertexColors[vertexIndex + 1] = myColor32;
            vertexColors[vertexIndex + 2] = myColor32;
            vertexColors[vertexIndex + 3] = myColor32;
        }

        if (string.IsNullOrEmpty(m_TextComponent.text)) return;
        m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}