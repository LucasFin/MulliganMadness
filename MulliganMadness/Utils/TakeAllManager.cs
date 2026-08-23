using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ModdingUtils.Utils;
using MulliganMadness.Stats;
using CardsApi = ModdingUtils.Utils.Cards;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public static class TakeAllManager
    {
        private static readonly Dictionary<int, int> UsesConsumed = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> DeferredKnowledge = new Dictionary<int, int>();
        private static bool _busy;
        private static readonly FieldInfo PickerTypeField = AccessTools.Field(typeof(CardChoice), "pickerType");
        private static readonly FieldInfo SpawnedCardsField = AccessTools.Field(typeof(CardChoice), "spawnedCards");
        private static MethodInfo _getPickerDraws;
        private static MethodInfo _isShuffleCard;
        private static Type _distillAcquisition;
        private static Type _powerDistillation;
        private static Type _nullCardInfo;
        private static int _lastSpawnCount;
        private static float _spawnStableSince;
        private static int _authorizedPlayerId = -1;
        private static bool _authorizedConsumeUse;
        private static bool _authorizedMercy;
        private static string[] _authorizedPayloads;
        private static bool _authorizedCashOut;
        private static int _actingPickerId = -1;

        internal static bool CollectingAll;

        public static bool IsBusy => _busy;

        public static void ResetForNewGame()
        {
            UsesConsumed.Clear();
            DeferredKnowledge.Clear();
            _busy = false;
            _lastSpawnCount = 0;
            _spawnStableSince = 0f;
            CollectingAll = false;
            ClearAuthorization();
            ClearActingPicker();
        }

        internal static bool HasAuthorization(int playerId) =>
            playerId >= 0 && _authorizedPlayerId == playerId;

        internal static void GrantAuthorization(
            int playerId,
            bool consumeUse,
            bool mercy,
            string[] payloads = null,
            bool cashOutWithNull = false)
        {
            _authorizedPlayerId = playerId;
            _authorizedConsumeUse = consumeUse;
            _authorizedMercy = mercy;
            _authorizedPayloads = payloads != null ? (string[])payloads.Clone() : null;
            _authorizedCashOut = cashOutWithNull;
        }

        internal static void ClearAuthorization()
        {
            _authorizedPlayerId = -1;
            _authorizedConsumeUse = false;
            _authorizedMercy = false;
            _authorizedPayloads = null;
            _authorizedCashOut = false;
        }

        /// <summary>
        /// Clears flags that must not stick into the next pick (Take All collect mode, busy).
        /// Call from pick-end hooks and FinishPick failure paths.
        /// </summary>
        internal static void ClearPickTransientState()
        {
            CollectingAll = false;
            _busy = false;
            _lastSpawnCount = 0;
            _spawnStableSince = 0f;
        }

        internal static void NoteActingPicker(int pickerId)
        {
            _actingPickerId = pickerId;
        }

        internal static void ClearActingPicker()
        {
            _actingPickerId = -1;
        }

        internal static bool TryExecuteAuthorization()
        {
            if (_authorizedPlayerId < 0) return false;
            if (!IsLocalPlayersTurn()) return false;

            var playerId = _authorizedPlayerId;
            var consumeUse = _authorizedConsumeUse;
            var mercy = _authorizedMercy;
            var payloads = _authorizedPayloads;
            var cashOut = _authorizedCashOut;
            var ok = payloads != null && (payloads.Length > 0 || cashOut)
                ? ExecuteAuthorizedTakeAllFromPayloads(
                    playerId,
                    payloads,
                    cashOut,
                    consumeUse,
                    bypassRemaining: mercy)
                : ExecuteAuthorizedTakeAll(playerId, consumeUse, bypassRemaining: mercy);
            if (ok) ClearAuthorization();
            return ok;
        }

        public static bool IsEnabled => SessionSettings.Current.EnableTakeAll;

        public static bool HasRemaining(Player player)
        {
            if (player == null) return false;
            if (!IsEnabled) return false;
            var limit = Mathf.Max(0, SessionSettings.Current.TakeAllUsesPerPlayer);
            if (limit <= 0) return false;
            UsesConsumed.TryGetValue(player.playerID, out var used);
            return used < limit;
        }

        public static bool HasBonus(Player player) => HasNestBonus(player);

        public static bool HasNestBonus(Player player) => NestEggManager.HasCharge(player, EggKind.Nest);

        public static bool CanUseTakeAll(Player player) => HasBonus(player) || HasRemaining(player);

        public static int UsesRemaining(Player player)
        {
            if (player == null || !IsEnabled) return 0;
            var limit = Mathf.Max(0, SessionSettings.Current.TakeAllUsesPerPlayer);
            UsesConsumed.TryGetValue(player.playerID, out var used);
            return Mathf.Max(0, limit - used);
        }

        private static void ConsumeUse(int playerId, bool consumeUse)
        {
            if (!consumeUse) return;
            UsesConsumed.TryGetValue(playerId, out var used);
            UsesConsumed[playerId] = used + 1;
        }

        public static bool IsLocalPlayersTurn()
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = GetCurrentPicker();
            return picker != null && PlayerStatsSnapshot.IsLocallyControlled(picker);
        }

        public static Player GetCurrentPicker()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;

            // RWF / Unbound TDM calls StartPick(playerID) per player even when pickerType
            // stays Team. Prefer that acting player so teammate B's eggs, curses, and Take
            // All bind to B instead of the lowest id on the team.
            if (choice.IsPicking && _actingPickerId >= 0)
            {
                var acting = FindPlayer(_actingPickerId);
                if (acting != null) return acting;
            }

            var pickerType = PickerTypeField != null
                ? (PickerType)PickerTypeField.GetValue(choice)
                : (PickerType)AccessTools.Field(typeof(CardChoice), "pickerType").GetValue(choice);
            if (pickerType == PickerType.Team)
            {
                var team = PlayerManager.instance != null
                    ? PlayerManager.instance.GetPlayersInTeam(choice.pickrID)
                    : null;
                if (team != null && team.Length > 0) return DesignateFromTeam(team);
            }

            return FindPlayer(choice.pickrID);
        }

        internal static Player FindPlayer(int playerId)
        {
            var players = PlayerManager.instance?.players;
            if (players == null) return null;
            foreach (var player in players)
            {
                if (player != null && player.playerID == playerId) return player;
            }

            return null;
        }

        internal static int EndPickPlayerId()
        {
            var picker = GetCurrentPicker();
            if (picker != null) return picker.playerID;
            return CardChoice.instance != null ? CardChoice.instance.pickrID : 0;
        }

        private static Player DesignateFromTeam(Player[] team)
        {
            if (team == null || team.Length == 0) return null;
            Player local = null;
            Player lowest = null;
            foreach (var player in team)
            {
                if (player == null) continue;
                if (lowest == null || player.playerID < lowest.playerID) lowest = player;
                if (local == null && PlayerStatsSnapshot.IsLocallyControlled(player)) local = player;
            }

            return local ?? lowest;
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

            // Drop destroyed Photon stubs so we don't treat a wiped hand as "ready".
            var alive = 0;
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null) alive++;
            }

            if (alive == 0)
            {
                _lastSpawnCount = 0;
                return false;
            }

            if (alive != _lastSpawnCount)
            {
                _lastSpawnCount = alive;
                _spawnStableSince = Time.unscaledTime;
                return false;
            }

            if (Time.unscaledTime - _spawnStableSince < 0.25f)
            {
                return false;
            }

            var expected = GetExpectedDrawCountInternal();
            // Online PPI FixHandSize/ReplaceCards often needs >0.45s. Never claim ready
            // while the hand is still short of the expected draw count.
            if (expected > 0 && alive < expected)
            {
                if (Time.unscaledTime - _spawnStableSince < 2.5f)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TryTakeAll()
        {
            if (_busy) return false;
            if (!IsLocalPlayersTurn()) return false;
            if (!IsOfferedHandReady()) return false;
            if (TakeAllVoteManager.IsActive) return false;

            var picker = GetCurrentPicker();
            if (picker == null) return false;

            if (HasNestBonus(picker))
            {
                return BeginTakeAll(picker.playerID, consumeUse: false, skipCurse: true, consumeBonus: true);
            }

            if (!IsEnabled) return false;
            if (!HasRemaining(picker)) return false;

            if (SessionSettings.Current.TakeAllMode == TakeAllMode.Vote)
            {
                return TakeAllVoteManager.TryRequestVote();
            }

            return BeginTakeAll(picker.playerID, consumeUse: true);
        }

        internal static bool ExecuteAuthorizedTakeAll(int playerId, bool consumeUse, bool bypassRemaining = false)
        {
            if (_busy || !IsEnabled) return false;
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerId);
            if (picker == null) return false;
            if (!bypassRemaining && !HasRemaining(picker)) return false;
            return BeginTakeAll(playerId, consumeUse);
        }

        internal static bool ExecuteAuthorizedTakeAllFromPayloads(
            int playerId,
            string[] payloads,
            bool cashOutWithNull,
            bool consumeUse,
            bool bypassRemaining = false,
            bool skipCurse = false,
            bool consumeBonus = false)
        {
            if (_busy) return false;
            if (!consumeBonus && !IsEnabled) return false;
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerId);
            if (picker == null) return false;
            if (!bypassRemaining && !consumeBonus && !HasRemaining(picker)) return false;
            if ((payloads == null || payloads.Length == 0) && !cashOutWithNull) return false;

            AutoPickController.ResetForCurrentPick();
            _busy = true;
            NetworkingManager.RPC(
                typeof(TakeAllManager),
                nameof(RPCA_TakeAll),
                playerId,
                payloads ?? Array.Empty<string>(),
                cashOutWithNull,
                consumeUse,
                skipCurse,
                consumeBonus);
            Plugin.Instance.Log(
                $"Player {playerId} authorized Take All from payloads ({payloads?.Length ?? 0} cards, cashOutNull={cashOutWithNull}).");
            return true;
        }

        internal static bool TryEncodeOfferedHand(out string[] payloads, out bool cashOutWithNull, int maxCards = 0)
        {
            payloads = null;
            cashOutWithNull = false;
            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0) return false;

            var keep = new List<string>();
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var source = SourceOf(go);
                if (source == null) continue;

                if (IsPlaceholderCard(source, go))
                {
                    cashOutWithNull = true;
                    var encoded = EncodeNull(source, go);
                    if (!string.IsNullOrEmpty(encoded)) keep.Add(encoded);
                    continue;
                }

                keep.Add(EncodeCard(source));
            }

            if (maxCards > 0 && keep.Count > maxCards)
            {
                keep = keep.GetRange(0, maxCards);
            }

            if (keep.Count == 0 && !cashOutWithNull) return false;
            payloads = keep.ToArray();
            return true;
        }

        private static bool BeginTakeAll(
            int playerId,
            bool consumeUse,
            bool skipCurse = false,
            bool consumeBonus = false)
        {
            if (!TryEncodeOfferedHand(out var payloads, out var cashOutWithNull)) return false;
            return ExecuteAuthorizedTakeAllFromPayloads(
                playerId,
                payloads,
                cashOutWithNull,
                consumeUse,
                bypassRemaining: true,
                skipCurse,
                consumeBonus);
        }


        public static List<GameObject> GetSpawnedCards()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;
            return SpawnedCardsField?.GetValue(choice) as List<GameObject>;
        }

        public static List<GameObject> GetReadySpawnedCards()
        {
            var spawned = GetSpawnedCards();
            if (spawned == null) return null;

            var ready = new List<GameObject>();
            foreach (var go in spawned)
            {
                if (go == null) continue;
                if (go.GetComponent<CardInfo>() == null) continue;
                var view = go.GetComponent<PhotonView>();
                if (view != null && view.ViewID == 0) continue;
                ready.Add(go);
            }

            return ready;
        }

        [UnboundRPC]
        public static void RPCA_TakeAll(
            int playerID,
            string[] payloads,
            bool cashOutWithNull,
            bool consumeUse,
            bool skipCurse,
            bool consumeBonus)
        {
            if (!IsEnabled && !consumeBonus)
            {
                _busy = false;
                return;
            }

            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                _busy = false;
                Plugin.Instance.LogWarn("Take All RPC ignored - not picking.");
                return;
            }

            var picker = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == playerID);
            if (picker == null)
            {
                _busy = false;
                return;
            }

            var current = GetCurrentPicker();
            if (current == null || current.playerID != playerID)
            {
                _busy = false;
                Plugin.Instance.LogWarn($"Take All RPC ignored - player {playerID} is not the current picker.");
                return;
            }

            var knowledgeBefore = ReadKnowledge(picker) ?? 0;

            var rest = new List<CardInfo>();
            var knowledgeCards = new List<CardInfo>();
            var powerCards = new List<CardInfo>();
            var nullCards = new List<CardInfo>();
            if (payloads != null)
            {
                foreach (var payload in payloads)
                {
                    if (IsNullPayload(payload))
                    {
                        var nulled = ResolveNull(payload, picker);
                        if (nulled != null) nullCards.Add(nulled);
                        continue;
                    }

                    var card = ResolveCard(payload);
                    if (card == null || IsPlaceholderCard(card, null)) continue;

                    if (IsDistillKnowledge(card, null)) knowledgeCards.Add(card);
                    else if (IsDistillPower(card, null)) powerCards.Add(card);
                    else rest.Add(card);
                }
            }

            if (nullCards.Count == 0 && cashOutWithNull)
            {
                nullCards.AddRange(CollectOfferedNulls(picker));
            }

            var hasKnowledge = knowledgeCards.Count > 0;
            var hasPower = powerCards.Count > 0;
            var grantCount = rest.Count
                             + (hasKnowledge ? rest.Count : 0)
                             + knowledgeCards.Count
                             + powerCards.Count
                             + nullCards.Count;
            if (grantCount == 0 && !cashOutWithNull)
            {
                _busy = false;
                Plugin.Instance.LogWarn("Take All RPC ignored - empty grant.");
                return;
            }

            ConsumeUse(playerID, consumeUse);
            if (consumeBonus) NestEggManager.TryConsumeCharge(playerID, EggKind.Nest);
            UI.TakeAllButton.RefreshVisibility();
            UI.PickAnnounceUi.ShowTookAll(playerID);

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
            grant.AddRange(nullCards);

            if (nullCards.Count > 0)
            {
                Plugin.Instance.Log($"Take All granting {nullCards.Count} Null cards for player {playerID}.");
            }

            // Simulacrum doubles ApplyStats by adding the picker twice in Pick. Take All
            // grants through AddCardsToPlayer, so copy the grant list here instead.
            if (HasSimulacrum(picker) && grant.Count > 0)
            {
                grant.AddRange(grant.ToArray());
                Plugin.Instance.Log($"Simulacrum: doubled Take All grant to {grant.Count} cards for player {playerID}.");
            }

            // Reroll / Table Flip OnAdd only flags WWM for PickEnd - they still fire after this grant.
            if ((PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient) && grant.Count > 0)
            {
                var codes = Enumerable.Repeat("", grant.Count).ToArray();
                var zeros = new float[grant.Count];
                CardsApi.instance.AddCardsToPlayer(picker, grant.ToArray(), false, codes, zeros, zeros, true);
            }

            if (!skipCurse && (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient))
            {
                TakeAllCurseCost.TryApplyAfterTakeAll(playerID);
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
            CollectingAll = true;

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
                if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
                {
                    CollectingAll = false;
                    return;
                }

                var spawned = GetSpawnedCards();
                if (spawned == null || spawned.Count == 0)
                {
                    CollectingAll = false;
                    return;
                }

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

                if (visual == null)
                {
                    CollectingAll = false;
                    return;
                }

                // Close the UI without ApplyCardStats.Pick - cards were already granted.
                CollectingAll = true;
                EndPickWithoutApplying(visual);
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>
        /// Ends the current pick via Photon RPCA_DoEndPick when possible (keeps remote clients in sync).
        /// </summary>
        public static void EndPickWithoutApplying(GameObject visual)
        {
            var choice = CardChoice.instance;
            if (choice == null || visual == null)
            {
                CollectingAll = false;
                return;
            }

            var view = choice.GetComponent<PhotonView>();
            var visualView = visual.GetComponent<PhotonView>();
            var pub = visual.GetComponent<PublicInt>();
            var cardIDs = AccessTools.Method(typeof(CardChoice), "CardIDs")?.Invoke(choice, null) as int[];
            var pickId = EndPickPlayerId();
            if (view == null || visualView == null || pub == null || cardIDs == null)
            {
                choice.StartCoroutine(choice.IDoEndPick(visual, pub != null ? pub.theInt : 0, pickId));
                return;
            }

            view.RPC("RPCA_DoEndPick", RpcTarget.All, cardIDs, visualView.ViewID, pub.theInt, pickId);
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

        private static bool HasSimulacrum(Player player)
        {
            var cards = player?.data?.currentCards;
            if (cards == null) return false;

            foreach (var card in cards)
            {
                if (card == null) continue;
                if (string.Equals(card.cardName, "Simulacrum", StringComparison.OrdinalIgnoreCase)) return true;
                var objectName = card.gameObject != null ? card.gameObject.name : "";
                if (objectName.IndexOf("Simulacrum", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        internal static CardInfo SourceOf(GameObject go)
        {
            if (go == null) return null;
            var visual = go.GetComponent<CardInfo>();
            if (visual == null) return null;
            if (CardChoice.instance == null) return visual.sourceCard ?? visual;
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
                try { card = CardsApi.instance.GetCardWithObjectName(objectName); }
                catch { /* ignore */ }
            }

            if (card == null && !string.IsNullOrEmpty(cardName))
            {
                try { card = CardsApi.instance.GetCardWithName(cardName); }
                catch { /* throws if missing */ }

                card ??= CardsApi.all?.FirstOrDefault(c =>
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

        private const string NullPayloadPrefix = "___NULL___";

        private static string EncodeNull(CardInfo card, GameObject visual)
        {
            var sourceName = GetNulledSourceName(card, visual);
            if (string.IsNullOrEmpty(sourceName)) return null;
            return NullPayloadPrefix + "\n" + sourceName;
        }

        private static bool IsNullPayload(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            return payload.StartsWith(NullPayloadPrefix, StringComparison.Ordinal);
        }

        private static CardInfo ResolveNull(string payload, Player picker)
        {
            DecodeCard(payload, out _, out var sourceName);
            if (string.IsNullOrEmpty(sourceName))
            {
                sourceName = payload.Length > NullPayloadPrefix.Length
                    ? payload.Substring(NullPayloadPrefix.Length).TrimStart('\n', '_')
                    : "";
            }

            return ResolveNullBySourceName(sourceName, picker);
        }

        private static List<CardInfo> CollectOfferedNulls(Player picker)
        {
            var result = new List<CardInfo>();
            var spawned = GetSpawnedCards();
            if (spawned == null || picker == null) return result;

            foreach (var go in spawned)
            {
                if (go == null) continue;
                var source = SourceOf(go);
                if (!IsPlaceholderCard(source, go)) continue;
                var resolved = ResolveNullBySourceName(GetNulledSourceName(source, go), picker);
                if (resolved != null) result.Add(resolved);
            }

            return result;
        }

        private static string GetNulledSourceName(CardInfo card, GameObject visual)
        {
            var info = GetNullCardInfoComponent(card, visual);
            if (info != null)
            {
                try
                {
                    var field = AccessTools.Field(info.GetType(), "NulledSorce");
                    if (field?.GetValue(info) is CardInfo source && source.gameObject != null)
                    {
                        return StripClone(source.gameObject.name);
                    }
                }
                catch
                {
                    // NullManager layout changed
                }
            }

            var name = card?.cardName ?? "";
            if (name.StartsWith("[]", StringComparison.Ordinal)) name = name.Substring(2);
            name = StripClone(name);
            return string.IsNullOrEmpty(name) ? null : name;
        }

        private static object GetNullCardInfoComponent(CardInfo card, GameObject visual)
        {
            _nullCardInfo ??= AccessTools.TypeByName("Nullmanager.NullCardInfo");
            if (_nullCardInfo == null) return null;

            if (visual != null)
            {
                var fromVisual = visual.GetComponent(_nullCardInfo) ?? visual.GetComponentInChildren(_nullCardInfo);
                if (fromVisual != null) return fromVisual;
            }

            if (card != null && _nullCardInfo.IsInstanceOfType(card)) return card;
            if (card?.gameObject != null)
            {
                return card.gameObject.GetComponent(_nullCardInfo)
                       ?? card.gameObject.GetComponentInChildren(_nullCardInfo);
            }

            return null;
        }

        private static CardInfo ResolveNullBySourceName(string sourceName, Player picker)
        {
            sourceName = StripClone(sourceName);
            if (string.IsNullOrEmpty(sourceName) || picker == null) return null;

            try
            {
                var type = AccessTools.TypeByName("Nullmanager.NullManager");
                var instance = type == null ? null : AccessTools.Property(type, "instance")?.GetValue(null);
                if (instance == null) return null;

                var method = AccessTools.Method(type, "GetNullCardInfo", new[] { typeof(string), typeof(Player) })
                             ?? AccessTools.Method(type, "GetNullCardInfo", new[] { typeof(string), typeof(int) });
                if (method == null) return null;

                if (method.GetParameters()[1].ParameterType == typeof(Player))
                {
                    return method.Invoke(instance, new object[] { sourceName, picker }) as CardInfo;
                }

                return method.Invoke(instance, new object[] { sourceName, picker.playerID }) as CardInfo;
            }
            catch (Exception ex)
            {
                Plugin.Instance.LogWarn($"Failed to resolve Null '{sourceName}': {ex.Message}");
                return null;
            }
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

        public static int GetExpectedDrawCount()
        {
            return GetExpectedDrawCountInternal();
        }

        private static int GetExpectedDrawCountInternal()
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
