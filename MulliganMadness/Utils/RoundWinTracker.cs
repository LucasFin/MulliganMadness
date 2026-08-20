using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class RoundWinTracker
    {
        private static readonly Dictionary<int, int> WinsByTeam = new Dictionary<int, int>();

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEnd);
        }

        internal static void Reset() => WinsByTeam.Clear();

        internal static int GetTeamWins(int teamId) =>
            WinsByTeam.TryGetValue(teamId, out var wins) ? wins : 0;

        internal static int GetDeficit(Player player)
        {
            if (player == null) return 0;
            var teamWins = GetTeamWins(player.teamID);
            var leader = 0;
            foreach (var wins in WinsByTeam.Values)
            {
                if (wins > leader) leader = wins;
            }

            return Mathf.Max(0, leader - teamWins);
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            Reset();
            yield break;
        }

        private static IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            yield return null;
            RecordWinners(gm);
            yield break;
        }

        private static void RecordWinners(IGameModeHandler gm)
        {
            if (gm?.GameMode == null) return;

            try
            {
                var method = AccessTools.Method(gm.GameMode.GetType(), "GetRoundWinners");
                if (method == null) return;

                var result = method.Invoke(gm.GameMode, null);
                if (result is IList winners)
                {
                    foreach (var entry in winners)
                    {
                        if (entry == null) continue;
                        var teamId = entry is int i ? i : (entry is Player p ? p.teamID : -1);
                        if (teamId < 0) continue;
                        WinsByTeam.TryGetValue(teamId, out var current);
                        WinsByTeam[teamId] = current + 1;
                    }

                    return;
                }

                if (result is IEnumerable<int> teamIds)
                {
                    foreach (var teamId in teamIds)
                    {
                        WinsByTeam.TryGetValue(teamId, out var current);
                        WinsByTeam[teamId] = current + 1;
                    }
                }
            }
            catch
            {
                // Unknown game mode — mercy vote won't track wins this match.
            }
        }
    }
}
