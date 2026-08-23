using System;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Safety net around the pick phase. Every member here is a Finalizer or a Postfix:
    /// nothing in this file changes who spawns cards, what is spawned, or when.
    ///
    /// Background — 0.3.27 through 0.3.31 shipped an "online pick spawn authority" that
    /// re-gated card spawning on PhotonNetwork.IsMasterClient. That was based on a misreading
    /// of a log line. Vanilla CardChoice.Pick is:
    ///
    ///     else if (PlayerManager.instance.GetPlayerWithID(pickrID).data.view.IsMine)
    ///         StartCoroutine(ReplaceCards(pickedCard, clear));
    ///
    /// The *picker* spawns their own hand, and SpawnUniqueCard uses PhotonNetwork.Instantiate
    /// so every client receives it. IsMine == false on the host is correct whenever a non-host
    /// is picking — it is not a stall. Overriding it with IsMasterClient meant:
    ///
    ///   * the picker's private spawnedCards list stayed empty, and CardChoice.DoPlayerSelect
    ///     returns early on spawnedCards.Count == 0, so a non-host picker could not move the
    ///     selection or confirm a card until an RPC back-filled the list up to 2.5s later;
    ///   * the host Photon-owned cards for someone else's pick, leaving orphans whose ViewIDs
    ///     got reused — the "friend sees Sneaky the host never took" bug;
    ///   * CardChoice.picks was written on remotes by reflection from an RPC payload.
    ///
    /// The empty-offer bug those releases were chasing was really the readonly-struct Harmony
    /// abort (fixed in 0.3.23) and destroying CardChoice.children on StartPick (fixed in
    /// 0.3.24). Both were already gone. Do not reintroduce a spawn-authority override here.
    /// </summary>
    // Class-level [HarmonyPatch] is required: PatchClassProcessor skips any class
    // without one, so the finalizers below were silently never installed.
    [HarmonyPatch]
    internal static class PickSafetyPatch
    {
        private static readonly FieldInfo IsPlayingField = AccessTools.Field(typeof(CardChoice), "isPlaying");

        /// <summary>
        /// ReplaceCards is a coroutine that builds the offered hand. If it throws partway
        /// through, isPlaying stays true and the pick softlocks with no cards. Log it and
        /// clear the flag so the next Pick can run.
        /// </summary>
        [HarmonyPatch(typeof(CardChoice), "ReplaceCards", typeof(GameObject), typeof(bool))]
        [HarmonyFinalizer]
        private static Exception CaptureReplaceCards(Exception __exception)
        {
            if (__exception == null) return null;

            Plugin.Instance?.LogWarn(
                $"CardChoice.ReplaceCards threw {__exception.GetType().Name}: {__exception.Message}");
            try
            {
                if (IsPlayingField != null && CardChoice.instance != null)
                {
                    IsPlayingField.SetValue(CardChoice.instance, false);
                }
            }
            catch
            {
                // Nothing more we can do; the exception still propagates.
            }

            return __exception;
        }

        [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
        [HarmonyFinalizer]
        private static Exception CapturePick(Exception __exception)
        {
            if (__exception != null)
            {
                Plugin.Instance?.LogWarn(
                    $"CardChoice.Pick threw {__exception.GetType().Name}: {__exception.Message}");
            }

            return __exception;
        }

        /// <summary>
        /// SpawnUniqueCard recurses while searching for a card the picker is allowed to have.
        /// A throw here aborts the whole hand build, so surface it by name instead of letting
        /// it disappear into a coroutine.
        /// </summary>
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
                        $"CardChoice.SpawnUniqueCard threw {__exception.GetType().Name}: {__exception.Message}");
                }

                return __exception;
            }
        }
    }

    /// <summary>
    /// Optional pick-phase logging, off unless Diagnostics/LogPickPhase is enabled.
    /// Postfix only.
    /// </summary>
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
    [HarmonyPriority(Priority.Last)]
    internal static class PickPhaseLogPatch
    {
        private static void Postfix(CardChoice __instance, int picksToSet, int pickerIDToSet)
        {
            if (Plugin.Configs == null || !Plugin.Configs.LogPickPhase.Value) return;

            try
            {
                var children = AccessTools.Field(typeof(CardChoice), "children")?.GetValue(__instance) as Transform[];
                var spawned = TakeAllManager.GetSpawnedCards();
                Plugin.Instance?.Log(
                    $"StartPick picks={picksToSet} picker={pickerIDToSet} pickrID={__instance.pickrID} " +
                    $"children={children?.Length ?? -1} spawned={spawned?.Count ?? -1} " +
                    $"master={Photon.Pun.PhotonNetwork.OfflineMode || Photon.Pun.PhotonNetwork.IsMasterClient}");

                // Follow the offer as it builds. This is the datum that separates "the hand
                // never spawned" from "it spawned and something removed it" — the two have
                // completely different causes and look identical on screen.
                if (Plugin.Instance != null) Plugin.Instance.StartCoroutine(WatchSpawn(pickerIDToSet));
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"Pick log skipped: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator WatchSpawn(int pickerId)
        {
            var isPlayingField = AccessTools.Field(typeof(CardChoice), "isPlaying");
            var picksField = AccessTools.Field(typeof(CardChoice), "picks");

            foreach (var delay in new[] { 0.3f, 0.8f, 1.6f, 3.0f })
            {
                yield return new WaitForSecondsRealtime(delay);

                var choice = CardChoice.instance;
                if (choice == null) yield break;

                var spawned = TakeAllManager.GetSpawnedCards();
                var alive = 0;
                if (spawned != null)
                {
                    foreach (var go in spawned)
                    {
                        if (go != null) alive++;
                    }
                }

                var picker = TakeAllManager.FindPlayer(pickerId);
                var mine = picker?.data?.view != null && picker.data.view.IsMine;

                Plugin.Instance?.Log(
                    $"PickWatch t={delay:0.0}s IsPicking={choice.IsPicking} " +
                    $"spawned={spawned?.Count ?? -1} alive={alive} " +
                    $"picks={(picksField != null ? picksField.GetValue(choice) : "?")} " +
                    $"isPlaying={(isPlayingField != null ? isPlayingField.GetValue(choice) : "?")} " +
                    $"pickerFound={picker != null} pickerIsMine={mine}");

                if (!choice.IsPicking) yield break;
            }
        }
    }
}
