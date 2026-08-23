using MulliganMadness.Cards;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using TabInfo.Utils;

namespace MulliganMadness.Stats
{
    internal static class MmStatus
    {
        internal static void Register()
        {
            var category = TabInfoManager.RegisterCategory("Mulligan Madness", 45);
            TabInfoManager.RegisterStat(category, "Bozo Shoes",
                player => BozoShoesRuntime.IsMarked(player),
                _ => "Clown shoes, +50% knockback");
            TabInfoManager.RegisterStat(category, "Safety Net",
                player => CurseOwnership.Has(player, SafetyNet.Card),
                _ => "No edge damage · OOB escape kill");
            TabInfoManager.RegisterStat(category, "Hard Edges",
                player => CurseOwnership.Has(player, HardEdges.Card),
                _ => "+60% edge bounce");
            TabInfoManager.RegisterStat(category, "TASER TASER TASER",
                player => CurseOwnership.Has(player, TaserTaserTaser.Card),
                _ => "+0.5s stun on hit");
            TabInfoManager.RegisterStat(category, "Yeet Cannon",
                player => CurseOwnership.Has(player, YeetCannon.Card),
                _ => "Strong kick away from gun");
            TabInfoManager.RegisterStat(category, "Kickback",
                player => CurseOwnership.Has(player, Kickback.Card),
                _ => "Strong kick away from gun");
            TabInfoManager.RegisterStat(category, "Dynamite",
                player => CurseOwnership.Has(player, Dynamite.Card),
                _ => "Delayed blast, huge knockback");
            TabInfoManager.RegisterStat(category, "Draft Sniper",
                player => DraftSniperManager.Remaining(player) > 0,
                player => DraftSniperManager.Remaining(player) == 1
                    ? "Click to lock"
                    : DraftSniperManager.Remaining(player) + " locks");
            TabInfoManager.RegisterStat(category, "Nest Egg",
                player => NestEggManager.ShowStat(player, EggKind.Nest),
                player => NestEggManager.StatusText(player, EggKind.Nest));
            TabInfoManager.RegisterStat(category, "Silver Egg",
                player => NestEggManager.ShowStat(player, EggKind.Silver),
                player => NestEggManager.StatusText(player, EggKind.Silver));
        }
    }
}
