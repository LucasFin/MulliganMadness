using MulliganMadness.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    internal sealed class StatsHudPanel : MonoBehaviour
    {
        private const float HorizontalPadding = 12f;
        private const float VerticalPadding = 12f;

        private GameObject _root;
        private TextMeshProUGUI _body;
        private PlayerStatsSnapshot _previewDelta;

        internal void Rebuild()
        {
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
                _body = null;
            }
        }

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            if (!StatsUiHelper.OverlayReady) return;

            var scale = StatsUiHelper.UiScale;
            var width = GetPanelWidth() * scale;
            var pos = new Vector2(
                Plugin.Configs.StatsHudOffsetX.Value * scale,
                Plugin.Configs.StatsHudOffsetY.Value * scale);

            _root = StatsUiHelper.CreateModernPanel(
                StatsUiHelper.OverlayRoot,
                "MM_StatsHud",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                pos,
                new Vector2(width, 160f * scale),
                0f);

            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0f, 0f, 0f, 0f);
                panelImage.raycastTarget = false;
            }

            _body = StatsUiHelper.CreateText(_root.transform, "Body", "", StatsUiHelper.BaseFont);
            var bodyRect = _body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(HorizontalPadding, VerticalPadding);
            bodyRect.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);
            _body.enableWordWrapping = true;
            _body.overflowMode = TextOverflowModes.Overflow;
            _body.raycastTarget = false;
        }

        internal void SetPreviewDelta(PlayerStatsSnapshot delta) => _previewDelta = delta;

        internal void Refresh(PlayerStatsSnapshot pickBaseline, Player hudPlayer, bool watchingPicker)
        {
            var enabled = Plugin.Configs.EnableStatsHud.Value && Plugin.Configs.StatsHudVisible.Value;
            var inGame = StatsController.InActiveMatch();
            var hidePick = Plugin.Configs.HideStatsHudDuringPick.Value && StatsController.InPickPhase;
            var hideBattle = Plugin.Configs.HideStatsHudDuringBattle.Value && StatsController.InBattlePhase;
            var peekMode = Plugin.Configs.StatsHudPeekMode.Value;
            var peeking = !peekMode || Input.GetKey(Plugin.Configs.StatsHudPeekKey.Value);

            if (hudPlayer == null || !PlayerStatsSnapshot.TryFrom(hudPlayer, out var snapshot))
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            if (!enabled || !inGame || hidePick || hideBattle || !peeking)
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            EnsureBuilt();
            if (_root == null || _body == null) return;
            _root.SetActive(true);

            var simple = Plugin.Configs.StatsHudSimpleMode.Value && !Plugin.Configs.StatsHudCollapsed.Value;
            if (Plugin.Configs.StatsHudCollapsed.Value) simple = true;

            _body.fontSize = StatsUiHelper.BaseFont * Plugin.Configs.StatsHudFontScale.Value;
            _body.color = new Color(
                Plugin.Configs.StatsHudColorR.Value,
                Plugin.Configs.StatsHudColorG.Value,
                Plugin.Configs.StatsHudColorB.Value,
                1f);
            _body.text = StatsViewBuilder.BuildHud(
                snapshot,
                simple: simple,
                baseline: pickBaseline,
                preview: StatsController.InPickPhase ? _previewDelta : null,
                extensions: null,
                headerSuffix: watchingPicker ? " · picking" : null,
                omitHealthDelta: StatsController.InBattlePhase);

            LayoutToContent();
        }

        private void LayoutToContent()
        {
            var scale = StatsUiHelper.UiScale;
            var canvasSize = StatsUiHelper.OverlaySize;
            var width = GetPanelWidth() * scale;
            var opacity = Plugin.Configs.StatsHudOpacity.Value;
            var showChrome = opacity > 0.04f;

            _body.ForceMeshUpdate();
            var textHeight = Mathf.Max(_body.preferredHeight, 36f);
            if (Plugin.Configs.StatsHudCollapsed.Value) textHeight = Mathf.Min(textHeight, 54f * scale);
            var height = textHeight + VerticalPadding * 2f + 6f;
            height = Mathf.Min(height, Mathf.Max(64f, canvasSize.y - 24f));
            width = Mathf.Min(width, Mathf.Max(120f, canvasSize.x - 24f));

            var posX = Mathf.Clamp(Plugin.Configs.StatsHudOffsetX.Value * scale, 8f, Mathf.Max(8f, canvasSize.x - width - 8f));
            var posY = Mathf.Clamp(Plugin.Configs.StatsHudOffsetY.Value * scale, 8f, Mathf.Max(8f, canvasSize.y - height - 8f));

            StatsUiHelper.ApplyRect(
                _root,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(posX, posY),
                new Vector2(width, height));

            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.03f, 0.05f, 0.08f, opacity);
                panelImage.raycastTarget = false;
            }

            StatsUiHelper.SetAccentVisible(_root, showChrome);
            var top = _root.transform.Find("AccentTop") ?? _root.transform.Find("Accent");
            var bottom = _root.transform.Find("AccentBottom");
            if (top != null) top.GetComponent<Image>().color = StatsUiHelper.AccentColor;
            if (bottom != null) bottom.GetComponent<Image>().color = StatsUiHelper.AccentColor;
        }

        private static float GetPanelWidth() =>
            Plugin.Configs.StatsHudUltraCompact.Value ? 210f : 260f;
    }
}
