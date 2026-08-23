using HarmonyLib;
using MulliganMadness.Utils;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Records who is actually picking.
    ///
    /// RWF's team deathmatch calls StartPick(picksToSet, playerID) with the acting player's id
    /// even when pickerType stays Team, so this is more accurate than CardChoice.pickrID for
    /// binding Take All uses and curses to the right player.
    ///
    /// Do not also track CardChoiceVisuals.Show — RWF can pass a local photon id there.
    /// Postfix only: this never changes the pick.
    /// </summary>
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
    [HarmonyPriority(Priority.First)]
    internal static class StartPickTrackerPatch
    {
        private static void Postfix(int pickerIDToSet)
        {
            TakeAllManager.NoteActingPicker(pickerIDToSet);
        }
    }
}
