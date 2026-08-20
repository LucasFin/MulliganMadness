using MulliganMadness.Stats;
using MulliganMadness.Utils;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal sealed class StatsHudPanel : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _body;
        private TextMeshProUGUI _collapsedBody;
        private Button _collapseButton;
        private PlayerStatsSnapshot _previewDelta;

        internal void Rebuild()
        {
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
                _body = null;
                _collapsedBody = null;
                _collapseButton = null;
            }
        }

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            var width = GetPanelWidth() * scale;
            var pos = new Vector2(
                Plugin.Configs.StatsHudOffsetX.Value * scale,
                Plugin.Configs.StatsHudOffsetY.Value * scale);
            var size = new Vector2(width, GetExpandedHeight() * scale);

            _root = StatsUiHelper.CreateModernPanel(
                canvas.transform,
                "MM_StatsHud",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                pos,
                size,
                Plugin.Configs.StatsHudOpacity.Value);

            _body = StatsUiHelper.CreateText(_root.transform, "Body", "", StatsUiHelper.BaseFont);
            var bodyRect = _body.rectTransform;
            bodyRect.offsetMin = new Vector2(12f, 10f);
            bodyRect.offsetMax = new Vector2(-36f, -8f);

            _collapsedBody = StatsUiHelper.CreateText(_root.transform, "Collapsed", "", StatsUiHelper.BaseFont * 0.95f);
            var collapsedRect = _collapsedBody.rectTransform;
            collapsedRect.offsetMin = new Vector2(10f, 8f);
            collapsedRect.offsetMax = new Vector2(-36f, -8f);
            _collapsedBody.gameObject.SetActive(false);

            var collapseGo = new GameObject("Collapse", typeof(RectTransform), typeof(Image), typeof(Button));
            collapseGo.transform.SetParent(_root.transform, false);
            var collapseRect = collapseGo.GetComponent<RectTransform>();
            collapseRect.anchorMin = new Vector2(1f, 1f);
            collapseRect.anchorMax = new Vector2(1f, 1f);
            collapseRect.pivot = new Vector2(1f, 1f);
            collapseRect.anchoredPosition = new Vector2(-6f, -6f);
            collapseRect.sizeDelta = new Vector2(24f, 24f);
            collapseGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.21f, 0.92f);
            _collapseButton = collapseGo.GetComponent<Button>();
            _collapseButton.onClick.AddListener(ToggleCollapsed);

            var collapseLabel = StatsUiHelper.CreateText(collapseGo.transform, "Label", "−", StatsUiHelper.BaseFont * 0.85f, TextAlignmentOptions.Center);
            collapseLabel.raycastTarget = false;
        }

        private void ToggleCollapsed()
        {
            Plugin.Configs.StatsHudCollapsed.Value = !Plugin.Configs.StatsHudCollapsed.Value;
        }

        internal void SetPreviewDelta(PlayerStatsSnapshot delta) => _previewDelta = delta;

        internal void Refresh(PlayerStatsSnapshot baseline)
        {
            var enabled = Plugin.Configs.EnableStatsHud.Value && Plugin.Configs.StatsHudVisible.Value;
            var inGame = StatsController.InActiveMatch();
            var hidePick = Plugin.Configs.HideStatsHudDuringPick.Value && StatsController.InPickPhase;
            var hideBattle = Plugin.Configs.HideStatsHudDuringBattle.Value && StatsController.InBattlePhase;
            var peekMode = Plugin.Configs.StatsHudPeekMode.Value;
            var peeking = !peekMode || Input.GetKey(Plugin.Configs.StatsHudPeekKey.Value);
            var player = PlayerStatsSnapshot.LocalPlayer();
            if (!enabled || !inGame || hidePick || hideBattle || !peeking || player == null ||
                !PlayerStatsSnapshot.TryFrom(player, out var snapshot))
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);

            var collapsed = Plugin.Configs.StatsHudCollapsed.Value;
            var scale = StatsUiHelper.UiScale;
            var width = GetPanelWidth() * scale;
            var height = (collapsed ? 44f : GetExpandedHeight(snapshot)) * scale;

            StatsUiHelper.ApplyRect(
                _root,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(Plugin.Configs.StatsHudOffsetX.Value * scale, Plugin.Configs.StatsHudOffsetY.Value * scale),
                new Vector2(width, height));

            var image = _root.GetComponent<Image>();
            if (image != null)
            {
                var c = image.color;
                image.color = new Color(c.r, c.g, c.b, Plugin.Configs.StatsHudOpacity.Value);
            }

            var textColor = new Color(
                Plugin.Configs.StatsHudColorR.Value,
                Plugin.Configs.StatsHudColorG.Value,
                Plugin.Configs.StatsHudColorB.Value,
                1f);

            if (_collapseButton != null)
            {
                var label = _collapseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = collapsed ? "+" : "−";
            }

            if (collapsed)
            {
                if (_body != null) _body.gameObject.SetActive(false);
                if (_collapsedBody != null)
                {
                    _collapsedBody.gameObject.SetActive(true);
                    _collapsedBody.fontSize = StatsUiHelper.BaseFont * Plugin.Configs.StatsHudFontScale.Value;
                    _collapsedBody.color = textColor;
                    _collapsedBody.text =
                        $"<b>{snapshot.PlayerName}</b>  HP {snapshot.GetDisplay("HP")}  DMG {snapshot.GetDisplay("DMG")}  · R{StatsController.CurrentRound}";
                }
            }
            else
            {
                if (_collapsedBody != null) _collapsedBody.gameObject.SetActive(false);
                if (_body != null)
                {
                    _body.gameObject.SetActive(true);
                    _body.fontSize = StatsUiHelper.BaseFont * Plugin.Configs.StatsHudFontScale.Value;
                    _body.color = textColor;
                    _body.text = StatsViewBuilder.BuildHud(
                        snapshot,
                        Plugin.Configs.StatsHudSimpleMode.Value,
                        baseline,
                        _previewDelta,
                        TabInfoBridge.GetExtensionStats(player));
                }
            }
        }

        private static float GetPanelWidth() =>
            Plugin.Configs.StatsHudUltraCompact.Value ? 200f : 268f;

        private float GetExpandedHeight(PlayerStatsSnapshot snapshot = null)
        {
            var extraRows = 0;
            if (snapshot != null)
            {
                if (snapshot.TryGetNumeric("Nulls", out var remaining) && remaining > 0.05f) extraRows++;
                if (snapshot.TryGetNumeric("NullCards", out var owned) && owned > 0.05f) extraRows++;
            }

            var baseHeight = Plugin.Configs.StatsHudSimpleMode.Value ? 168f : 300f;
            if (Plugin.Configs.StatsHudUltraCompact.Value) baseHeight = Plugin.Configs.StatsHudSimpleMode.Value ? 132f : 220f;
            return baseHeight + extraRows * 20f;
        }
    }
}
