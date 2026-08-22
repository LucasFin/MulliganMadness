using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Utils;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Patches
{
    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    internal static class CombatEffectPatch
    {
        // Mark before damage/knockback so the first Bozo hit already gets +50%.
        private static void Prefix(HealthHandler __instance, Player damagingPlayer)
        {
            TryBozoMark(__instance, damagingPlayer);
        }

        private static void Postfix(HealthHandler __instance, Player damagingPlayer)
        {
            if (damagingPlayer == null) return;
            var victim = __instance.GetComponentInParent<Player>();
            if (victim == null || victim.playerID == damagingPlayer.playerID) return;
            if (!CurseOwnership.Has(damagingPlayer, TaserTaserTaser.Card)) return;
            var stun = victim.data?.stunHandler ?? victim.GetComponentInChildren<StunHandler>(true);
            if (stun == null) return;
            stun.AddStun(TaserTaserTaser.ExtraStunSeconds);
        }

        internal static void TryBozoMark(HealthHandler health, Player damagingPlayer)
        {
            if (damagingPlayer == null || health == null) return;
            var victim = health.GetComponentInParent<Player>();
            if (victim == null || victim.playerID == damagingPlayer.playerID) return;
            if (!CurseOwnership.Has(damagingPlayer, BozoShoes.Card)) return;

            BozoShoesRuntime.Mark(victim);
            if (damagingPlayer.data?.view != null && damagingPlayer.data.view.IsMine)
            {
                NetworkingManager.RPC(typeof(CombatEffectPatch), nameof(RPCA_BozoMark), victim.playerID);
            }
        }

        [UnboundRPC]
        public static void RPCA_BozoMark(int victimId)
        {
            BozoShoesRuntime.Mark(TakeAllManager.FindPlayer(victimId));
        }
    }

    // Backup path — some hits skip DoDamage or arrive via RPC damage first.
    [HarmonyPatch(typeof(HealthHandler), "CallTakeDamage", new[]
    {
        typeof(Vector2), typeof(Vector2), typeof(GameObject), typeof(Player), typeof(bool)
    })]
    internal static class BozoMarkCallTakeDamagePatch
    {
        private static void Prefix(HealthHandler __instance, Player damagingPlayer)
        {
            CombatEffectPatch.TryBozoMark(__instance, damagingPlayer);
        }
    }
}
