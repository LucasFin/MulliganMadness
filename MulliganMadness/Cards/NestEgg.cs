using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class NestEgg : MMCard
    {
        public const string Title = "Nest Egg";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "After 3 rounds, gain 1 curse-free Take All. If you already have a Take All left, this adds another. Extra copies each hatch another.";

        protected override CardInfo.Rarity GetRarity() => RarityHelper.Legendary;

        protected override GameObject GetCardArt() => CardArtFactory.Create("nestegg");

        protected override bool AllowMultiple => true;

        protected override CardThemeColor.CardThemeColorType GetTheme() =>
            CardThemeColor.CardThemeColorType.FirepowerYellow;

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Hatch", "3 rounds"),
            CardStatApply.Stat(true, "Take All", "Curse-free"),
            CardStatApply.Stat(true, "Per copy", "+1")
        };

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            NestEggManager.NotifyGained(player, EggKind.Nest);
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            NestEggManager.NotifyRemoved(player, EggKind.Nest);
        }
    }
}
