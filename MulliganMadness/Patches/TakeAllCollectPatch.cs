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
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.IDoEndPick))]
    internal static class TakeAllCollectPatch
    {
        private static bool Prefix(CardChoice __instance, GameObject pickedCard, int theInt, int pickId, ref IEnumerator __result)
        {
            if (!TakeAllManager.CollectingAll) return true;

            TakeAllManager.CollectingAll = false;
            __result = CollectAll(__instance, pickedCard, theInt, pickId);
            return false;
        }

        private static IEnumerator CollectAll(CardChoice choice, GameObject pickedCard, int theInt, int pickId)
        {
            try
            {
                var spawnedField = AccessTools.Field(typeof(CardChoice), "spawnedCards");
                var spawned = spawnedField?.GetValue(choice) as List<GameObject> ?? new List<GameObject>();
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

                    if (theInt >= 0 && theInt < choice.transform.childCount)
                    {
                        choice.transform.GetChild(theInt).position = Vector3.LerpUnclamped(pickedStart, endPos, lerp);
                    }

                    yield return null;
                }

                try
                {
                    GamefeelManager.GameFeel((pickedStart - endPos).normalized * 2f);
                }
                catch
                {
                    // Gamefeel is optional
                }

                foreach (var go in cards)
                {
                    if (go == null) continue;
                    var visuals = go.GetComponentInChildren<CardVisuals>();
                    if (visuals != null) visuals.Leave();
                }

                yield return new WaitForSeconds(0.3f);

                if (theInt >= 0 && theInt < choice.transform.childCount)
                {
                    var child = choice.transform.GetChild(theInt);
                    var from = child.position;
                    t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime * speed * 1.5f;
                        child.position = Vector3.LerpUnclamped(from, pickedStart, Mathf.Clamp01(t));
                        yield return null;
                    }

                    child.position = pickedStart;
                }

                spawned.Clear();

                var picker = TakeAllManager.FindPlayer(pickId) ?? TakeAllManager.GetCurrentPicker();
                if (picker != null && LocalPlayerUtil.IsLocallyControlled(picker))
                {
                    var replace = AccessTools.Method(typeof(CardChoice), "ReplaceCards", new[] { typeof(GameObject), typeof(bool) });
                    if (replace != null)
                    {
                        choice.StartCoroutine((IEnumerator)replace.Invoke(choice, new object[] { pickedCard, false }));
                    }
                }
            }
            finally
            {
                TakeAllManager.CollectingAll = false;
            }
        }
    }
}
