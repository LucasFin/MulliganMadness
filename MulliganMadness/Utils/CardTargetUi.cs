using System;
using System.Collections.Generic;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Utils
{
    internal static class CardTargetUi
    {
        private static Overlay _overlay;

        internal static void OpenSandbag(Player user, Action<Player> onConfirm)
        {
            EnsureOverlay();
            _overlay.OpenTargetOnly(
                user,
                "Sandbag Simulator",
                "Choose who rerolls their current pick hand.",
                "REROLL HAND",
                onConfirm);
        }

        internal static void ShowToast(string message)
        {
            EnsureOverlay();
            _overlay.ShowToast(message);
        }

        internal static void Close()
        {
            if (_overlay != null) _overlay.Close();
        }

        private static void EnsureOverlay()
        {
            if (_overlay != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var go = new GameObject("MM_CardTargetUi", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            _overlay = go.AddComponent<Overlay>();
            _overlay.Build(canvas.transform);
            go.SetActive(false);
        }

        private class Overlay : MonoBehaviour
        {
            private RectTransform _panel;
            private TextMeshProUGUI _title;
            private TextMeshProUGUI _subtitle;
            private TextMeshProUGUI _toast;
            private Transform _playerGrid;
            private Button _confirmButton;
            private Button _cancelButton;
            private Player _actor;
            private Player _selected;
            private Action<Player> _onConfirm;
            private string _confirmLabel;

            internal void Build(Transform canvasRoot)
            {
                var rect = gameObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var dim = CreateImage("Dim", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                dim.color = new Color(0f, 0f, 0f, 0.72f);

                _panel = CreatePanel("Panel", transform, new Vector2(0.5f, 0.5f), new Vector2(680f, 520f));
                var panelBg = _panel.gameObject.AddComponent<Image>();
                panelBg.color = new Color(0.08f, 0.12f, 0.10f, 0.98f);

                _title = CreateText("Title", _panel, new Vector2(24f, -20f), 30f, FontStyles.Bold);
                _subtitle = CreateText("Subtitle", _panel, new Vector2(24f, -58f), 18f, FontStyles.Normal);
                _subtitle.color = new Color(0.85f, 0.88f, 0.82f);

                var gridGo = new GameObject("PlayerGrid", typeof(RectTransform));
                gridGo.transform.SetParent(_panel, false);
                _playerGrid = gridGo.transform;
                var gridRect = gridGo.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0f, 0.22f);
                gridRect.anchorMax = new Vector2(1f, 0.78f);
                gridRect.offsetMin = new Vector2(24f, 0f);
                gridRect.offsetMax = new Vector2(-24f, 0f);
                var layout = gridGo.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(190f, 72f);
                layout.spacing = new Vector2(12f, 12f);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 2;

                _confirmButton = CreateButton("Confirm", _panel, new Vector2(0.56f, 0.08f), _confirmLabel = "CONFIRM");
                _cancelButton = CreateButton("Cancel", _panel, new Vector2(0.08f, 0.08f), "CANCEL");
                _cancelButton.onClick.AddListener(Close);

                _toast = CreateText("Toast", _panel, new Vector2(24f, -90f), 16f, FontStyles.Italic);
                _toast.color = new Color(1f, 0.92f, 0.55f);
                _toast.gameObject.SetActive(false);
            }

            internal void OpenTargetOnly(Player actor, string title, string subtitle, string confirmLabel, Action<Player> onConfirm)
            {
                _actor = actor;
                _onConfirm = onConfirm;
                _confirmLabel = confirmLabel;
                _title.text = title;
                _subtitle.text = subtitle;
                _confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = confirmLabel;
                _selected = actor;
                RebuildPlayerButtons(includeSelf: true, filterStealable: false);
                gameObject.SetActive(true);
            }

            internal void ShowToast(string message)
            {
                if (_toast == null) return;
                _toast.text = message;
                _toast.gameObject.SetActive(true);
                CancelInvoke(nameof(HideToast));
                Invoke(nameof(HideToast), 3f);
            }

            private void HideToast()
            {
                if (_toast != null) _toast.gameObject.SetActive(false);
            }

            internal void Close()
            {
                gameObject.SetActive(false);
                ClearGrid();
            }

            private void RebuildPlayerButtons(bool includeSelf, bool filterStealable)
            {
                ClearGrid();
                if (PlayerManager.instance?.players == null) return;

                foreach (var player in PlayerManager.instance.players)
                {
                    if (player == null) continue;
                    if (!includeSelf && _actor != null && player.playerID == _actor.playerID) continue;

                    var stealable = _actor != null ? StealRules.CountStealableCards(_actor, player) : 0;
                    if (filterStealable && stealable <= 0) continue;

                    var label = BuildPlayerLabel(player, stealable, filterStealable);
                    var button = CreateGridButton(label, player);
                    var captured = player;
                    button.onClick.AddListener(() => SelectPlayer(captured, button));
                }

                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(() =>
                {
                    if (_selected == null) return;
                    _onConfirm?.Invoke(_selected);
                    Close();
                });
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(Close);
            }

            private void SelectPlayer(Player player, Button button)
            {
                _selected = player;
                foreach (Transform child in _playerGrid)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.14f, 0.20f, 0.16f, 1f);
                }

                button.GetComponent<Image>().color = new Color(0.18f, 0.48f, 0.30f, 1f);
            }

            private Button CreateGridButton(string label, Player player)
            {
                var go = new GameObject("Player_" + player.playerID, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_playerGrid, false);
                var image = go.GetComponent<Image>();
                image.color = _selected != null && _selected.playerID == player.playerID
                    ? new Color(0.18f, 0.48f, 0.30f, 1f)
                    : new Color(0.14f, 0.20f, 0.16f, 1f);
                var button = go.GetComponent<Button>();
                var textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var rect = textGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8f, 4f);
                rect.offsetMax = new Vector2(-8f, -4f);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.fontSize = 16f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                return button;
            }

            private static string BuildPlayerLabel(Player player, int stealable, bool showStealable)
            {
                var name = player.data?.view?.Owner?.NickName;
                if (string.IsNullOrEmpty(name)) name = "Player " + (player.playerID + 1);
                if (!showStealable) return name;
                return stealable > 0 ? $"{name}\n{stealable} stealable" : $"{name}\nno cards";
            }

            private void ClearGrid()
            {
                if (_playerGrid == null) return;
                for (var i = _playerGrid.childCount - 1; i >= 0; i--)
                {
                    Destroy(_playerGrid.GetChild(i).gameObject);
                }
            }

            private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = Vector2.zero;
                return rect;
            }

            private static Image CreateImage(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
                return go.GetComponent<Image>();
            }

            private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPos, float size, FontStyles style)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = new Vector2(-48f, 32f);
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = size;
                tmp.fontStyle = style;
                tmp.color = Color.white;
                return tmp;
            }

            private static Button CreateButton(string name, Transform parent, Vector2 anchorMin, string label)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMin;
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(260f, 48f);
                rect.anchoredPosition = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0.12f, 0.42f, 0.28f, 1f);
                var button = go.GetComponent<Button>();
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
                return button;
            }
        }
    }
}
