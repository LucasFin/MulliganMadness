using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Curses
{
    public class Kickback : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "kickback";

        protected override string GetTitle() => "Kickback";

        protected override string GetDescription() =>
            "Your gun hits 25% harder and strongly kicks you backward when you fire (aim down to hop).";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = true,
                stat = "Damage",
                amount = "+25%",
                simepleAmount = CardInfoStat.SimpleAmount.Some
            }
        };

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            base.SetupCard(cardInfo, gun, cardStats, statModifiers);
            gun.damage = CurseOwnership.KickbackDamageMultiplier;
            gun.recoil = 2.2f;
            gun.recoilMuiltiplier = 2.4f;
            gun.bodyRecoil = 3.5f;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            base.OnAddCard(player, gun, gunAmmo, data, health, gravity, block, characterStats);
            SelfKick.Ensure(player);
        }
    }
}
