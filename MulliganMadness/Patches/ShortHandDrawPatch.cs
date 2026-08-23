using System.Reflection;
using HarmonyLib;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    // Only shrink via GetPickerDraws. Do NOT destroy CardChoice.children on StartPick —
    // that races Pick Phase Improvements' online FixHandSize/ReplaceCards and aborts
    // the Photon spawn loop (empty offer, no Take All button).
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
}
