using System.Linq;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    [HarmonyPatch]
    internal static class ShortHandDrawPatch
    {
        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("DrawNCards.DrawNCards")
                       ?? AccessTools.TypeByName("PickNCards.DrawNCards")
                       ?? AccessTools.TypeByName("PickNCards.PickNCards");
            return type == null ? null : AccessTools.Method(type, "GetPickerDraws", new[] { typeof(int) });
        }

        private static void Postfix(int __0, ref int __result)
        {
            if (__result <= 1) return;
            var picker = ResolvePicker(__0);
            if (picker == null || !CurseOwnership.Has(picker, ShortHand.Card)) return;
            __result = Mathf.Max(1, __result - 1);
        }

        internal static Player ResolvePicker(int playerOrTeamId)
        {
            var current = TakeAllManager.GetCurrentPicker();
            if (current != null)
            {
                if (playerOrTeamId == current.playerID) return current;
                if (CardChoice.instance != null && playerOrTeamId == CardChoice.instance.pickrID) return current;
            }

            return FindPlayer(playerOrTeamId) ?? current;
        }

        internal static Player FindPlayer(int playerId)
        {
            if (PlayerManager.instance?.players == null) return null;
            foreach (var player in PlayerManager.instance.players)
            {
                if (player != null && player.playerID == playerId) return player;
            }

            return null;
        }
    }

    // Pick N Cards rebuilds CardChoice.children from the draw count every StartPick.
    // Shrink that list after it runs so the Photon spawn loop offers one fewer card.
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.StartPick))]
    internal static class ShortHandStartPickPatch
    {
        private static bool Prepare() =>
            AccessTools.TypeByName("DrawNCards.DrawNCards") != null
            || AccessTools.TypeByName("PickNCards.DrawNCards") != null;

        private static void Postfix(CardChoice __instance, int pickerIDToSet)
        {
            var picker = TakeAllManager.GetCurrentPicker()
                         ?? ShortHandDrawPatch.FindPlayer(__instance.pickrID)
                         ?? ShortHandDrawPatch.FindPlayer(pickerIDToSet);
            if (picker == null || !CurseOwnership.Has(picker, ShortHand.Card)) return;

            var field = AccessTools.Field(typeof(CardChoice), "children");
            if (field == null) return;
            if (!(field.GetValue(__instance) is Transform[] children) || children.Length <= 1) return;

            var last = children[children.Length - 1];
            if (last != null) Object.Destroy(last.gameObject);
            field.SetValue(__instance, children.Take(children.Length - 1).ToArray());
        }
    }
}
