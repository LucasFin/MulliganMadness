using CardChoiceSpawnUniqueCardPatch.CustomCategories;
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
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            Plugin.Instance.Log($"Curse '{GetTitle()}' added to player {player.playerID}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
        }

        protected override GameObject GetCardArt() => null;

        protected override CardThemeColor.CardThemeColorType GetTheme() => CardThemeColor.CardThemeColorType.EvilPurple;

        public override string GetModName() => Plugin.CurseInitials;

        public override bool GetEnabled() => Plugin.Configs == null || Plugin.Configs.EnableAutoPickCurses.Value;

        public static void RegisterAll()
        {
            if (Plugin.Configs != null && !Plugin.Configs.EnableAutoPickCurses.Value)
            {
                Plugin.Instance.Log("Auto-pick curses disabled in config; skipping registration.");
                return;
            }

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
        }
    }
}
