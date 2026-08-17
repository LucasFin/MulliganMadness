using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal static class StatsUiHelper
    {
        internal static float UiScale => Mathf.Clamp(Screen.height / 1080f, 0.65f, 1.35f) * (Plugin.Configs?.StatsPanelScale.Value ?? 1f);
        internal static float BaseFont => 13.5f * UiScale;
        internal static float HeaderFont => 15f * UiScale;
        internal static float TitleFont => 19f * UiScale;
        internal static float HeroFont => 15.5f * UiScale;

        internal static Color AccentColor => Plugin.Configs == null
            ? new Color(0.35f, 0.82f, 0.72f)
            : new Color(Plugin.Configs.StatsAccentR.Value, Plugin.Configs.StatsAccentG.Value, Plugin.Configs.StatsAccentB.Value, 1f);

        internal static GameObject CreateModernPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, float alpha = 0.88f)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta, new Color(0.05f, 0.07f, 0.10f), alpha);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(root.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 3f);
            accent.GetComponent<Image>().color = AccentColor;
            accent.GetComponent<Image>().raycastTarget = false;

            var border = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            border.transform.SetParent(root.transform, false);
            var borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImage = border.GetComponent<Image>();
            borderImage.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.22f);
            borderImage.raycastTarget = false;

            return root;
        }

        internal static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bg, float alpha = 0.82f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = new Color(bg.r, bg.g, bg.b, alpha);
            image.raycastTarget = false;
            return go;
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.enableWordWrapping = true;
            tmp.lineSpacing = -2f;
            return tmp;
        }

        internal static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Vector2 size)
        {
            var go = new GameObject(label + "_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.21f, 0.96f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.24f, 0.30f, 1f);
            colors.pressedColor = new Color(0.10f, 0.13f, 0.17f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", label, BaseFont * 0.92f, TextAlignmentOptions.Center, new Color(0.88f, 0.93f, 0.98f));
            return button;
        }

        internal static void ApplyRect(GameObject root, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            if (root == null) return;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
        }
    }
}
