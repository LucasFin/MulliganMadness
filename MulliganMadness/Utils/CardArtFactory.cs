using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Utils
{
    internal static class CardArtFactory
    {
        // Match KeysCards / Root card-art canvas (C_* prefab root RectTransform).
        private const float ArtWidth = 1100f;
        private const float ArtHeight = 864.9600219726562f;

        private static readonly string ArtFolder = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "Art");

        internal static GameObject Create(string artName)
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

                var go = new GameObject("MM_CardArt_" + artName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(ArtWidth, ArtHeight);
                rect.localPosition = new Vector3(0f, 0.028729f, 0f);

                var image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;

                return go;
            }
            catch
            {
                return null;
            }
        }
    }
}
