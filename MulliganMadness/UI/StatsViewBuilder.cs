using System.Collections.Generic;
using System.Text;
using MulliganMadness.Stats;
using UnityEngine;

namespace MulliganMadness.UI
{
    internal static class StatsViewBuilder
    {
        private static readonly Color LabelColor = new Color(0.62f, 0.72f, 0.82f, 1f);
        private static readonly Color ValueColor = new Color(0.94f, 0.97f, 1f, 1f);
        private static readonly Color SectionColor = new Color(0.45f, 0.58f, 0.72f, 0.95f);

        internal static string BuildHud(PlayerStatsSnapshot snap, bool simple, PlayerStatsSnapshot baseline, PlayerStatsSnapshot preview, IEnumerable<(string category, string label, string value)> extensions, string headerSuffix = null, bool omitHealthDelta = false)
        {
            var sb = new StringBuilder();
            var suffix = string.IsNullOrEmpty(headerSuffix) ? "" : headerSuffix;
            sb.AppendLine($"<size=105%><b>{snap.PlayerName}{suffix}</b></size>");

            var hpBaseline = omitHealthDelta ? null : baseline;
            AppendHeroRow(sb, "HP", snap.GetDisplay("HP"), snap, hpBaseline, preview, isHero: true);
            AppendHeroRow(sb, "DMG", snap.GetDisplay("DMG"), snap, baseline, preview, isHero: true);
            if (HasCount(snap, "Nulls")) AppendRow(sb, "Nulls", snap.GetDisplay("Nulls"), "Nulls", snap, baseline, preview);
            if (HasCount(snap, "NullCards")) AppendRow(sb, "Null cards", snap.GetDisplay("NullCards"), "NullCards", snap, baseline, preview);

            if (!simple)
            {
                AppendSection(sb, "Survival");
                AppendRow(sb, "Lives", snap.GetDisplay("Lives"), "Lives", snap, baseline, preview);
                AppendRow(sb, "Block CD", snap.GetDisplay("BlockCD"), "BlockCD", snap, baseline, preview);
                AppendRow(sb, "Blocks", snap.GetDisplay("BlockCount"), "BlockCount", snap, baseline, preview);
            }
            else
            {
                AppendRow(sb, "Block CD", snap.GetDisplay("BlockCD"), "BlockCD", snap, baseline, preview);
            }

            AppendSection(sb, "Combat");
            AppendRow(sb, "Attack SPD", snap.GetDisplay("AttackSPD"), "AttackSPD", snap, baseline, preview);
            if (!simple)
            {
                AppendRow(sb, "Bullets", snap.GetDisplay("Bullets"), "Bullets", snap, baseline, preview);
                AppendRow(sb, "Knockback", snap.GetDisplay("Knockback"), "Knockback", snap, baseline, preview);
                AppendRow(sb, "Life Steal", snap.GetDisplay("LifeSteal"), "LifeSteal", snap, baseline, preview);
            }

            AppendSection(sb, "Mobility");
            AppendRow(sb, "Move SPD", snap.GetDisplay("MoveSPD"), "MoveSPD", snap, baseline, preview);
            if (!simple)
            {
                AppendRow(sb, "Jump", snap.GetDisplay("Jump"), "Jump", snap, baseline, preview);
                AppendRow(sb, "Size", snap.GetDisplay("Size"), "Size", snap, baseline, preview);
            }

            if (!simple)
            {
                AppendSection(sb, "Projectile");
                AppendRow(sb, "Bullet SPD", snap.GetDisplay("BulletSPD"), "BulletSPD", snap, baseline, preview);
                AppendRow(sb, "Reload", snap.GetDisplay("Reload"), "Reload", snap, baseline, preview);
                AppendRow(sb, "Ammo", snap.GetDisplay("Ammo"), "Ammo", snap, baseline, preview);
                AppendRow(sb, "Range", snap.GetDisplay("Range"), "Range", snap, baseline, preview);
                AppendRow(sb, "Bounces", snap.GetDisplay("Bounces"), "Bounces", snap, baseline, preview);
            }

            if (extensions != null)
            {
                string lastCategory = null;
                foreach (var ext in extensions)
                {
                    if (IsNullStat(ext.label)) continue;
                    if (ext.category != lastCategory)
                    {
                        AppendSection(sb, ext.category);
                        lastCategory = ext.category;
                    }

                    sb.Append(Label(ext.label)).Append("  ").Append(Value(ext.value)).AppendLine();
                }
            }

            return sb.ToString().Trim();
        }

        internal static string BuildCompareColumn(PlayerStatsSnapshot snap, PlayerStatsSnapshot baseline)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=108%><b>{snap.PlayerName}</b></size>");
            AppendHeroRow(sb, "HP", snap.GetDisplay("HP"), snap, baseline, null, true);
            AppendHeroRow(sb, "DMG", snap.GetDisplay("DMG"), snap, baseline, null, true);
            if (HasCount(snap, "Nulls")) AppendRow(sb, "Nulls", snap.GetDisplay("Nulls"), "Nulls", snap, baseline, null);
            if (HasCount(snap, "NullCards")) AppendRow(sb, "Null cards", snap.GetDisplay("NullCards"), "NullCards", snap, baseline, null);
            AppendRow(sb, "Block", snap.GetDisplay("BlockCD"), "BlockCD", snap, baseline, null);
            AppendRow(sb, "Move", snap.GetDisplay("MoveSPD"), "MoveSPD", snap, baseline, null);
            AppendRow(sb, "Atk", snap.GetDisplay("AttackSPD"), "AttackSPD", snap, baseline, null);
            return sb.ToString().Trim();
        }

