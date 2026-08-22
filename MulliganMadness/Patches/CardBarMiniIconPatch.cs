using System.Reflection;
using HarmonyLib;
using ModdingUtils.Utils;
using MulliganMadness.Utils;
using UnityEngine;
using CardsApi = ModdingUtils.Utils.Cards;

namespace MulliganMadness.Patches
{
    [HarmonyPatch(typeof(CardBar), "AddCard")]
    [HarmonyPriority(Priority.Last)]
    internal static class CardBarMiniIconPatch
    {
        private static void Postfix(CardBar __instance)
        {
            CardBarMiniIcons.ApplyToLatestButton(__instance);
        }
    }

    [HarmonyPatch(typeof(CardInfo), "Awake")]
    internal static class CardInfoMiniSpritePatch
    {
        private static void Postfix(CardInfo __instance)
        {
            CardArtFactory.TryAssignSprite(__instance);
        }
    }

    [HarmonyPatch(typeof(CardBarUtils))]
    internal static class CardBarUtilsMiniIconPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(nameof(CardBarUtils.SilentAddToCardBar), typeof(int), typeof(CardInfo), typeof(string))]
        private static void AfterSilentId(int playerID)
        {
            ApplyPlayer(playerID);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(nameof(CardBarUtils.SilentAddToCardBar), typeof(Player), typeof(CardInfo), typeof(string))]
        private static void AfterSilentPlayer(Player player)
        {
            if (player != null) ApplyPlayer(player.playerID);
        }

        private static void ApplyPlayer(int playerID)
        {
            try
            {
                CardBarMiniIcons.ApplyToLatestButton(CardBarUtils.instance.PlayersCardBar(playerID));
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch]
    internal static class CardsSilentAddMiniIconPatch
    {
        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(CardsApi),
                "SilentAddToCardBar",
                new[] { typeof(int), typeof(CardInfo), typeof(string), typeof(float), typeof(float) });
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(int playerID)
        {
            try
            {
                CardBarMiniIcons.ApplyToLatestButton(CardBarUtils.instance.PlayersCardBar(playerID));
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch]
    internal static class TabInfoMiniIconPatch
    {
        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("TabInfo.Utils.PlayerCardButton");
            return type == null ? null : AccessTools.Method(type, "Start");
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(object __instance)
        {
            if (!(__instance is MonoBehaviour mb)) return;
            var field = AccessTools.Field(__instance.GetType(), "card");
            var card = field?.GetValue(__instance) as CardInfo;
            CardBarMiniIcons.Apply(mb.gameObject, card);
        }
    }
}
