using HarmonyLib;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    // RWF TDM calls StartPick(playerID) even when pickerType stays Team. Do not also
    // track CardChoiceVisuals.Show: RWF can pass a local photon id there, which is
    // not a playerID and would bind eggs / Take All / curses to the wrong person.
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
    [HarmonyPriority(Priority.First)]
    internal static class StartPickTrackerPatch
    {
        private static void Postfix(CardChoice __instance, int pickerIDToSet)
        {
            TakeAllManager.NoteActingPicker(pickerIDToSet);
            try
            {
                var children = AccessTools.Field(typeof(CardChoice), "children")?.GetValue(__instance) as Transform[];
                var spawned = TakeAllManager.GetSpawnedCards();
                Plugin.Instance?.Log(
                    $"StartPick acting={pickerIDToSet} pickrID={__instance.pickrID} " +
                    $"children={children?.Length ?? -1} spawned={spawned?.Count ?? -1} " +
                    $"master={Photon.Pun.PhotonNetwork.OfflineMode || Photon.Pun.PhotonNetwork.IsMasterClient}");
            }
            catch
            {
            }
        }
    }
}
