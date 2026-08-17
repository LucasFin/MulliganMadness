using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using UnboundLib.Cards;
using UnityEngine;

namespace MulliganMadness.Cards
{
    public abstract class MMCard : CustomCard
    {
        public const string TakebacksiesOnlyCategoryName = "MulliganMadness_TakebacksiesOnly";

        public static CardCategory TakebacksiesOnlyCategory =>
            CustomCardCategories.instance.CardCategory(TakebacksiesOnlyCategoryName);

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.allowMultiple = AllowMultiple;
        }

        protected virtual bool AllowMultiple => false;

        public override string GetModName() => Plugin.ModInitials;

        protected override CardThemeColor.CardThemeColorType GetTheme() =>
            CardThemeColor.CardThemeColorType.EvilPurple;
    }
}
