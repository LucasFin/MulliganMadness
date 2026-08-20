using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Utils
{
    internal enum ArtFitMode
    {
        Cover,
        Contain
    }

    internal static class CardArtFactory
    {
        private const float ArtWidth = 1100f;
        private const float ArtHeight = 864.9600219726562f;
        private const ArtFitMode DefaultFit = ArtFitMode.Cover;

        private static readonly string ArtFolder = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "Art");

        internal static GameObject Create(string artName) => Create(artName, DefaultFit);

        internal static GameObject Create(string artName, ArtFitMode fit)
        {
            var path = Path.Combine(ArtFolder, artName + ".png");
            if (!File.Exists(path)) return null;

            try
            {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Object.Destroy(texture);
                    return null;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                var root = new GameObject("MM_CardArt_" + artName, typeof(RectTransform), typeof(RectMask2D));
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(ArtWidth, ArtHeight);
                rootRect.localPosition = new Vector3(0f, 0.028729f, 0f);

                var go = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;

                if (fit == ArtFitMode.Cover)
                {
                    image.preserveAspect = true;
                    var frameAspect = ArtWidth / ArtHeight;
                    var spriteAspect = (float)texture.width / texture.height;
                    var scale = spriteAspect >= frameAspect
                        ? ArtHeight / (texture.height / 100f)
                        : ArtWidth / (texture.width / 100f);
                    rect.sizeDelta = new Vector2(texture.width / 100f * scale, texture.height / 100f * scale);
                }
                else
                {
                    rect.sizeDelta = new Vector2(ArtWidth, ArtHeight);
                    image.preserveAspect = true;
                }

                return root;
            }
            catch
            {
                return null;
            }
        }
    }
}
