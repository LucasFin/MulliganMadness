using System;
using System.Collections.Generic;
using System.Linq;
using MulliganMadness.UI;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class TakeAllVoteManager
    {
        private static int _requesterId = -1;
        private static readonly Dictionary<int, bool> _votes = new Dictionary<int, bool>();
        private static float _expiresAt;
        private static bool _active;
        private static bool _mercyVote;
        private static string[] _pendingPayloads;
        private static bool _pendingCashOutNull;

        internal static bool IsActive => _active;

        internal static void ResetForNewGame()
        {
            _mercyVote = false;
            ClearPendingHand();
            CancelVote(false);
        }

        internal static void CancelIfActive(string reason)
        {
            if (!_active) return;
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                FailVote(string.IsNullOrEmpty(reason) ? "Take All vote cancelled — pick ended." : reason);
            }
        }

        internal static bool TryBeginMercyVote(int requesterId)
        {
            if (_active) return false;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return false;
            if (!TakeAllManager.IsOfferedHandReady()) return false;
            if (!TakeAllManager.TryEncodeOfferedHand(out var payloads, out var cashOut)) return false;
            BeginVote(requesterId, mercy: true, payloads, cashOut);
            return true;
        }

        internal static bool TryRequestVote()
        {
            if (!SessionSettings.Current.EnableTakeAll || SessionSettings.Current.TakeAllMode != TakeAllMode.Vote) return false;
            if (_active) return false;
            if (!TakeAllManager.IsLocalPlayersTurn()) return false;
            if (!TakeAllManager.IsOfferedHandReady()) return false;

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || !TakeAllManager.HasRemaining(picker)) return false;
            if (!TakeAllManager.TryEncodeOfferedHand(out var payloads, out var cashOut)) return false;

            NetworkingManager.RPC(
                typeof(TakeAllVoteManager),
                nameof(RPCA_RequestVote),
                picker.playerID,
                payloads,
                cashOut);
            return true;
        }

        [UnboundRPC]
        public static void RPCA_RequestVote(int requesterId, string[] payloads, bool cashOutWithNull)
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            if (payloads == null || (payloads.Length == 0 && !cashOutWithNull)) return;
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return;
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || picker.playerID != requesterId) return;
            if (!TakeAllManager.HasRemaining(picker)) return;
            BeginVote(requesterId, mercy: false, payloads, cashOutWithNull);
        }

        [UnboundRPC]
        public static void RPCA_BeginVote(int requesterId, float timeoutSeconds, bool mercy)
        {
            _requesterId = requesterId;
            _votes.Clear();
            _expiresAt = Time.unscaledTime + Mathf.Max(1f, timeoutSeconds);
            _active = true;
            _mercyVote = mercy;
            TakeAllVoteUi.ShowVote(requesterId, timeoutSeconds, mercy);
            TakeAllButton.RefreshVisibility();
        }

        [UnboundRPC]
        public static void RPCA_SubmitVote(int voterId, bool accepted)
        {
            if (!_active || voterId == _requesterId) return;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            _votes[voterId] = accepted;
            TryResolve();
        }

        [UnboundRPC]
        public static void RPCA_VoteResult(int requesterId, bool passed, string message, bool mercy)
        {
            var payloads = _pendingPayloads;
            var cashOut = _pendingCashOutNull;
            var wasMercy = mercy;

            _active = false;
            _votes.Clear();
            _requesterId = -1;
            _mercyVote = false;
            ClearPendingHand();
            TakeAllVoteUi.Hide();
            TakeAllButton.RefreshVisibility();

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == requesterId);
            if (picker?.data?.view != null && picker.data.view.IsMine && !string.IsNullOrEmpty(message))
            {
                CardTargetUi.ShowToast(message);
            }

            if (!passed || !(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                Plugin.Instance.LogWarn("Take All vote passed but pick is no longer active.");
                return;
            }

            if ((payloads == null || payloads.Length == 0) && !cashOut)
            {
                Plugin.Instance.LogWarn("Take All vote passed but hand payloads were empty.");
                CardTargetUi.ShowToast("Take All failed — hand unavailable.");
                return;
            }

            var ok = TakeAllManager.ExecuteAuthorizedTakeAllFromPayloads(
                requesterId,
                payloads,
                cashOut,
                consumeUse: wasMercy ? false : SessionSettings.Current.VoteConsumesUse,
                bypassRemaining: wasMercy);

            if (ok && wasMercy)
            {
                MercyTakeAllManager.MarkMercyUsed(requesterId);
            }
        }

        internal static void SubmitLocalVote(bool accepted)
        {
            if (!_active) return;
            var local = PlayerManager.instance.players.FirstOrDefault(p => p?.data?.view != null && p.data.view.IsMine);
            if (local == null || local.playerID == _requesterId) return;
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_SubmitVote), local.playerID, accepted);
            TakeAllVoteUi.Hide();
        }

        internal static void Tick()
        {
            if (!_active) return;
            if (Time.unscaledTime < _expiresAt) return;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            FailVote("Take All vote timed out.");
        }

        private static void BeginVote(int requesterId, bool mercy, string[] payloads, bool cashOutWithNull)
        {
            _pendingPayloads = payloads;
            _pendingCashOutNull = cashOutWithNull;
            _mercyVote = mercy;

            var voters = GetVoters(requesterId);
            if (voters.Count == 0)
            {
                ExecuteStoredTakeAll(requesterId, mercy);
                ClearPendingHand();
                _mercyVote = false;
                return;
            }

            var timeout = SessionSettings.Current.VoteTimeoutSeconds;
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_BeginVote), requesterId, timeout, mercy);
        }

        private static void ExecuteStoredTakeAll(int requesterId, bool mercy)
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                CardTargetUi.ShowToast("Take All failed — pick ended.");
                return;
            }

            var ok = TakeAllManager.ExecuteAuthorizedTakeAllFromPayloads(
                requesterId,
                _pendingPayloads,
                _pendingCashOutNull,
                consumeUse: mercy ? false : SessionSettings.Current.VoteConsumesUse,
                bypassRemaining: mercy);

            if (ok && mercy)
            {
                MercyTakeAllManager.MarkMercyUsed(requesterId);
            }
        }

        private static List<int> GetVoters(int requesterId)
        {
            var voters = new List<int>();
            foreach (var player in PlayerManager.instance.players)
            {
                if (player == null || player.playerID == requesterId) continue;
                voters.Add(player.playerID);
            }

            return voters;
        }

        private static void TryResolve()
        {
            var voters = GetVoters(_requesterId);
            if (voters.Count == 0)
            {
                PassVote();
                return;
            }

            foreach (var voterId in voters)
            {
                if (!_votes.ContainsKey(voterId)) return;
            }

            var yes = _votes.Values.Count(v => v);
            var needed = Mathf.CeilToInt(voters.Count * SessionSettings.Current.VoteThreshold);
            if (yes >= needed) PassVote();
            else FailVote("Take All vote failed.");
        }

        private static void PassVote()
        {
            var message = _mercyVote ? "Mercy Take All vote passed!" : "Take All vote passed!";
            var requesterId = _requesterId;
            var mercy = _mercyVote;
            // Keep pending payloads until RPCA_VoteResult runs (local invoke is sync).
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_VoteResult), requesterId, true, message, mercy);
            _active = false;
            _mercyVote = false;
        }

        private static void FailVote(string message)
        {
            var requesterId = _requesterId;
            var mercy = _mercyVote;
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_VoteResult), requesterId, false, message, mercy);
            _active = false;
            _mercyVote = false;
            ClearPendingHand();
        }

        private static void CancelVote(bool notify)
        {
            if (!_active) return;
            if (notify) FailVote("Take All vote cancelled.");
            else
            {
                _active = false;
                _mercyVote = false;
                _votes.Clear();
                _requesterId = -1;
                ClearPendingHand();
                TakeAllVoteUi.Hide();
            }
        }

        private static void ClearPendingHand()
        {
            _pendingPayloads = null;
            _pendingCashOutNull = false;
        }
    }
}
