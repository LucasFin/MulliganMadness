using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    // Vanilla IDoEndPick flies the chosen card in and flings the rest away, which makes
    // Take All look like a single pick. When we are collecting the whole hand, gather
    // every offered card to the center and play the taken animation on all of them.
    //
    // Priority.First is deliberate. Root's Nulled Cards installs its own Prefix on this same
    // method, taking ___spawnedCards, pickedCard and ___speed by ref and starting a GrabCard
    // coroutine over them. Distill Knowledge is a Nulled card, so that prefix and this one
    // meet on every Distill taken through Take All, and without a declared priority which
    // runs first is plugin load order. Going first means CollectingAll is consumed and
    // __result is replaced before anything else inspects the offer.
    //
    // Do not rely on this alone to keep GrabCard from running: whether a prefix returning
    // false also skips the remaining prefixes is a Harmony implementation detail. The
    // per-frame guards in CollectAll are what actually make a hand that gets rebuilt
    // underneath us survivable.
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.IDoEndPick))]
    [HarmonyPriority(Priority.First)]
    internal static class TakeAllCollectPatch
    {
        private static bool Prefix(CardChoice __instance, GameObject pickedCard, int theInt, int pickId, ref IEnumerator __result)
        {
            if (!TakeAllManager.CollectingAll) return true;

            TakeAllManager.CollectingAll = false;
            __result = CollectAll(__instance, pickedCard, theInt, pickId);
            return false;
        }

        /// <summary>
        /// Everything above the finally is decoration. The finally is the pick phase.
        ///
        /// Vanilla IDoEndPick ends with spawnedCards.Clear() and, on the picker's client,
        /// StartCoroutine(ReplaceCards(pickedCard)) - that call is the only thing that deals
        /// the next hand. Reaching it is therefore not optional, and a coroutine that throws
        /// never reaches its own last line. So the handoff lives in a finally, and the
        /// animation above is written so that a card, a transform, or the whole CardChoice
        /// vanishing mid-flight costs at most a dropped frame of movement.
        /// </summary>
        private static IEnumerator CollectAll(CardChoice choice, GameObject pickedCard, int theInt, int pickId)
        {
            // Hoisted so the finally can still clear the offer if the try dies early.
            List<GameObject> spawned = null;

            try
            {
                var spawnedField = AccessTools.Field(typeof(CardChoice), "spawnedCards");
                spawned = spawnedField?.GetValue(choice) as List<GameObject> ?? new List<GameObject>();
                var speed = Traverse.Create(choice).Field("speed").GetValue<float>();
                if (speed <= 0.01f) speed = 4f;

                var endPos = CardChoiceVisuals.instance != null
                    ? CardChoiceVisuals.instance.transform.position
                    : Vector3.zero;
                var pickedStart = pickedCard != null ? pickedCard.transform.position : endPos;

                var cards = new List<GameObject>();
                var starts = new List<Vector3>();
                foreach (var go in spawned)
                {
                    if (go == null) continue;
                    cards.Add(go);
                    starts.Add(go.transform.position);
                }

                float t = 0f;
                while (t < 1f)
                {
                    if (choice == null) break;

                    if (CardChoiceVisuals.instance != null)
                    {
                        CardChoiceVisuals.instance.framesToSnap = 1;
                    }

                    t += Time.deltaTime * speed;
                    var lerp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    for (var i = 0; i < cards.Count; i++)
                    {
                        if (cards[i] == null) continue;
                        cards[i].transform.position = Vector3.LerpUnclamped(starts[i], endPos, lerp);
                    }

                    // Re-resolved every frame on purpose. Distill's grant destroys and
                    // rebuilds the offer children while this coroutine is suspended, so a
                    // Transform cached before the first yield is a dangling reference by the
                    // time the loop resumes.
                    MoveTo(LiveChild(choice, theInt), Vector3.LerpUnclamped(pickedStart, endPos, lerp));

                    yield return null;
                }

                LeaveAll(cards, pickedStart, endPos);

                yield return new WaitForSeconds(0.3f);

                var from = ChildPosition(choice, theInt);
                if (from.HasValue)
                {
                    t = 0f;
                    while (t < 1f)
                    {
                        if (choice == null) break;
                        t += Time.deltaTime * speed * 1.5f;
                        MoveTo(LiveChild(choice, theInt), Vector3.LerpUnclamped(from.Value, pickedStart, Mathf.Clamp01(t)));
                        yield return null;
                    }

                    MoveTo(LiveChild(choice, theInt), pickedStart);
                }
            }
            finally
            {
                TakeAllManager.CollectingAll = false;
                HandBackThePick(choice, spawned, pickedCard, pickId);
            }
        }

        /// <summary>
        /// The offer child for this slot, or null when the parent or the slot is gone.
        /// </summary>
        private static Transform LiveChild(CardChoice choice, int theInt)
        {
            if (choice == null) return null;
            var root = choice.transform;
            if (root == null || theInt < 0 || theInt >= root.childCount) return null;
            var child = root.GetChild(theInt);
            return child == null ? null : child;
        }

        private static Vector3? ChildPosition(CardChoice choice, int theInt)
        {
            var child = LiveChild(choice, theInt);
            return child == null ? (Vector3?)null : child.position;
        }

        private static void MoveTo(Transform target, Vector3 position)
        {
            if (target == null) return;
            target.position = position;
        }

        private static void LeaveAll(List<GameObject> cards, Vector3 pickedStart, Vector3 endPos)
        {
            try
            {
                GamefeelManager.GameFeel((pickedStart - endPos).normalized * 2f);
            }
            catch
            {
                // Gamefeel is optional.
            }

            foreach (var go in cards)
            {
                if (go == null) continue;
                try
                {
                    var visuals = go.GetComponentInChildren<CardVisuals>();
                    if (visuals != null) visuals.Leave();
                }
                catch
                {
                    // One card refusing to animate out must not strand the rest.
                }
            }
        }

        /// <summary>
        /// Clears the consumed offer and deals the next hand, exactly as vanilla's tail does.
        /// Runs from a finally, so it also fires when the animation threw or was disposed -
        /// the IsPicking check is what keeps a torn-down pick from being restarted.
        /// </summary>
        private static void HandBackThePick(CardChoice choice, List<GameObject> spawned, GameObject pickedCard, int pickId)
        {
            try
            {
                spawned?.Clear();

                if (choice == null || !choice.IsPicking) return;

                var picker = TakeAllManager.FindPlayer(pickId) ?? TakeAllManager.GetCurrentPicker();
                if (picker == null || !LocalPlayerUtil.IsLocallyControlled(picker)) return;

                var replace = AccessTools.Method(typeof(CardChoice), "ReplaceCards", new[] { typeof(GameObject), typeof(bool) });
                if (replace == null) return;

                choice.StartCoroutine((IEnumerator)replace.Invoke(choice, new object[] { pickedCard, false }));
            }
            catch (Exception ex)
            {
                // Losing the next hand is the one failure here that ends the match, so it is
                // logged loudly rather than swallowed.
                Plugin.Instance?.LogWarn($"Take All could not hand the pick back: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
