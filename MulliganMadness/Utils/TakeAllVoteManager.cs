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

        internal static bool IsActive => _active;

        internal static void ResetForNewGame()
        {
            _mercyVote = false;
            CancelVote(false);
        }

        internal static bool TryBeginMercyVote(int requesterId)
        {
            if (_active) return false;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return false;
            BeginVote(requesterId, mercy: true);
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

            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_RequestVote), picker.playerID);
            return true;
        }

        [UnboundRPC]
        public static void RPCA_RequestVote(int requesterId)
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            BeginVote(requesterId);
        }

        [UnboundRPC]
        public static void RPCA_BeginVote(int requesterId, float expiresAt, bool mercy)
        {
            _requesterId = requesterId;
            _votes.Clear();
            _expiresAt = expiresAt;
            _active = true;
            _mercyVote = mercy;
            TakeAllVoteUi.ShowVote(requesterId, expiresAt, mercy);
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
        public static void RPCA_VoteResult(int requesterId, bool passed, string message)
        {
            _active = false;
            _votes.Clear();
            _requesterId = -1;
            TakeAllVoteUi.Hide();
            TakeAllButton.RefreshVisibility();

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == requesterId);
            if (picker?.data?.view != null && picker.data.view.IsMine && !string.IsNullOrEmpty(message))
            {
                CardTargetUi.ShowToast(message);
            }

            if (passed && (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient))
            {
                if (_mercyVote)
                {
                    TakeAllManager.ExecuteAuthorizedTakeAll(requesterId, consumeUse: false, bypassRemaining: true);
                }
                else
                {
                    TakeAllManager.ExecuteAuthorizedTakeAll(requesterId, consumeUse: SessionSettings.Current.VoteConsumesUse);
                }
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

        private static void BeginVote(int requesterId, bool mercy = false)
        {
            var voters = GetVoters(requesterId);
            if (voters.Count == 0)
            {
                if (mercy)
                {
                    TakeAllManager.ExecuteAuthorizedTakeAll(requesterId, consumeUse: false, bypassRemaining: true);
                }
                else
                {
                    TakeAllManager.ExecuteAuthorizedTakeAll(requesterId, consumeUse: SessionSettings.Current.VoteConsumesUse);
                }

                return;
            }

            var expiresAt = Time.unscaledTime + SessionSettings.Current.VoteTimeoutSeconds;
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_BeginVote), requesterId, expiresAt, mercy);
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
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_VoteResult), _requesterId, true, message);
            _active = false;
            _mercyVote = false;
        }

        private static void FailVote(string message)
        {
            NetworkingManager.RPC(typeof(TakeAllVoteManager), nameof(RPCA_VoteResult), _requesterId, false, message);
            _active = false;
            _mercyVote = false;
        }

        private static void CancelVote(bool notify)
        {
            if (!_active) return;
            if (notify) FailVote("Take All vote cancelled.");
            _active = false;
            _mercyVote = false;
            _votes.Clear();
            _requesterId = -1;
            TakeAllVoteUi.Hide();
        }
    }
}
