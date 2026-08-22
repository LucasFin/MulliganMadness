using System.Collections;
using TMPro;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal static class PickAnnounceUi
    {
        private static Overlay _overlay;

        internal static void BroadcastPanic(int playerId, float seconds)
        {
            NetworkingManager.RPC(typeof(PickAnnounceUi), nameof(RPCA_PanicTimer), playerId, seconds);
        }

        internal static void HidePanic(bool broadcast = false)
        {
            _overlay?.HidePanic();
            if (broadcast)
            {
                NetworkingManager.RPC(typeof(PickAnnounceUi), nameof(RPCA_HidePanicTimer));
            }
        }

        internal static void ShowTookAll(int playerId)
        {
            EnsureOverlay();
            _overlay?.ShowTookAll(playerId);
        }

        [UnboundRPC]
        public static void RPCA_PanicTimer(int playerId, float seconds)
        {
            EnsureOverlay();
            _overlay?.ShowPanic(playerId, seconds);
        }

        [UnboundRPC]
        public static void RPCA_HidePanicTimer()
        {
            _overlay?.HidePanic();
        }

        private static void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var go = new GameObject("MM_PickAnnounceUi", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            _overlay = go.AddComponent<Overlay>();
            _overlay.Build();
            go.SetActive(false);
        }

        private sealed class Overlay : MonoBehaviour
        {
            private CanvasGroup _group;
            private Image _bg;
            private Image _barFill;
            private GameObject _barRoot;
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _subtitle;
            private Coroutine _fade;
            private float _panicExpiresAt;
            private float _panicDuration;
            private int _panicPlayerId = -1;
            private bool _panicActive;

            internal void Build()
            {
                var rect = gameObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -36f);
                rect.sizeDelta = new Vector2(620f, 92f);

                _group = gameObject.AddComponent<CanvasGroup>();
                _group.blocksRaycasts = false;
                _group.interactable = false;
                _group.alpha = 0f;

                _bg = gameObject.AddComponent<Image>();
                _bg.raycastTarget = false;

                _title = CreateText("Title", new Vector2(16f, -8f), new Vector2(-16f, -42f), 26f, FontStyles.Bold);
                _subtitle = CreateText("Subtitle", new Vector2(16f, -42f), new Vector2(-16f, -70f), 18f, FontStyles.Normal);

                _barRoot = new GameObject("Bar", typeof(RectTransform), typeof(Image));
                _barRoot.transform.SetParent(transform, false);
                var barRect = _barRoot.GetComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0f, 0f);
                barRect.anchorMax = new Vector2(1f, 0f);
                barRect.pivot = new Vector2(0.5f, 0f);
                barRect.anchoredPosition = new Vector2(0f, 10f);
                barRect.sizeDelta = new Vector2(-32f, 8f);
                _barRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
                _barRoot.GetComponent<Image>().raycastTarget = false;

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(_barRoot.transform, false);
                var fillRect = fillGo.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(1f, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillRect.pivot = new Vector2(0f, 0.5f);
                _barFill = fillGo.GetComponent<Image>();
                _barFill.raycastTarget = false;
            }

            internal void ShowPanic(int playerId, float seconds)
            {
                _panicPlayerId = playerId;
                _panicDuration = Mathf.Max(0.1f, seconds);
                _panicExpiresAt = Time.unscaledTime + _panicDuration;
                _panicActive = true;

                var rect = gameObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(620f, 92f);
                if (_barRoot != null) _barRoot.SetActive(true);

                var name = PlayerLabel(playerId);
                var local = IsLocal(playerId);
                _title.text = local ? "PANIC PICK" : $"{name.ToUpperInvariant()} · PANIC PICK";
                ApplyPanicColors(1f);
                RefreshPanic();
                ShowImmediate();
            }

            internal void HidePanic()
            {
                if (!_panicActive) return;
                _panicActive = false;
                _panicPlayerId = -1;
                FadeOut(0.15f);
            }

            internal void ShowTookAll(int playerId)
            {
                _panicActive = false;
                _panicPlayerId = -1;
                if (_barRoot != null) _barRoot.SetActive(false);

                var name = PlayerLabel(playerId);
                _title.text = $"{name.ToUpperInvariant()} TOOK ALL";
                _subtitle.text = "Grabbed every card in the offer";
                _title.color = new Color(1f, 0.92f, 0.55f, 1f);
                _subtitle.color = new Color(0.95f, 0.88f, 0.70f, 0.95f);
                if (_bg != null) _bg.color = new Color(0.12f, 0.08f, 0.02f, 0.94f);

                var rect = gameObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(680f, 86f);

                ShowImmediate();
                FadeOutAfter(2.8f);
            }

            private void Update()
            {
                if (!_panicActive || !gameObject.activeSelf) return;
                RefreshPanic();
                if (Time.unscaledTime >= _panicExpiresAt)
                {
                    _panicActive = false;
                    FadeOut(0.2f);
                }
            }

            private void RefreshPanic()
            {
                var remaining = Mathf.Max(0f, _panicExpiresAt - Time.unscaledTime);
                var t = _panicDuration > 0.01f ? remaining / _panicDuration : 0f;
                _subtitle.text = $"{remaining:0.0}s to pick";
                ApplyPanicColors(t);
                if (_barFill != null)
                {
                    _barFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
                }
            }

            private void ApplyPanicColors(float t)
            {
                var hot = t < 0.35f;
                if (_bg != null)
                {
                    _bg.color = hot
                        ? new Color(0.22f, 0.05f, 0.04f, 0.94f)
                        : new Color(0.16f, 0.08f, 0.02f, 0.92f);
                }

                _title.color = hot
                    ? new Color(1f, 0.55f, 0.45f, 1f)
                    : new Color(1f, 0.78f, 0.40f, 1f);
                _subtitle.color = new Color(1f, 0.92f, 0.82f, 0.95f);
                if (_barFill != null)
                {
                    _barFill.color = hot
                        ? new Color(0.95f, 0.28f, 0.22f, 1f)
                        : new Color(0.95f, 0.72f, 0.28f, 1f);
                }
            }

            private void ShowImmediate()
            {
                if (_fade != null)
                {
                    StopCoroutine(_fade);
                    _fade = null;
                }

                gameObject.SetActive(true);
                _group.alpha = 1f;
            }

            private void FadeOutAfter(float delay)
            {
                if (_fade != null) StopCoroutine(_fade);
                _fade = StartCoroutine(FadeRoutine(delay, 0.25f));
            }

            private void FadeOut(float duration)
            {
                if (_fade != null) StopCoroutine(_fade);
                _fade = StartCoroutine(FadeRoutine(0f, duration));
            }

            private IEnumerator FadeRoutine(float delay, float duration)
            {
                if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
                var start = _group != null ? _group.alpha : 1f;
                var elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (_group != null) _group.alpha = Mathf.Lerp(start, 0f, elapsed / duration);
                    yield return null;
                }

                if (_group != null) _group.alpha = 0f;
                if (!_panicActive) gameObject.SetActive(false);
                _fade = null;
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
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                return tmp;
            }

            private static string PlayerLabel(int playerId)
            {
                if (PlayerManager.instance?.players != null)
                {
                    foreach (var player in PlayerManager.instance.players)
                    {
                        if (player == null || player.playerID != playerId) continue;
                        var name = player.data?.view?.Owner?.NickName;
                        if (!string.IsNullOrEmpty(name)) return name;
                        return "Player " + (playerId + 1);
                    }
                }

                return "Player " + (playerId + 1);
            }

            private static bool IsLocal(int playerId)
            {
                if (PlayerManager.instance?.players == null) return false;
                foreach (var player in PlayerManager.instance.players)
                {
                    if (player == null || player.playerID != playerId) continue;
                    return player.data?.view != null && player.data.view.IsMine;
                }

                return false;
            }
        }
    }
}
