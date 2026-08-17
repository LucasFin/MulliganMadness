using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TabInfo.Utils
{
    /// <summary>
    /// TabInfo-compatible extension API shipped with Mulligan Madness so Root/NullManager mods keep working.
    /// </summary>
    public static class TabInfoManager
    {
        private static readonly Dictionary<string, StatCategory> CategoriesInternal = new Dictionary<string, StatCategory>();

        public static ReadOnlyDictionary<string, StatCategory> Categories =>
            new ReadOnlyDictionary<string, StatCategory>(CategoriesInternal);

        public static StatCategory basicStats { get; private set; }

        public static int CurrentRound { get; internal set; }
        public static int CurrentPoint { get; internal set; }

        static TabInfoManager()
        {
            basicStats = RegisterCategory("Basic Stats", -1);
            RegisterStat(basicStats, "HP", _ => true, p => $"{p.data.health:F0}/{p.data.maxHealth:F0}");
            RegisterStat(basicStats, "Damage", _ => true, p =>
            {
                var gun = p.data.weaponHandler.gun;
                return $"{(gun.damage * 55f) * gun.bulletDamageMultiplier:F0}";
            });
            RegisterStat(basicStats, "Block Cooldown", _ => true, p => $"{p.data.block.Cooldown():F2}s");
            RegisterStat(basicStats, "Movespeed", _ => true, p => $"{p.data.stats.movementSpeed:F2}");
        }

        public static StatCategory RegisterCategory(string name, int priority)
        {
            var key = name.ToLower();
            if (CategoriesInternal.ContainsKey(key))
            {
                return CategoriesInternal[key];
            }

            var category = new StatCategory(name, priority);
            CategoriesInternal[key] = category;
            return category;
        }

        public static Stat RegisterStat(StatCategory category, string name, Func<Player, bool> displayCondition, Func<Player, string> displayValue)
        {
            return category.RegisterStat(name, displayCondition, displayValue);
        }

        public static IEnumerable<(StatCategory category, Stat stat, string value)> GetVisibleStats(Player player)
        {
            if (player == null) yield break;

            foreach (var category in CategoriesInternal.Values.OrderBy(c => c.priority).ThenBy(c => c.name))
            {
                foreach (var stat in category.Stats.Values.OrderBy(s => s.name))
                {
                    if (!stat.DisplayCondition(player)) continue;
                    yield return (category, stat, stat.DisplayValue(player));
                }
            }
        }
    }

    public class StatCategory
    {
        public readonly string name;
        public readonly int priority;

        private readonly Dictionary<string, Stat> _stats = new Dictionary<string, Stat>();

        public ReadOnlyDictionary<string, Stat> Stats => new ReadOnlyDictionary<string, Stat>(_stats);

        internal StatCategory(string name, int priority)
        {
            this.name = name;
            this.priority = priority;
        }

        internal Stat RegisterStat(string name, Func<Player, bool> condition, Func<Player, string> value)
        {
            var stat = new Stat(name, this, condition, value);
            _stats[name.ToLower()] = stat;
            return stat;
        }
    }

    public class Stat
    {
        public readonly string name;
        internal StatCategory category;
        private readonly Func<Player, bool> displayCondition;
        private readonly Func<Player, string> displayValue;

        internal Stat(string name, StatCategory category, Func<Player, bool> condition, Func<Player, string> value)
        {
            this.name = name;
            this.category = category;
            displayCondition = condition;
            displayValue = value;
        }

        internal bool DisplayCondition(Player player)
        {
            try { return displayCondition(player); }
            catch { return false; }
        }

        internal string DisplayValue(Player player)
        {
            try { return displayValue(player); }
            catch { return "-"; }
        }
    }
}
