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

            _title = StatsUiHelper.CreateText(_root.transform, "Title", "Player Stats", StatsUiHelper.TitleFont, TextAlignmentOptions.TopLeft, StatsUiHelper.TitleColor);
            var titleRect = _title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(18f, -12f);
            titleRect.sizeDelta = new Vector2(-18f, 26f);

            _hint = StatsUiHelper.CreateText(_root.transform, "Hint", "", StatsUiHelper.BaseFont * 0.78f, TextAlignmentOptions.TopLeft, StatsUiHelper.HintColor);
            var hintRect = _hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.anchoredPosition = new Vector2(18f, -36f);
            hintRect.sizeDelta = new Vector2(-18f, 16f);

            var closeBtn = StatsUiHelper.CreateGhostButton(_root.transform, "Close", () => SetOpen(false), new Vector2(72f, 24f));
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-14f, -12f);

            var compareBtn = StatsUiHelper.CreateGhostButton(_root.transform, "Compare", ToggleCompareMode, new Vector2(78f, 24f));
            var compareRect = compareBtn.GetComponent<RectTransform>();
            compareRect.anchorMin = new Vector2(1f, 1f);
            compareRect.anchorMax = new Vector2(1f, 1f);
            compareRect.pivot = new Vector2(1f, 1f);
            compareRect.anchoredPosition = new Vector2(-92f, -12f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(_root.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(8f, 10f);
            scrollRect.offsetMax = new Vector2(-8f, -56f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0f);
            scrollBg.raycastTarget = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
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
            vlg.padding = new RectOffset(10, 10, 4, 8);
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
            scroll.verticalScrollbar = null;
            scroll.horizontalScrollbar = null;
            scroll.inertia = true;

            var rootImage = _root.GetComponent<Image>();
            if (rootImage != null) rootImage.raycastTarget = false;

            AddDragHandles();
            _root.SetActive(false);
        }

        internal void RebuildLayout()
        {
            if (_root == null) return;
            ApplyPanelLayout();
            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null)
            {
                StatsUiHelper.ApplyRounded(panelImage, StatsUiHelper.PanelColor);
                panelImage.raycastTarget = false;
            }

            StatsUiHelper.SetAccentVisible(_root, false);
        }

        private void ApplyPanelLayout()
        {
            if (!StatsUiHelper.OverlayReady) return;

            var canvasSize = StatsUiHelper.OverlaySize;
            var margin = 16f;
            var width = Mathf.Clamp(Plugin.Configs.TabPanelWidth.Value, 280f, Mathf.Min(640f, canvasSize.x * 0.55f));
            var height = Mathf.Min(canvasSize.y * 0.74f, canvasSize.y - margin * 2f);
            var pos = GetSavedOrDefaultPosition(canvasSize, width, height);

            if (_root == null)
            {
                _root = StatsUiHelper.CreateGlassPanel(
                    StatsUiHelper.OverlayRoot,
                    "MM_StatsTab",
                    pos,
                    new Vector2(width, height),
                    StatsUiHelper.PanelColor.a);
                return;
            }

            StatsUiHelper.ApplyRect(_root, Vector2.zero, Vector2.zero, Vector2.zero, pos, new Vector2(width, height));
        }

        private static Vector2 GetSavedOrDefaultPosition(Vector2 canvas, float width, float height)
        {
            if (Plugin.Configs.TabPosX.Value < 0f || Plugin.Configs.TabPosY.Value < 0f)
            {
                return new Vector2((canvas.x - width) * 0.5f, (canvas.y - height) * 0.5f);
            }

            return new Vector2(
                Mathf.Clamp(Plugin.Configs.TabPosX.Value, 8f, Mathf.Max(8f, canvas.x - width - 8f)),
                Mathf.Clamp(Plugin.Configs.TabPosY.Value, 8f, Mathf.Max(8f, canvas.y - height - 8f)));
        }

        private void AddDragHandles()
        {
            if (_root == null || _root.transform.Find("HeaderDrag") != null) return;
            var target = _root.GetComponent<RectTransform>();

            var header = new GameObject("HeaderDrag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(OverlayDrag));
            header.transform.SetParent(_root.transform, false);
            header.transform.SetSiblingIndex(0);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 58f);
            var headerImage = header.GetComponent<Image>();
            headerImage.color = new Color(1f, 1f, 1f, 0.01f);
            headerImage.raycastTarget = true;
            var headerDrag = header.GetComponent<OverlayDrag>();
            headerDrag.Target = target;

            var resize = new GameObject("Resize", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(OverlayDrag));
            resize.transform.SetParent(_root.transform, false);
            var resizeRect = resize.GetComponent<RectTransform>();
            resizeRect.anchorMin = new Vector2(0f, 0f);
            resizeRect.anchorMax = new Vector2(0f, 1f);
            resizeRect.pivot = new Vector2(0f, 0.5f);
            resizeRect.offsetMin = new Vector2(0f, 0f);
            resizeRect.offsetMax = new Vector2(12f, -58f);
            var resizeImage = resize.GetComponent<Image>();
            resizeImage.color = new Color(1f, 1f, 1f, 0.01f);
            resizeImage.raycastTarget = true;
            var resizeDrag = resize.GetComponent<OverlayDrag>();
            resizeDrag.Target = target;
            resizeDrag.ResizeWidth = true;

            _root.transform.Find("Close_Btn")?.SetAsLastSibling();
            _root.transform.Find("Compare_Btn")?.SetAsLastSibling();
        }

        internal void SetOpen(bool open)
        {
            var allow = open && StatsController.InActiveMatch();
            if (allow) EnsureBuilt();
            IsOpen = allow;
            if (_root != null) _root.SetActive(allow);
            if (allow)
            {
                RebuildLayout();
                EnsureCompareTarget();
                Refresh();
            }
        }

        internal void Toggle() => SetOpen(!IsOpen);

        internal void ToggleCompareMode()
        {
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
            if (!IsOpen) return;

            if (Input.GetKeyDown(KeyCode.C)) ToggleCompareMode();
            if (_compareMode)
            {
                if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma)) CycleCompareTarget(-1);
                if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period)) CycleCompareTarget(1);
            }

            if (Input.GetKeyDown(KeyCode.P)) PinLocalBaseline();
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) ResetPinnedBaseline();
        }

        internal void Refresh()
        {
            if (_root == null || !_root.activeSelf) return;

            if (_compareMode)
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

            var dirty = false;
            for (var i = 0; i < _playerBlocks.Count; i++)
            {
                var active = i < players.Count;
                var block = _playerBlocks[i].transform.parent.gameObject;
                if (block.activeSelf != active)
                {
                    block.SetActive(active);
                    dirty = true;
                }
                if (!active) continue;

                if (!PlayerStatsSnapshot.TryFrom(players[i], out var snap))
                {
                    dirty |= AssignBlockText(_playerBlocks[i], "Unavailable");
                }
                else
                {
                    var baseline = GetPinnedBaselineFor(players[i]);
                    var text = baseline != null
                        ? StatsViewBuilder.BuildHud(snap, false, baseline, null, TabInfoBridge.GetExtensionStats(players[i]))
                        : StatsViewBuilder.BuildTabBlock(snap, TabInfoBridge.GetExtensionStats(players[i]));
                    dirty |= AssignBlockText(_playerBlocks[i], text);
                }
            }

            if (dirty) RelayoutBlocks();
            _title.text = FormatAllPlayersTitle();
            _hint.text = "Drag the header to move";
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
                AssignBlockText(_playerBlocks[0], "Local player unavailable");
                RelayoutBlocks();
                _title.text = "Compare";
                _hint.text = "No local player";
                return;
            }

            if (opponent == null || !PlayerStatsSnapshot.TryFrom(opponent, out var otherSnap))
            {
                AssignBlockText(_playerBlocks[0], StatsViewBuilder.BuildTabBlock(localSnap, TabInfoBridge.GetExtensionStats(local)));
                RelayoutBlocks();
                _title.text = "Compare";
                _hint.text = "Need another player to compare";
                return;
            }

            var vsBaseline = otherSnap;
            var pinBaseline = _pinnedLocal != null && _pinnedLocalPlayerId == local.playerID ? _pinnedLocal : null;

            AssignBlockText(_playerBlocks[0], StatsViewBuilder.BuildTabCompare(localSnap, vsBaseline, pinBaseline, TabInfoBridge.GetExtensionStats(local)));
            AssignBlockText(_playerBlocks[1], StatsViewBuilder.BuildTabBlock(otherSnap, TabInfoBridge.GetExtensionStats(opponent)));
            RelayoutBlocks();
            _title.text = $"You vs {otherSnap.PlayerName}";
            _hint.text = "[ ] switch player · C back to all";
        }

        private static string FormatAllPlayersTitle()
        {
            var title = $"All players · R{StatsController.CurrentRound} P{StatsController.CurrentPoint}";
            var ping = PingTracker.LocalPing();
            return ping >= 0 ? $"{title} · you {ping}ms" : title;
        }

        private static bool AssignBlockText(TextMeshProUGUI block, string text)
        {
            if (block == null) return false;
            block.lineSpacing = 4f;
            if (block.text == text) return false;
            block.text = text;
            return true;
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
                StatsUiHelper.ApplyRounded(image, StatsUiHelper.BlockColor);
                image.raycastTarget = false;

                var le = blockGo.GetComponent<LayoutElement>();
                le.minHeight = 96f;
                le.preferredHeight = 120f;
                le.flexibleWidth = 1f;
                le.flexibleHeight = 0f;

                var text = StatsUiHelper.CreateText(blockGo.transform, "Text", "", StatsUiHelper.BaseFont);
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Overflow;
                text.lineSpacing = 4f;
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
