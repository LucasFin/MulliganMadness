using UnboundLib.Cards;

namespace MulliganMadness.Cards
{
    internal static class CardRegistration
    {
        internal static void RegisterAll()
        {
            CustomCard.BuildCard<ReturnToSender>(info => ReturnToSender.Card = info);
        }
    }
}
