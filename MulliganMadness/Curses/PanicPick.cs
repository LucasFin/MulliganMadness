using UnityEngine;

namespace MulliganMadness.Curses
{
    public class PanicPick : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "panicpick";

        protected override string GetTitle() => "Panic Pick";

        protected override string GetDescription() =>
            "You have a few seconds. Hesitate and the game grabs a random card for you.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Pick timer",
                amount = "Short",
                simepleAmount = CardInfoStat.SimpleAmount.slightlyLower
            }
        };
    }
}
