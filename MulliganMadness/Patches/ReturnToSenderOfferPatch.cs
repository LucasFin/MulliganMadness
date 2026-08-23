using System;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Utils;
using CardsApi = ModdingUtils.Utils.Cards;

namespace MulliganMadness.Patches
{
    /// <summary>
    /// Return to Sender is only worth offering to someone who actually holds a curse to pass on.
    /// </summary>
    [HarmonyPatch]
    internal static class ReturnToSenderOfferPatch
    {
        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CardsApi), "PlayerIsAllowedCard", new[] { typeof(Player), typeof(CardInfo) })
                   ?? AccessTools.Method(typeof(CardsApi), "PlayerIsAllowedCard");
        }

        private static void Postfix(Player player, CardInfo card, ref bool __result)
        {
            try
            {
                if (!__result || player == null || card == null) return;
                if (!IsReturnToSender(card)) return;

                __result = ReturnToSenderManager.SenderHasCurse(OfferPlayer(player))
                           || ReturnToSenderManager.SenderHasCurse(player);
            }
            catch (Exception ex)
            {
                // Leave __result unchanged. A throw here aborts ReplaceCards and empties
                // the online offer entirely.
                Plugin.Instance?.LogWarn($"ReturnToSenderOfferPatch skipped: {ex.Message}");
            }
        }

        private static Player OfferPlayer(Player player)
        {
            if (CardChoice.instance != null && CardChoice.instance.IsPicking)
            {
                var picker = TakeAllManager.GetCurrentPicker();
                if (picker != null) return picker;
            }

            return player;
        }

        private static bool IsReturnToSender(CardInfo card)
        {
            if (ReturnToSender.Card != null && card == ReturnToSender.Card) return true;
            return string.Equals(card.cardName, ReturnToSender.Title, StringComparison.OrdinalIgnoreCase);
        }
    }
}
