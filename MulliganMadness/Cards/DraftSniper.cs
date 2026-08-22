using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class DraftSniper : MMCard
    {
        public const string Title = "Draft Sniper";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "During someone else's pick, click a card in their offer to replace it. Extra copies give extra snipes.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;

        protected override GameObject GetCardArt() => CardArtFactory.Create("draftsniper");

        protected override bool AllowMultiple => true;

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Snipe", "Click an offer"),
            CardStatApply.Stat(true, "Per copy", "1 snipe")
        };

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            DraftSniperManager.NotifyGained(player);
        }
    }
}
