using System.Collections.Generic;
using MulliganMadness.Stats;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal sealed class StatsTabOverlay : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _title;
        private RectTransform _content;
        private readonly List<TextMeshProUGUI> _playerBlocks = new List<TextMeshProUGUI>();
        internal bool IsOpen { get; private set; }

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            _root = StatsUiHelper.CreateModernPanel(
                canvas.transform,
                "MM_StatsTab",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(Mathf.Min(Screen.width * 0.88f, 960f * scale), Mathf.Min(Screen.height * 0.82f, 700f * scale)),
                0.95f);

            _title = StatsUiHelper.CreateText(_root.transform, "Title", "Player Stats", StatsUiHelper.TitleFont, TextAlignmentOptions.TopLeft, StatsUiHelper.AccentColor);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(14f, -10f);
            titleRect.sizeDelta = new Vector2(-120f, 30f);

            var closeBtn = StatsUiHelper.CreateButton(_root.transform, "Close", () => SetOpen(false), new Vector2(72f, 26f));
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-12f, -10f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(12f, 12f);
            scrollRect.offsetMax = new Vector2(-12f, -44f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewport.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            _root.SetActive(false);
        }

        internal void SetOpen(bool open)
        {
            EnsureBuilt();
            IsOpen = open;
            if (_root != null) _root.SetActive(open && Plugin.Configs.EnableStatsTab.Value);
            if (open) Refresh();
        }

        internal void Toggle() => SetOpen(!IsOpen);

        internal void Refresh()
        {
            if (_root == null || !_root.activeSelf) return;

            var players = new List<Player>();
            foreach (var player in PlayerStatsSnapshot.ActivePlayers()) players.Add(player);

            while (_playerBlocks.Count < players.Count)
            {
                var blockGo = StatsUiHelper.CreatePanel(_content, $"Player_{_playerBlocks.Count}", Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 160f), new Color(0.08f, 0.10f, 0.13f), 0.98f);
                var le = blockGo.AddComponent<LayoutElement>();
                le.minHeight = 150f;
                le.preferredHeight = 190f;
                var text = StatsUiHelper.CreateText(blockGo.transform, "Text", "", StatsUiHelper.BaseFont);
                text.rectTransform.offsetMin = new Vector2(10f, 8f);
                text.rectTransform.offsetMax = new Vector2(-10f, -8f);
                _playerBlocks.Add(text);
            }

            for (var i = 0; i < _playerBlocks.Count; i++)
            {
                var active = i < players.Count;
                _playerBlocks[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue;

                if (!PlayerStatsSnapshot.TryFrom(players[i], out var snap))
                {
                    _playerBlocks[i].text = "Unavailable";
                    continue;
                }

                _playerBlocks[i].text = StatsViewBuilder.BuildTabBlock(snap, TabInfoBridge.GetExtensionStats(players[i]));
            }

            _title.text = $"Player Stats · R{StatsController.CurrentRound} P{StatsController.CurrentPoint} · {players.Count} players";
        }
    }
}
