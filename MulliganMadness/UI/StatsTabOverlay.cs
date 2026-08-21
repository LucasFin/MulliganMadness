using System.Collections.Generic;
using System.Linq;
using MulliganMadness.Stats;
using TMPro;
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
            if (!StatsUiHelper.OverlayReady) return;

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
            viewport.GetComponent<Image>().raycastTarget = true;
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
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperLeft;

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
            StatsUiHelper.SetAccentVisible(_root, true);
            var top = _root.transform.Find("AccentTop") ?? _root.transform.Find("Accent");
            var bottom = _root.transform.Find("AccentBottom");
            if (top != null) top.GetComponent<Image>().color = StatsUiHelper.AccentColor;
            if (bottom != null) bottom.GetComponent<Image>().color = StatsUiHelper.AccentColor;
        }

        private void ApplyPanelLayout()
        {
            if (!StatsUiHelper.OverlayReady) return;

            var scale = StatsUiHelper.UiScale;
            var canvasSize = StatsUiHelper.OverlaySize;
            var margin = 12f * scale;
            var width = Mathf.Min(Plugin.Configs.TabPanelWidth.Value * scale, canvasSize.x * 0.48f);
            var height = Mathf.Min(
                canvasSize.y * Plugin.Configs.TabPanelHeightFraction.Value,
                canvasSize.y - margin * 2f);

            if (_root == null)
            {
                var left = Plugin.Configs.TabAnchorLeft.Value;
                _root = StatsUiHelper.CreateModernPanel(
                    StatsUiHelper.OverlayRoot,
                    "MM_StatsTab",
                    left ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
                    left ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
                    left ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
                    left ? new Vector2(margin, 0f) : Vector2.zero,
                    new Vector2(left ? width : Mathf.Min(width * 1.2f, canvasSize.x * 0.88f), height),
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
                    new Vector2(Mathf.Min(width * 1.2f, canvasSize.x * 0.88f), height));
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
            }

            RelayoutBlocks();
            _title.text = $"All players · R{StatsController.CurrentRound} P{StatsController.CurrentPoint}";
            _hint.text = "Scroll · C compare vs someone · Esc close";
        }

        private void RefreshCompareView()
        {
            var local = PlayerStatsSnapshot.LocalPlayer();
            if (local == null)
            {
                foreach (var player in PlayerStatsSnapshot.ActivePlayers())
                {
                    local = player;
                    break;
                }
            }

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
                RelayoutBlocks();
                _title.text = "Compare";
                _hint.text = "No local player";
                return;
            }

            if (opponent == null || !PlayerStatsSnapshot.TryFrom(opponent, out var otherSnap))
            {
                _playerBlocks[0].text = StatsViewBuilder.BuildTabBlock(localSnap, TabInfoBridge.GetExtensionStats(local));
                RelayoutBlocks();
                _title.text = "Compare";
                _hint.text = "Need another player to compare";
                return;
            }

            var vsBaseline = otherSnap;
            var pinBaseline = _pinnedLocal != null && _pinnedLocalPlayerId == local.playerID ? _pinnedLocal : null;

            _playerBlocks[0].text = StatsViewBuilder.BuildTabCompare(localSnap, vsBaseline, pinBaseline, TabInfoBridge.GetExtensionStats(local));
            _playerBlocks[1].text = StatsViewBuilder.BuildTabBlock(otherSnap, TabInfoBridge.GetExtensionStats(opponent));
            RelayoutBlocks();
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

            if (local == null && list.Count > 1)
            {
                list.RemoveAt(0);
            }

            return list;
        }

        private void EnsureBlockCount(int count)
        {
            while (_playerBlocks.Count < count)
            {
                var blockGo = new GameObject($"Player_{_playerBlocks.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                blockGo.transform.SetParent(_content, false);
                var blockRect = blockGo.GetComponent<RectTransform>();
                blockRect.anchorMin = new Vector2(0f, 1f);
                blockRect.anchorMax = new Vector2(1f, 1f);
                blockRect.pivot = new Vector2(0.5f, 1f);
                blockRect.sizeDelta = new Vector2(0f, 120f);

                var image = blockGo.GetComponent<Image>();
                image.color = new Color(0.08f, 0.10f, 0.13f, 0.98f);
                image.raycastTarget = false;

                var le = blockGo.GetComponent<LayoutElement>();
                le.minHeight = 96f;
                le.preferredHeight = 120f;
                le.flexibleWidth = 1f;
                le.flexibleHeight = 0f;

                var text = StatsUiHelper.CreateText(blockGo.transform, "Text", "", StatsUiHelper.BaseFont);
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Overflow;
                text.rectTransform.anchorMin = new Vector2(0f, 0f);
                text.rectTransform.anchorMax = new Vector2(1f, 1f);
                text.rectTransform.offsetMin = new Vector2(10f, 8f);
                text.rectTransform.offsetMax = new Vector2(-10f, -8f);
                _playerBlocks.Add(text);
            }
        }

        private void RelayoutBlocks()
        {
            if (_content == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            foreach (var text in _playerBlocks)
            {
                if (text == null || !text.gameObject.activeInHierarchy) continue;
                UpdateBlockHeight(text.transform.parent.gameObject, text);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private static void UpdateBlockHeight(GameObject block, TextMeshProUGUI text)
        {
            text.ForceMeshUpdate();
            var layout = block.GetComponent<LayoutElement>();
            if (layout != null)
            {
                var height = Mathf.Max(96f, text.preferredHeight + 20f);
                layout.minHeight = height;
                layout.preferredHeight = height;
            }
        }
    }
}
