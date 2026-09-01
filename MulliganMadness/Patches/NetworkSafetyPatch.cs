using HarmonyLib;
using MulliganMadness.Utils;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Null-guards for two vanilla networked-physics paths that throw once a PhotonView has
    /// been destroyed on the receiving client. Both were confirmed from a match log:
    /// 1,389 faults in NetworkPhysicsObject.OnCollisionEnter2D and six in
    /// ProjectileHit.RPCA_DoHit, every one of the latter landing seconds before a pick.
    ///
    /// Neither is our bug to own, but an exception thrown out of either one aborts the rest
    /// of the method, and in RPCA_DoHit that means the bounce is skipped *after* the
    /// incoming velocity has already been written. Guarding them removes the symptom while
    /// the Diag counters below identify what is destroying the views in the first place.
    /// </summary>
    [HarmonyPatch(typeof(NetworkPhysicsObject), "OnCollisionEnter2D")]
    internal static class NpoCollisionNullViewPatch
    {
        /// <summary>
        /// Vanilla guards `photonView` in Update() but not here, so an object that carries a
        /// NetworkPhysicsObject without a live view throws on every single contact.
        /// </summary>
        private static bool Prefix(NetworkPhysicsObject __instance, Collision2D collision)
        {
            if (__instance == null) return false;

            if (__instance.photonView == null)
            {
                Diag.Event("npo.collision.nullview", Diag.Describe(__instance.gameObject));
                return false;
            }

            // contacts[0] is read unconditionally by the original; an empty contact set is
            // rare but throws IndexOutOfRange rather than NRE, so it reads as a different bug.
            if (collision == null || collision.contactCount == 0)
            {
                Diag.Count("npo.collision.nocontacts");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// RPCA_DoHit resolves its target with PhotonNetwork.GetPhotonView(viewID) and never null
    /// checks it. When the sender's target has already been torn down locally — the log shows
    /// `Received RPC "RPCA_DoHit" for viewID N but this PhotonView does not exist` — the
    /// dereference throws.
    ///
    /// The throw lands *after* `move.velocity = vel` (the sender's pre-bounce vector) and
    /// *before* ProjectileHitSurface.HitSurface, which is the bounce handler. That is exactly
    /// "velocity applied, bounce lost". Rather than skip the method and lose the hit entirely,
    /// blank both target ids so vanilla takes its own no-target path and the rest of the
    /// method — including the bounce — still runs.
    /// </summary>
    [HarmonyPatch(typeof(ProjectileHit), nameof(ProjectileHit.RPCA_DoHit))]
    internal static class ProjectileDoHitDeadViewPatch
    {
        private static void Prefix(ref int viewID, ref int colliderID)
        {
            if (viewID == -1) return;

            PhotonView view = null;
            try
            {
                view = PhotonNetwork.GetPhotonView(viewID);
            }
            catch
            {
                view = null;
            }

            if (view != null) return;

            Diag.Event("projectile.dohit.deadview", "viewID=" + viewID + " colliderID=" + colliderID);

            // Blank both: falling through to the colliderID branch would index
            // GetComponentsInChildren<Collider2D>()[colliderID] against a map that may since
            // have changed, trading an NRE for an IndexOutOfRange.
            viewID = -1;
            colliderID = -1;
        }
    }
}
