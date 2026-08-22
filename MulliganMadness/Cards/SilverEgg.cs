using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class SilverEgg : MMCard
    {
        public const string Title = "Silver Egg";
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "After 2 rounds, gain 1 curse-free Take All of half the offer (rounded up). If you already have a Take All left, this adds another. Extra copies each hatch another.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;

        protected override GameObject GetCardArt() => CardArtFactory.Create("silveregg");

        protected override bool AllowMultiple => true;

        protected override CardThemeColor.CardThemeColorType GetTheme() =>
            CardThemeColor.CardThemeColorType.TechWhite;

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Hatch", "2 rounds"),
            CardStatApply.Stat(true, "Take All", "Half the hand"),
            CardStatApply.Stat(true, "Per copy", "+1")
        };

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            NestEggManager.NotifyGained(player, EggKind.Silver);
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            NestEggManager.NotifyRemoved(player, EggKind.Silver);
        }
    }
}