        internal static string BuildTabCompare(
            PlayerStatsSnapshot local,
            PlayerStatsSnapshot opponent,
            PlayerStatsSnapshot pinnedLocal,
            IEnumerable<(string category, string label, string value)> extensions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=108%><b>You vs {opponent.PlayerName}</b></size>");
            sb.AppendLine("<size=82%><color=#9AA8B5>Numbers in ( ) compare you to their current build</color></size>");
            sb.AppendLine();
            sb.Append(BuildHud(local, simple: false, baseline: opponent, preview: null, extensions));
            if (pinnedLocal != null)
            {
                sb.AppendLine();
                sb.AppendLine("<size=88%><color=#9AA8B5>— Since your pin —</color></size>");
                sb.Append(BuildHud(local, simple: true, baseline: pinnedLocal, preview: null, extensions: null));
            }

            return sb.ToString().Trim();
        }

        internal static string BuildTabBlock(PlayerStatsSnapshot snap, IEnumerable<(string category, string label, string value)> extensions)
        {
            return BuildHud(snap, simple: false, baseline: null, preview: null, extensions);
        }

        private static bool HasCount(PlayerStatsSnapshot snap, string key) =>
            snap != null && snap.TryGetNumeric(key, out var value) && value > 0.05f;

        private static bool IsNullStat(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            return label.Equals("Nulls", System.StringComparison.OrdinalIgnoreCase)
                   || label.Equals("Null", System.StringComparison.OrdinalIgnoreCase)
                   || label.Equals("Null cards", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendSection(StringBuilder sb, string title)
        {
            sb.Append("<size=88%><color=#").Append(ColorToHex(SectionColor)).Append(">— ").Append(title).Append(" —</color></size>").AppendLine();
        }

        private static void AppendHeroRow(StringBuilder sb, string label, string value, PlayerStatsSnapshot snap, PlayerStatsSnapshot baseline, PlayerStatsSnapshot preview, bool isHero)
        {
            var size = isHero ? "102%" : "100%";
            sb.Append("<size=").Append(size).Append(">").Append(Label(label)).Append("  ").Append(Value(value));
            AppendDeltaSuffix(sb, label, snap, baseline, preview);
            sb.Append("</size>").AppendLine();
        }

        private static void AppendRow(StringBuilder sb, string label, string value, string key, PlayerStatsSnapshot snap, PlayerStatsSnapshot baseline, PlayerStatsSnapshot preview)
        {
            sb.Append(Label(label)).Append("  ").Append(Value(value));
            AppendDeltaSuffix(sb, key, snap, baseline, preview);
            sb.AppendLine();
        }

        private static void AppendDeltaSuffix(StringBuilder sb, string key, PlayerStatsSnapshot snap, PlayerStatsSnapshot baseline, PlayerStatsSnapshot preview)
        {
            var statKey = MapKey(key);
            if (baseline != null && snap.TryGetNumeric(statKey, out var current) && baseline.TryGetNumeric(statKey, out var baseNum))
            {
                var diff = current - baseNum;
                if (Mathf.Abs(diff) >= 0.005f)
                {
                    var good = GunStatReader.IsPositiveChange(statKey, diff);
                    var color = good ? "7CFF7C" : "FF8585";
                    sb.Append(" <color=#").Append(color).Append(">(").Append(diff > 0f ? "+" : "").Append(FormatDelta(statKey, diff)).Append(")</color>");
                }
            }

            if (preview != null && preview.TryGetNumeric(statKey, out var previewDiff) && Mathf.Abs(previewDiff) >= 0.005f)
            {
                var good = GunStatReader.IsPositiveChange(statKey, previewDiff);
                var color = good ? "9DFFB0" : "FFB0B0";
                sb.Append(" <color=#").Append(color).Append(">[").Append(previewDiff > 0f ? "+" : "").Append(FormatDelta(statKey, previewDiff)).Append("]</color>");
            }
        }

        private static string FormatDelta(string key, float diff)
        {
            if (key == "DMG" || key == "HP" || key == "MaxHP" || key == "Bullets" || key == "Bounces" || key == "Bursts" || key == "Ammo" || key == "BlockCount" || key == "Lives" || key == "Nulls" || key == "NullCards")
            {
                return diff.ToString("F0");
            }

            return diff.ToString("F1");
        }

        private static string Label(string text) => $"<color=#{ColorToHex(LabelColor)}>{text}</color>";
        private static string Value(string text) => $"<color=#{ColorToHex(ValueColor)}><b>{text}</b></color>";

        private static string MapKey(string label)
        {
            switch (label)
            {
                case "Nulls": return "Nulls";
                case "Null cards": return "NullCards";
                case "HP": return "HP";
                case "DMG": return "DMG";
                case "Lives": return "Lives";
                case "Block CD":
                case "Block": return "BlockCD";
                case "Blocks": return "BlockCount";
                case "Move SPD":
                case "Move": return "MoveSPD";
                case "Attack SPD":
                case "Atk": return "AttackSPD";
                case "Jump": return "Jump";
                case "Size": return "Size";
                case "Knockback": return "Knockback";
                case "Life Steal": return "LifeSteal";
                case "Bullets": return "Bullets";
                case "Bullet SPD": return "BulletSPD";
                case "Reload": return "Reload";
                case "Ammo": return "Ammo";
                case "Range": return "Range";
                case "Bounces": return "Bounces";
                default: return label;
            }
        }

        private static string ColorToHex(Color c)
        {
            var r = Mathf.Clamp01(c.r);
            var g = Mathf.Clamp01(c.g);
            var b = Mathf.Clamp01(c.b);
            return ColorUtility.ToHtmlStringRGB(new Color(r, g, b));
        }
    }
}
