using System.Collections.Generic;
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
            TryTaserStun(__instance, damagingPlayer);
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

        internal static bool TryTaserStun(HealthHandler health, Player damagingPlayer)
        {
            if (damagingPlayer == null || health == null) return false;
            var victim = health.GetComponentInParent<Player>();
            if (victim == null || victim.playerID == damagingPlayer.playerID) return false;
            if (!CurseOwnership.Has(damagingPlayer, TaserTaserTaser.Card)) return false;
            if (!ApplyStun(victim)) return false;
            if (damagingPlayer.data?.view != null && damagingPlayer.data.view.IsMine)
            {
                NetworkingManager.RPC(typeof(CombatEffectPatch), nameof(RPCA_TaserStun), victim.playerID);
            }

            return true;
        }

        internal static bool ApplyStun(Player victim)
        {
            if (victim?.data == null) return false;
            if (TaserStunGate.WasRecent(victim.playerID)) return false;
            TaserStunGate.Mark(victim.playerID);

            victim.data.stunTime = Mathf.Max(victim.data.stunTime, TaserTaserTaser.ExtraStunSeconds);
            var stun = victim.data.stunHandler ?? victim.GetComponentInChildren<StunHandler>(true);
            if (stun == null) return true;
            stun.AddStun(TaserTaserTaser.ExtraStunSeconds);
            AccessTools.Method(typeof(StunHandler), "StartStun")?.Invoke(stun, null);
            return true;
        }

        [UnboundRPC]
        public static void RPCA_BozoMark(int victimId)
        {
            BozoShoesRuntime.Mark(TakeAllManager.FindPlayer(victimId));
        }

        [UnboundRPC]
        public static void RPCA_TaserStun(int victimId)
        {
            ApplyStun(TakeAllManager.FindPlayer(victimId));
        }
    }

    internal static class TaserStunGate
    {
        private static readonly Dictionary<int, float> Times = new Dictionary<int, float>();

        internal static bool WasRecent(int playerId) =>
            Times.TryGetValue(playerId, out var t) && Time.time - t < 0.2f;

        internal static void Mark(int playerId) => Times[playerId] = Time.time;
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

        private static void Postfix(HealthHandler __instance, Player damagingPlayer)
        {
            CombatEffectPatch.TryTaserStun(__instance, damagingPlayer);
        }
    }

    // Vanilla bullets stun via ProjectileHit.stun, which is networked with the shot.
    [HarmonyPatch(typeof(Gun), "ApplyProjectileStats")]
    internal static class TaserProjectilePatch
    {
        private static void Postfix(Gun __instance, GameObject __0)
        {
            if (__0 == null || __instance?.player == null) return;
            if (!CurseOwnership.Has(__instance.player, TaserTaserTaser.Card)) return;
            var hit = __0.GetComponent<ProjectileHit>() ?? __0.GetComponentInChildren<ProjectileHit>(true);
            if (hit == null) return;
            hit.stun += TaserTaserTaser.ExtraStunSeconds;
        }
    }

    [HarmonyPatch(typeof(ProjectileHit), "Hit")]
    internal static class BozoProjectileHitPatch
    {
        private static void Postfix(ProjectileHit __instance, HitInfo hit)
        {
            if (__instance?.ownPlayer == null || hit?.transform == null) return;
            var health = hit.transform.GetComponentInParent<HealthHandler>();
            CombatEffectPatch.TryBozoMark(health, __instance.ownPlayer);
        }
    }
}
