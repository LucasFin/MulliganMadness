using UnityEngine;

namespace MulliganMadness.Curses
{
    public class HardEdges : AutoPickCurse
    {
        internal static CardInfo Card;
        internal const float BounceMultiplier = 1.6f;

        protected override string GetArtName() => "hardedges";

        protected override string GetTitle() => "Hard Edges";

        protected override string GetDescription() =>
            "Map edges bounce you 60% harder.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = false,
                stat = "Edge bounce",
                amount = "+60%",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };
    }
}
