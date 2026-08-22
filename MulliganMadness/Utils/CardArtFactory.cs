using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal sealed class MmCardArtTag : MonoBehaviour
    {
        public string ArtName;
    }

    internal static class CardArtFactory
    {
        // Match ROUNDS center art slot; Cover-fit into ~1100x865 pick frames.
        private const float TargetWorldWidth = 11f;
        private const float TargetWorldHeight = 8.65f;

        private static readonly string ArtFolder = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "Art");

        private static readonly Dictionary<string, Sprite> FullSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> MiniSprites = new Dictionary<string, Sprite>();

        internal static GameObject Create(string artName)
        {
            if (string.IsNullOrEmpty(artName)) return null;

            var sprite = GetFullSprite(artName);
            if (sprite == null) return null;

            // Fresh instance every time — Toggle Cards / picks Instantiate or reparent art.
            var root = new GameObject("MM_CardArt_" + artName);
            Object.DontDestroyOnLoad(root);
            root.SetActive(false);

            var tag = root.AddComponent<MmCardArtTag>();
            tag.ArtName = artName;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 1;

            var bounds = sprite.bounds.size;
            if (bounds.x > 0.01f && bounds.y > 0.01f)
            {
                var scale = Mathf.Min(TargetWorldWidth / bounds.x, TargetWorldHeight / bounds.y);
                root.transform.localScale = new Vector3(scale, scale, 1f);
            }

            root.SetActive(true);
            return root;
        }

        internal static Sprite GetMiniSprite(string artName)
        {
            if (string.IsNullOrEmpty(artName)) return null;
            if (MiniSprites.TryGetValue(artName, out var cached) && cached != null) return cached;

            var miniPath = Path.Combine(ArtFolder, artName + "_mini.png");
            var path = File.Exists(miniPath) ? miniPath : Path.Combine(ArtFolder, artName + ".png");
            if (!File.Exists(path)) return null;

            try
            {
                var texture = LoadTexture(path);
                if (texture == null) return null;
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                MiniSprites[artName] = sprite;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        internal static void TryAssignSprite(CardInfo info)
        {
            if (info == null) return;
            string artName = null;
            if (info.cardArt != null)
            {
                var tag = info.cardArt.GetComponent<MmCardArtTag>();
                if (tag != null) artName = tag.ArtName;
            }

            if (string.IsNullOrEmpty(artName)) return;
            var mini = GetMiniSprite(artName);
            if (mini != null) info.sprite = mini;
        }

        internal static void BindLoadedCardInfos()
        {
            foreach (var info in Resources.FindObjectsOfTypeAll<CardInfo>())
                TryAssignSprite(info);
        }

        private static Sprite GetFullSprite(string artName)
        {
            if (FullSprites.TryGetValue(artName, out var cached) && cached != null) return cached;

            var path = Path.Combine(ArtFolder, artName + ".png");
            if (!File.Exists(path)) return null;

            try
            {
                var texture = LoadTexture(path);
                if (texture == null) return null;
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                FullSprites[artName] = sprite;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        private static Texture2D LoadTexture(string path)
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
            Object.DontDestroyOnLoad(texture);
            return texture;
        }
    }
}
