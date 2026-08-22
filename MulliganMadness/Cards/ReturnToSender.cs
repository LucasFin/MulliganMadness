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
            "Give your Mulligan Madness curse to a player of your choosing. If they already have one, they keep theirs and still get yours. Only offered if you have a curse.";

        protected override CardInfo.Rarity GetRarity() => RarityHelper.Unique;

        protected override GameObject GetCardArt() => CardArtFactory.Create("returntosender");

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Your curse", "Moved"),
            CardStatApply.Stat(false, "Target", "Keeps existing curse")
        };

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            ReturnToSenderManager.TryPrompt(player);
        }
    }
}
