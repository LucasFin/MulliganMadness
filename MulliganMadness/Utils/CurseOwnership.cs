using System;
using MulliganMadness.Curses;
using MulliganMadness.Stats;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class CurseOwnership
    {
        internal const float FumbleChance = 0.5f;
        internal const float KickbackDamageMultiplier = 1.25f;
        internal const float KickbackForce = 620f;

        internal static bool Has(Player player, CardInfo curse)
        {
            if (player?.data?.currentCards == null || curse == null) return false;
            foreach (var card in player.data.currentCards)
            {
                if (card == null) continue;
                if (card == curse) return true;
                if (!string.IsNullOrEmpty(curse.cardName) &&
                    string.Equals(card.cardName, curse.cardName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool PickerHas(CardInfo curse)
        {
            return Has(TakeAllManager.GetCurrentPicker(), curse);
        }

        internal static bool LocalPickerHas(CardInfo curse)
        {
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || !Has(picker, curse)) return false;
            return PlayerStatsSnapshot.IsLocallyControlled(picker);
        }
    }
}
