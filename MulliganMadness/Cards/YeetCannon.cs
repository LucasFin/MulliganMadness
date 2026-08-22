using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public class YeetCannon : MMCard
    {
        public const string Title = "Yeet Cannon";
        internal const float SelfKickForce = 240f;
        internal static CardInfo Card;

        protected override string GetTitle() => Title;

        protected override string GetDescription() =>
            "+100% bullet knockback and +15% damage. Your shots kick you backward.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Rare;

        protected override GameObject GetCardArt() => CardArtFactory.Create("yeetcannon");

        protected override CardInfoStat[] GetStats() => new[]
        {
            CardStatApply.Stat(true, "Knockback", "+100%"),
            CardStatApply.Stat(true, "Damage", "+15%"),
            CardStatApply.Stat(false, "Weapon kick", "Self")
        };

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            base.SetupCard(cardInfo, gun, cardStats, statModifiers);
            gun.damage = 1.15f;
            gun.knockback = 2f;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health,
            Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            SelfKickOnFire.Ensure(player, SelfKickForce);
        }
    }
}
