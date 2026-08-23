using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal static class StatsUiHelper
    {
        private static Canvas _overlay;
        private static Sprite _roundedSprite;

        internal static float UiScale => 1f;
        internal static float BaseFont => 13.5f * UiScale;
        internal static float HeaderFont => 15f * UiScale;
        internal static float TitleFont => 18f * UiScale;
        internal static float HeroFont => 15.5f * UiScale;

        internal static Color AccentColor => new Color(0.78f, 0.80f, 0.84f);
        internal static Color TitleColor => new Color(0.93f, 0.94f, 0.96f, 0.98f);
        internal static Color HintColor => new Color(0.84f, 0.87f, 0.92f, 0.96f);
        internal static Color PanelColor => new Color(0.07f, 0.08f, 0.10f, 0.90f);
        internal static Color BlockColor => new Color(1f, 1f, 1f, 0.04f);
        internal static Color ButtonFill => new Color(1f, 1f, 1f, 0.07f);

        internal static Transform OverlayRoot
        {
            get
            {
                EnsureOverlayCanvas();
                return _overlay.transform;
            }
        }

        internal static Vector2 OverlaySize
        {
            get
            {
                EnsureOverlayCanvas();
                var size = _overlay.GetComponent<RectTransform>().rect.size;
                if (size.x < 8f || size.y < 8f) return new Vector2(1920f, 1080f);
                return size;
            }
        }

        internal static bool OverlayReady
        {
            get
            {
                EnsureOverlayCanvas();
                return _overlay != null;
            }
        }

        private static void EnsureOverlayCanvas()
        {
            if (_overlay != null) return;

            var go = new GameObject("MM_StatsOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);
            _overlay = go.GetComponent<Canvas>();
            _overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlay.sortingOrder = 300;
            _overlay.pixelPerfect = false;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
        }

        internal static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;

                const int size = 64;
                const int radius = 14;
                var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                var pixels = new Color[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, RoundedAlpha(x + 0.5f, y + 0.5f, size, radius));
                    }
                }

                tex.SetPixels(pixels);
                tex.Apply(false, false);
                _roundedSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                _roundedSprite.name = "MM_RoundedPanel";
                return _roundedSprite;
            }
        }

        private static float RoundedAlpha(float x, float y, int size, int radius)
        {
            var innerLeft = radius;
            var innerRight = size - radius;
            var innerBottom = radius;
            var innerTop = size - radius;

            if (x >= innerLeft && x <= innerRight) return x >= 0f && x <= size && y >= 0f && y <= size ? 1f : 0f;
            if (y >= innerBottom && y <= innerTop) return x >= 0f && x <= size && y >= 0f && y <= size ? 1f : 0f;

            var cx = x < innerLeft ? innerLeft : innerRight;
            var cy = y < innerBottom ? innerBottom : innerTop;
            var dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - dist + 0.5f);
        }

        internal static void ApplyRounded(Image image, Color color)
        {
            if (image == null) return;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        internal static GameObject CreateModernPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, float alpha = 0.88f)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta, new Color(0.05f, 0.07f, 0.10f), alpha);
            CreateAccentBar(root.transform, "AccentTop", top: true);
            CreateAccentBar(root.transform, "AccentBottom", top: false);
            return root;
        }

        internal static GameObject CreateGlassPanel(Transform parent, string name, Vector2 anchoredPos, Vector2 sizeDelta, float alpha = 0.90f)
        {
            var go = CreatePanel(
                parent,
                name,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                anchoredPos,
                sizeDelta,
                PanelColor,
                alpha);
            ApplyRounded(go.GetComponent<Image>(), new Color(PanelColor.r, PanelColor.g, PanelColor.b, alpha));
            go.GetComponent<Image>().raycastTarget = false;
            return go;
        }

        internal static void SetAccentVisible(GameObject root, bool visible)
        {
            if (root == null) return;
            var top = root.transform.Find("AccentTop") ?? root.transform.Find("Accent");
            var bottom = root.transform.Find("AccentBottom");
            if (top != null) top.gameObject.SetActive(visible);
            if (bottom != null) bottom.gameObject.SetActive(visible);
        }

        private static void CreateAccentBar(Transform parent, string name, bool top)
        {
            var accent = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(parent, false);
            var accentRect = accent.GetComponent<RectTransform>();
            if (top)
            {
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(1f, 0f);
                accentRect.pivot = new Vector2(0.5f, 0f);
            }

            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 3f);
            accent.GetComponent<Image>().color = AccentColor;
            accent.GetComponent<Image>().raycastTarget = false;
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
            tmp.lineSpacing = 0f;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        internal static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Vector2 size)
        {
            return CreateGhostButton(parent, label, onClick, size);
        }

        internal static Button CreateGhostButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Vector2 size)
        {
            var go = new GameObject(label + "_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            ApplyRounded(image, ButtonFill);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            CreateText(go.transform, "Label", label, BaseFont * 0.88f, TextAlignmentOptions.Center, new Color(0.86f, 0.88f, 0.91f, 0.95f));
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
