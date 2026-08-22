using System.Collections.Generic;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using ModdingUtils.Extensions;
using MulliganMadness.Curses;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using CardsApi = ModdingUtils.Utils.Cards;

namespace MulliganMadness.Utils
{
    internal static class ReturnToSenderManager
    {
        internal static bool SenderHasCurse(Player player) => FindCurses(player).Count > 0;

        internal static void TryPrompt(Player sender)
        {
            if (sender == null) return;
            if (CardTargetUi.IsOpen) return;
            if (!SenderHasCurse(sender))
            {
                PlayerNotice.Show(sender, "Return to Sender needs a Mulligan Madness curse.");
                return;
            }

            CardTargetUi.OpenPlayerTarget(
                sender,
                "Return to Sender",
                "Give your Mulligan Madness curse to this player. They keep any curse they already have.",
                "SEND CURSE",
                target =>
                {
                    if (target == null) return;
                    NetworkingManager.RPC(typeof(ReturnToSenderManager), nameof(RPCA_Transfer), sender.playerID, target.playerID);
                },
                includeSelf: false);
        }

        [UnboundRPC]
        public static void RPCA_Transfer(int senderId, int targetId)
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            var sender = FindPlayer(senderId);
            var target = FindPlayer(targetId);
            if (sender == null || target == null || sender.playerID == target.playerID) return;

            var curses = FindCurses(sender);
            if (curses.Count == 0) return;

            foreach (var curse in curses)
            {
                RemoveCard(sender, curse);
                AddIgnoringExclusive(target, curse);
            }

            NetworkingManager.RPC(
                typeof(ReturnToSenderManager),
                nameof(RPCA_Announce),
                PlayerLabel(sender, senderId),
                PlayerLabel(target, targetId),
                curses[0].cardName ?? "a curse");
        }

        [UnboundRPC]
        public static void RPCA_Announce(string senderName, string targetName, string curseName)
        {
            CardTargetUi.ShowToast($"{senderName} sent {curseName} to {targetName}.");
        }

        private static List<CardInfo> FindCurses(Player player)
        {
            var result = new List<CardInfo>();
            var cards = player?.data?.currentCards;
            if (cards == null) return result;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (HasExclusiveCategory(card)) result.Add(card);
            }

            return result;
        }

        private static bool HasExclusiveCategory(CardInfo card)
        {
            if (card.categories == null) return false;
            foreach (var category in card.categories)
            {
                if (category != null && category.name == AutoPickCurse.ExclusiveCategoryName) return true;
            }

            return false;
        }

        private static void RemoveCard(Player player, CardInfo card)
        {
            var cards = player?.data?.currentCards;
            if (cards == null || card == null) return;
            for (var i = 0; i < cards.Count; i++)
            {
                if (cards[i] != card) continue;
                CardsApi.instance.RemoveCardFromPlayer(player, i);
                return;
            }
        }

        private static void AddIgnoringExclusive(Player player, CardInfo card)
        {
            if (player?.data?.stats == null || card == null) return;
            List<CardCategory> list = null;
            var removed = false;
            try
            {
                list = player.data.stats.GetAdditionalData().blacklistedCategories;
                if (list != null)
                {
                    var exclusive = CustomCardCategories.instance.CardCategory(AutoPickCurse.ExclusiveCategoryName);
                    removed = exclusive != null && list.Remove(exclusive);
                }
            }
            catch
            {
                list = null;
            }

            var wasMultiple = card.allowMultiple;
            card.allowMultiple = true;
            try
            {
                CardsApi.instance.AddCardToPlayer(player, card, false, "", 2f, 2f, true);
            }
            finally
            {
                card.allowMultiple = wasMultiple;
            }

            if (removed && list != null)
            {
                var exclusive = CustomCardCategories.instance.CardCategory(AutoPickCurse.ExclusiveCategoryName);
                if (exclusive != null && !list.Contains(exclusive)) list.Add(exclusive);
            }
        }

        private static Player FindPlayer(int playerId)
        {
            if (PlayerManager.instance?.players == null) return null;
            foreach (var player in PlayerManager.instance.players)
            {
                if (player != null && player.playerID == playerId) return player;
            }

            return null;
        }

        private static string PlayerLabel(Player player, int id)
        {
            var name = player?.data?.view?.Owner?.NickName;
            return string.IsNullOrEmpty(name) ? "Player " + (id + 1) : name;
        }
    }
}
