using System.Collections.Generic;
using MulliganMadness.Stats;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal sealed class CompactComparePanel : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _columns;
        private TextMeshProUGUI _hint;
        private readonly List<TextMeshProUGUI> _columnTexts = new List<TextMeshProUGUI>();
        private readonly Dictionary<int, PlayerStatsSnapshot> _baseline = new Dictionary<int, PlayerStatsSnapshot>();

        internal bool CompareActive => _baseline.Count > 0;

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            _root = StatsUiHelper.CreateModernPanel(
                canvas.transform,
                "MM_ComparePanel",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(Plugin.Configs.CompareOffsetX.Value * scale, Plugin.Configs.CompareOffsetY.Value * scale),
                new Vector2(500f * scale, 196f * scale),
                0.90f);

            var header = StatsUiHelper.CreateText(_root.transform, "Header", "<b>Compare</b>", StatsUiHelper.HeaderFont, TextAlignmentOptions.TopLeft, StatsUiHelper.AccentColor);
            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(12f, -10f);
            headerRect.sizeDelta = new Vector2(-150f, 22f);

            var pinBtn = StatsUiHelper.CreateButton(_root.transform, "Pin", PinBaseline, new Vector2(62f, 22f));
            var pinRect = pinBtn.GetComponent<RectTransform>();
            pinRect.anchorMin = new Vector2(1f, 1f);
            pinRect.anchorMax = new Vector2(1f, 1f);
            pinRect.pivot = new Vector2(1f, 1f);
            pinRect.anchoredPosition = new Vector2(-74f, -10f);

            var resetBtn = StatsUiHelper.CreateButton(_root.transform, "Reset", ResetBaseline, new Vector2(62f, 22f));
            var resetRect = resetBtn.GetComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(1f, 1f);
            resetRect.anchorMax = new Vector2(1f, 1f);
            resetRect.pivot = new Vector2(1f, 1f);
            resetRect.anchoredPosition = new Vector2(-10f, -10f);

            var columnsGo = new GameObject("Columns", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            columnsGo.transform.SetParent(_root.transform, false);
            _columns = columnsGo.GetComponent<RectTransform>();
            _columns.anchorMin = new Vector2(0f, 0f);
            _columns.anchorMax = new Vector2(1f, 1f);
            _columns.offsetMin = new Vector2(8f, 24f);
            _columns.offsetMax = new Vector2(-8f, -6f);

            var layout = columnsGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _hint = StatsUiHelper.CreateText(_root.transform, "Hint", "Pin locks baseline · Reset clears", StatsUiHelper.BaseFont * 0.82f, TextAlignmentOptions.BottomRight, new Color(0.68f, 0.74f, 0.80f, 0.92f));
            var hintRect = _hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-10f, 3f);
            hintRect.sizeDelta = new Vector2(-20f, 16f);
        }

        internal void PinBaseline()
        {
            _baseline.Clear();
            foreach (var player in PlayerStatsSnapshot.ActivePlayers())
            {
                if (PlayerStatsSnapshot.TryFrom(player, out var snap))
                {
                    _baseline[snap.PlayerId] = snap;
                }
            }
        }

        internal void ResetBaseline() => _baseline.Clear();

        internal PlayerStatsSnapshot GetLocalBaseline()
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            if (local == null) return null;
            return _baseline.TryGetValue(local.playerID, out var snap) ? snap : null;
        }

        internal void Refresh()
        {
            var enabled = Plugin.Configs.EnableCompactCompare.Value;
            var show = enabled && StatsController.InActiveMatch() && StatsController.TabIsOpen;
            if (!show)
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);

            var scale = StatsUiHelper.UiScale;
            StatsUiHelper.ApplyRect(
                _root,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(Plugin.Configs.CompareOffsetX.Value * scale, Plugin.Configs.CompareOffsetY.Value * scale),
                new Vector2(500f * scale, 196f * scale));

            var players = new List<Player>();
            foreach (var player in PlayerStatsSnapshot.ActivePlayers())
            {
                players.Add(player);
                if (players.Count >= Mathf.Clamp(Plugin.Configs.CompactCompareMaxPlayers.Value, 2, 4)) break;
            }

            while (_columnTexts.Count < players.Count)
            {
                var col = StatsUiHelper.CreatePanel(_columns, $"Col_{_columnTexts.Count}", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.10f, 0.13f), 0.96f);
                col.GetComponent<Image>().raycastTarget = false;
                var text = StatsUiHelper.CreateText(col.transform, "Text", "", StatsUiHelper.BaseFont * 0.9f);
                text.rectTransform.offsetMin = new Vector2(7f, 6f);
                text.rectTransform.offsetMax = new Vector2(-7f, -6f);
                _columnTexts.Add(text);
            }

            for (var i = 0; i < _columnTexts.Count; i++)
            {
                var active = i < players.Count;
                _columnTexts[i].transform.parent.gameObject.SetActive(active);
                if (!active) continue;

                if (!PlayerStatsSnapshot.TryFrom(players[i], out var current))
                {
                    _columnTexts[i].text = "-";
                    continue;
                }

                _baseline.TryGetValue(current.PlayerId, out var baseline);
                _columnTexts[i].text = StatsViewBuilder.BuildCompareColumn(current, baseline);
            }

            _hint.text = CompareActive ? "Baseline pinned" : "Pin locks baseline · Reset clears";
        }
    }
}
