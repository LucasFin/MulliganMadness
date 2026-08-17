using System;
using HarmonyLib;
using UnboundLib.Cards;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal static class CardStatPreview
    {
        private static readonly object Gate = new object();

        public static bool TryPreview(Player player, CardInfo cardInfo, out PlayerStatsSnapshot delta)
        {
            delta = null;
            if (player == null || cardInfo == null) return false;
            if (!PlayerStatsSnapshot.TryFrom(player, out var before)) return false;

            lock (Gate)
            {
                var backup = PlayerStatRawBackup.Capture(player);
                if (!ApplyPreview(player, cardInfo))
                {
                    backup.Apply(player);
                    return false;
                }

                if (!PlayerStatsSnapshot.TryFrom(player, out var after))
                {
                    backup.Apply(player);
                    return false;
                }

                delta = after.Delta(before);
                backup.Apply(player);
                return delta != null;
            }
        }

        private static bool ApplyPreview(Player player, CardInfo cardInfo)
        {
            try
            {
                var gun = player.data.weaponHandler.gun;
                var block = player.data.block;
                var stats = player.data.stats;
                var apply = player.GetComponentInChildren<ApplyCardStats>(true);
                if (apply == null) return false;

                var custom = cardInfo.GetComponent<CustomCard>();
                if (custom != null)
                {
                    custom.SetupCard(cardInfo, gun, apply, stats);
                    return true;
                }

                var method = AccessTools.Method(typeof(ApplyCardStats), "ApplyStats", new[] { typeof(CardInfo) })
                             ?? AccessTools.Method(typeof(ApplyCardStats), "ApplyStats");
                if (method == null) return false;

                var args = method.GetParameters().Length switch
                {
                    1 => new object[] { cardInfo },
                    _ => new object[] { cardInfo, gun, player.data, stats, block }
                };
                method.Invoke(apply, args);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"Card preview failed for {cardInfo?.cardName}: {ex.Message}");
                return false;
            }
        }
    }
}
