using MulliganMadness.Curses;
using MulliganMadness.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Patches
{
    internal sealed class BlindDraftController : MonoBehaviour
    {
        private const string OverlayName = "MM_BlindBack";

        private void LateUpdate()
        {
            var picking = CardChoice.instance != null && CardChoice.instance.IsPicking;
            var spawned = TakeAllManager.GetSpawnedCards();
            if (spawned == null) return;

            var blind = picking && CurseOwnership.LocalPickerHas(BlindDraft.Card);
            if (!blind)
            {
                ClearOverlays(spawned);
                return;
            }

            foreach (var go in spawned)
            {
                if (go != null) EnsureOverlay(go);
            }
        }

        private static void EnsureOverlay(GameObject card)
        {
            foreach (var t in card.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == OverlayName) return;
            }

            var canvas = card.GetComponentInChildren<Canvas>(true);
            Transform parent = canvas != null ? canvas.transform : card.transform;

            var go = new GameObject(OverlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.06f, 0.18f, 0.96f);
            image.raycastTarget = false;

            var textGo = new GameObject("Mark", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "?";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 160f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.92f, 0.82f, 1f, 0.95f);
            tmp.raycastTarget = false;
        }

        private static void ClearOverlays(System.Collections.Generic.List<GameObject> spawned)
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t.name == OverlayName)
                    {
                        Destroy(t.gameObject);
                    }
                }
            }
        }
    }
}
