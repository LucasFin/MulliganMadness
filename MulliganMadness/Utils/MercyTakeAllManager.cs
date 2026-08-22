using System.Collections.Generic;
using Photon.Pun;

namespace MulliganMadness.Utils
{
    internal static class MercyTakeAllManager
    {
        private static readonly HashSet<int> UsedMercyThisGame = new HashSet<int>();

        internal static void ResetForNewGame() => UsedMercyThisGame.Clear();

        internal static void MarkMercyUsed(int playerId)
        {
            if (SessionSettings.Current.MercyOncePerGame)
            {
                UsedMercyThisGame.Add(playerId);
            }
        }

        internal static void TryOfferMercy(Player picker)
        {
            if (picker == null) return;
            if (!SessionSettings.Current.EnableMercyVote) return;
            if (!SessionSettings.Current.EnableTakeAll) return;
            if (TakeAllVoteManager.IsActive) return;

            if (SessionSettings.Current.MercyOncePerGame && UsedMercyThisGame.Contains(picker.playerID)) return;

            var deficit = RoundWinTracker.GetDeficit(picker);
            if (deficit < SessionSettings.Current.MercyRoundDeficit) return;

            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            if (!TakeAllVoteManager.TryBeginMercyVote(picker.playerID))
            {
                return;
            }

            // Vote UI already labels mercy for the requester; toast everyone via dedicated toast root.
            CardTargetUi.ShowToast($"Mercy vote — {PlayerLabel(picker)} is down {deficit} round(s).");
        }

        private static string PlayerLabel(Player player)
        {
            var name = player?.data?.view?.Owner?.NickName;
            return string.IsNullOrEmpty(name) ? $"Player {player.playerID + 1}" : name;
        }
    }
}
