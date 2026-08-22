using UnityEngine;

namespace MulliganMadness.Curses
{
    public class Fumble : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "fumble";

        protected override string GetTitle() => "Fumble";

        protected override string GetDescription() =>
            "50% chance the card you confirm is swapped for a neighbor in the offer.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Fumble",
                amount = "50%",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };
    }
}
