using MulliganMadness.Curses;
using ModdingUtils.Utils;
using MulliganMadness.Cards;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using CardsApi = ModdingUtils.Utils.Cards;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using WillsWackyManagers.Utils;

namespace MulliganMadness.Utils
{
    internal static class TakeAllCurseCost
    {
        [UnboundRPC]
        public static void RPCA_ApplyCurse(int playerId, int curseIndex)
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            var player = FindPlayer(playerId);
            if (player == null) return;

            var curse = ResolveCurse(curseIndex);
            if (curse == null) return;

            ApplyCurseToPlayer(player, curse);
        }

        internal static void TryApplyAfterTakeAll(int playerId)
        {
            if (!SessionSettings.Current.TakeAllCurseCost) return;
            var curseIndex = PickActiveCurseIndex();
            if (curseIndex < 0) return;
            NetworkingManager.RPC(typeof(TakeAllCurseCost), nameof(RPCA_ApplyCurse), playerId, curseIndex);
        }

        private static int PickActiveCurseIndex()
        {
            var options = new System.Collections.Generic.List<int>(8);
            if (CardPool.IsActive(ForcedChoice.Card)) options.Add(0);
            if (CardPool.IsActive(PanicPick.Card)) options.Add(1);
            if (CardPool.IsActive(LeftmostLuck.Card)) options.Add(2);
            if (CardPool.IsActive(BlindDraft.Card)) options.Add(3);
            if (CardPool.IsActive(ShortHand.Card)) options.Add(4);
            if (CardPool.IsActive(Fumble.Card)) options.Add(5);
            if (CardPool.IsActive(Kickback.Card)) options.Add(6);
            if (CardPool.IsActive(HardEdges.Card)) options.Add(7);
            if (options.Count == 0) return -1;
            return options[Random.Range(0, options.Count)];
        }

        private static void ApplyCurseToPlayer(Player player, CardInfo curse)
        {
            if (player == null || curse == null) return;

            var existing = FindExistingAutoPickCurse(player);
            if (existing != null)
            {
                if (SessionSettings.Current.CurseOnExisting == TakeAllCurseOnExisting.SkipCurse) return;
                RemoveAutoPickCurse(player, existing);
            }

            CardsApi.instance.AddCardToPlayer(player, curse, false, "", 2f, 2f, true);
            if (player.data?.view != null && player.data.view.IsMine)
            {
                CardTargetUi.ShowToast($"Take All curse: {curse.cardName}");
            }

            Plugin.Instance.Log($"Applied Take All curse '{curse.cardName}' to player {player.playerID}.");
        }

        private static CardInfo FindExistingAutoPickCurse(Player player)
        {
            var cards = player?.data?.currentCards;
            if (cards == null) return null;

            foreach (var card in cards)
            {
                if (card == null) continue;
                if (HasExclusiveCategory(card)) return card;
            }

            return null;
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

        private static void RemoveAutoPickCurse(Player player, CardInfo card)
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

        private static CardInfo ResolveCurse(int index)
        {
            switch (index)
            {
                case 0: return ForcedChoice.Card;
                case 1: return PanicPick.Card;
                case 2: return LeftmostLuck.Card;
                case 3: return BlindDraft.Card;
                case 4: return ShortHand.Card;
                case 5: return Fumble.Card;
                case 6: return Kickback.Card;
                case 7: return HardEdges.Card;
                default: return ForcedChoice.Card;
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
