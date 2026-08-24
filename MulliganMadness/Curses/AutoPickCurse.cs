using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using MulliganMadness.Utils;
using UnboundLib.Cards;
using UnityEngine;
using WillsWackyManagers.UnityTools;
using WillsWackyManagers.Utils;

namespace MulliganMadness.Curses
{
    public abstract class AutoPickCurse : CustomCard, ICurseCard
    {
        public const string ExclusiveCategoryName = "MulliganMadness_AutoPick";

        protected static CardCategory ExclusiveCategory =>
            CustomCardCategories.instance.CardCategory(ExclusiveCategoryName);

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            cardInfo.allowMultiple = false;
            cardInfo.categories = new[]
            {
                CurseManager.instance.curseCategory,
                ExclusiveCategory
            };
            cardInfo.blacklistedCategories = new[] { ExclusiveCategory };
            CardArtFactory.TryAssignSprite(cardInfo);
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            Plugin.Instance.Log($"Curse '{GetTitle()}' added to player {player.playerID}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
        }

        public override bool GetEnabled() => true;

        public static void RegisterAll()
        {
            CustomCard.BuildCard<ForcedChoice>(info =>
            {
                ForcedChoice.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<PanicPick>(info =>
            {
                PanicPick.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<LeftmostLuck>(info =>
            {
                LeftmostLuck.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<BlindDraft>(info =>
            {
                BlindDraft.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<ShortHand>(info =>
            {
                ShortHand.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<Fumble>(info =>
            {
                Fumble.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<Kickback>(info =>
            {
                Kickback.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
            CustomCard.BuildCard<HardEdges>(info =>
            {
                HardEdges.Card = info;
                CurseManager.instance.RegisterCurse(info);
            });
        }

        protected override GameObject GetCardArt()
        {
            var artName = GetArtName();
            return string.IsNullOrEmpty(artName) ? null : CardArtFactory.Create(artName);
        }

        protected abstract string GetArtName();

        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.EvilPurple;

        public override string GetModName() => Plugin.CardsMenuName;
    }
}
