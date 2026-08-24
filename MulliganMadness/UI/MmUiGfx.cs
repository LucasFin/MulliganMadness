using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    /// <summary>
    /// Solid-color UI images. Unity's default UISprite is 9-sliced, so a colored
    /// Image without a sprite gets a rounded frame that does not match the rect.
    /// </summary>
    internal static class MmUiGfx
    {
        private static Sprite _white;

        internal static Sprite White
        {
            get
            {
                if (_white != null) return _white;
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Point;
                tex.SetPixel(0, 0, Color.white);
                tex.Apply(false, true);
                tex.hideFlags = HideFlags.HideAndDontSave;
                _white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        internal static Image Solid(Image image, Color color, bool raycast = false)
        {
            if (image == null) return null;
            image.sprite = White;
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        internal static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
