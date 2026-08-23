using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MulliganMadness.Cards;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;
using CardsApi = ModdingUtils.Utils.Cards;

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
        // Faster than KeysCards' Golden Egg (3), but weaker loot.
        internal const int SilverHatchRounds = 2;

        private struct Hatch
        {
            public int PlayerId;
            public int RoundsLeft;
            public EggKind Kind;
        }

        private static readonly List<Hatch> Pending = new List<Hatch>();
        private static readonly Dictionary<int, int> NestCharges = new Dictionary<int, int>();
        private static bool _ignoringSilverCardRemoval;

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEnd);
        }

        internal static void ResetForNewGame()
        {
            Pending.Clear();
            NestCharges.Clear();
            _ignoringSilverCardRemoval = false;
        }

        internal static int HatchRounds(EggKind kind) =>
            kind == EggKind.Silver ? SilverHatchRounds : NestHatchRounds;

        internal static int Charges(Player player, EggKind kind)
        {
            if (kind != EggKind.Nest || player == null) return 0;
            return NestCharges.TryGetValue(player.playerID, out var n) ? n : 0;
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

        internal static bool HasCharge(Player player, EggKind kind) =>
            kind == EggKind.Nest && Charges(player, kind) > 0;

        internal static bool TryConsumeCharge(int playerId, EggKind kind)
        {
            if (kind != EggKind.Nest) return false;
            if (!NestCharges.TryGetValue(playerId, out var n) || n <= 0) return false;
            NestCharges[playerId] = n - 1;
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
                    ? $"Silver Egg: hatches into random cards in {rounds} rounds."
                    : $"Nest Egg: curse-free Take All hatches in {rounds} rounds.");
        }

        internal static void NotifyRemoved(Player player, EggKind kind)
        {
            if (player == null) return;
            if (kind == EggKind.Silver && _ignoringSilverCardRemoval) return;

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

            if (kind == EggKind.Silver)
            {
                if (pending > 0 && next >= 0)
                {
                    return pending == 1
                        ? "hatches in " + RoundWord(next)
                        : pending + " eggs, next in " + RoundWord(next);
                }

                return "";
            }

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

        internal static bool ShowStat(Player player, EggKind kind) =>
            Charges(player, kind) > 0 || PendingCount(player, kind) > 0;

        private static string RoundWord(int n)
        {
            if (n <= 0) return "this pick";
            return n == 1 ? "1 round" : n + " rounds";
        }

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
            if (kind == EggKind.Nest)
            {
                NestCharges.TryGetValue(playerId, out var current);
                var next = current + 1;
                NestCharges[playerId] = next;

                var nestPlayer = FindPlayer(playerId);
                if (nestPlayer?.data?.view == null || !nestPlayer.data.view.IsMine) return;
                CardTargetUi.ShowToast(
                    next == 1
                        ? "Nest Egg hatched: 1 curse-free Take All ready."
                        : $"Nest Egg hatched: {next} curse-free Take Alls ready.");
                return;
            }

            // Silver Egg: roll once on the host, sync the same loot. All clients
            // used to roll independently (different cards per screen).
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            var hatchPlayer = FindPlayer(playerId);
            if (hatchPlayer == null) return;
            var grants = BuildSilverLoot(hatchPlayer);
            var payloads = new List<string>();
            foreach (var card in grants)
            {
                if (card == null) continue;
                payloads.Add(CardEncoding.Encode(card));
            }

            NetworkingManager.RPC(typeof(NestEggManager), nameof(RPCA_HatchSilver), playerId, payloads.ToArray());
        }

        [UnboundRPC]
        public static void RPCA_HatchSilver(int playerId, string[] payloads)
        {
            var player = FindPlayer(playerId);
            if (player == null)
            {
                Plugin.Instance.LogWarn("Silver Egg hatch failed - player missing.");
                return;
            }

            var grants = new List<CardInfo>();
            if (payloads != null)
            {
                foreach (var payload in payloads)
                {
                    var card = CardEncoding.Resolve(payload);
                    if (card != null) grants.Add(card);
                }
            }

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                RemoveOneSilverEgg(player);
                foreach (var card in grants)
                {
                    if (card == null) continue;
                    CardsApi.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
                }
            }

            if (player.data?.view != null && player.data.view.IsMine)
            {
                var names = string.Join(", ", grants.Where(c => c != null).Select(c => c.cardName));
                CardTargetUi.ShowToast(
                    grants.Count == 0
                        ? "Silver Egg hatched, but no cards were available."
                        : $"Silver Egg hatched: {names}");
            }

            Plugin.Instance.Log($"Silver Egg hatched for player {playerId} -> {grants.Count} card(s).");
        }

        // Weaker than Keys Golden Egg (3-4 cards / rare / treasure / blessing rolls).
        // Roll 0-99: 55% one common, 30% two commons, 15% one uncommon.
        private static List<CardInfo> BuildSilverLoot(Player player)
        {
            var roll = UnityEngine.Random.Range(0, 100);
            var grants = new List<CardInfo>();
            if (roll < 55)
            {
                AddRandomOfRarity(player, grants, CardInfo.Rarity.Common, 1);
            }
            else if (roll < 85)
            {
                AddRandomOfRarity(player, grants, CardInfo.Rarity.Common, 2);
            }
            else
            {
                AddRandomOfRarity(player, grants, CardInfo.Rarity.Uncommon, 1);
            }

            return grants;
        }

        private static void AddRandomOfRarity(Player player, List<CardInfo> into, CardInfo.Rarity rarity, int count)
        {
            for (var n = 0; n < count; n++)
            {
                var card = PickRandom(player, rarity, into);
                if (card != null) into.Add(card);
            }
        }

        private static CardInfo PickRandom(Player player, CardInfo.Rarity rarity, List<CardInfo> already)
        {
            var all = CardsApi.all;
            if (all == null || all.Count == 0) return null;

            var options = new List<CardInfo>();
            foreach (var card in all)
            {
                if (card == null) continue;
                if (card.rarity != rarity) continue;
                if (IsBlockedHatchCard(card)) continue;
                if (already.Contains(card)) continue;
                if (!CardPool.IsActive(card)) continue;

                try
                {
                    if (!CardsApi.instance.PlayerIsAllowedCard(player, card)) continue;
                }
                catch
                {
                    continue;
                }

                options.Add(card);
            }

            if (options.Count == 0) return null;
            return options[UnityEngine.Random.Range(0, options.Count)];
        }

        private static bool IsBlockedHatchCard(CardInfo card)
        {
            if (SilverEgg.Card != null && card == SilverEgg.Card) return true;
            if (NestEgg.Card != null && card == NestEgg.Card) return true;
            var name = card.cardName ?? "";
            if (name.Equals(SilverEgg.Title, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals(NestEgg.Title, StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("The Golden Egg", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void RemoveOneSilverEgg(Player player)
        {
            var cards = player.data?.currentCards;
            if (cards == null) return;

            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var card = cards[i];
                if (card == null) continue;
                var match = (SilverEgg.Card != null && card == SilverEgg.Card)
                            || string.Equals(card.cardName, SilverEgg.Title, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;

                _ignoringSilverCardRemoval = true;
                try
                {
                    CardsApi.instance.RemoveCardFromPlayer(player, i);
                }
                finally
                {
                    _ignoringSilverCardRemoval = false;
                }

                return;
            }
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
