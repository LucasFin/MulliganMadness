using UnityEngine;

namespace MulliganMadness.Curses
{
    public class ShortHand : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "shorthand";

        protected override string GetTitle() => "Short Hand";

        protected override string GetDescription() =>
            "Your offers come with one fewer card. Less to choose from, every pick.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Common;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Cards offered",
                amount = "-1",
                simepleAmount = CardInfoStat.SimpleAmount.slightlyLower
            }
        };
    }
}
