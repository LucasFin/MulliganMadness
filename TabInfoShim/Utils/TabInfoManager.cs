using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace TabInfo.Utils
{
    /// <summary>
    /// Public surface matches willuwontu TabInfo so Root/NullManager can register stats
    /// without the original UI bundle. Mulligan Madness owns the actual overlay.
    /// </summary>
    public static class TabInfoManager
    {
        private static readonly Dictionary<string, StatCategory> _categories = new Dictionary<string, StatCategory>();

        public static ReadOnlyDictionary<string, StatCategory> Categories =>
            new ReadOnlyDictionary<string, StatCategory>(_categories);

        // RootCore.TabInfoRegesterer reads this as a field, not a property.
        public static readonly StatCategory basicStats;

        public static int CurrentRound { get; internal set; }
        public static int CurrentPoint { get; internal set; }

        internal static GameObject canvas;
        internal static GameObject tabFrameTemplate;
        internal static GameObject teamFrameTemplate;
        internal static GameObject playerFrameTemplate;
        internal static GameObject cardButtonTemplate;
        internal static GameObject statSectionTemplate;
        internal static GameObject statObjectTemplate;
        internal static GameObject cardHolderTemplate;
        internal static TabFrame tabFrame = null;

        private static readonly List<string> hiddenGameModes = new List<string>();

        static TabInfoManager()
        {
            basicStats = new StatCategory("Basic Stats", -1);
            _categories.Add(basicStats.name.ToLower(), basicStats);

            basicStats.RegisterStat("HP", _ => true, player =>
                string.Format("{0:F0}/{1:F0}", player.data.health, player.data.maxHealth));
            basicStats.RegisterStat("Damage", _ => true, player =>
            {
                var gun = player.data.weaponHandler.gun;
                return string.Format("{0:F0}", gun.damage * gun.bulletDamageMultiplier * 55f);
            });
            basicStats.RegisterStat("Block Cooldown", _ => true, player =>
                string.Format("{0:F2}s", player.data.block.Cooldown()));
            basicStats.RegisterStat("Movespeed", _ => true, player =>
                string.Format("{0:F2}", player.data.stats.movementSpeed));
        }

        public static StatCategory RegisterCategory(string name, int priority)
        {
            if (priority < 0)
            {
                throw new ArgumentException("Category priority cannot be less than 0.");
            }

            var key = name.ToLower();
            if (_categories.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var category = new StatCategory(name, priority);
            _categories.Add(key, category);
            return category;
        }

        public static Stat RegisterStat(StatCategory category, string name, Func<Player, bool> displayCondition, Func<Player, string> displayValue)
        {
            if (category.Stats.ContainsKey(name.ToLower()))
            {
                return category.Stats[name.ToLower()];
            }

            return category.RegisterStat(name, displayCondition, displayValue);
        }

        public static int RoundsToWin
        {
            get
            {
                try
                {
                    return (int)UnboundLib.GameModes.GameModeManager.CurrentHandler.Settings["roundsToWinGame"];
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static int PointsToWin
        {
            get
            {
                try
                {
                    return (int)UnboundLib.GameModes.GameModeManager.CurrentHandler.Settings["pointsToWinRound"];
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static bool IsLockingInput => tabFrame != null && tabFrame.gameObject.activeSelf;

        public static void RegisterHiddenGameMode(string gameModeID)
        {
            if (!string.IsNullOrEmpty(gameModeID) && !hiddenGameModes.Contains(gameModeID))
            {
                hiddenGameModes.Add(gameModeID);
            }
        }

        public static void ToggleTabFrame()
        {
            // UI is owned by Mulligan Madness; keep this as a no-op so TabInfo consumers don't NRE.
        }

        internal static void EnsureTemplates()
        {
            if (canvas != null) return;

            canvas = new GameObject("MM_TabInfoCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvas);
            canvas.SetActive(false);

            tabFrameTemplate = HiddenChild(canvas, "TabFrameTemplate");
            teamFrameTemplate = HiddenChild(canvas, "TeamFrameTemplate");
            playerFrameTemplate = HiddenChild(canvas, "PlayerFrameTemplate");
            cardButtonTemplate = HiddenChild(canvas, "CardButtonTemplate");
            statSectionTemplate = HiddenChild(canvas, "StatSectionTemplate");
            statObjectTemplate = HiddenChild(canvas, "StatObjectTemplate");
            cardHolderTemplate = HiddenChild(canvas, "CardHolderTemplate");
        }

        private static GameObject HiddenChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.SetActive(false);
            return go;
        }

        public static IEnumerable<(StatCategory category, Stat stat, string value)> GetVisibleStats(Player player)
        {
            if (player == null) yield break;

            foreach (var category in _categories.Values.OrderBy(c => c.priority).ThenBy(c => c.name))
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
            var key = name.ToLower();
            if (_stats.TryGetValue(key, out var existing)) return existing;

            var result = new Stat(name, this, condition, value);
            _stats.Add(key, result);
            return result;
        }

        internal bool DisplayCondition(Player player)
        {
            return Stats.Values.Any(stat => stat.DisplayCondition(player));
        }
    }

    public class Stat
    {
        public readonly string name;
        internal StatCategory category;
        private readonly Func<Player, string> displayValue;
        private readonly Func<Player, bool> displayCondition;

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
