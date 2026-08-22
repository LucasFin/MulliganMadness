using System;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal static class CardStatPreview
    {
        private static readonly object Gate = new object();

        public static bool TryPreview(Player player, CardInfo cardInfo, out PlayerStatsSnapshot delta, CardInfo pickVisual = null)
        {
            delta = null;
            if (player == null || cardInfo == null) return false;
            if (!PlayerStatsSnapshot.TryFrom(player, out var before)) return false;

            lock (Gate)
            {
                var backup = PlayerStatRawBackup.Capture(player);
                var pick = pickVisual ?? cardInfo;
                ApplyPreview(player, cardInfo);
                if (!PlayerStatsSnapshot.TryFrom(player, out var after))
                {
                    backup.Apply(player);
                    return false;
                }

                NullStatReader.ApplyPickPreview(player, pick, after);
                delta = after.Delta(before);
                backup.Apply(player);
                return delta != null;
            }
        }

        private static void ApplyPreview(Player player, CardInfo cardInfo)
        {
            try
            {
                var toGun = player.data?.weaponHandler?.gun;
                var fromGun = cardInfo.GetComponent<Gun>() ?? cardInfo.GetComponentInChildren<Gun>(true);
                if (fromGun != null && toGun != null)
                {
                    ApplyCardStats.CopyGunStats(fromGun, toGun);
                    var fromAmmo = fromGun.GetComponent<GunAmmo>() ?? fromGun.GetComponentInChildren<GunAmmo>(true);
                    var toAmmo = toGun.GetComponent<GunAmmo>() ?? toGun.GetComponentInChildren<GunAmmo>(true);
                    if (fromAmmo != null && toAmmo != null)
                    {
                        toAmmo.maxAmmo += fromAmmo.maxAmmo;
                        toAmmo.reloadTime += fromAmmo.reloadTime;
                        toAmmo.reloadTimeAdd += fromAmmo.reloadTimeAdd;
                        toAmmo.reloadTimeMultiplier *= fromAmmo.reloadTimeMultiplier == 0f ? 1f : fromAmmo.reloadTimeMultiplier;
                    }
                }

                var fromBlock = cardInfo.GetComponent<Block>() ?? cardInfo.GetComponentInChildren<Block>(true);
                var toBlock = player.data?.block;
                if (fromBlock != null && toBlock != null)
                {
                    toBlock.additionalBlocks += fromBlock.additionalBlocks;
                    toBlock.cdAdd += fromBlock.cdAdd;
                    if (fromBlock.cdMultiplier != 0f) toBlock.cdMultiplier *= fromBlock.cdMultiplier;
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogWarn($"Card preview failed for {cardInfo?.cardName}: {ex.Message}");
            }
        }
    }
}
