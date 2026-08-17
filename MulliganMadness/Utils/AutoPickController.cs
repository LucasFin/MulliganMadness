using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MulliganMadness.Curses;
using UnboundLib;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public class AutoPickController : MonoBehaviour
    {
        private static AutoPickController _instance;
        private Coroutine _running;
        private int _pickGeneration;

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
            _instance._pickGeneration++;
        }

        public static void NotifyPlayerPickStarted()
        {
            if (_instance == null) return;
            _instance.BeginForCurrentPick();
        }

        private void BeginForCurrentPick()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || picker.data?.currentCards == null) return;
            if (picker.data.view == null || !picker.data.view.IsMine) return;

            var mode = ResolveMode(picker);
            if (mode == AutoPickMode.None) return;

            _pickGeneration++;
            var gen = _pickGeneration;
            _running = StartCoroutine(RunAutoPick(picker.playerID, mode, gen));
        }

        private static AutoPickMode ResolveMode(Player player)
        {
            var cards = player.data.currentCards;
            if (ForcedChoice.Card != null && cards.Contains(ForcedChoice.Card)) return AutoPickMode.ForcedImmediate;
            if (LeftmostLuck.Card != null && cards.Contains(LeftmostLuck.Card)) return AutoPickMode.Leftmost;
            if (PanicPick.Card != null && cards.Contains(PanicPick.Card)) return AutoPickMode.PanicTimer;
            return AutoPickMode.None;
        }

        private IEnumerator RunAutoPick(int playerID, AutoPickMode mode, int generation)
        {
            // Wait for the offered hand to actually spawn.
            float timeout = 8f;
            List<GameObject> spawned = null;
            while (timeout > 0f && generation == _pickGeneration)
            {
                if (TakeAllManager.IsBusy)
                {
                    _running = null;
                    yield break;
                }

                if (CardChoice.instance != null && CardChoice.instance.IsPicking)
                {
                    var picker = TakeAllManager.GetCurrentPicker();
                    if (picker != null && picker.playerID == playerID)
                    {
                        spawned = TakeAllManager.GetSpawnedCards();
                        if (spawned != null && spawned.Count > 0) break;
                    }
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (generation != _pickGeneration || spawned == null || spawned.Count == 0)
            {
                _running = null;
                yield break;
            }

            if (mode == AutoPickMode.PanicTimer)
            {
                var wait = Plugin.Configs != null ? Plugin.Configs.PanicTimerSeconds.Value : 3f;
                float elapsed = 0f;
                while (elapsed < wait && generation == _pickGeneration && CardChoice.instance != null && CardChoice.instance.IsPicking)
                {
                    if (TakeAllManager.IsBusy)
                    {
                        _running = null;
                        yield break;
                    }

                    // If the player already picked, abort.
                    if (TakeAllManager.GetSpawnedCards() == null || TakeAllManager.GetSpawnedCards().Count == 0)
                    {
                        _running = null;
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (generation != _pickGeneration || CardChoice.instance == null || !CardChoice.instance.IsPicking)
                {
                    _running = null;
                    yield break;
                }

                spawned = TakeAllManager.GetSpawnedCards();
                if (spawned == null || spawned.Count == 0)
                {
                    _running = null;
                    yield break;
                }
            }

            GameObject pick = null;
            switch (mode)
            {
                case AutoPickMode.Leftmost:
                    pick = spawned[0];
                    break;
                case AutoPickMode.ForcedImmediate:
                case AutoPickMode.PanicTimer:
                    pick = spawned[Random.Range(0, spawned.Count)];
                    break;
            }

            if (pick != null && CardChoice.instance != null && CardChoice.instance.IsPicking)
            {
                Plugin.Instance.Log($"Auto-pick ({mode}) for player {playerID}.");
                CardChoice.instance.Pick(pick, true);
            }

            _running = null;
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
