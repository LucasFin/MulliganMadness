using UnboundLib.Cards;
using MulliganMadness.Utils;

namespace MulliganMadness.Cards
{
    internal static class CardRegistration
    {
        internal static void RegisterAll()
        {
            CustomCard.BuildCard<Thief>(info => Thief.Card = info);
            CustomCard.BuildCard<Takebacksies>(info => Takebacksies.Card = info);
            TakebacksiesInjector.Register();
            CustomCard.BuildCard<SandbagSimulator>(info => SandbagSimulator.Card = info);
            CustomCard.BuildCard<JarOfDirt>(info => JarOfDirt.Card = info);
        }
    }
}
