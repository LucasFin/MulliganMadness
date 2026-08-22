using UnityEngine;

namespace MulliganMadness.Curses
{
    public class BlindDraft : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "blinddraft";

        protected override string GetTitle() => "Blind Draft";

        protected override string GetDescription() =>
            "Your card offers are face-down. You still pick - you just can't see what you're picking.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Card faces",
                amount = "Hidden",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };
    }
}
