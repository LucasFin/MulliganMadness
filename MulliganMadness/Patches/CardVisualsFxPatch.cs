using System;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Softens vanilla CardVisuals particle glow for MulliganMadness cards, and tints
    /// MmMovingCardBackground from the card theme colors.
    /// </summary>
    [HarmonyPatch(typeof(CardVisuals))]
    internal static class CardVisualsFxPatch
    {
        private static readonly FieldInfo PartField = AccessTools.Field(typeof(CardVisuals), "part");
        private static readonly FieldInfo ImagesField = AccessTools.Field(typeof(CardVisuals), "images");
        private static readonly FieldInfo SelectedColorField = AccessTools.Field(typeof(CardVisuals), "selectedColor");
        private static readonly FieldInfo DefaultColorField = AccessTools.Field(typeof(CardVisuals), "defaultColor");
        private static readonly FieldInfo IsSelectedField = AccessTools.Field(typeof(CardVisuals), "isSelected");

        private sealed class GlowBaseline : MonoBehaviour
        {
            public float Rate;
            public float Saturation;
            public bool Captured;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void AfterStart(CardVisuals __instance)
        {
            if (__instance == null) return;
            try
            {
                Apply(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"CardVisualsFx Start skipped: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ChangeSelected")]
        private static void AfterChangeSelected(CardVisuals __instance)
        {
            if (__instance == null) return;
            try
            {
                Apply(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"CardVisualsFx ChangeSelected skipped: {ex.Message}");
            }
        }

        private static void Apply(CardVisuals visuals)
        {
            var fx = FindFx(visuals);
            if (fx == null) return;

            var glow = CardArtFx.GlowScaleFor(fx);
            var isSelected = IsSelectedField != null && (bool)IsSelectedField.GetValue(visuals);
            var part = PartField?.GetValue(visuals) as GeneralParticleSystem;

            if (part != null)
            {
                var baseline = part.GetComponent<GlowBaseline>()
                               ?? part.gameObject.AddComponent<GlowBaseline>();
                if (!baseline.Captured)
                {
                    baseline.Rate = part.rate;
                    baseline.Saturation = part.saturationMultiplier;
                    baseline.Captured = true;
                }

                part.saturationMultiplier = baseline.Saturation * (0.5f * glow);
                part.rate = Mathf.Max(0.04f, baseline.Rate * (0.3f + glow * 0.45f));
                part.simulationSpeedMultiplier = isSelected
                    ? Mathf.Min(part.simulationSpeedMultiplier, 0.5f + glow * 0.35f)
                    : Mathf.Min(part.simulationSpeedMultiplier, 0.22f);

                if (part.particleSettings != null)
                {
                    var c = part.particleSettings.randomColor;
                    c = Color.Lerp(c, Color.white, 0.28f);
                    c.a = Mathf.Clamp01(c.a * (0.4f + glow * 0.35f));
                    part.particleSettings.randomColor = c;
                }
            }

            if (ImagesField?.GetValue(visuals) is Image[] images)
            {
                for (var i = 0; i < images.Length; i++)
                {
                    var img = images[i];
                    if (img == null) continue;
                    var c = img.color;
                    c.a = Mathf.Min(c.a, 0.5f + glow * 0.35f);
                    img.color = c;
                }
            }

            var moving = visuals.GetComponentInChildren<MmMovingCardBackground>(true);
            if (moving == null) return;

            var selected = SelectedColorField != null
                ? (Color)SelectedColorField.GetValue(visuals)
                : Color.white;
            var def = DefaultColorField != null
                ? (Color)DefaultColorField.GetValue(visuals)
                : selected;
            var tint = isSelected ? selected : def;
            if (tint.maxColorComponent < 0.05f) tint = selected;
            moving.SetTint(tint);
            // Ambient drift stays on (vanilla art particles often loop while parked).
            moving.enabled = true;
        }

        private static MmCardArtFxTag FindFx(CardVisuals visuals)
        {
            var fx = visuals.GetComponentInChildren<MmCardArtFxTag>(true);
            if (fx != null) return fx;

            var info = visuals.GetComponentInParent<CardInfo>();
            if (info == null) info = visuals.GetComponent<CardInfo>();
            if (info?.cardArt == null) return null;
            return info.cardArt.GetComponent<MmCardArtFxTag>();
        }
    }
}
