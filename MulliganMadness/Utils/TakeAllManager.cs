using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModdingUtils.Utils;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public static class TakeAllManager
    {
        private static readonly HashSet<int> UsedThisGame = new HashSet<int>();
        private static bool _busy;

        public static void ResetForNewGame()
        {
            UsedThisGame.Clear();
            _busy = false;
        }

        public static bool IsEnabled => Plugin.Configs == null || Plugin.Configs.EnableTakeAll.Value;

        public static bool HasRemaining(Player player)
        {
            if (player == null) return false;
            return IsEnabled && !UsedThisGame.Contains(player.playerID);
        }

        public static bool IsLocalPlayersTurn()
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = GetCurrentPicker();
            return picker != null && picker.data?.view != null && picker.data.view.IsMine;
        }

        public static Player GetCurrentPicker()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;

            var pickerType = (PickerType)AccessTools.Field(typeof(CardChoice), "pickerType").GetValue(choice);
            if (pickerType == PickerType.Team)
            {
                var team = PlayerManager.instance.GetPlayersInTeam(choice.pickrID);
                if (team == null || team.Length == 0) return null;
                return team.FirstOrDefault(p => p.data?.view != null && p.data.view.IsMine) ?? team[0];
            }

            return PlayerManager.instance.players.FirstOrDefault(p => p.playerID == choice.pickrID);
        }

        public static bool TryTakeAll()
        {
            if (_busy || !IsEnabled) return false;
            if (!IsLocalPlayersTurn()) return false;

            var picker = GetCurrentPicker();
            if (picker == null || !HasRemaining(picker)) return false;

            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0) return false;

            var objectNames = new List<string>();
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var visual = go.GetComponent<CardInfo>();
                if (visual == null) continue;
                var source = CardChoice.instance.GetSourceCard(visual) ?? visual;
                if (source == null) continue;
                objectNames.Add(source.gameObject.name);
            }

            if (objectNames.Count == 0) return false;

            _busy = true;
            NetworkingManager.RPC(typeof(TakeAllManager), nameof(RPCA_TakeAll), picker.playerID, objectNames.ToArray());
            Plugin.Instance.Log($"Player {picker.playerID} requested Take All ({objectNames.Count} cards).");
            return true;
        }

        public static List<GameObject> GetSpawnedCards()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;
            var field = AccessTools.Field(typeof(CardChoice), "spawnedCards");
            return field?.GetValue(choice) as List<GameObject>;
        }

        [UnboundRPC]
        public static void RPCA_TakeAll(int playerID, string[] cardObjectNames)
        {
            UsedThisGame.Add(playerID);
            UI.TakeAllButton.RefreshVisibility();

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerID);
            if (picker == null || cardObjectNames == null || cardObjectNames.Length == 0)
            {
                _busy = false;
                return;
            }

            var cards = new List<CardInfo>();
            foreach (var objectName in cardObjectNames)
            {
                if (string.IsNullOrEmpty(objectName)) continue;
                try
                {
                    var card = Cards.instance.GetCardWithObjectName(objectName);
                    if (card != null) cards.Add(card);
                }
                catch
                {
                    // Skip unknown / inactive names rather than aborting the whole take.
                }
            }

            if (cards.Count == 0)
            {
                Plugin.Instance.LogWarn($"Take All resolved 0/{cardObjectNames.Length} cards for player {playerID}.");
                _busy = false;
                return;
            }

            // ModdingUtils only network-syncs card grants from the master client.
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                if (cards.Count > 1)
                {
                    var extras = cards.Skip(1).ToArray();
                    var codes = Enumerable.Repeat("", extras.Length).ToArray();
                    var zeros = new float[extras.Length];
                    Cards.instance.AddCardsToPlayer(picker, extras, false, codes, zeros, zeros, true);
                }
            }

            // Picking client finishes the hand via normal Pick so the pick phase ends cleanly.
            if (picker.data?.view != null && picker.data.view.IsMine)
            {
                Plugin.Instance.ExecuteAfterSeconds(0.15f, () => FinishPickWithFirstCard(cards[0].gameObject.name));
            }
            else
            {
                _busy = false;
            }
        }

        private static void FinishPickWithFirstCard(string firstCardObjectName)
        {
            try
            {
                if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
                {
                    return;
                }

                var spawned = GetSpawnedCards();
                if (spawned == null || spawned.Count == 0) return;

                GameObject pickVisual = null;
                foreach (var go in spawned)
                {
                    if (go == null) continue;
                    var visual = go.GetComponent<CardInfo>();
                    if (visual == null) continue;
                    var source = CardChoice.instance.GetSourceCard(visual) ?? visual;
                    if (source != null && source.gameObject.name == firstCardObjectName)
                    {
                        pickVisual = go;
                        break;
                    }
                }

                pickVisual ??= spawned[0];
                CardChoice.instance.Pick(pickVisual, true);
            }
            finally
            {
                _busy = false;
            }
        }

        [UnboundRPC]
        public static void RPCA_MarkUsed(int playerID)
        {
            UsedThisGame.Add(playerID);
            UI.TakeAllButton.RefreshVisibility();
        }
    }
}
