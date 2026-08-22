using System.Collections;
using System.Collections.Generic;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class BozoShoesRuntime
    {
        private const string VisualName = "MM_BozoShoes";
        private static readonly HashSet<int> Marked = new HashSet<int>();
        private static Sprite _shoeSprite;

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnClear);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnClear);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
        }

        internal static void Clear()
        {
            Marked.Clear();
            StripAllVisuals();
        }

        internal static bool IsMarked(Player player) =>
            player != null && Marked.Contains(player.playerID);

        internal static void Mark(Player victim)
        {
            if (victim == null) return;
            var first = Marked.Add(victim.playerID);
            AttachShoes(victim);
            if (!first) return;

            var name = victim.data?.view?.Owner?.NickName;
            if (string.IsNullOrEmpty(name)) name = "Player " + (victim.playerID + 1);
            CardTargetUi.ShowToast($"{name} is wearing Bozo Shoes (+50% knockback).");
        }

        private static IEnumerator OnClear(IGameModeHandler gm)
        {
            Clear();
            yield break;
        }

        private static IEnumerator OnPointStart(IGameModeHandler gm)
        {
            yield return null;
            ReapplyVisuals();
        }

        private static void ReapplyVisuals()
        {
            var players = PlayerManager.instance?.players;
            if (players == null) return;
            foreach (var player in players)
            {
                if (IsMarked(player)) AttachShoes(player);
            }
        }

        private static void StripAllVisuals()
        {
            var players = PlayerManager.instance?.players;
            if (players == null) return;
            foreach (var player in players)
            {
                StripShoes(player);
            }
        }

        private static void StripShoes(Player player)
        {
            if (player == null) return;
            var root = FindVisual(player);
            if (root != null) Object.Destroy(root);
        }

        private static GameObject FindVisual(Player player)
        {
            if (player == null) return null;
            var t = Body(player);
            var found = t.Find(VisualName);
            return found != null ? found.gameObject : null;
        }

        private static Transform Body(Player player)
        {
            if (player.data?.playerVel != null) return player.data.playerVel.transform;
            return player.transform;
        }

        private static void AttachShoes(Player player)
        {
            if (player == null) return;
            var body = Body(player);
            if (FindVisual(player) != null) return;

            var root = new GameObject(VisualName);
            root.transform.SetParent(body, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var radius = 0.5f;
            var col = player.data?.mainCol as CircleCollider2D
                      ?? body.GetComponent<CircleCollider2D>()
                      ?? body.GetComponentInChildren<CircleCollider2D>();
            if (col != null) radius = Mathf.Max(0.25f, col.radius);

            var sort = 20;
            var layer = "Default";
            var bodySprite = body.GetComponent<SpriteRenderer>() ?? body.GetComponentInChildren<SpriteRenderer>();
            if (bodySprite != null)
            {
                sort = bodySprite.sortingOrder + 8;
                layer = bodySprite.sortingLayerName;
            }

            var sprite = ShoeSprite();
            var scale = radius * 1.45f;
            AddShoe(root.transform, sprite, new Vector3(-radius * 0.52f, -radius * 0.78f, 0f), 16f, new Vector3(scale, scale, 1f), sort, layer);
            AddShoe(root.transform, sprite, new Vector3(radius * 0.52f, -radius * 0.78f, 0f), -16f, new Vector3(-scale, scale, 1f), sort, layer);
        }

        private static void AddShoe(Transform parent, Sprite sprite, Vector3 localPos, float zRot, Vector3 localScale, int sort, string layer)
        {
            var go = new GameObject("Shoe", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
            go.transform.localScale = localScale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sort;
            sr.sortingLayerName = layer;
            sr.color = Color.white;
        }

        private static Sprite ShoeSprite()
        {
            if (_shoeSprite != null) return _shoeSprite;

            const int w = 96;
            const int h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var cx = (w - 1) * 0.5f;
            var cy = (h - 1) * 0.48f;
            var rx = w * 0.46f;
            var ry = h * 0.40f;
            var cream = new Color(1f, 0.93f, 0.82f, 1f);
            var red = new Color(0.93f, 0.10f, 0.16f, 1f);
            var yellow = new Color(1f, 0.84f, 0.18f, 1f);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var nx = (x - cx) / rx;
                    var ny = (y - cy) / ry;
                    var d = nx * nx + ny * ny;
                    if (d > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color c;
                    if (d > 0.78f) c = cream;
                    else if (ny > -0.15f && ny < 0.22f && Mathf.Abs(nx) < 0.72f) c = yellow;
                    else c = red;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            Object.DontDestroyOnLoad(tex);
            _shoeSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 64f);
            return _shoeSprite;
        }
    }
}
