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
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            var width = GetPanelWidth() * scale;
            var pos = new Vector2(
                Plugin.Configs.StatsHudOffsetX.Value * scale,
                Plugin.Configs.StatsHudOffsetY.Value * scale);

            _root = StatsUiHelper.CreateModernPanel(
                canvas.transform,
                "MM_StatsHud",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                pos,
                new Vector2(width, 140f * scale),
                0f);

            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null) panelImage.color = new Color(0f, 0f, 0f, 0f);

            var accent = _root.transform.Find("Accent");
            if (accent != null) accent.gameObject.SetActive(false);

            _body = StatsUiHelper.CreateText(_root.transform, "Body", "", StatsUiHelper.BaseFont);
            var bodyRect = _body.rectTransform;
            bodyRect.offsetMin = new Vector2(4f, 4f);
            bodyRect.offsetMax = new Vector2(-4f, -4f);
            _body.enableWordWrapping = true;
        }

        internal void SetPreviewDelta(PlayerStatsSnapshot delta) => _previewDelta = delta;

        internal void Refresh(PlayerStatsSnapshot roundBaseline, PlayerStatsSnapshot pickBaseline, Player hudPlayer, bool watchingPicker)
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

            var scale = StatsUiHelper.UiScale;
            var width = GetPanelWidth() * scale;
            var height = GetExpandedHeight(snapshot) * scale;

            StatsUiHelper.ApplyRect(
                _root,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(Plugin.Configs.StatsHudOffsetX.Value * scale, Plugin.Configs.StatsHudOffsetY.Value * scale),
                new Vector2(width, height));

            var panelImage = _root.GetComponent<Image>();
            if (panelImage != null)
            {
                var opacity = Plugin.Configs.StatsHudOpacity.Value;
                panelImage.color = new Color(0f, 0f, 0f, opacity);
            }

            _body.fontSize = StatsUiHelper.BaseFont * Plugin.Configs.StatsHudFontScale.Value;
            _body.color = Color.white;
            _body.text = StatsViewBuilder.BuildHud(
                snapshot,
                simple: true,
                baseline: StatsController.InPickPhase ? pickBaseline : roundBaseline,
                preview: StatsController.InPickPhase ? _previewDelta : null,
                extensions: null,
                headerSuffix: watchingPicker ? " · picking" : null);
        }

        private static float GetPanelWidth() =>
            Plugin.Configs.StatsHudUltraCompact.Value ? 210f : 248f;

        private static float GetExpandedHeight(PlayerStatsSnapshot snapshot)
        {
            var extraRows = 0;
            if (snapshot != null)
            {
                if (snapshot.TryGetNumeric("Nulls", out var remaining) && remaining > 0.05f) extraRows++;
                if (snapshot.TryGetNumeric("NullCards", out var owned) && owned > 0.05f) extraRows++;
            }

            return 118f + extraRows * 18f;
        }
    }
}
