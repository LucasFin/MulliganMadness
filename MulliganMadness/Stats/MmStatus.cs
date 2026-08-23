using System;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Curses;
using MulliganMadness.Utils;

namespace MulliganMadness.Stats
{
    /// <summary>
    /// Publishes Mulligan Madness' per-player statuses to willuwontu's TabInfo when it is installed.
    ///
    /// Reflection-only and entirely optional — this mod must never reference or ship
    /// TabInfo.dll. Shipping a stub that claims another mod's plugin GUID collides with the
    /// real mod and forces users to disable it.
    /// </summary>
    internal static class MmStatus
    {
        private const string CategoryName = "Mulligan Madness";
        private const int CategoryPriority = 45;

        private static MethodInfo _registerStat;
        private static object _category;

        internal static void Register()
        {
            if (!Bind()) return;

            try
            {
                Stat("Hard Edges",
                    player => CurseOwnership.Has(player, HardEdges.Card),
                    _ => "+60% edge bounce");

                Stat("Kickback",
                    player => CurseOwnership.Has(player, Kickback.Card),
                    _ => "Strong kick away from gun");

                Stat("Blind Draft",
                    player => CurseOwnership.Has(player, BlindDraft.Card),
                    _ => "Offers are face-down");

                Stat("Fumble",
                    player => CurseOwnership.Has(player, Fumble.Card),
                    _ => "50% chance to take a neighbour");

                Stat("Short Hand",
                    player => CurseOwnership.Has(player, ShortHand.Card),
                    _ => "One fewer card per offer");

                Stat("Nest Egg",
                    player => NestEggManager.ShowStat(player, EggKind.Nest),
                    player => NestEggManager.StatusText(player, EggKind.Nest));

                Stat("Silver Egg",
                    player => NestEggManager.ShowStat(player, EggKind.Silver),
                    player => NestEggManager.StatusText(player, EggKind.Silver));

                Plugin.Instance?.Log("Registered Mulligan Madness statuses with TabInfo.");
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"TabInfo stat registration skipped: {ex.Message}");
            }
        }

        private static bool Bind()
        {
            var manager = AccessTools.TypeByName("TabInfo.Utils.TabInfoManager");
            if (manager == null) return false;

            try
            {
                var registerCategory = AccessTools.Method(manager, "RegisterCategory");
                _registerStat = AccessTools.Method(manager, "RegisterStat");
                if (registerCategory == null || _registerStat == null) return false;

                _category = registerCategory.Invoke(null, new object[] { CategoryName, CategoryPriority });
                return _category != null;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"TabInfo bind failed: {ex.Message}");
                return false;
            }
        }

        private static void Stat(string name, Func<Player, bool> visible, Func<Player, string> value)
        {
            var parameters = _registerStat.GetParameters();
            if (parameters.Length != 4) return;

            // TabInfo takes its own delegate types; build them from ours.
            var visibleDelegate = Delegate.CreateDelegate(parameters[2].ParameterType, visible.Target, visible.Method);
            var valueDelegate = Delegate.CreateDelegate(parameters[3].ParameterType, value.Target, value.Method);
            _registerStat.Invoke(null, new[] { _category, name, visibleDelegate, valueDelegate });
        }
    }
}
