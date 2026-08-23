using UnboundLib.Cards;

namespace MulliganMadness.Cards
{
    internal static class CardRegistration
    {
        internal static void RegisterAll()
        {
            CustomCard.BuildCard<NestEgg>(info => NestEgg.Card = info);
            CustomCard.BuildCard<SilverEgg>(info => SilverEgg.Card = info);
            CustomCard.BuildCard<ReturnToSender>(info => ReturnToSender.Card = info);
        }
    }
}
