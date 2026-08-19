using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal struct StatValue
    {
        public readonly string Label;
        public readonly string Value;
        public readonly float Numeric;

        public StatValue(string label, string value, float numeric = float.NaN)
        {
            Label = label;
            Value = value;
            Numeric = numeric;
        }
    }

    internal sealed class PlayerStatsSnapshot
    {
        private readonly Dictionary<string, float> _numbers = new Dictionary<string, float>();
        private readonly Dictionary<string, string> _display = new Dictionary<string, string>();

        public int PlayerId { get; private set; } = -1;
        public string PlayerName { get; private set; } = "?";

        public static bool TryFrom(Player player, out PlayerStatsSnapshot snapshot)
        {
            snapshot = null;
            if (player?.data == null) return false;

            var gun = player.data.weaponHandler?.gun;
            var block = player.data.block;
            var data = player.data;
            if (gun == null || block == null || data.stats == null) return false;

            var ammo = gun.GetComponentInChildren<GunAmmo>();
            var damage = GunStatReader.ComputeDamage(gun);
            snapshot = new PlayerStatsSnapshot
            {
                PlayerId = player.playerID,
                PlayerName = player.data.view?.Owner?.NickName ?? $"P{player.playerID + 1}"
            };

            snapshot.Set("HP", data.health, $"{data.health:F0}/{data.maxHealth:F0}");
            snapshot.Set("MaxHP", data.maxHealth, $"{data.maxHealth:F0}");
            snapshot.Set("DMG", damage, $"{damage:F0}");

            var remainingNulls = NullStatReader.GetRemaining(player);
            if (remainingNulls > 0.05f)
            {
                snapshot.Set("Nulls", remainingNulls, FormatCount(remainingNulls));
            }

            var ownedNulls = NullStatReader.GetOwned(player);
            if (ownedNulls > 0)
            {
                snapshot.Set("NullCards", ownedNulls, ownedNulls.ToString("F0"));
            }
            snapshot.Set("Lives", data.stats.respawns + 1f, $"{data.stats.respawns + 1:F0}");
            snapshot.Set("BlockCD", block.Cooldown(), $"{block.Cooldown():F2}s");
            snapshot.Set("BlockCount", block.additionalBlocks + 1f, $"{block.additionalBlocks + 1:F0}");
            snapshot.Set("MoveSPD", data.stats.movementSpeed, $"{data.stats.movementSpeed:F2}");
            snapshot.Set("Jump", data.stats.jump, $"{data.stats.jump:F2}");
            snapshot.Set("Size", data.stats.sizeMultiplier, $"{data.stats.sizeMultiplier:F2}");
            snapshot.Set("Knockback", gun.knockback, $"{gun.knockback:F2}");
            snapshot.Set("LifeSteal", data.stats.lifeSteal, $"{data.stats.lifeSteal:F2}");
            snapshot.Set("DmgGrow", gun.damageAfterDistanceMultiplier, $"{gun.damageAfterDistanceMultiplier:F2}");
            snapshot.Set("BulletSlow", gun.slow, $"{gun.slow:F2}");
            snapshot.Set("AttackSPD", gun.attackSpeed * gun.attackSpeedMultiplier, $"{(gun.attackSpeed * gun.attackSpeedMultiplier):F2}s");
            snapshot.Set("BulletSPD", gun.projectileSpeed, $"{gun.projectileSpeed:F2}");
            snapshot.Set("ProjectileSPD", gun.projectielSimulatonSpeed, $"{gun.projectielSimulatonSpeed:F2}");

            if (ammo != null)
            {
                var reload = GunStatReader.ComputeReloadSeconds(ammo);
                snapshot.Set("Reload", reload, $"{reload:F2}s");
                snapshot.Set("Ammo", ammo.maxAmmo, $"{ammo.maxAmmo:F0}");
            }

            snapshot.Set("Bullets", gun.numberOfProjectiles, $"{gun.numberOfProjectiles:F0}");
            snapshot.Set("Range", gun.destroyBulletAfter, $"{gun.destroyBulletAfter:F2}");
            snapshot.Set("Bounces", gun.reflects, $"{gun.reflects:F0}");
            snapshot.Set("Bursts", gun.bursts, $"{gun.bursts:F0}");
            snapshot.Set("Gravity", gun.gravity, $"{gun.gravity:F2}");

            return true;
        }

        public static Player LocalPlayer()
        {
            if (PlayerManager.instance?.players == null) return null;
            foreach (var player in PlayerManager.instance.players)
            {
                if (player?.data?.view != null && player.data.view.IsMine && player.GetComponent<PlayerAPI>()?.enabled != false)
                {
                    return player;
                }
            }

            return null;
        }

        public static IEnumerable<Player> ActivePlayers()
        {
            if (PlayerManager.instance?.players == null) yield break;
            foreach (var player in PlayerManager.instance.players)
            {
                if (player?.data != null) yield return player;
            }
        }

        public bool TryGetNumeric(string key, out float value) => _numbers.TryGetValue(key, out value);

        public string GetDisplay(string key) => _display.TryGetValue(key, out var value) ? value : "-";

        public IEnumerable<StatValue> CompactStats()
        {
            yield return new StatValue("HP", GetDisplay("HP"), GetNumeric("HP"));
            yield return new StatValue("DMG", GetDisplay("DMG"), GetNumeric("DMG"));
            if (HasCount("Nulls")) yield return new StatValue("Nulls", GetDisplay("Nulls"), GetNumeric("Nulls"));
            if (HasCount("NullCards")) yield return new StatValue("Null cards", GetDisplay("NullCards"), GetNumeric("NullCards"));
            yield return new StatValue("Block CD", GetDisplay("BlockCD"), GetNumeric("BlockCD"));
            yield return new StatValue("Move", GetDisplay("MoveSPD"), GetNumeric("MoveSPD"));
            yield return new StatValue("Atk SPD", GetDisplay("AttackSPD"), GetNumeric("AttackSPD"));
        }

        public IEnumerable<StatValue> FullStats()
        {
            yield return new StatValue("HP", GetDisplay("HP"), GetNumeric("HP"));
            yield return new StatValue("DMG", GetDisplay("DMG"), GetNumeric("DMG"));
            if (HasCount("Nulls")) yield return new StatValue("Nulls", GetDisplay("Nulls"), GetNumeric("Nulls"));
            if (HasCount("NullCards")) yield return new StatValue("Null cards", GetDisplay("NullCards"), GetNumeric("NullCards"));
            yield return new StatValue("Lives", GetDisplay("Lives"), GetNumeric("Lives"));
            yield return new StatValue("Block CD", GetDisplay("BlockCD"), GetNumeric("BlockCD"));
            yield return new StatValue("Blocks", GetDisplay("BlockCount"), GetNumeric("BlockCount"));
            yield return new StatValue("Move SPD", GetDisplay("MoveSPD"), GetNumeric("MoveSPD"));
            yield return new StatValue("Jump", GetDisplay("Jump"), GetNumeric("Jump"));
            yield return new StatValue("Size", GetDisplay("Size"), GetNumeric("Size"));
            yield return new StatValue("Knockback", GetDisplay("Knockback"), GetNumeric("Knockback"));
            yield return new StatValue("Life Steal", GetDisplay("LifeSteal"), GetNumeric("LifeSteal"));
            yield return new StatValue("Dmg Grow", GetDisplay("DmgGrow"), GetNumeric("DmgGrow"));
            yield return new StatValue("Bullet Slow", GetDisplay("BulletSlow"), GetNumeric("BulletSlow"));
            yield return new StatValue("Attack SPD", GetDisplay("AttackSPD"), GetNumeric("AttackSPD"));
            yield return new StatValue("Bullet SPD", GetDisplay("BulletSPD"), GetNumeric("BulletSPD"));
            yield return new StatValue("Projectile SPD", GetDisplay("ProjectileSPD"), GetNumeric("ProjectileSPD"));
            yield return new StatValue("Reload", GetDisplay("Reload"), GetNumeric("Reload"));
            yield return new StatValue("Ammo", GetDisplay("Ammo"), GetNumeric("Ammo"));
            yield return new StatValue("Bullets", GetDisplay("Bullets"), GetNumeric("Bullets"));
            yield return new StatValue("Range", GetDisplay("Range"), GetNumeric("Range"));
            yield return new StatValue("Bounces", GetDisplay("Bounces"), GetNumeric("Bounces"));
            yield return new StatValue("Bursts", GetDisplay("Bursts"), GetNumeric("Bursts"));
            yield return new StatValue("Gravity", GetDisplay("Gravity"), GetNumeric("Gravity"));
        }

        public PlayerStatsSnapshot Delta(PlayerStatsSnapshot other)
        {
            var delta = CloneMeta();
            foreach (var pair in _numbers)
            {
                if (!other._numbers.TryGetValue(pair.Key, out var before) || float.IsNaN(before) || float.IsNaN(pair.Value))
                {
                    delta.Set(pair.Key, pair.Value, GetDisplay(pair.Key));
                    continue;
                }

                var diff = pair.Value - before;
                if (Mathf.Abs(diff) < 0.005f)
                {
                    delta.Set(pair.Key, 0f, "±0");
                    continue;
                }

                var sign = diff > 0f ? "+" : "";
                delta.Set(pair.Key, diff, $"{sign}{diff:F2}");
            }

            return delta;
        }

        public string FormatLines(bool simple, PlayerStatsSnapshot baseline = null, PlayerStatsSnapshot previewDelta = null)
        {
            var stats = simple ? CompactStats() : FullStats();
            var sb = new StringBuilder();
            foreach (var stat in stats)
            {
                sb.Append(stat.Label).Append(": ").Append(stat.Value);
                if (baseline != null && baseline.TryGetNumeric(StatKey(stat.Label), out var baseNum) && !float.IsNaN(stat.Numeric))
                {
                    var diff = stat.Numeric - baseNum;
                    if (Mathf.Abs(diff) >= 0.005f)
                    {
                        sb.Append(" (").Append(diff > 0f ? "+" : "").Append(diff.ToString("F2", CultureInfo.InvariantCulture)).Append(')');
                    }
                }

                if (previewDelta != null && previewDelta.TryGetNumeric(StatKey(stat.Label), out var previewDiff) && Mathf.Abs(previewDiff) >= 0.005f)
                {
                    sb.Append(" <color=#7CFF7C>[").Append(previewDiff > 0f ? "+" : "").Append(previewDiff.ToString("F2", CultureInfo.InvariantCulture)).Append("]</color>");
                }

                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private static string StatKey(string label)
        {
            switch (label)
            {
                case "Nulls": return "Nulls";
                case "Null cards": return "NullCards";
                case "HP": return "HP";
                case "DMG": return "DMG";
                case "Lives": return "Lives";
                case "Block CD": return "BlockCD";
                case "Blocks": return "BlockCount";
                case "Move": return "MoveSPD";
                case "Move SPD": return "MoveSPD";
                case "Jump": return "Jump";
                case "Size": return "Size";
                case "Knockback": return "Knockback";
                case "Life Steal": return "LifeSteal";
                case "Dmg Grow": return "DmgGrow";
                case "Bullet Slow": return "BulletSlow";
                case "Atk SPD":
                case "Attack SPD": return "AttackSPD";
                case "Bullet SPD": return "BulletSPD";
                case "Projectile SPD": return "ProjectileSPD";
                case "Reload": return "Reload";
                case "Ammo": return "Ammo";
                case "Bullets": return "Bullets";
                case "Range": return "Range";
                case "Bounces": return "Bounces";
                case "Bursts": return "Bursts";
                case "Gravity": return "Gravity";
                default: return label;
            }
        }

        private float GetNumeric(string key) => _numbers.TryGetValue(key, out var value) ? value : float.NaN;

        private bool HasCount(string key) => TryGetNumeric(key, out var value) && value > 0.05f;

        private static string FormatCount(float value)
        {
            return Mathf.Abs(value - Mathf.Round(value)) < 0.05f
                ? Mathf.Round(value).ToString("F0")
                : value.ToString("F1");
        }

        internal void WriteCount(string key, float value)
        {
            var clamped = Mathf.Max(0f, value);
            Set(key, clamped, FormatCount(clamped));
        }

        private void Set(string key, float numeric, string display)
        {
            _numbers[key] = numeric;
            _display[key] = display;
        }

        private PlayerStatsSnapshot CloneMeta()
        {
            return new PlayerStatsSnapshot
            {
                PlayerId = PlayerId,
                PlayerName = PlayerName
            };
        }
    }
}
