using MulliganMadness.Utils;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal static class TakeAllVoteUi
    {
        private static VoteOverlay _overlay;

        internal static void ShowVote(int requesterId, float expiresAt, bool mercy = false)
        {
            EnsureOverlay();
            _overlay?.Show(requesterId, expiresAt, mercy);
        }

        internal static void Hide()
        {
            if (_overlay != null) _overlay.Hide();
        }

        private static void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var go = new GameObject("MM_TakeAllVoteUi", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            _overlay = go.AddComponent<VoteOverlay>();
            _overlay.Build();
            go.SetActive(false);
        }

        private sealed class VoteOverlay : MonoBehaviour
        {
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _subtitle;
            private GameObject _buttonsRow;

            internal void Build()
            {
                var rect = gameObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 110f);
                rect.sizeDelta = new Vector2(420f, 120f);

                var bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.06f, 0.10f, 0.14f, 0.96f);

                _title = CreateText("Title", new Vector2(12f, -8f), new Vector2(-12f, -38f), 22f, FontStyles.Bold);
                _subtitle = CreateText("Subtitle", new Vector2(12f, -40f), new Vector2(-12f, -62f), 15f, FontStyles.Normal);

                _buttonsRow = new GameObject("Buttons", typeof(RectTransform));
                _buttonsRow.transform.SetParent(transform, false);
                var rowRect = _buttonsRow.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 0f);
                rowRect.anchorMax = new Vector2(1f, 0f);
                rowRect.pivot = new Vector2(0.5f, 0f);
                rowRect.anchoredPosition = new Vector2(0f, 10f);
                rowRect.sizeDelta = new Vector2(-24f, 44f);

                CreateButton("Yes (Y)", 0f, () => TakeAllVoteManager.SubmitLocalVote(true));
                CreateButton("No (N)", 0.52f, () => TakeAllVoteManager.SubmitLocalVote(false));
            }

            internal void Show(int requesterId, float expiresAt, bool mercy)
            {
                var requester = FindPlayer(requesterId);
                var name = requester?.data?.view?.Owner?.NickName;
                if (string.IsNullOrEmpty(name)) name = "Player " + (requesterId + 1);

                var local = FindLocalPlayer();
                var isRequester = local != null && local.playerID == requesterId;
                var isVoter = local != null && local.playerID != requesterId;

                _title.text = mercy
                    ? (isRequester ? "Mercy Take All vote" : $"{name} wants mercy Take All")
                    : (isRequester ? "Take All vote requested" : $"{name} wants Take All");
                _subtitle.text = isRequester
                    ? "Waiting for other players..."
                    : $"Accept? ({Mathf.Max(0, Mathf.CeilToInt(expiresAt - Time.unscaledTime))}s)";

                if (_buttonsRow != null) _buttonsRow.SetActive(isVoter);
                gameObject.SetActive(true);
            }

            internal void Hide() => gameObject.SetActive(false);

            private void Update()
            {
                if (!gameObject.activeSelf) return;
                if (Input.GetKeyDown(KeyCode.Y)) TakeAllVoteManager.SubmitLocalVote(true);
                if (Input.GetKeyDown(KeyCode.N)) TakeAllVoteManager.SubmitLocalVote(false);
            }

            private TextMeshProUGUI CreateText(string name, Vector2 offsetMin, Vector2 offsetMax, float size, FontStyles style)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = size;
                tmp.fontStyle = style;
                tmp.alignment = TextAlignmentOptions.TopLeft;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                return tmp;
            }

            private void CreateButton(string label, float anchorX, UnityEngine.Events.UnityAction onClick)
            {
                var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_buttonsRow.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(anchorX, 0f);
                rect.anchorMax = new Vector2(anchorX + 0.48f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                go.GetComponent<Image>().color = new Color(0.12f, 0.42f, 0.28f, 1f);
                var button = go.GetComponent<Button>();
                button.onClick.AddListener(onClick);

                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 18f;
                tmp.fontStyle = FontStyles.Bold;
            }

            private static Player FindPlayer(int id)
            {
                foreach (var player in PlayerManager.instance.players)
                {
                    if (player != null && player.playerID == id) return player;
                }

                return null;
            }

            private static Player FindLocalPlayer()
            {
                foreach (var player in PlayerManager.instance.players)
                {
                    if (player?.data?.view != null && player.data.view.IsMine) return player;
                }

                return null;
            }
        }
    }
}
