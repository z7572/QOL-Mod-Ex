using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

public class CheatTextManager : MonoBehaviour
{
    private struct FeatureData
    {
        public string Info;
        public Command Cmd;
    }

    private static readonly Dictionary<string, FeatureData> activeFeatures = new();
    private static TextMeshProUGUI cheatTextTMP;
    private static TextMeshProUGUI cheatTextTMP2;

    private void Start()
    {
        CreateTextObject("CheatText", out cheatTextTMP);
        CreateTextObject("CheatText2", out cheatTextTMP2);
        UpdateCheatText();
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

    public static void ToggleFeature(Command cmd, bool isEnabled, string name = null, string info = "")
    {
        string key = !string.IsNullOrEmpty(name) ? name : cmd?.Name;

        if (string.IsNullOrEmpty(key)) return;

        if (isEnabled)
        {
            var data = new FeatureData
            {
                Info = info,
                Cmd = cmd
            };

            if (activeFeatures.ContainsKey(key))
            {
                activeFeatures[key] = data;
            }
            else
            {
                activeFeatures.Add(key, data);
            }
        }
        else
        {
            activeFeatures.Remove(key);
        }

        UpdateCheatText();
    }

    public static void ClearCheats()
    {
        var commandsToDisable = new List<Command>();

        foreach (var data in activeFeatures.Values)
        {
            var cmd = data.Cmd;
            if (cmd != null && cmd.IsCheat && cmd.IsToggle && cmd.IsEnabled)
            {
                commandsToDisable.Add(cmd);
            }
        }
        foreach (var cmd in commandsToDisable)
        {
            try
            {
                cmd.Execute();

                if (cmd.IsEnabled)
                {
                    cmd.Execute();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to disable cheat command: {cmd.Name}" +
                    $"\n{e.GetType().Name}: {e.Message}");
            }
            if (cmd.IsEnabled)
            {
                Debug.LogWarning($"Cannot disable cheat command {cmd.Name}, force disabling...");
                cmd.IsEnabled = false;
                var keysToRemove = activeFeatures
                    .Where(kv => kv.Value.Cmd == cmd)
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var key in keysToRemove)
                {
                    activeFeatures.Remove(key);
                }
            }
        }
        UpdateCheatText();
    }

    private static void UpdateCheatText()
    {
        var featuresList = activeFeatures
            .Select(kv => FormatFeatureText(kv.Key, kv.Value.Info))
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
    private float hueSpeed = 0.12f;

    private void Start()
    {
        m_TextComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        m_hueOffset += hueSpeed * Time.deltaTime;
        m_hueOffset = Mathf.Repeat(m_hueOffset, 1f);

        for (var i = 0; i < m_TextComponent.textInfo.characterCount; ++i)
        {
            var charInfo = m_TextComponent.textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;
            if (charInfo.color != Color.white) continue;

            var meshIndex = m_TextComponent.textInfo.characterInfo[i].materialReferenceIndex;
            var vertexIndex = m_TextComponent.textInfo.characterInfo[i].vertexIndex;

            if (m_TextComponent.textInfo.meshInfo[meshIndex].colors32 == null ||
                vertexIndex + 3 >= m_TextComponent.textInfo.meshInfo[meshIndex].colors32.Length) continue;

            var vertexColors = m_TextComponent.textInfo.meshInfo[meshIndex].colors32;

            var hue = Mathf.Repeat(m_hueOffset + (i * colorSpacing), 1f);
            var hsbColor = new HSBColor(hue, 0.5f, 1f);
            var myColor32 = hsbColor.ToColor();

            vertexColors[vertexIndex + 0] = myColor32;
            vertexColors[vertexIndex + 1] = myColor32;
            vertexColors[vertexIndex + 2] = myColor32;
            vertexColors[vertexIndex + 3] = myColor32;
        }

        if (string.IsNullOrEmpty(m_TextComponent.text)) return;
        m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}