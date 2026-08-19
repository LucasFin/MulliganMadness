using MulliganMadness.Stats;
using TMPro;
using UnboundLib;
using UnityEngine;

namespace MulliganMadness.UI
{
    internal sealed class StatsHudPanel : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _body;
        private PlayerStatsSnapshot _previewDelta;

        internal void EnsureBuilt()
        {
            if (_root != null) return;
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;

            var scale = StatsUiHelper.UiScale;
            var pos = new Vector2(
                Plugin.Configs.StatsHudOffsetX.Value * scale,
                Plugin.Configs.StatsHudOffsetY.Value * scale);
            var size = new Vector2(268f * scale, Plugin.Configs.StatsHudSimpleMode.Value ? 168f * scale : 300f * scale);

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
            bodyRect.offsetMax = new Vector2(-12f, -8f);
        }

        internal void SetPreviewDelta(PlayerStatsSnapshot delta) => _previewDelta = delta;

        internal void Refresh(PlayerStatsSnapshot baseline)
        {
            var enabled = Plugin.Configs.EnableStatsHud.Value && Plugin.Configs.StatsHudVisible.Value;
            var inGame = StatsController.InActiveMatch();
            var hidePick = Plugin.Configs.HideStatsHudDuringPick.Value && StatsController.InPickPhase;
            var hideBattle = Plugin.Configs.HideStatsHudDuringBattle.Value && StatsController.InBattlePhase;
            var player = PlayerStatsSnapshot.LocalPlayer();
            if (!enabled || !inGame || hidePick || hideBattle || player == null ||
                !PlayerStatsSnapshot.TryFrom(player, out var snapshot))
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            EnsureBuilt();
            if (_root == null) return;
            _root.SetActive(true);

            var extraRows = 0;
            if (snapshot.TryGetNumeric("Nulls", out var remaining) && remaining > 0.05f) extraRows++;
            if (snapshot.TryGetNumeric("NullCards", out var owned) && owned > 0.05f) extraRows++;
            var height = (Plugin.Configs.StatsHudSimpleMode.Value ? 168f : 300f) + extraRows * 20f;

            var scale = StatsUiHelper.UiScale;
            StatsUiHelper.ApplyRect(
                _root,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(Plugin.Configs.StatsHudOffsetX.Value * scale, Plugin.Configs.StatsHudOffsetY.Value * scale),
                new Vector2(268f * scale, height * scale));

            var image = _root.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                var c = image.color;
                image.color = new Color(c.r, c.g, c.b, Plugin.Configs.StatsHudOpacity.Value);
            }

            _body.fontSize = StatsUiHelper.BaseFont * Plugin.Configs.StatsHudFontScale.Value;
            _body.color = Color.white;
            _body.text = StatsViewBuilder.BuildHud(
                snapshot,
                Plugin.Configs.StatsHudSimpleMode.Value,
                baseline,
                _previewDelta,
                TabInfoBridge.GetExtensionStats(player));
        }
    }
}
