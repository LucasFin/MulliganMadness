using System;
using System.Collections;
using System.Collections.Generic;
using MulliganMadness.Curses;
using MulliganMadness.Stats;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public class AutoPickController : MonoBehaviour
    {
        private static AutoPickController _instance;
        private Coroutine _running;
        private int _pickGeneration;
        private int _runningPlayerId = -1;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void ResetForNewGame()
        {
            ResetForCurrentPick();
        }

        public static void ResetForCurrentPick()
        {
            if (_instance == null) return;
            if (_instance._running != null)
            {
                _instance.StopCoroutine(_instance._running);
                _instance._running = null;
            }
            _instance._runningPlayerId = -1;
            _instance._pickGeneration++;
        }

        public static void NotifyPlayerPickStarted()
        {
            if (_instance == null) return;
            _instance.BeginForCurrentPick();
        }

        private void BeginForCurrentPick()
        {
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || picker.data?.currentCards == null) return;
            if (!PlayerStatsSnapshot.IsLocallyControlled(picker)) return;

            var mode = ResolveMode(picker);
            if (mode == AutoPickMode.None) return;

            // Extra picks in the same turn reuse this coroutine instead of restarting
            // (restarting was picking a half-spawned hand and looping the pick sound).
            if (_running != null && _runningPlayerId == picker.playerID) return;

            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            _pickGeneration++;
            var gen = _pickGeneration;
            _runningPlayerId = picker.playerID;
            _running = StartCoroutine(RunAutoPick(picker.playerID, mode, gen));
        }

        private static AutoPickMode ResolveMode(Player player)
        {
            if (HasCurse(player, ForcedChoice.Card)) return AutoPickMode.ForcedImmediate;
            if (HasCurse(player, LeftmostLuck.Card)) return AutoPickMode.Leftmost;
            if (HasCurse(player, PanicPick.Card)) return AutoPickMode.PanicTimer;
            return AutoPickMode.None;
        }

        private static bool HasCurse(Player player, CardInfo curse)
        {
            if (player?.data?.currentCards == null || curse == null) return false;
            foreach (var card in player.data.currentCards)
            {
                if (card == null) continue;
                if (card == curse) return true;
                if (!string.IsNullOrEmpty(curse.cardName) &&
                    string.Equals(card.cardName, curse.cardName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator RunAutoPick(int playerID, AutoPickMode mode, int generation)
        {
            try
            {
                var safety = 12;
                while (safety-- > 0 && generation == _pickGeneration)
                {
                    if (!WaitForOwnPick(playerID)) yield break;

                    var timeout = 10f;
                    while (timeout > 0f && generation == _pickGeneration)
                    {
                        if (TakeAllManager.IsBusy) yield break;
                        if (!WaitForOwnPick(playerID)) yield break;

                        if (TakeAllManager.IsOfferedHandReady() && HandIsSelectable()) break;

                        timeout -= Time.unscaledDeltaTime;
                        yield return null;
                    }

                    if (generation != _pickGeneration) yield break;
                    if (!WaitForOwnPick(playerID)) yield break;

                    if (mode == AutoPickMode.PanicTimer)
                    {
                        var wait = Mathf.Max(0.1f, SessionSettings.Current.PanicTimerSeconds);
                        var elapsed = 0f;
                        while (elapsed < wait && generation == _pickGeneration && WaitForOwnPick(playerID))
                        {
                            if (TakeAllManager.IsBusy) yield break;
                            var live = TakeAllManager.GetSpawnedCards();
                            if (live == null || live.Count == 0) yield break;
                            elapsed += Time.unscaledDeltaTime;
                            yield return null;
                        }

                        if (generation != _pickGeneration || !WaitForOwnPick(playerID)) yield break;
                    }

                    var spawned = TakeAllManager.GetReadySpawnedCards();
                    if (spawned == null || spawned.Count == 0) yield break;

                    var pick = SelectCard(spawned, mode);
                    if (pick == null || !spawned.Contains(pick))
                    {
                        yield return new WaitForSecondsRealtime(0.1f);
                        continue;
                    }

                    Plugin.Instance.Log($"Auto-pick ({mode}) for player {playerID}.");
                    TryPick(pick);

                    var picked = pick;
                    var settle = 2.5f;
                    while (settle > 0f && generation == _pickGeneration)
                    {
                        yield return null;
                        settle -= Time.unscaledDeltaTime;
                        if (!WaitForOwnPick(playerID)) yield break;

                        var now = TakeAllManager.GetSpawnedCards();
                        if (now == null || now.Count == 0 || !now.Contains(picked)) break;
                    }

                    yield return new WaitForSecondsRealtime(0.2f);
                    if (!WaitForOwnPick(playerID)) yield break;
                }
            }
            finally
            {
                if (generation == _pickGeneration)
                {
                    _running = null;
                    _runningPlayerId = -1;
                }
            }
        }

        private static bool WaitForOwnPick(int playerID)
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = TakeAllManager.GetCurrentPicker();
            return picker != null && picker.playerID == playerID;
        }

        private static GameObject SelectCard(List<GameObject> spawned, AutoPickMode mode)
        {
            if (spawned == null || spawned.Count == 0) return null;
            switch (mode)
            {
                case AutoPickMode.Leftmost:
                    return SelectLeftmost(spawned);
                case AutoPickMode.ForcedImmediate:
                case AutoPickMode.PanicTimer:
                    return spawned[UnityEngine.Random.Range(0, spawned.Count)];
                default:
                    return null;
            }
        }

        private static GameObject SelectLeftmost(List<GameObject> spawned)
        {
            GameObject best = null;
            var bestX = float.MaxValue;
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var x = go.transform.position.x;
                if (x >= bestX) continue;
                bestX = x;
                best = go;
            }

            return best ?? spawned[0];
        }

        private static bool HandIsSelectable()
        {
            var spawned = TakeAllManager.GetReadySpawnedCards();
            var raw = TakeAllManager.GetSpawnedCards();
            if (spawned == null || raw == null || spawned.Count == 0) return false;
            return spawned.Count >= raw.Count;
        }

        private static void TryPick(GameObject pick)
        {
            var choice = CardChoice.instance;
            if (choice == null || pick == null || !choice.IsPicking) return;

            var spawned = TakeAllManager.GetSpawnedCards();
            if (spawned == null || !spawned.Contains(pick)) return;

            try
            {
                TakeAllManager.EndPickWithoutApplying(pick);
            }
            catch (Exception ex)
            {
                Plugin.Instance.LogWarn($"Auto-pick EndPickWithoutApplying failed: {ex.Message}");
                var pub = pick.GetComponent<PublicInt>();
                var index = spawned.IndexOf(pick);
                var theInt = pub != null ? pub.theInt : index;
                try
                {
                    choice.StartCoroutine(choice.IDoEndPick(pick, theInt, choice.pickrID));
                }
                catch (Exception ex2)
                {
                    Plugin.Instance.LogWarn($"Auto-pick IDoEndPick failed: {ex2.Message}");
                    choice.Pick(pick, false);
                }
            }
        }

        private enum AutoPickMode
        {
            None,
            ForcedImmediate,
            PanicTimer,
            Leftmost
        }
    }
}
