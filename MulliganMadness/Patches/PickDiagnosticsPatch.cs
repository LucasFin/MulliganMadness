using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Stats;
using MulliganMadness.Utils;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Online empty-offer diagnostics + stall recovery.
    /// Vanilla only starts ReplaceCards when the picker's view IsMine; if that coroutine
    /// throws, IsPicking stays true with zero cards (camera + rules toast, no hand).
    /// </summary>
    [HarmonyPatch]
    internal static class PickDiagnosticsPatch
    {
        private static readonly FieldInfo ChildrenField = AccessTools.Field(typeof(CardChoice), "children");
        private static readonly FieldInfo PicksField = AccessTools.Field(typeof(CardChoice), "picks");
        private static readonly FieldInfo IsPlayingField = AccessTools.Field(typeof(CardChoice), "isPlaying");
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

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
        [HarmonyFinalizer]
        private static Exception AfterPick(Exception __exception)
        {
            if (__exception != null)
            {
                Plugin.Instance?.LogWarn(
                    $"CardChoice.Pick threw {__exception.GetType().Name}: {__exception.Message}");
            }

            return null; // swallow so IsPicking softlock can still be recovered by WatchSpawn
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
                    "Pick spawn stalled (IsPicking, zero cards). Clearing isPlaying and retrying Pick().");
                try
                {
                    if (IsPlayingField != null) IsPlayingField.SetValue(choice, false);
                    choice.Pick(null, false);
                }
                catch (Exception ex)
                {
                    Plugin.Instance?.LogWarn($"Pick spawn retry failed: {ex.Message}");
                }
            }
        }

        private static bool ShouldRetrySpawn(CardChoice choice)
        {
            if (choice == null || !choice.IsPicking) return false;
            var spawned = TakeAllManager.GetSpawnedCards();
            if (spawned != null)
            {
                foreach (var go in spawned)
                {
                    if (go != null) return false;
                }
            }

            // Vanilla only starts ReplaceCards when view.IsMine. RWF / local PlayerAPI can
            // show pick UI for a "local" picker whose Photon view is not IsMine yet — then
            // nobody on this client spawns. Retry if MM considers them locally controlled.
            var picker = TakeAllManager.FindPlayer(choice.pickrID) ?? TakeAllManager.GetCurrentPicker();
            if (picker == null) return PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient;
            return PlayerStatsSnapshot.IsLocallyControlled(picker)
                   || PhotonNetwork.OfflineMode
                   || PhotonNetwork.IsMasterClient;
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
            var isPlaying = IsPlayingField != null && (bool)IsPlayingField.GetValue(choice);
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
