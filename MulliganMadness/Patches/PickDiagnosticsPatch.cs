using System;
using System.Collections;
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

            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient) return true;

            var picks = PicksField != null && __instance != null
                ? (int)PicksField.GetValue(__instance)
                : 0;
            if (picks <= 0) return true;

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
            if (_syncedThisPick) return;
            if (choice == null || !choice.IsPicking) return;
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            if (IsPlaying(choice)) return;
            if (!TakeAllManager.IsOfferedHandReady()) return;

            var ids = TakeAllManager.GetSpawnedCardViewIds();
            if (ids == null || ids.Length == 0) return;

            var picks = PicksField != null ? (int)PicksField.GetValue(choice) : 0;
            _syncedThisPick = true;

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
                TakeAllManager.ApplySyncedOfferedHand(viewIds, picksRemaining);
                _syncedThisPick = true;
                Plugin.Instance?.Log(
                    $"RPCA_SyncOfferedHand applied n={viewIds?.Length ?? 0} picks={picksRemaining}");
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"RPCA_SyncOfferedHand: {ex.Message}");
            }
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
                if (Plugin.Instance != null)
                {
                    Plugin.Instance.StartCoroutine(SyncAfterReplace(reason));
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"ReplaceCards via {reason} failed: {ex.Message}");
                return false;
            }
        }

        private static IEnumerator SyncAfterReplace(string reason)
        {
            // Wait out PPI deal delays, then push ViewIDs once the hand is stable.
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForSecondsRealtime(0.1f);
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
        }
    }
}
