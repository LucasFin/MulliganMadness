using System;
using System.Collections;
using System.Collections.Generic;
using MulliganMadness.Cards;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class NestEggManager
    {
        internal const int HatchRounds = 3;

        private sealed class Hatch
        {
            public int PlayerId;
            public int RoundsLeft;
        }

        private static readonly List<Hatch> Pending = new List<Hatch>();

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEnd);
        }

        internal static void ResetForNewGame()
        {
            Pending.Clear();
        }

        internal static int PendingCount(Player player)
        {
            if (player == null) return 0;
            var count = 0;
            foreach (var hatch in Pending)
            {
                if (hatch.PlayerId == player.playerID) count++;
            }

            return count;
        }

        internal static int NextHatchRounds(Player player)
        {
            if (player == null) return -1;
            var best = int.MaxValue;
            foreach (var hatch in Pending)
            {
                if (hatch.PlayerId != player.playerID) continue;
                if (hatch.RoundsLeft < best) best = hatch.RoundsLeft;
            }

            return best == int.MaxValue ? -1 : best;
        }

        internal static void NotifyGained(Player player)
        {
            if (player == null) return;
            // Simulacrum runs ApplyStats twice for one copy. Cap hatches to owned eggs.
            var owned = CountOwned(player);
            var tracked = PendingCount(player) + TakeAllManager.BonusCount(player);
            if (tracked >= Math.Max(owned, 1)) return;

            Pending.Add(new Hatch { PlayerId = player.playerID, RoundsLeft = HatchRounds });
            if (player.data?.view == null || !player.data.view.IsMine) return;
            CardTargetUi.ShowToast($"Nest Egg: curse-free Take All hatches in {HatchRounds} rounds.");
        }

        internal static void NotifyRemoved(Player player)
        {
            if (player == null) return;

            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                if (Pending[i].PlayerId != player.playerID) continue;
                Pending.RemoveAt(i);
                return;
            }

            TakeAllManager.TryConsumeBonus(player.playerID);
        }

        internal static string StatusText(Player player)
        {
            var ready = TakeAllManager.BonusCount(player);
            var pending = PendingCount(player);
            var next = NextHatchRounds(player);

            if (ready > 0 && pending > 0)
            {
                return $"{ready} ready, next in {RoundWord(next)}";
            }

            if (ready > 0)
            {
                return ready == 1 ? "1 curse-free Take All" : ready + " curse-free Take Alls";
            }

            if (pending > 0 && next >= 0)
            {
                return pending == 1
                    ? "hatches in " + RoundWord(next)
                    : pending + " eggs, next in " + RoundWord(next);
            }

            return "";
        }

        internal static bool ShowStat(Player player) =>
            TakeAllManager.BonusCount(player) > 0 || PendingCount(player) > 0;

        private static string RoundWord(int n)
        {
            if (n <= 0) return "this pick";
            return n == 1 ? "1 round" : n + " rounds";
        }

        private static int CountOwned(Player player)
        {
            var cards = player?.data?.currentCards;
            if (cards == null) return 0;
            var count = 0;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (NestEgg.Card != null && card == NestEgg.Card) count++;
                else if (string.Equals(card.cardName, NestEgg.Title, StringComparison.OrdinalIgnoreCase)) count++;
            }

            return count;
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            ResetForNewGame();
            yield break;
        }

        private static IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            yield return null;
            TickHatches();
        }

        private static void TickHatches()
        {
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                var hatch = Pending[i];
                hatch.RoundsLeft--;
                if (hatch.RoundsLeft > 0) continue;

                Pending.RemoveAt(i);
                HatchNow(hatch.PlayerId);
            }
        }

        private static void HatchNow(int playerId)
        {
            TakeAllManager.GrantBonusCharge(playerId);

            var player = TakeAllManager.FindPlayer(playerId);
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            var next = TakeAllManager.BonusCount(player);
            CardTargetUi.ShowToast(
                next == 1
                    ? "Nest Egg hatched: 1 curse-free Take All ready."
                    : $"Nest Egg hatched: {next} curse-free Take Alls ready.");
        }
    }
}
