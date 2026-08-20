using System.Text;

namespace MulliganMadness.Utils
{
    internal static class SessionRulesSummary
    {
        internal static string BuildOneLine(SessionSettingsData s)
        {
            if (s == null) return "Mulligan Madness";

            var parts = new StringBuilder();

            if (!s.EnableTakeAll) parts.Append("Take All off");
            else
            {
                switch (s.TakeAllMode)
                {
                    case TakeAllMode.OncePerGame:
                        parts.Append("Take All once");
                        break;
                    case TakeAllMode.MultiUse:
                        parts.Append($"Take All x{s.TakeAllUsesPerPlayer}");
                        break;
                    case TakeAllMode.Vote:
                        parts.Append("Vote Take All");
                        break;
                }

                if (s.TakeAllCurseCost && s.EnableAutoPickCurses) parts.Append(" + curse");
            }

            if (s.EnableMercyVote) parts.Append(Separator(parts)).Append($"Mercy -{s.MercyRoundDeficit}");

            if (s.EnableAutoPickCurses) parts.Append(Separator(parts)).Append($"Panic {s.PanicTimerSeconds:0.#}s");
            else parts.Append(Separator(parts)).Append("No curses");

            if (!s.EnableThiefCard) parts.Append(Separator(parts)).Append("No Thief");
            if (!s.EnableSandbagSimulator) parts.Append(Separator(parts)).Append("No Sandbag");
            if (!s.EnableJarOfDirt) parts.Append(Separator(parts)).Append("No Jar");

            return parts.Length > 0 ? parts.ToString() : "Default rules";
        }

        private static string Separator(StringBuilder sb) => sb.Length > 0 ? " · " : "";
    }
}
