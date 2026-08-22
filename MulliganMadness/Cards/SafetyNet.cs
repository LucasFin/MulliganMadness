using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class SafetyNet : MMCard
    {
        public const string Title = "Safety Net";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "Map edges no longer deal damage (top, bottom, or sides). Soft-locks outside the map kill you after a few seconds.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;

        protected override GameObject GetCardArt() => CardArtFactory.Create("safetynet");

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Edge damage", "Negated")
        };
    }
}
