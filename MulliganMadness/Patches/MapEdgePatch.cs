using System;
using HarmonyLib;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Marks the window in which a map-edge bounce is being processed, so Hard Edges can
    /// amplify only that knockback.
    ///
    /// Vanilla OutOfBoundsHandler.LateUpdate only calls CallTakeForce when data.view.IsMine,
    /// so the flag is set on the owning client alone. The Finalizer guarantees it is cleared
    /// even if the LateUpdate body throws — a stale flag would keep multiplying unrelated
    /// knockback for the rest of the match.
    /// </summary>
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    internal static class MapEdgeOobFlagPatch
    {
        internal static Player Current;

        private static void Prefix(OutOfBoundsHandler __instance)
        {
            Current = null;
            try
            {
                var tr = Traverse.Create(__instance);
                if (!IsOobFlag(tr.Field("outOfBounds").GetValue()) &&
                    !IsOobFlag(tr.Field("almostOutOfBounds").GetValue()))
                {
                    return;
                }

                var data = tr.Field("data").GetValue<CharacterData>();
                var player = data != null ? data.player : null;

                var view = data?.view ?? player?.GetComponent<Photon.Pun.PhotonView>();
                if (view == null || !view.IsMine) return;

                Current = player;
            }
            catch
            {
                Current = null;
            }
        }

        private static bool IsOobFlag(object value) => value is bool b && b;

        private static void Postfix() => Current = null;

        private static Exception Finalizer(Exception __exception)
        {
            Current = null;
            return __exception;
        }
    }

    /// <summary>
    /// Hard Edges: map edges bounce you harder.
    ///
    /// CallTakeForce is the broadcast point (view.RPC("RPCA_SendTakeForce", RpcTarget.All)),
    /// so the multiplier is baked into the value every client receives and only the owning
    /// client — the one that raised it — needs to apply it.
    /// </summary>
    [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.CallTakeForce))]
    internal static class HardEdgesForcePatch
    {
        private static void Prefix(HealthHandler __instance, ref Vector2 force)
        {
            var player = MapEdgeOobFlagPatch.Current;
            if (player == null) return;

            var victim = __instance.GetComponentInParent<Player>();
            if (victim != player) return;
            if (!CurseOwnership.Has(victim, HardEdges.Card)) return;

            force *= HardEdges.BounceMultiplier;
        }
    }
}
