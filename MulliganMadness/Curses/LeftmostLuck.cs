using UnityEngine;

namespace MulliganMadness.Curses
{
    public class LeftmostLuck : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "leftmostluck";

        protected override string GetTitle() => "Leftmost Luck";

        protected override string GetDescription() =>
            "Strategy is overrated. You always take the leftmost card in the hand.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Common;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Card pick",
                amount = "Leftmost",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };
    }
}
