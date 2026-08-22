using System;
using System.Collections;
using System.Collections.Generic;
using MulliganMadness.Cards;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal enum EggKind
    {
        Nest,
        Silver
    }

    internal static class NestEggManager
    {
        internal const int NestHatchRounds = 3;
        internal const int SilverHatchRounds = 2;

        private struct Hatch
        {
            public int PlayerId;
            public int RoundsLeft;
            public EggKind Kind;
        }

        private static readonly List<Hatch> Pending = new List<Hatch>();
        private static readonly Dictionary<int, int> NestCharges = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> SilverCharges = new Dictionary<int, int>();

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEnd);
        }

        internal static void ResetForNewGame()
        {
            Pending.Clear();
            NestCharges.Clear();
            SilverCharges.Clear();
        }

        internal static int HatchRounds(EggKind kind) =>
            kind == EggKind.Silver ? SilverHatchRounds : NestHatchRounds;

        internal static int Charges(Player player, EggKind kind)
        {
            if (player == null) return 0;
            var map = Map(kind);
            return map.TryGetValue(player.playerID, out var n) ? n : 0;
        }

        internal static int PendingCount(Player player, EggKind kind)
        {
            if (player == null) return 0;
            var count = 0;
            foreach (var hatch in Pending)
            {
                if (hatch.PlayerId == player.playerID && hatch.Kind == kind) count++;
            }

            return count;
        }

        internal static int NextHatchRounds(Player player, EggKind kind)
        {
            if (player == null) return -1;
            var best = int.MaxValue;
            foreach (var hatch in Pending)
            {
                if (hatch.PlayerId != player.playerID || hatch.Kind != kind) continue;
                if (hatch.RoundsLeft < best) best = hatch.RoundsLeft;
            }

            return best == int.MaxValue ? -1 : best;
        }

        internal static bool HasCharge(Player player, EggKind kind) => Charges(player, kind) > 0;

        internal static bool TryConsumeCharge(int playerId, EggKind kind)
        {
            var map = Map(kind);
            if (!map.TryGetValue(playerId, out var n) || n <= 0) return false;
            map[playerId] = n - 1;
            return true;
        }

        internal static void NotifyGained(Player player, EggKind kind)
        {
            if (player == null) return;
            // Simulacrum runs ApplyStats twice for one copy. Cap hatches to owned eggs.
            var owned = CountOwned(player, kind);
            var tracked = PendingCount(player, kind) + Charges(player, kind);
            if (tracked >= Math.Max(owned, 1)) return;
            var rounds = HatchRounds(kind);
            Pending.Add(new Hatch { PlayerId = player.playerID, RoundsLeft = rounds, Kind = kind });
            if (player.data?.view == null || !player.data.view.IsMine) return;
            CardTargetUi.ShowToast(
                kind == EggKind.Silver
                    ? $"Silver Egg: curse-free half-hand Take All hatches in {rounds} rounds."
                    : $"Nest Egg: curse-free Take All hatches in {rounds} rounds.");
        }

        internal static void NotifyRemoved(Player player, EggKind kind)
        {
            if (player == null) return;
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                if (Pending[i].PlayerId != player.playerID || Pending[i].Kind != kind) continue;
                Pending.RemoveAt(i);
                return;
            }

            TryConsumeCharge(player.playerID, kind);
        }

        internal static string StatusText(Player player, EggKind kind)
        {
            var ready = Charges(player, kind);
            var pending = PendingCount(player, kind);
            var next = NextHatchRounds(player, kind);
            var unit = kind == EggKind.Silver ? "half-hand Take All" : "Take All";
            if (ready > 0 && pending > 0)
            {
                return $"{ready} ready, next in {RoundWord(next)}";
            }

            if (ready > 0)
            {
                return ready == 1 ? "1 curse-free " + unit : ready + " curse-free " + unit + "s";
            }

            if (pending > 0 && next >= 0)
            {
                return pending == 1
                    ? "hatches in " + RoundWord(next)
                    : pending + " eggs, next in " + RoundWord(next);
            }

            return "";
        }

        internal static bool ShowStat(Player player, EggKind kind) =>
            Charges(player, kind) > 0 || PendingCount(player, kind) > 0;

        private static string RoundWord(int n)
        {
            if (n <= 0) return "this pick";
            return n == 1 ? "1 round" : n + " rounds";
        }

        private static Dictionary<int, int> Map(EggKind kind) =>
            kind == EggKind.Silver ? SilverCharges : NestCharges;

        private static int CountOwned(Player player, EggKind kind)
        {
            var cards = player?.data?.currentCards;
            if (cards == null) return 0;
            var info = kind == EggKind.Silver ? SilverEgg.Card : NestEgg.Card;
            var title = kind == EggKind.Silver ? SilverEgg.Title : NestEgg.Title;
            var count = 0;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (info != null && card == info) count++;
                else if (string.Equals(card.cardName, title, StringComparison.OrdinalIgnoreCase)) count++;
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
                if (hatch.RoundsLeft > 0)
                {
                    Pending[i] = hatch;
                    continue;
                }

                Pending.RemoveAt(i);
                HatchNow(hatch.PlayerId, hatch.Kind);
            }
        }

        private static void HatchNow(int playerId, EggKind kind)
        {
            var map = Map(kind);
            map.TryGetValue(playerId, out var current);
            var next = current + 1;
            map[playerId] = next;

            var player = FindPlayer(playerId);
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            if (kind == EggKind.Silver)
            {
                CardTargetUi.ShowToast(
                    next == 1
                        ? "Silver Egg hatched: 1 curse-free half-hand Take All ready."
                        : $"Silver Egg hatched: {next} curse-free half-hand Take Alls ready.");
                return;
            }

            CardTargetUi.ShowToast(
                next == 1
                    ? "Nest Egg hatched: 1 curse-free Take All ready."
                    : $"Nest Egg hatched: {next} curse-free Take Alls ready.");
        }

        private static Player FindPlayer(int playerId)
        {
            var players = PlayerManager.instance?.players;
            if (players == null) return null;
            foreach (var player in players)
            {
                if (player != null && player.playerID == playerId) return player;
            }

            return null;
        }
    }
}
