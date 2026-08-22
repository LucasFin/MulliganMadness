using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Patches
{
    [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
    internal static class MapEdgeOobFlagPatch
    {
        internal static Player Current;

        private static void Prefix(OutOfBoundsHandler __instance)
        {
            Current = null;
            var tr = Traverse.Create(__instance);
            if (!IsOobFlag(tr.Field("outOfBounds").GetValue()) && !IsOobFlag(tr.Field("almostOutOfBounds").GetValue()))
            {
                return;
            }
            var data = tr.Field("data").GetValue<CharacterData>();
            Current = data != null ? data.player : null;
        }

        private static bool IsOobFlag(object value)
        {
            if (value is bool b) return b;
            return false;
        }

        private static void Postfix() => Current = null;
    }

    [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.CallTakeDamage))]
    internal static class MapEdgeDamagePatch
    {
        private static bool Prefix(HealthHandler __instance)
        {
            var player = MapEdgeOobFlagPatch.Current;
            if (player == null) return true;
            var victim = __instance.GetComponentInParent<Player>() ?? player;
            if (victim != player) return true;
            return !CurseOwnership.Has(player, SafetyNet.Card);
        }
    }

    [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
    internal static class MapEdgeDoDamagePatch
    {
        private static bool Prefix(HealthHandler __instance)
        {
            var player = MapEdgeOobFlagPatch.Current;
            if (player == null) return true;
            var victim = __instance.GetComponentInParent<Player>() ?? player;
            if (victim != player) return true;
            return !CurseOwnership.Has(player, SafetyNet.Card);
        }
    }
    [HarmonyPatch(typeof(HealthHandler), nameof(HealthHandler.CallTakeForce))]
    internal static class KnockbackForcePatch
    {
        private static void Prefix(HealthHandler __instance, ref Vector2 force, bool forceIgnoreMass)
        {
            var victim = __instance.GetComponentInParent<Player>();
            if (victim == null) return;

            if (MapEdgeOobFlagPatch.Current != null
                && MapEdgeOobFlagPatch.Current == victim
                && CurseOwnership.Has(victim, HardEdges.Card))
            {
                force *= HardEdges.BounceMultiplier;
            }

            if (!BozoShoesRuntime.IsMarked(victim)) return;
            // Self-kicks use forceIgnoreMass. Leave those alone unless this is a map-edge bounce.
            if (forceIgnoreMass && MapEdgeOobFlagPatch.Current != victim) return;
            force *= BozoShoes.KnockbackMultiplier;
        }
    }
}
