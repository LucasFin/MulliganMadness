using System;
using System.Collections.Generic;
using System.Linq;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using ModdingUtils.Extensions;
using ModdingUtils.Utils;
using CardsApi = ModdingUtils.Utils.Cards;
using MulliganMadness.Cards;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public static class StealLedger
    {
        private static readonly HashSet<int> ThiefUsedThisGame = new HashSet<int>();
        private static readonly HashSet<int> PendingThiefPrompt = new HashSet<int>();
        private static readonly Dictionary<int, string> PendingTakebackByVictim = new Dictionary<int, string>();

        internal static void ResetForNewGame()
        {
            ThiefUsedThisGame.Clear();
            PendingThiefPrompt.Clear();
            PendingTakebackByVictim.Clear();
            TakebacksiesBlacklist.EnsureGlobalBlacklist();
        }

        internal static bool HasUsedThief(Player player) =>
            player != null && ThiefUsedThisGame.Contains(player.playerID);

        internal static bool HasAnyStealableTarget(Player thief)
        {
            if (thief == null || PlayerManager.instance?.players == null) return false;

            foreach (var other in PlayerManager.instance.players)
            {
                if (other == null || other.playerID == thief.playerID) continue;
                if (StealRules.CountStealableCards(thief, other) > 0) return true;
            }

            return false;
        }

        internal static void TryPromptSteal(Player thief)
        {
            if (thief == null || !SessionSettings.Current.EnableThiefCard) return;
            if (HasUsedThief(thief))
            {
                Plugin.Instance.Log($"Player {thief.playerID} already used Thief this game.");
                return;
            }

            if (!HasAnyStealableTarget(thief))
            {
                PendingThiefPrompt.Add(thief.playerID);
                PlayerNotice.Show(thief, "Nobody has cards to steal yet.");
                Plugin.Instance.Log($"Thief deferred for player {thief.playerID} — no stealable targets.");
                return;
            }

            PendingThiefPrompt.Remove(thief.playerID);
            StealUi.TryOpen(thief);
        }

        internal static void TryOpenDeferredThiefPrompt()
        {
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker?.data?.view == null || !picker.data.view.IsMine) return;
            if (!PendingThiefPrompt.Contains(picker.playerID)) return;
            if (HasUsedThief(picker)) return;
            if (!HasAnyStealableTarget(picker)) return;

            PendingThiefPrompt.Remove(picker.playerID);
            StealUi.TryOpen(picker);
        }

        internal static void OnStealUiOpened(Player thief)
        {
            PendingThiefPrompt.Remove(thief.playerID);
        }

        internal static void OnStealUiClosedWithoutSteal(Player thief)
        {
            if (HasUsedThief(thief)) return;
            if (!HasAnyStealableTarget(thief)) PendingThiefPrompt.Add(thief.playerID);
        }

        internal static void RequestSteal(Player thief, Player victim, CardInfo card)
        {
            if (thief == null || victim == null || card == null) return;
            if (!StealRules.IsStealable(thief, victim, card, out var reason))
            {
                PlayerNotice.Show(thief, reason ?? "Can't steal that card.");
                return;
            }

            var index = CardEncoding.FindCardIndex(victim, CardEncoding.Encode(card));
            if (index < 0)
            {
                PlayerNotice.Show(thief, "That card is gone.");
                return;
            }

            NetworkingManager.RPC(
                typeof(StealLedger),
                nameof(RPCA_ExecuteSteal),
                thief.playerID,
                victim.playerID,
                CardEncoding.Encode(card),
                index);
        }

        [UnboundRPC]
        public static void RPCA_ExecuteSteal(int thiefId, int victimId, string payload, int victimCardIndex)
        {
            var thief = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == thiefId);
            var victim = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == victimId);
            if (thief == null || victim == null)
            {
                Plugin.Instance.LogWarn("Steal RPC failed — player missing.");
                return;
            }

            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            var card = CardEncoding.Resolve(payload);
            if (card == null)
            {
                Plugin.Instance.LogWarn("Steal RPC failed — card missing.");
                return;
            }

            if (!StealRules.IsStealable(thief, victim, card, out _))
            {
                Plugin.Instance.LogWarn("Steal RPC rejected — card not stealable.");
                return;
            }

            var liveIndex = CardEncoding.FindCardIndex(victim, payload);
            if (liveIndex < 0) liveIndex = victimCardIndex;
            if (liveIndex < 0 || victim.data?.currentCards == null || liveIndex >= victim.data.currentCards.Count)
            {
                Plugin.Instance.LogWarn("Steal RPC failed — card index invalid.");
                return;
            }

            CardsApi.instance.RemoveCardFromPlayer(victim, liveIndex);
            CardsApi.instance.AddCardToPlayer(thief, card, false, "", 2f, 2f, true);

            ThiefUsedThisGame.Add(thiefId);
            PendingThiefPrompt.Remove(thiefId);
            PendingTakebackByVictim[victimId] = payload;

            Plugin.Instance.Log($"Player {thiefId} stole '{card.cardName}' from player {victimId}.");
        }

        internal static bool HasPendingTakeback(int victimId) =>
            PendingTakebackByVictim.ContainsKey(victimId);

        internal static string GetPendingTakebackPayload(int victimId)
        {
            PendingTakebackByVictim.TryGetValue(victimId, out var payload);
            return payload;
        }

        internal static void TryExecuteTakeback(Player victim)
        {
            if (victim == null) return;
            if (!PendingTakebackByVictim.TryGetValue(victim.playerID, out var payload))
            {
                PlayerNotice.Show(victim, "Nothing to take back.");
                return;
            }

            NetworkingManager.RPC(typeof(StealLedger), nameof(RPCA_ExecuteTakeback), victim.playerID, payload);
        }

        [UnboundRPC]
        public static void RPCA_ExecuteTakeback(int victimId, string payload)
        {
            var victim = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == victimId);
            if (victim == null) return;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            var holder = CardEncoding.FindHolder(payload);
            if (holder == null)
            {
                PendingTakebackByVictim.Remove(victimId);
                Plugin.Instance.LogWarn($"Takeback cleared — stolen card no longer exists for victim {victimId}.");
                return;
            }

            var index = CardEncoding.FindCardIndex(holder, payload);
            if (index < 0)
            {
                PendingTakebackByVictim.Remove(victimId);
                return;
            }

            var card = CardEncoding.Resolve(payload);
            if (card == null)
            {
                PendingTakebackByVictim.Remove(victimId);
                return;
            }

            CardsApi.instance.RemoveCardFromPlayer(holder, index);
            CardsApi.instance.AddCardToPlayer(victim, card, false, "", 2f, 2f, true);
            PendingTakebackByVictim.Remove(victimId);

            Plugin.Instance.Log($"Player {victimId} took back '{card.cardName}' from player {holder.playerID}.");
        }
    }

    internal static class TakebacksiesBlacklist
    {
        internal static void EnsureGlobalBlacklist()
        {
            if (Takebacksies.Card == null) return;
            if (PlayerManager.instance?.players == null) return;
            if (CustomCardCategories.instance == null) return;

            var category = MMCard.TakebacksiesOnlyCategory;
            if (category == null) return;

            foreach (var player in PlayerManager.instance.players)
            {
                if (player?.data?.stats == null) continue;
                try
                {
                    var list = player.data.stats.GetAdditionalData().blacklistedCategories;
                    if (list != null && !list.Contains(category)) list.Add(category);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    internal static class PlayerNotice
    {
        internal static void Show(Player player, string message)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            Plugin.Instance.Log(message);
            CardTargetUi.ShowToast(message);
        }
    }
}
