using System.Collections.Generic;
using System.Linq;
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
        private TextMeshProUGUI _hint;
        private RectTransform _content;
        private readonly List<TextMeshProUGUI> _playerBlocks = new List<TextMeshProUGUI>();
        private bool _compareMode;
        private int _compareTargetPlayerId = -1;
        private PlayerStatsSnapshot _pinnedLocal;
        private int _pinnedLocalPlayerId = -1;

        internal bool IsOpen { get; private set; }

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            ApplyPanelLayout();

            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0.03f, 0.05f, 0.08f, Plugin.Configs.TabPanelOpacity.Value);

            _title = StatsUiHelper.CreateText(_root.transform, "Title", "Player Stats", StatsUiHelper.TitleFont * 1.05f, TextAlignmentOptions.TopLeft, StatsUiHelper.AccentColor);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(14f, -10f);
            titleRect.sizeDelta = new Vector2(-14f, 28f);

            _hint = StatsUiHelper.CreateText(_root.transform, "Hint", "", StatsUiHelper.BaseFont * 0.82f, TextAlignmentOptions.TopLeft, new Color(0.68f, 0.74f, 0.80f, 0.92f));
            var hintRect = _hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(14f, -36f);
            hintRect.sizeDelta = new Vector2(-14f, 18f);

            var closeBtn = StatsUiHelper.CreateButton(_root.transform, "Close (Esc)", () => SetOpen(false), new Vector2(96f, 26f));
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-12f, -8f);

            var compareBtn = StatsUiHelper.CreateButton(_root.transform, "Compare", ToggleCompareMode, new Vector2(88f, 26f));
            var compareRect = compareBtn.GetComponent<RectTransform>();
            compareRect.anchorMin = new Vector2(1f, 1f);
            compareRect.anchorMax = new Vector2(1f, 1f);
            compareRect.pivot = new Vector2(1f, 1f);
            compareRect.anchoredPosition = new Vector2(-112f, -8f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(10f, 10f);
            scrollRect.offsetMax = new Vector2(-10f, -58f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
            scrollGo.GetComponent<Image>().raycastTarget = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-12f, 0f);
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
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var scrollbarGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGo.transform.SetParent(scrollGo.transform, false);
            var scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.sizeDelta = new Vector2(10f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarGo.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.85f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(scrollbarGo.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(8f, 24f);
            handleGo.GetComponent<Image>().color = new Color(0.35f, 0.82f, 0.72f, 0.85f);

            var scrollbar = scrollbarGo.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var rootImage = _root.GetComponent<Image>();
            if (rootImage != null) rootImage.raycastTarget = false;

            _root.SetActive(false);
        }

        internal void RebuildLayout()
        {
            if (_root == null) return;
            ApplyPanelLayout();
            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0.03f, 0.05f, 0.08f, Plugin.Configs.TabPanelOpacity.Value);
        }

        private void ApplyPanelLayout()
        {
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            var width = Plugin.Configs.TabPanelWidth.Value * scale;
            var height = Mathf.Min(Screen.height * Plugin.Configs.TabPanelHeightFraction.Value, 760f * scale);
            var margin = 12f * scale;

            if (_root == null)
            {
                _root = StatsUiHelper.CreateModernPanel(
                    canvas.transform,
                    "MM_StatsTab",
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(0f, 0.5f),
                    Vector2.zero,
                    new Vector2(width, height),
                    Plugin.Configs.TabPanelOpacity.Value);
                return;
            }

            if (Plugin.Configs.TabAnchorLeft.Value)
            {
                StatsUiHelper.ApplyRect(
                    _root,
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Vector2(margin, 0f),
                    new Vector2(width, height));
            }
            else
            {
                StatsUiHelper.ApplyRect(
                    _root,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(Mathf.Min(width * 1.2f, Screen.width * 0.88f), height));
            }
        }

        internal void SetOpen(bool open)
        {
            var allow = open && Plugin.Configs.EnableStatsTab.Value && StatsController.InActiveMatch();
            if (allow) EnsureBuilt();
            IsOpen = allow;
            if (_root != null) _root.SetActive(allow);
            if (allow)
            {
                EnsureCompareTarget();
                Refresh();
            }
        }

        internal void Toggle() => SetOpen(!IsOpen);

        internal void ToggleCompareMode()
        {
            if (!Plugin.Configs.EnableTabCompare.Value) return;
            _compareMode = !_compareMode;
            EnsureCompareTarget();
            Refresh();
        }

        internal void CycleCompareTarget(int direction)
        {
            if (!_compareMode) return;
            var others = GetOtherPlayers();
            if (others.Count == 0) return;

            var index = others.FindIndex(p => p.playerID == _compareTargetPlayerId);
            if (index < 0) index = 0;
            index = (index + direction + others.Count) % others.Count;
            _compareTargetPlayerId = others[index].playerID;
            Refresh();
        }

        internal void PinLocalBaseline()
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            if (local == null || !PlayerStatsSnapshot.TryFrom(local, out var snap)) return;
            _pinnedLocal = snap;
            _pinnedLocalPlayerId = local.playerID;
        }

        internal void ResetPinnedBaseline()
        {
            _pinnedLocal = null;
            _pinnedLocalPlayerId = -1;
        }

        internal void HandleShortcuts()
        {
            if (!IsOpen || !Plugin.Configs.EnableStatsTab.Value) return;

            if (Plugin.Configs.EnableTabCompare.Value)
            {
                if (Input.GetKeyDown(KeyCode.C)) ToggleCompareMode();
                if (_compareMode)
                {
                    if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma)) CycleCompareTarget(-1);
                    if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period)) CycleCompareTarget(1);
                }
            }

            if (Input.GetKeyDown(KeyCode.P)) PinLocalBaseline();
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) ResetPinnedBaseline();
        }

        internal void Refresh()
        {
            if (_root == null || !_root.activeSelf) return;

            RebuildLayout();

            if (_compareMode && Plugin.Configs.EnableTabCompare.Value)
            {
                RefreshCompareView();
                return;
            }

            RefreshAllPlayersView();
        }

        private void RefreshAllPlayersView()
        {
            var players = new List<Player>();
            foreach (var player in PlayerStatsSnapshot.ActivePlayers()) players.Add(player);

            EnsureBlockCount(players.Count);

            for (var i = 0; i < _playerBlocks.Count; i++)
            {
                var active = i < players.Count;
                var block = _playerBlocks[i].transform.parent.gameObject;
                block.SetActive(active);
                if (!active) continue;

                if (!PlayerStatsSnapshot.TryFrom(players[i], out var snap))
                {
                    _playerBlocks[i].text = "Unavailable";
                }
                else
                {
                    var baseline = GetPinnedBaselineFor(players[i]);
                    _playerBlocks[i].text = baseline != null
                        ? StatsViewBuilder.BuildHud(snap, false, baseline, null, TabInfoBridge.GetExtensionStats(players[i]))
                        : StatsViewBuilder.BuildTabBlock(snap, TabInfoBridge.GetExtensionStats(players[i]));
                }

                UpdateBlockHeight(block, _playerBlocks[i]);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _title.text = $"All players · R{StatsController.CurrentRound} P{StatsController.CurrentPoint}";
            _hint.text = "Scroll · C compare vs someone · Esc close";
        }

        private void RefreshCompareView()
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            var others = GetOtherPlayers();
            var opponent = others.Find(p => p.playerID == _compareTargetPlayerId) ?? (others.Count > 0 ? others[0] : null);
            if (opponent != null) _compareTargetPlayerId = opponent.playerID;

            EnsureBlockCount(opponent != null ? 2 : 1);

            for (var i = 0; i < _playerBlocks.Count; i++)
            {
                _playerBlocks[i].transform.parent.gameObject.SetActive(i < (opponent != null ? 2 : 1));
            }

            if (local == null || !PlayerStatsSnapshot.TryFrom(local, out var localSnap))
            {
                _playerBlocks[0].text = "Local player unavailable";
                UpdateBlockHeight(_playerBlocks[0].transform.parent.gameObject, _playerBlocks[0]);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                _title.text = "Compare";
                _hint.text = "No local player";
                return;
            }

            if (opponent == null || !PlayerStatsSnapshot.TryFrom(opponent, out var otherSnap))
            {
                _playerBlocks[0].text = StatsViewBuilder.BuildTabBlock(localSnap, TabInfoBridge.GetExtensionStats(local));
                UpdateBlockHeight(_playerBlocks[0].transform.parent.gameObject, _playerBlocks[0]);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
                _title.text = "Compare";
                _hint.text = "Need another player to compare";
                return;
            }

            var vsBaseline = otherSnap;
            var pinBaseline = _pinnedLocal != null && _pinnedLocalPlayerId == local.playerID ? _pinnedLocal : null;

            _playerBlocks[0].text = StatsViewBuilder.BuildTabCompare(localSnap, vsBaseline, pinBaseline, TabInfoBridge.GetExtensionStats(local));
            UpdateBlockHeight(_playerBlocks[0].transform.parent.gameObject, _playerBlocks[0]);

            _playerBlocks[1].text = StatsViewBuilder.BuildTabBlock(otherSnap, TabInfoBridge.GetExtensionStats(opponent));
            UpdateBlockHeight(_playerBlocks[1].transform.parent.gameObject, _playerBlocks[1]);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _title.text = $"You vs {otherSnap.PlayerName}";
            _hint.text = "Green/red = ahead or behind them · [ ] switch player · C back to all · Esc close";
        }

        private PlayerStatsSnapshot GetPinnedBaselineFor(Player player)
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            if (local == null || player.playerID != local.playerID) return null;
            return _pinnedLocal != null && _pinnedLocalPlayerId == local.playerID ? _pinnedLocal : null;
        }

        private void EnsureCompareTarget()
        {
            if (!_compareMode) return;
            var others = GetOtherPlayers();
            if (others.Count == 0)
            {
                _compareTargetPlayerId = -1;
                return;
            }

            if (others.All(p => p.playerID != _compareTargetPlayerId))
            {
                _compareTargetPlayerId = others[0].playerID;
            }
        }

        private static List<Player> GetOtherPlayers()
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            var list = new List<Player>();
            foreach (var player in PlayerStatsSnapshot.ActivePlayers())
            {
                if (local != null && player.playerID == local.playerID) continue;
                list.Add(player);
            }

            return list;
        }

        private void EnsureBlockCount(int count)
        {
            while (_playerBlocks.Count < count)
            {
                var blockGo = StatsUiHelper.CreatePanel(_content, $"Player_{_playerBlocks.Count}", Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 120f), new Color(0.08f, 0.10f, 0.13f), 0.98f);
                var le = blockGo.AddComponent<LayoutElement>();
                le.minHeight = 96f;
                le.flexibleWidth = 1f;
                var text = StatsUiHelper.CreateText(blockGo.transform, "Text", "", StatsUiHelper.BaseFont);
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Overflow;
                text.rectTransform.anchorMin = new Vector2(0f, 1f);
                text.rectTransform.anchorMax = new Vector2(1f, 1f);
                text.rectTransform.pivot = new Vector2(0.5f, 1f);
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.sizeDelta = new Vector2(-16f, 0f);
                var fitter = text.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                _playerBlocks.Add(text);
            }
        }

        private static void UpdateBlockHeight(GameObject block, TextMeshProUGUI text)
        {
            var layout = block.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(96f, text.preferredHeight + 18f);
            }
        }
    }
}
