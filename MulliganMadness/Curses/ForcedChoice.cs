using UnboundLib.Cards;
using UnityEngine;

namespace MulliganMadness.Curses
{
    public class ForcedChoice : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "forcedchoice";

        protected override string GetTitle() => "Forced Choice";

        protected override string GetDescription() =>
            "The house picks for you. When it's your turn to draft, a random offered card is taken immediately.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Common;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Card pick",
                amount = "Random",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };
    }
}
