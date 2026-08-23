using HarmonyLib;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
    [HarmonyPriority(Priority.Last)]
    internal static class DraftSniperPickPatch
    {
        private static bool Prefix(GameObject pickedCard)
        {
            if (TakeAllManager.CollectingAll) return true;
            if (!DraftSniperManager.IsBlocked(pickedCard)) return true;
            DraftSniperManager.NotifyLockedClick();
            return false;
        }
    }
}
