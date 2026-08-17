using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static MethodInfo _getPickerDraws;
        private static MethodInfo _isShuffleCard;

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

        public static bool IsOfferedHandReady()
        {
            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0) return false;

            var expected = GetExpectedDrawCount();
            if (expected > 0)
            {
                return spawned.Count >= expected;
            }

            return true;
        }

        public static bool TryTakeAll()
        {
            if (_busy || !IsEnabled) return false;
            if (!IsLocalPlayersTurn()) return false;
            if (!IsOfferedHandReady()) return false;

            var picker = GetCurrentPicker();
            if (picker == null || !HasRemaining(picker)) return false;

            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0) return false;

            var payloads = new List<string>();
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var visual = go.GetComponent<CardInfo>();
                if (visual == null) continue;
                var source = CardChoice.instance.GetSourceCard(visual) ?? visual.sourceCard ?? visual;
                if (source == null || IsPlaceholderCard(source)) continue;
                payloads.Add(EncodeCard(source));
            }

            if (payloads.Count == 0) return false;

            _busy = true;
            NetworkingManager.RPC(typeof(TakeAllManager), nameof(RPCA_TakeAll), picker.playerID, payloads.ToArray());
            Plugin.Instance.Log($"Player {picker.playerID} requested Take All ({payloads.Count} cards).");
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
        public static void RPCA_TakeAll(int playerID, string[] payloads)
        {
            UsedThisGame.Add(playerID);
            UI.TakeAllButton.RefreshVisibility();

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerID);
            if (picker == null || payloads == null || payloads.Length == 0)
            {
                _busy = false;
                return;
            }

            var cards = new List<CardInfo>();
            foreach (var payload in payloads)
            {
                var card = ResolveCard(payload);
                if (card != null && !IsPlaceholderCard(card)) cards.Add(card);
            }

            if (cards.Count == 0)
            {
                Plugin.Instance.LogWarn($"Take All resolved 0/{payloads.Length} cards for player {playerID}.");
                _busy = false;
                return;
            }

            // Prefer ending the pick on a normal card. Shuffle cards (Distill Knowledge, etc.)
            // must be granted via AddCardsToPlayer — Pick() would start a redraw instead.
            var pickIndex = cards.FindIndex(c => !IsShuffleCard(c));
            if (pickIndex < 0) pickIndex = 0;

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                var extras = cards.Where((_, i) => i != pickIndex).ToArray();
                if (extras.Length > 0)
                {
                    var codes = Enumerable.Repeat("", extras.Length).ToArray();
                    var zeros = new float[extras.Length];
                    Cards.instance.AddCardsToPlayer(picker, extras, false, codes, zeros, zeros, true);
                }
            }

            if (picker.data?.view != null && picker.data.view.IsMine)
            {
                var pickId = EncodeCard(cards[pickIndex]);
                Plugin.Instance.ExecuteAfterSeconds(0.35f, () => FinishPick(pickId));
            }
            else
            {
                _busy = false;
            }
        }

        private static void FinishPick(string pickPayload)
        {
            try
            {
                if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return;

                var spawned = GetSpawnedCards();
                if (spawned == null || spawned.Count == 0) return;

                GameObject pickVisual = FindSpawnedMatching(spawned, pickPayload);
                if (pickVisual == null)
                {
                    foreach (var go in spawned)
                    {
                        if (go == null) continue;
                        var visual = go.GetComponent<CardInfo>();
                        if (visual == null) continue;
                        var source = CardChoice.instance.GetSourceCard(visual) ?? visual.sourceCard ?? visual;
                        if (source != null && !IsShuffleCard(source) && !IsPlaceholderCard(source))
                        {
                            pickVisual = go;
                            break;
                        }
                    }
                }

                pickVisual ??= spawned.FirstOrDefault(go => go != null);
                if (pickVisual != null)
                {
                    CardChoice.instance.Pick(pickVisual, true);
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private static GameObject FindSpawnedMatching(List<GameObject> spawned, string payload)
        {
            DecodeCard(payload, out var objectName, out var cardName);
            objectName = StripClone(objectName);

            foreach (var go in spawned)
            {
                if (go == null) continue;
                var visual = go.GetComponent<CardInfo>();
                if (visual == null) continue;
                var source = CardChoice.instance.GetSourceCard(visual) ?? visual.sourceCard ?? visual;
                if (source == null) continue;

                var sourceObject = StripClone(source.gameObject != null ? source.gameObject.name : "");
                if (!string.IsNullOrEmpty(objectName) && string.Equals(sourceObject, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return go;
                }

                if (!string.IsNullOrEmpty(cardName) && !string.IsNullOrEmpty(source.cardName)
                    && string.Equals(source.cardName, cardName, StringComparison.OrdinalIgnoreCase))
                {
                    return go;
                }
            }

            return null;
        }

        private static string EncodeCard(CardInfo card)
        {
            var objectName = card.gameObject != null ? StripClone(card.gameObject.name) : "";
            var cardName = card.cardName ?? "";
            return objectName + "\n" + cardName;
        }

        private static void DecodeCard(string payload, out string objectName, out string cardName)
        {
            objectName = payload ?? "";
            cardName = "";
            if (string.IsNullOrEmpty(payload)) return;
            var split = payload.IndexOf('\n');
            if (split < 0) return;
            objectName = payload.Substring(0, split);
            cardName = payload.Substring(split + 1);
        }

        private static CardInfo ResolveCard(string payload)
        {
            DecodeCard(payload, out var objectName, out var cardName);
            objectName = StripClone(objectName);

            CardInfo card = null;
            if (!string.IsNullOrEmpty(objectName))
            {
                try { card = Cards.instance.GetCardWithObjectName(objectName); }
                catch { /* ignore */ }
            }

            if (card == null && !string.IsNullOrEmpty(cardName))
            {
                try { card = Cards.instance.GetCardWithName(cardName); }
                catch { /* GetCardWithName throws if missing */ }

                if (card == null)
                {
                    card = Cards.instance.allCards?.FirstOrDefault(c =>
                        c != null && !string.IsNullOrEmpty(c.cardName)
                        && string.Equals(c.cardName, cardName, StringComparison.OrdinalIgnoreCase));
                }
            }

            return card;
        }

        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            const string clone = "(Clone)";
            while (name.EndsWith(clone, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - clone.Length).Trim();
            }
            return name.Trim();
        }

        private static bool IsPlaceholderCard(CardInfo card)
        {
            if (card == null) return true;
            var objectName = card.gameObject != null ? card.gameObject.name : "";
            var cardName = card.cardName ?? "";
            // Real "Null" placeholder cards — not Distill Knowledge / Null-themed names on real cards.
            if (string.Equals(cardName, "Null", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(StripClone(objectName), "Null", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(StripClone(objectName), "NullCard", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool IsShuffleCard(CardInfo card)
        {
            if (card == null) return false;
            try
            {
                if (_isShuffleCard == null)
                {
                    var type = AccessTools.TypeByName("PickPhaseImprovements.PickManager");
                    _isShuffleCard = type != null ? AccessTools.Method(type, "IsShuffleCard", new[] { typeof(CardInfo) }) : null;
                }

                if (_isShuffleCard != null)
                {
                    return (bool)_isShuffleCard.Invoke(null, new object[] { card });
                }
            }
            catch
            {
                // PPI not loaded
            }

            var n = (card.cardName ?? "") + " " + (card.gameObject != null ? card.gameObject.name : "");
            return n.IndexOf("Distill Knowledge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int GetExpectedDrawCount()
        {
            try
            {
                if (_getPickerDraws == null)
                {
                    var type = AccessTools.TypeByName("DrawNCards.DrawNCards")
                               ?? AccessTools.TypeByName("PickNCards.DrawNCards");
                    if (type != null)
                    {
                        _getPickerDraws = AccessTools.Method(type, "GetPickerDraws", new[] { typeof(int) });
                    }
                }

                if (_getPickerDraws != null && CardChoice.instance != null)
                {
                    return (int)_getPickerDraws.Invoke(null, new object[] { CardChoice.instance.pickrID });
                }
            }
            catch
            {
                // Pick N Cards not present
            }

            return -1;
        }
    }
}
