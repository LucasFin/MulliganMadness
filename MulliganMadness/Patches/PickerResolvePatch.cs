using HarmonyLib;
using MulliganMadness.Utils;

namespace MulliganMadness.Patches
{
    // RWF TDM calls StartPick(playerID) even when pickerType stays Team. Do not also
    // track CardChoiceVisuals.Show: RWF can pass a local photon id there, which is
    // not a playerID and would bind eggs / Take All / curses to the wrong person.
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
