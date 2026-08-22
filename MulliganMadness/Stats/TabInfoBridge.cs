using System.Collections.Generic;
using TabInfo.Utils;

namespace MulliganMadness.Stats
{
    internal static class TabInfoBridge
    {
        internal static IEnumerable<(string category, string label, string value)> GetExtensionStats(Player player)
        {
            if (player == null) yield break;
            foreach (var row in TabInfoManager.GetVisibleStats(player))
            {
                if (row.category == TabInfoManager.basicStats) continue;
                yield return (row.category.name, row.stat.name, row.value);
            }
        }

        internal static IEnumerable<(string category, string label, string value)> GetMmStats(Player player)
        {
            if (player == null) yield break;
            foreach (var row in TabInfoManager.GetVisibleStats(player))
            {
                if (row.category == null || row.category == TabInfoManager.basicStats) continue;
                if (!string.Equals(row.category.name, "Mulligan Madness", System.StringComparison.OrdinalIgnoreCase)) continue;
                yield return (row.category.name, row.stat.name, row.value);
            }
        }
    }
}
