using System;
using System.Collections;
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
        private static readonly Dictionary<int, int> DeferredKnowledge = new Dictionary<int, int>();
        private static bool _busy;
        private static MethodInfo _getPickerDraws;
        private static MethodInfo _isShuffleCard;
        private static Type _distillAcquisition;
        private static Type _powerDistillation;
        private static Type _nullCardInfo;
        private static int _lastSpawnCount;
        private static float _spawnStableSince;

        public static void ResetForNewGame()
        {
            UsedThisGame.Clear();
            DeferredKnowledge.Clear();
            _busy = false;
            _lastSpawnCount = 0;
            _spawnStableSince = 0f;
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

        public static void ApplyDeferredKnowledge()
        {
            var picker = GetCurrentPicker();
            if (picker == null) return;
            if (!DeferredKnowledge.TryGetValue(picker.playerID, out var extra) || extra <= 0) return;

            var current = ReadKnowledge(picker) ?? 0;
            WriteKnowledge(picker, current + extra);
            DeferredKnowledge.Remove(picker.playerID);
            Plugin.Instance.Log($"Applied {extra} deferred Distill Power Nulls for player {picker.playerID}.");
        }

        public static bool IsOfferedHandReady()
        {
            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0)
            {
                _lastSpawnCount = 0;
                return false;
            }

            if (spawned.Count != _lastSpawnCount)
            {
                _lastSpawnCount = spawned.Count;
                _spawnStableSince = Time.unscaledTime;
                return false;
            }

            if (Time.unscaledTime - _spawnStableSince < 0.2f)
            {
                return false;
            }

            var expected = GetExpectedDrawCount();
            // Distill / shuffle redraws are often smaller than Pick N Cards' draw count.
            if (expected > 0 && spawned.Count < expected && Time.unscaledTime - _spawnStableSince < 0.45f)
            {
                return false;
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

            var keep = new List<string>();
            var hasNullToCashOut = false;
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var source = SourceOf(go);
                if (source == null) continue;

                if (IsPlaceholderCard(source, go))
                {
                    hasNullToCashOut = true;
                    continue;
                }

                keep.Add(EncodeCard(source));
            }

            if (keep.Count == 0 && !hasNullToCashOut) return false;

            _busy = true;
            NetworkingManager.RPC(
                typeof(TakeAllManager),
                nameof(RPCA_TakeAll),
                picker.playerID,
                keep.ToArray(),
                hasNullToCashOut);
            Plugin.Instance.Log($"Player {picker.playerID} requested Take All ({keep.Count} cards, cashOutNull={hasNullToCashOut}).");
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
        public static void RPCA_TakeAll(int playerID, string[] payloads, bool cashOutWithNull)
        {
            UsedThisGame.Add(playerID);
            UI.TakeAllButton.RefreshVisibility();

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerID);
            if (picker == null)
            {
                _busy = false;
                return;
            }

            var knowledgeBefore = ReadKnowledge(picker) ?? 0;

            var rest = new List<CardInfo>();
            var knowledgeCards = new List<CardInfo>();
            var powerCards = new List<CardInfo>();
            if (payloads != null)
            {
                foreach (var payload in payloads)
                {
                    var card = ResolveCard(payload);
                    if (card == null || IsPlaceholderCard(card, null)) continue;

                    if (IsDistillKnowledge(card, null)) knowledgeCards.Add(card);
                    else if (IsDistillPower(card, null)) powerCards.Add(card);
                    else rest.Add(card);
                }
            }

            var hasKnowledge = knowledgeCards.Count > 0;
            var hasPower = powerCards.Count > 0;

            // Don't Distill the cards we're about to grant from this hand.
            WriteKnowledge(picker, 0);

            var grant = new List<CardInfo>();
            if (hasKnowledge)
            {
                grant.AddRange(rest);
                grant.AddRange(rest);
            }
            else
            {
                grant.AddRange(rest);
            }

            grant.AddRange(knowledgeCards);
            grant.AddRange(powerCards);

            // Reroll / Table Flip OnAdd only flags WWM for PickEnd — they still fire after this grant.
            if ((PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient) && grant.Count > 0)
            {
                var codes = Enumerable.Repeat("", grant.Count).ToArray();
                var zeros = new float[grant.Count];
                Cards.instance.AddCardsToPlayer(picker, grant.ToArray(), false, codes, zeros, zeros, true);
            }

            if (hasKnowledge)
            {
                GiveDistillNulls(playerID, rest.Count * 2);
            }

            int? knowledgeHold = 0;
            if (hasKnowledge || cashOutWithNull)
            {
                knowledgeHold = 0;
            }
            else if (hasPower)
            {
                var after = ReadKnowledge(picker) ?? 0;
                if (after > knowledgeBefore)
                {
                    DeferredKnowledge[playerID] = after - knowledgeBefore;
                }

                knowledgeHold = knowledgeBefore;
            }
            else
            {
                knowledgeHold = knowledgeBefore;
            }

            StabilizeAfterGrant(picker, knowledgeHold);
            StripDistillAcquisitionFromHand();

            Plugin.Instance.ExecuteAfterSeconds(0.12f, () => StabilizeAfterGrant(picker, knowledgeHold));
            Plugin.Instance.ExecuteAfterSeconds(0.28f, () => StabilizeAfterGrant(picker, knowledgeHold));

            if (picker.data?.view != null && picker.data.view.IsMine)
            {
                Plugin.Instance.ExecuteAfterSeconds(0.35f, () =>
                {
                    StabilizeAfterGrant(picker, knowledgeHold);
                    FinishPick(cashOutWithNull);
                });
            }
            else
            {
                _busy = false;
            }
        }

        private static void StabilizeAfterGrant(Player picker, int? knowledgeHold)
        {
            if (picker == null) return;
            WriteKnowledge(picker, knowledgeHold);
            CancelQueuedShuffles(picker);
        }

        private static void FinishPick(bool cashOutWithNull)
        {
            try
            {
                if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return;

                var spawned = GetSpawnedCards();
                if (spawned == null || spawned.Count == 0) return;

                StripDistillAcquisitionFromHand();

                GameObject visual = null;

                if (cashOutWithNull)
                {
                    visual = FindSpawned(spawned, (source, go) => IsPlaceholderCard(source, go));
                }

                if (visual == null)
                {
                    visual = FindSpawned(spawned, (source, go) =>
                        !IsPlaceholderCard(source, go)
                        && !IsDistillKnowledge(source, go)
                        && !IsShuffleRitual(source, go));
                }

                if (visual == null)
                {
                    visual = FindSpawned(spawned, (source, go) => !IsDistillKnowledge(source, go));
                }

                if (visual == null)
                {
                    visual = spawned.FirstOrDefault(go => go != null);
                }

                // Close the UI without ApplyCardStats.Pick — cards were already granted.
                ClosePickWithoutApplying(visual);
            }
            finally
            {
                _busy = false;
            }
        }

        private static void ClosePickWithoutApplying(GameObject visual)
        {
            var choice = CardChoice.instance;
            if (choice == null || visual == null) return;

            var view = choice.GetComponent<PhotonView>();
            var visualView = visual.GetComponent<PhotonView>();
            var pub = visual.GetComponent<PublicInt>();
            var cardIDs = AccessTools.Method(typeof(CardChoice), "CardIDs")?.Invoke(choice, null) as int[];
            if (view == null || visualView == null || pub == null || cardIDs == null)
            {
                choice.StartCoroutine(choice.IDoEndPick(visual, pub != null ? pub.theInt : 0, choice.pickrID));
                return;
            }

            view.RPC("RPCA_DoEndPick", RpcTarget.All, cardIDs, visualView.ViewID, pub.theInt, choice.pickrID);
        }

        private static GameObject FindSpawned(List<GameObject> spawned, Func<CardInfo, GameObject, bool> match)
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var source = SourceOf(go);
                if (source != null && match(source, go)) return go;
            }

            return null;
        }

        private static CardInfo SourceOf(GameObject go)
        {
            if (go == null) return null;
            var visual = go.GetComponent<CardInfo>();
            if (visual == null) return null;
            return CardChoice.instance.GetSourceCard(visual) ?? visual.sourceCard ?? visual;
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
                catch { /* throws if missing */ }

                card ??= Cards.all?.FirstOrDefault(c =>
                    c != null && !string.IsNullOrEmpty(c.cardName)
                    && string.Equals(c.cardName, cardName, StringComparison.OrdinalIgnoreCase));
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

        private static string Identity(CardInfo card)
        {
            var objectName = card.gameObject != null ? StripClone(card.gameObject.name) : "";
            var cardName = card.cardName ?? "";
            return (cardName + " " + objectName).Trim();
        }

        private static bool NameIs(CardInfo card, params string[] names)
        {
            var objectName = card.gameObject != null ? StripClone(card.gameObject.name) : "";
            var cardName = card.cardName ?? "";
            foreach (var name in names)
            {
                if (string.Equals(cardName, name, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(objectName, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool HasComponentNamed(GameObject go, ref Type cached, string typeName)
        {
            if (go == null) return false;
            cached ??= AccessTools.TypeByName(typeName);
            if (cached == null) return false;
            return go.GetComponent(cached) != null || go.GetComponentInChildren(cached) != null;
        }

        private static bool IsPlaceholderCard(CardInfo card, GameObject visual)
        {
            if (card == null) return true;
            if (HasComponentNamed(visual, ref _nullCardInfo, "Nullmanager.NullCardInfo")) return true;
            if (HasComponentNamed(card.gameObject, ref _nullCardInfo, "Nullmanager.NullCardInfo")) return true;
            if (NameIs(card, "Null", "NullCard", "Null Card", "nullCard", "___NULL___", "__NULL__")) return true;

            var cardName = (card.cardName ?? "").Trim();
            if (cardName.Equals("null", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static bool IsDistillKnowledge(CardInfo card, GameObject visual)
        {
            if (card == null) return false;
            if (HasComponentNamed(visual, ref _distillAcquisition, "RootNulledCards.DistillAcquisition")) return true;
            if (HasComponentNamed(card.gameObject, ref _distillAcquisition, "RootNulledCards.DistillAcquisition")) return true;
            if (NameIs(card, "Null_Knowledge")) return true;
            return Identity(card).IndexOf("Distill Knowledge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDistillPower(CardInfo card, GameObject visual)
        {
            if (card == null) return false;
            if (HasComponentNamed(visual, ref _powerDistillation, "RootNulledCards.PowerDistillation")) return true;
            if (HasComponentNamed(card.gameObject, ref _powerDistillation, "RootNulledCards.PowerDistillation")) return true;
            if (NameIs(card, "Null_Power", "Distill Power")) return true;
            return Identity(card).IndexOf("Distill Power", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsShuffleRitual(CardInfo card, GameObject visual)
        {
            if (card == null) return false;
            if (IsDistillKnowledge(card, visual)) return true;

            try
            {
                if (_isShuffleCard == null)
                {
                    var type = AccessTools.TypeByName("PickPhaseImprovements.PickManager");
                    _isShuffleCard = type != null ? AccessTools.Method(type, "IsShuffleCard", new[] { typeof(CardInfo) }) : null;
                }

                if (_isShuffleCard != null && (bool)_isShuffleCard.Invoke(null, new object[] { card }))
                {
                    return true;
                }
            }
            catch
            {
                // PPI not loaded, or instance method
            }

            return NameIs(card, "Shuffle");
        }

        private static void GiveDistillNulls(int playerID, int amount)
        {
            if (amount <= 0) return;
            try
            {
                var type = AccessTools.TypeByName("RootNulledCards.Patches.CardChoicePatchIDoEndPick");
                var method = type == null ? null : AccessTools.Method(type, "GiveNulls", new[] { typeof(int), typeof(int) });
                if (method != null)
                {
                    method.Invoke(null, new object[] { playerID, amount });
                    return;
                }
            }
            catch
            {
                // Nulled Cards not loaded
            }

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerID);
            if (picker?.data?.stats == null) return;
            try
            {
                var ext = AccessTools.TypeByName("Nullmanager.CharacterStatModifiersExtension");
                AccessTools.Method(ext, "AjustNulls", new[] { typeof(CharacterStatModifiers), typeof(int) })
                    ?.Invoke(null, new object[] { picker.data.stats, (int)(amount * 3.5f) });
            }
            catch
            {
                // NullManager not loaded
            }
        }

        private static void StripDistillAcquisitionFromHand()
        {
            var spawned = GetSpawnedCards();
            if (spawned == null) return;
            _distillAcquisition ??= AccessTools.TypeByName("RootNulledCards.DistillAcquisition");
            if (_distillAcquisition == null) return;

            foreach (var go in spawned)
            {
                if (go == null) continue;
                var component = go.GetComponent(_distillAcquisition) ?? go.GetComponentInChildren(_distillAcquisition);
                if (component != null)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
        }

        private static int? ReadKnowledge(Player player)
        {
            try
            {
                var data = GetRootData(player);
                if (data == null) return null;
                var field = AccessTools.Field(data.GetType(), "knowledge");
                return field == null ? (int?)null : (int)field.GetValue(data);
            }
            catch
            {
                return null;
            }
        }

        private static void WriteKnowledge(Player player, int? value)
        {
            if (value == null) return;
            try
            {
                var data = GetRootData(player);
                if (data == null) return;
                AccessTools.Field(data.GetType(), "knowledge")?.SetValue(data, value.Value);
            }
            catch
            {
                // Root Core not loaded
            }
        }

        private static object GetRootData(Player player)
        {
            var ext = AccessTools.TypeByName("RootCore.CharacterStatModifiersExtension");
            if (ext == null || player == null) return null;
            var method = AccessTools.Method(ext, "GetRootData", new[] { typeof(Player) })
                         ?? AccessTools.Method(ext, "GetRootData", new[] { typeof(CharacterStatModifiers) });
            if (method == null) return null;
            if (method.GetParameters()[0].ParameterType == typeof(Player))
            {
                return method.Invoke(null, new object[] { player });
            }

            return method.Invoke(null, new object[] { player.data.stats });
        }

        private static void CancelQueuedShuffles(Player picker)
        {
            if (picker == null) return;
            try
            {
                var type = AccessTools.TypeByName("PickPhaseImprovements.PickManager");
                var field = type == null ? null : AccessTools.Field(type, "ShuffleQueue");
                if (field?.GetValue(null) is not IDictionary queue) return;
                if (!queue.Contains(picker)) return;
                var list = queue[picker];
                list?.GetType().GetMethod("Clear")?.Invoke(list, null);
            }
            catch
            {
                // PPI not loaded
            }
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
