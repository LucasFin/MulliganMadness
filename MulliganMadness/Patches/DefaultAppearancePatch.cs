using HarmonyLib;
using MulliganMadness.Utils;

namespace MulliganMadness.Patches
{
    [HarmonyPatch(typeof(Player), "Start")]
    internal static class DefaultAppearancePatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance?.data?.view == null) return;
            if (!Photon.Pun.PhotonNetwork.OfflineMode && !__instance.data.view.IsMine) return;
            DefaultAppearance.TryApply();
        }
    }
}
