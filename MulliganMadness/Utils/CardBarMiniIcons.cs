using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Utils
{
    internal static class CardBarMiniIcons
    {
        private const string ChildName = "MM_MiniIcon";
        private static readonly Dictionary<string, GameObject> FancyPrefabs = new Dictionary<string, GameObject>();
        private static GameObject _fancyHolder;
        private static Type _fancyIconType;
        private static bool _fancyResolved;

        internal static void ApplyToLatestButton(CardBar bar)
        {
            if (bar == null) return;
            var t = bar.transform;
            if (t.childCount < 1) return;
            var go = t.GetChild(t.childCount - 1).gameObject;
            var button = go.GetComponent<CardBarButton>();
            if (button == null) return;
            var cardField = AccessTools.Field(typeof(CardBarButton), "card");
            var card = cardField?.GetValue(button) as CardInfo;
            Apply(go, card);
        }

        internal static void Apply(GameObject button, CardInfo card)
        {
            if (button == null || card == null) return;
            CardArtFactory.TryAssignSprite(card);
            AttachFancyIcon(card);

            var sprite = SpriteFor(card);
            if (sprite == null) return;

            foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.enabled = false;
                tmp.text = "";
            }

            // FancyCardBar's I_RGB / RGBColorShift rainbow overlays muddy our stickers.
            StripFancyOverlays(button);

            // Prefer a single clean overlay — do NOT stamp every Image (that paints the
            // chromatic RGB copies FancyCardBar may have added).
            EnsureCleanOverlay(button, sprite);

            if (card.sprite == null)
                card.sprite = sprite;
        }

        internal static Sprite SpriteFor(CardInfo card)
        {
            if (card == null) return null;
            if (card.cardArt != null)
            {
                var tag = card.cardArt.GetComponent<MmCardArtTag>();
                if (tag != null && !string.IsNullOrEmpty(tag.ArtName))
                {
                    var mini = CardArtFactory.GetMiniSprite(tag.ArtName);
                    if (mini != null) return mini;
                }
            }

            return card.sprite;
        }

        /// <summary>
        /// FancyCardBar looks for FancyIcon on the CardInfo GameObject and instantiates
        /// fancyIcon into the bar button — that skips the muddy "generate from cardArt" path.
        /// </summary>
        internal static void AttachFancyIcon(CardInfo info)
        {
            if (info == null) return;
            var fancyType = FancyIconType();
            if (fancyType == null) return;

            string artName = null;
            if (info.cardArt != null)
            {
                var tag = info.cardArt.GetComponent<MmCardArtTag>();
                if (tag != null) artName = tag.ArtName;
            }

            if (string.IsNullOrEmpty(artName)) return;
            var prefab = GetOrCreateFancyPrefab(artName);
            if (prefab == null) return;

            var existing = info.gameObject.GetComponent(fancyType);
            if (existing == null)
                existing = info.gameObject.AddComponent(fancyType);
            var field = AccessTools.Field(fancyType, "fancyIcon");
            field?.SetValue(existing, prefab);
        }

        private static Type FancyIconType()
        {
            if (_fancyResolved) return _fancyIconType;
            _fancyResolved = true;
            _fancyIconType = AccessTools.TypeByName("FancyCardBar.FancyIcon");
            return _fancyIconType;
        }

        private static GameObject GetOrCreateFancyPrefab(string artName)
        {
            if (FancyPrefabs.TryGetValue(artName, out var cached) && cached != null)
                return cached;

            var sprite = CardArtFactory.GetMiniSprite(artName);
            if (sprite == null) return null;

            // FancyCardBar Instantiates this prefab; inactive templates stay inactive.
            // Keep the icon GO active under a hidden inactive holder.
            if (!_fancyHolder)
            {
                _fancyHolder = new GameObject("MM_FancyBarIconHolder");
                UnityEngine.Object.DontDestroyOnLoad(_fancyHolder);
                _fancyHolder.SetActive(false);
            }

            var go = new GameObject(
                "MM_FancyBarIcon_" + artName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(_fancyHolder.transform, false);
            go.SetActive(true);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(128f, 128f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.overrideSprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            FancyPrefabs[artName] = go;
            return go;
        }

        private static void StripFancyOverlays(GameObject button)
        {
            if (button == null) return;

            // Disable rainbow hue shifters (I_RGB).
            foreach (var mb in button.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                if (mb.GetType().Name != "RGBColorShift") continue;
                mb.enabled = false;
                if (mb.gameObject != button)
                    mb.gameObject.SetActive(false);
            }

            // Hide auto-generated blankIcon clones that nest a shrunk full cardArt copy.
            // Keep our MM_MiniIcon and any FancyIcon instance that is just an Image sprite.
            for (var i = button.transform.childCount - 1; i >= 0; i--)
            {
                var child = button.transform.GetChild(i);
                if (child == null) continue;
                if (child.name == ChildName) continue;
                if (child.name.StartsWith("MM_FancyBarIcon_", StringComparison.Ordinal)) continue;

                // genIcons path: blank template containing Instantiated cardArt (has MmCardArtTag).
                if (child.GetComponentInChildren<MmCardArtTag>(true) != null)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                // Extra Image stacks FancyCardBar sometimes leaves behind — mute non-ours.
                // Leave the button's own Graphic (frame) alone.
            }
        }

        private static void EnsureCleanOverlay(GameObject button, Sprite sprite)
        {
            var existing = button.transform.Find(ChildName);
            Image ours;
            if (existing == null)
            {
                var go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(button.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = new Vector2(3f, 3f);
                rt.offsetMax = new Vector2(-3f, -3f);
                ours = go.GetComponent<Image>();
                ours.raycastTarget = false;
                ours.preserveAspect = true;
            }
            else
            {
                ours = existing.GetComponent<Image>();
                existing.SetAsLastSibling();
            }

            if (ours == null) return;
            ours.sprite = sprite;
            ours.overrideSprite = sprite;
            ours.color = Color.white;
            ours.enabled = true;
            ours.gameObject.SetActive(true);
        }
    }
}
