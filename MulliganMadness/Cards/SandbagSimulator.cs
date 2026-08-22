using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class SandbagSimulator : MMCard
    {
        public const string Title = "Sandbag Simulator";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "Reroll any player's current pick hand — including your own. Host can limit uses per game.";

        protected override CardInfo.Rarity GetRarity() => RarityHelper.Legendary;

        protected override GameObject GetCardArt() => CardArtFactory.Create("sandbag");

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = true,
                stat = "Reroll",
                amount = "Any player",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            },
            new CardInfoStat
            {
                positive = false,
                stat = "Limit",
                amount = "Session setting",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };

        public override bool GetEnabled() => true;

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            SandbagManager.TryPromptSandbag(player);
        }
    }
}
