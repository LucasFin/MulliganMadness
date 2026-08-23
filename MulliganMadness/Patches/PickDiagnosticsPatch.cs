using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Utils;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Online pick spawn authority + diagnostics.
    /// Master alone Instantiates the offer hand (avoids stacked Photon cards). Remotes must
    /// still receive spawnedCards ViewIDs — DoPlayerSelect only reads the local list, so
    /// without a sync the picker sees face-down backs and cannot flip or pick.
    /// </summary>
    [HarmonyPatch]
    internal static class PickDiagnosticsPatch
    {
        private static readonly FieldInfo ChildrenField = AccessTools.Field(typeof(CardChoice), "children");
        private static readonly FieldInfo PicksField = AccessTools.Field(typeof(CardChoice), "picks");
        private static readonly FieldInfo IsPlayingField = AccessTools.Field(typeof(CardChoice), "isPlaying");
        private static readonly MethodInfo ReplaceCardsMethod =
            AccessTools.Method(typeof(CardChoice), "ReplaceCards", new[] { typeof(GameObject), typeof(bool) });
        private static int _watchGeneration;
        private static int _syncGeneration;
        private static bool _retriedThisPick;
        private static bool _syncedThisPick;

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void AfterStartPick(CardChoice __instance, int picksToSet, int pickerIDToSet)
        {
            try
            {
                _retriedThisPick = false;
                _syncedThisPick = false;
                LogSnapshot($"StartPick.Post picksToSet={picksToSet}", __instance, pickerIDToSet);
                // #region agent log
                DebugAgentLog.Write("D", "PickDiagnostics.StartPick", "start",
                    "{\"picksToSet\":" + picksToSet +
                    ",\"picker\":" + pickerIDToSet +
                    ",\"master\":" + (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient).ToString().ToLower() + "}");
                // #endregion
                _watchGeneration++;
                var gen = _watchGeneration;
                if (Plugin.Instance != null)
                {
                    Plugin.Instance.StartCoroutine(WatchSpawn(gen));
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"PickDiagnostics StartPick: {ex.Message}");
            }
        }

        /// <summary>
        /// Intercept offer spawn (pickedCard == null). Selecting a card still uses vanilla.
        /// Online: master alone starts ReplaceCards; other clients skip (avoids stacked hands).
        /// </summary>
        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool BeforePick(CardChoice __instance, GameObject pickedCard, bool clear)
        {
            if (pickedCard != null) return true;
            if (__instance == null || !__instance.IsPicking) return false;

            if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
            {
                Plugin.Instance?.Log("Pick(null) skipped on non-master (master owns ReplaceCards).");
                return false;
            }

            if (IsPlaying(__instance)) return false;

            return !TryStartReplaceCards(__instance, clear, "Pick.Prefix");
        }

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
        [HarmonyFinalizer]
        private static Exception AfterPick(Exception __exception)
        {
            if (__exception != null)
            {
                Plugin.Instance?.LogWarn(
                    $"CardChoice.Pick threw {__exception.GetType().Name}: {__exception.Message}");
            }

            return __exception;
        }

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void AfterPickApply(CardChoice __instance, GameObject pickedCard)
        {
            if (pickedCard == null) return;
            try
            {
                var view = pickedCard.GetComponent<PhotonView>();
                var source = TakeAllManager.SourceOf(pickedCard);
                var cardName = source != null && !string.IsNullOrEmpty(source.cardName)
                    ? source.cardName
                    : pickedCard.name;
                var spawned = TakeAllManager.GetSpawnedCards();
                var alive = 0;
                if (spawned != null)
                {
                    for (var i = 0; i < spawned.Count; i++)
                    {
                        if (spawned[i] != null) alive++;
                    }
                }

                Plugin.Instance?.Log(
                    $"PickDiag [Pick.apply] card={cardName} view={view?.ViewID ?? 0} " +
                    $"pickrID={(__instance != null ? __instance.pickrID : -1)} " +
                    $"spawnedAlive={alive} " +
                    $"master={PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient}");
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"PickDiag Pick.apply: {ex.Message}");
            }
        }

        /// <summary>
        /// IDoEndPick calls ReplaceCards directly (bypasses Pick). Non-masters must not
        /// Photon-Instantiate when picks&gt;0. picks&lt;=0 still runs (PPI DonePicking).
        /// </summary>
        [HarmonyPatch(typeof(CardChoice), "ReplaceCards", typeof(GameObject), typeof(bool))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool BeforeReplaceCards(
            CardChoice __instance,
            GameObject pickedCard,
            bool clear,
            ref IEnumerator __result)
        {
            try
            {
                LogSnapshot(
                    $"ReplaceCards.Pre clear={clear} picked={(pickedCard != null)}",
                    __instance,
                    __instance != null ? __instance.pickrID : -1);
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"PickDiagnostics ReplaceCards.Pre: {ex.Message}");
            }

            var picks = PicksField != null && __instance != null
                ? (int)PicksField.GetValue(__instance)
                : 0;

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                // A second offer ReplaceCards while PPI is still Instantiating stacks
                // Photon cards. Remotes keep the leftovers; RPCA_Pick then applies the
                // wrong CardInfo (ghost Sneaky / unblockable). Stall retry clears isPlaying first.
                if (pickedCard == null && picks > 0 && IsPlaying(__instance))
                {
                    Plugin.Instance?.Log(
                        "ReplaceCards overlapping offer spawn skipped on master.");
                    __result = SkipReplaceCards();
                    return false;
                }

                if (pickedCard == null && picks > 0)
                {
                    _syncedThisPick = false;
                    _syncGeneration++;
                    if (Plugin.Instance != null)
                    {
                        Plugin.Instance.StartCoroutine(SyncAfterReplace("ReplaceCards", _syncGeneration));
                    }
                }

                return true;
            }

            if (picks <= 0) return true;

            // #region agent log
            DebugAgentLog.Write("C", "PickDiagnostics.BeforeReplaceCards", "non_master_spawn_blocked",
                "{\"picks\":" + picks +
                ",\"picked\":" + (pickedCard != null).ToString().ToLower() +
                ",\"clear\":" + clear.ToString().ToLower() +
                ",\"pickrID\":" + (__instance != null ? __instance.pickrID : -1) + "}");
            // #endregion

            Plugin.Instance?.Log(
                "ReplaceCards spawn skipped on non-master (master owns Instantiates).");
            __result = SkipReplaceCards();
            return false;
        }

        private static IEnumerator SkipReplaceCards()
        {
            yield break;
        }

        [HarmonyPatch(typeof(CardChoice), "ReplaceCards", typeof(GameObject), typeof(bool))]
        [HarmonyFinalizer]
        private static Exception CaptureReplaceCards(Exception __exception)
        {
            if (__exception != null)
            {
                Plugin.Instance?.LogWarn(
                    $"ReplaceCards threw {__exception.GetType().Name}: {__exception.Message}");
                try
                {
                    if (IsPlayingField != null && CardChoice.instance != null)
                    {
                        IsPlayingField.SetValue(CardChoice.instance, false);
                    }
                }
                catch
                {
                }
            }

            return __exception;
        }

        [HarmonyPatch]
        private static class SpawnUniqueCardFinalizer
        {
            private static bool Prepare() => TargetMethod() != null;

            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(CardChoice), "SpawnUniqueCard");

            private static Exception Finalizer(Exception __exception)
            {
                if (__exception != null)
                {
                    Plugin.Instance?.LogWarn(
                        $"SpawnUniqueCard threw {__exception.GetType().Name}: {__exception.Message}");
                }

                return __exception;
            }
        }

        private static IEnumerator WatchSpawn(int generation)
        {
            var delays = new[] { 0.35f, 0.9f, 1.6f, 2.5f };
            foreach (var delay in delays)
            {
                yield return new WaitForSecondsRealtime(delay);
                if (generation != _watchGeneration) yield break;
                var choice = CardChoice.instance;
                if (choice == null || !choice.IsPicking)
                {
                    Plugin.Instance?.Log($"PickWatch t={delay:0.0}s IsPicking=false (pick ended)");
                    yield break;
                }

                LogSnapshot($"PickWatch t={delay:0.0}s", choice, choice.pickrID);

                TrySyncOfferedHand(choice, "watch");

                if (delay < 1.5f || _retriedThisPick) continue;
                if (!ShouldRetrySpawn(choice)) continue;

                _retriedThisPick = true;
                Plugin.Instance?.LogWarn(
                    "Pick spawn stalled on master — forcing ReplaceCards once.");
                TryStartReplaceCards(choice, clear: false, reason: "stall");
            }
        }

        private static void TrySyncOfferedHand(CardChoice choice, string reason)
        {
            // #region agent log
            if (_syncedThisPick)
            {
                DebugAgentLog.Write("A", "PickDiagnostics.TrySync", "skip_already_synced",
                    "{\"reason\":\"" + reason + "\"}");
                return;
            }
            // #endregion
            if (choice == null || !choice.IsPicking) return;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            if (IsPlaying(choice))
            {
                // #region agent log
                DebugAgentLog.Write("A", "PickDiagnostics.TrySync", "skip_isPlaying",
                    "{\"reason\":\"" + reason + "\"}");
                // #endregion
                return;
            }

            var ready = TakeAllManager.IsOfferedHandReady();
            if (!ready)
            {
                // #region agent log
                var spawnedEarly = TakeAllManager.GetSpawnedCards();
                var alive = 0;
                if (spawnedEarly != null)
                {
                    foreach (var go in spawnedEarly)
                    {
                        if (go != null) alive++;
                    }
                }

                DebugAgentLog.Write("A", "PickDiagnostics.TrySync", "skip_not_ready",
                    "{\"reason\":\"" + reason + "\",\"alive\":" + alive + "}");
                // #endregion
                return;
            }

            var ids = TakeAllManager.GetSpawnedCardViewIds();
            if (ids == null || ids.Length == 0)
            {
                // #region agent log
                DebugAgentLog.Write("A", "PickDiagnostics.TrySync", "skip_no_ids",
                    "{\"reason\":\"" + reason + "\"}");
                // #endregion
                return;
            }

            DestroyOrphanOfferCards(choice, ids);

            var picks = PicksField != null ? (int)PicksField.GetValue(choice) : 0;
            _syncedThisPick = true;

            // #region agent log
            DebugAgentLog.Write("A", "PickDiagnostics.TrySync", "send_sync",
                "{\"reason\":\"" + reason + "\",\"n\":" + ids.Length + ",\"picks\":" + picks +
                ",\"master\":" + (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient).ToString().ToLower() + "}");
            // #endregion

            if (PhotonNetwork.OfflineMode)
            {
                TakeAllManager.ApplySyncedOfferedHand(ids, picks);
                Plugin.Instance?.Log($"Synced offered hand locally via {reason} (n={ids.Length})");
                return;
            }

            NetworkingManager.RPC(
                typeof(PickDiagnosticsPatch),
                nameof(RPCA_SyncOfferedHand),
                ids,
                picks);
            Plugin.Instance?.Log($"RPC SyncOfferedHand via {reason} (n={ids.Length} picks={picks})");
        }

        [UnboundRPC]
        public static void RPCA_SyncOfferedHand(int[] viewIds, int picksRemaining)
        {
            try
            {
                // Master's spawnedCards is the live PPI list. Replacing it mid-coroutine
                // (or with a snapshot) desyncs Instantiates vs remotes.
                if (!PhotonNetwork.OfflineMode && PhotonNetwork.IsMasterClient)
                {
                    _syncedThisPick = true;
                    Plugin.Instance?.Log(
                        $"RPCA_SyncOfferedHand ignored on master (n={viewIds?.Length ?? 0}).");
                    return;
                }

                if (Plugin.Instance != null)
                {
                    Plugin.Instance.StartCoroutine(ApplyOfferedHandWhenReady(viewIds, picksRemaining));
                    return;
                }

                ApplyOfferedHandNow(viewIds, picksRemaining);
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugAgentLog.Write("B", "PickDiagnostics.RPCA_SyncOfferedHand", "exception",
                    "{\"err\":\"" + ex.GetType().Name + "\"}");
                // #endregion
                Plugin.Instance?.LogWarn($"RPCA_SyncOfferedHand: {ex.Message}");
            }
        }

        private static IEnumerator ApplyOfferedHandWhenReady(int[] viewIds, int picksRemaining)
        {
            for (var i = 0; i < 30; i++)
            {
                var choice = CardChoice.instance;
                if (choice == null || !choice.IsPicking) yield break;
                if (CountMissingViews(viewIds) == 0) break;
                yield return null;
            }

            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) yield break;
            ApplyOfferedHandNow(viewIds, picksRemaining);
        }

        private static void ApplyOfferedHandNow(int[] viewIds, int picksRemaining)
        {
            var before = TakeAllManager.GetSpawnedCards();
            var beforeAlive = 0;
            if (before != null)
            {
                foreach (var go in before)
                {
                    if (go != null) beforeAlive++;
                }
            }

            TakeAllManager.ApplySyncedOfferedHand(viewIds, picksRemaining);
            _syncedThisPick = true;
            HideOrphanOfferCards(CardChoice.instance, viewIds);

            var after = TakeAllManager.GetSpawnedCards();
            var afterAlive = 0;
            var missing = CountMissingViews(viewIds);
            if (after != null)
            {
                foreach (var go in after)
                {
                    if (go != null) afterAlive++;
                }
            }

            // #region agent log
            DebugAgentLog.Write("B", "PickDiagnostics.RPCA_SyncOfferedHand", "applied",
                "{\"requested\":" + (viewIds?.Length ?? 0) +
                ",\"beforeAlive\":" + beforeAlive +
                ",\"afterAlive\":" + afterAlive +
                ",\"missingViews\":" + missing +
                ",\"picks\":" + picksRemaining +
                ",\"isMaster\":" + PhotonNetwork.IsMasterClient.ToString().ToLower() +
                ",\"offline\":" + PhotonNetwork.OfflineMode.ToString().ToLower() + "}");
            // #endregion

            Plugin.Instance?.Log(
                $"RPCA_SyncOfferedHand applied n={viewIds?.Length ?? 0} picks={picksRemaining} afterAlive={afterAlive} missing={missing}");
        }

        private static int CountMissingViews(int[] viewIds)
        {
            if (viewIds == null) return 0;
            var missing = 0;
            foreach (var id in viewIds)
            {
                if (PhotonNetwork.GetPhotonView(id) == null) missing++;
            }

            return missing;
        }

        private static bool ShouldRetrySpawn(CardChoice choice)
        {
            if (choice == null || !choice.IsPicking) return false;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return false;
            if (IsPlaying(choice)) return false;

            var spawned = TakeAllManager.GetSpawnedCards();
            if (spawned != null)
            {
                foreach (var go in spawned)
                {
                    if (go != null) return false;
                }
            }

            return true;
        }

        private static bool TryStartReplaceCards(CardChoice choice, bool clear, string reason)
        {
            if (choice == null || ReplaceCardsMethod == null) return false;
            try
            {
                if (IsPlayingField != null) IsPlayingField.SetValue(choice, false);
                var routine = ReplaceCardsMethod.Invoke(choice, new object[] { null, clear }) as IEnumerator;
                if (routine == null)
                {
                    Plugin.Instance?.LogWarn($"ReplaceCards invoke null ({reason})");
                    return false;
                }

                choice.StartCoroutine(routine);
                Plugin.Instance?.Log($"Started ReplaceCards via {reason}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"ReplaceCards via {reason} failed: {ex.Message}");
                return false;
            }
        }

        private static IEnumerator SyncAfterReplace(string reason, int generation)
        {
            // Wait out PPI deal delays, then push ViewIDs once the hand is stable.
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                if (generation != _syncGeneration) yield break;
                var choice = CardChoice.instance;
                if (choice == null || !choice.IsPicking) yield break;
                if (_syncedThisPick) yield break;
                if (IsPlaying(choice)) continue;
                TrySyncOfferedHand(choice, "after:" + reason);
                if (_syncedThisPick) yield break;
            }
        }

        private static bool IsPlaying(CardChoice choice)
        {
            return IsPlayingField != null && choice != null && (bool)IsPlayingField.GetValue(choice);
        }

        private static void DestroyOrphanOfferCards(CardChoice choice, int[] keepIds)
        {
            try
            {
                if (choice == null || keepIds == null) return;
                if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

                var keep = new HashSet<int>(keepIds);
                var children = ChildrenField?.GetValue(choice) as Transform[];
                CardVisuals[] visuals;
                try
                {
                    visuals = UnityEngine.Object.FindObjectsOfType<CardVisuals>();
                }
                catch
                {
                    return;
                }

                if (visuals == null) return;
                var destroyed = 0;
                foreach (var visual in visuals)
                {
                    if (visual == null) continue;
                    if (visual.GetComponentInParent<Player>() != null) continue;
                    var view = visual.GetComponentInParent<PhotonView>();
                    if (view == null || view.gameObject == null || view.ViewID == 0) continue;
                    if (keep.Contains(view.ViewID)) continue;
                    if (!NearPickSlot(visual.transform.position, children)) continue;
                    try
                    {
                        PhotonNetwork.Destroy(view.gameObject);
                        destroyed++;
                    }
                    catch
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(view.gameObject);
                            destroyed++;
                        }
                        catch
                        {
                        }
                    }
                }

                if (destroyed > 0)
                {
                    Plugin.Instance?.Log($"Destroyed {destroyed} orphan pick-card Photon object(s).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"DestroyOrphanOfferCards: {ex.Message}");
            }
        }

        private static void HideOrphanOfferCards(CardChoice choice, int[] keepIds)
        {
            try
            {
                if (choice == null || keepIds == null) return;
                var keep = new HashSet<int>(keepIds);
                var children = ChildrenField?.GetValue(choice) as Transform[];
                CardVisuals[] visuals;
                try
                {
                    visuals = UnityEngine.Object.FindObjectsOfType<CardVisuals>();
                }
                catch
                {
                    return;
                }

                if (visuals == null) return;
                var hidden = 0;
                foreach (var visual in visuals)
                {
                    if (visual == null) continue;
                    if (visual.GetComponentInParent<Player>() != null) continue;
                    var view = visual.GetComponentInParent<PhotonView>();
                    if (view == null || view.gameObject == null || view.ViewID == 0) continue;
                    if (keep.Contains(view.ViewID)) continue;
                    if (!NearPickSlot(visual.transform.position, children)) continue;
                    try
                    {
                        view.gameObject.SetActive(false);
                        hidden++;
                    }
                    catch
                    {
                    }
                }

                if (hidden > 0)
                {
                    Plugin.Instance?.Log($"Hid {hidden} orphan pick-card Photon object(s).");
                }
            }
            catch
            {
            }
        }

        private static bool NearPickSlot(Vector3 pos, Transform[] children)
        {
            if (children == null || children.Length == 0) return false;
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i] == null) continue;
                if ((children[i].position - pos).sqrMagnitude < 16f) return true;
            }

            return false;
        }

        private static void LogSnapshot(string tag, CardChoice choice, int pickerId)
        {
            if (choice == null)
            {
                Plugin.Instance?.Log($"PickDiag [{tag}] choice=null");
                return;
            }

            var children = ChildrenField?.GetValue(choice) as Transform[];
            var spawned = TakeAllManager.GetSpawnedCards();
            var picks = PicksField != null ? (int)PicksField.GetValue(choice) : -999;
            var isPlaying = IsPlaying(choice);
            var aliveChildren = 0;
            if (children != null)
            {
                foreach (var t in children)
                {
                    if (t != null) aliveChildren++;
                }
            }

            var aliveSpawned = 0;
            if (spawned != null)
            {
                foreach (var go in spawned)
                {
                    if (go != null) aliveSpawned++;
                }
            }

            var isMine = "?";
            try
            {
                var p = TakeAllManager.FindPlayer(choice.pickrID);
                isMine = p?.data?.view == null ? "noview" : p.data.view.IsMine.ToString();
            }
            catch (Exception ex)
            {
                isMine = "err:" + ex.GetType().Name;
            }

            Plugin.Instance?.Log(
                $"PickDiag [{tag}] picker={pickerId} pickrID={choice.pickrID} picks={picks} " +
                $"children={children?.Length ?? -1} (alive={aliveChildren}) " +
                $"spawned={spawned?.Count ?? -1} (alive={aliveSpawned}) " +
                $"IsPicking={choice.IsPicking} isPlaying={isPlaying} IsMine={isMine} " +
                $"CollectingAll={TakeAllManager.CollectingAll} busy={TakeAllManager.IsBusy} " +
                $"master={PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient} " +
                $"ready={TakeAllManager.IsOfferedHandReady()} synced={_syncedThisPick}");

            // #region agent log
            if (tag.StartsWith("PickWatch") || tag.StartsWith("StartPick"))
            {
                DebugAgentLog.Write("E", "PickDiagnostics.LogSnapshot", tag,
                    "{\"aliveSpawned\":" + aliveSpawned +
                    ",\"picks\":" + picks +
                    ",\"isMine\":\"" + isMine + "\"" +
                    ",\"master\":" + (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient).ToString().ToLower() +
                    ",\"synced\":" + _syncedThisPick.ToString().ToLower() +
                    ",\"ready\":" + TakeAllManager.IsOfferedHandReady().ToString().ToLower() + "}");
            }
            // #endregion
        }
    }

    /// <summary>Probe DoPlayerSelect input path — empty local spawnedCards = cannot flip/pick.</summary>
    [HarmonyPatch(typeof(CardChoice), "DoPlayerSelect")]
    internal static class DoPlayerSelectProbePatch
    {
        private static float _lastLog;

        private static void Prefix(CardChoice __instance)
        {
            if (Time.unscaledTime - _lastLog < 0.75f) return;
            _lastLog = Time.unscaledTime;
            if (__instance == null || !__instance.IsPicking) return;

            var spawned = TakeAllManager.GetSpawnedCards();
            var alive = 0;
            if (spawned != null)
            {
                foreach (var go in spawned)
                {
                    if (go != null) alive++;
                }
            }

            var isMine = "?";
            try
            {
                var p = TakeAllManager.FindPlayer(__instance.pickrID);
                isMine = p?.data?.view == null ? "noview" : p.data.view.IsMine.ToString();
            }
            catch
            {
                isMine = "err";
            }

            if (alive == 0)
            {
                Plugin.Instance?.Log(
                    $"PickDiag [DoPlayerSelect] spawnedAlive=0 list={spawned?.Count ?? -1} " +
                    $"pickrID={__instance.pickrID} IsMine={isMine} " +
                    $"master={PhotonNetwork.IsMasterClient}");
            }

            // #region agent log
            DebugAgentLog.Write("E", "DoPlayerSelect.Prefix", "tick",
                "{\"aliveSpawned\":" + alive +
                ",\"listCount\":" + (spawned?.Count ?? -1) +
                ",\"pickrID\":" + __instance.pickrID +
                ",\"isMine\":\"" + isMine + "\"" +
                ",\"master\":" + PhotonNetwork.IsMasterClient.ToString().ToLower() + "}");
            // #endregion
        }
    }
}
