using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Utils;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Online pick spawn authority + diagnostics.
    /// Vanilla Pick(null) only StartCoroutine(ReplaceCards) when view.IsMine. In RWF TDM the
    /// host often has IsMine=false for the acting picker, so nobody spawns. Forcing ReplaceCards
    /// on every client that passes a local IsMine/stall check double-spawns (ghost borders +
    /// extra flip sounds). Online: only the master starts ReplaceCards.
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

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void AfterStartPick(CardChoice __instance, int picksToSet, int pickerIDToSet)
        {
            try
            {
                _retriedThisPick = false;
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

            // Do not swallow — Prefix already owns the null-card path.
            return __exception;
        }

        [HarmonyPatch(typeof(CardChoice), "ReplaceCards", typeof(GameObject), typeof(bool))]
        [HarmonyPrefix]
        private static void BeforeReplaceCards(CardChoice __instance, GameObject pickedCard, bool clear)
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

                if (delay < 1.5f || _retriedThisPick) continue;
                if (!ShouldRetrySpawn(choice)) continue;

                _retriedThisPick = true;
                Plugin.Instance?.LogWarn(
                    "Pick spawn stalled on master — forcing ReplaceCards once.");
                TryStartReplaceCards(choice, clear: false, reason: "stall");
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
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"ReplaceCards via {reason} failed: {ex.Message}");
                return false;
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
                $"ready={TakeAllManager.IsOfferedHandReady()}");
        }
    }
}
