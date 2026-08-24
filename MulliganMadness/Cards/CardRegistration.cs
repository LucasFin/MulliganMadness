using System;
using MulliganMadness.Utils;
using UnboundLib.Cards;

namespace MulliganMadness.Cards
{
    internal static class CardRegistration
    {
        internal static void RegisterAll()
        {
            Bind<NestEgg>(info => NestEgg.Card = info);
            Bind<ReturnToSender>(info => ReturnToSender.Card = info);
        }

        private static void Bind<T>(Action<CardInfo> setStatic) where T : CustomCard
        {
            CustomCard.BuildCard<T>(info =>
            {
                setStatic(info);
                // Unbound sets cardArt and cardName after SetupCard. The callback
                // is the first moment both exist on the prefab.
                CardArtFactory.TryAssignSprite(info);
            });
        }
    }
}
