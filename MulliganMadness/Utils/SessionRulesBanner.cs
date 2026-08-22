using System.Collections;
using MulliganMadness.UI;
using UnboundLib.GameModes;

namespace MulliganMadness.Utils
{
    internal static class SessionRulesBanner
    {
        private static bool _shownThisGame;

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            _shownThisGame = false;
            yield break;
        }

        private static IEnumerator OnPickStart(IGameModeHandler gm)
        {
            if (_shownThisGame) yield break;
            _shownThisGame = true;

            var summary = SessionRulesSummary.BuildOneLine(SessionSettings.Current);
            CardTargetUi.ShowToast($"MulliganMadness rules: {summary}");
            Plugin.Instance?.Log($"Session rules banner: {summary}");
            yield break;
        }
    }
}
