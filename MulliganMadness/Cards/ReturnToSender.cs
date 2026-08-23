using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class ReturnToSender : MMCard
    {
        public const string Title = "Return to Sender";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "Give your curse to another player, they deserve it.";

        protected override CardInfo.Rarity GetRarity() => RarityHelper.Unique;

        protected override GameObject GetCardArt() => CardArtFactory.Create("returntosender");

        protected override CardInfoStat[] GetStats() => System.Array.Empty<CardInfoStat>();

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            ReturnToSenderManager.TryPrompt(player);
        }
    }
}
