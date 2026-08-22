using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Utils
{
    internal static class CardBarMiniIcons
    {
        private const string ChildName = "MM_MiniIcon";

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
            var sprite = SpriteFor(card);
            if (sprite == null) return;

            foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
            {
                tmp.enabled = false;
                tmp.text = "";
            }

            // FancyCardBar / vanilla both use Image children — stamp our mini on all of them.
            var images = button.GetComponentsInChildren<Image>(true);
            var stamped = false;
            foreach (var image in images)
            {
                if (image == null) continue;
                image.sprite = sprite;
                image.overrideSprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                image.enabled = true;
                stamped = true;
            }

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
                rt.offsetMin = new Vector2(2f, 2f);
                rt.offsetMax = new Vector2(-2f, -2f);
                ours = go.GetComponent<Image>();
                ours.raycastTarget = false;
                ours.preserveAspect = true;
            }
            else
            {
                ours = existing.GetComponent<Image>();
                existing.SetAsLastSibling();
            }

            if (ours != null)
            {
                ours.sprite = sprite;
                ours.overrideSprite = sprite;
                ours.color = Color.white;
                ours.enabled = true;
                stamped = true;
            }

            if (!stamped && card.sprite == null)
            {
                card.sprite = sprite;
            }
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
    }
}
