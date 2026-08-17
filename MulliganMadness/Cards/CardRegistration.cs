using UnboundLib.Cards;
using MulliganMadness.Utils;

namespace MulliganMadness.Cards
{
    internal static class CardRegistration
    {
        internal static void RegisterAll()
        {
            if (Plugin.Configs != null && Plugin.Configs.EnableThiefCard.Value)
            {
                CustomCard.BuildCard<Thief>(info => Thief.Card = info);
            }

            if (Plugin.Configs != null && Plugin.Configs.EnableTakebacksies.Value)
            {
                CustomCard.BuildCard<Takebacksies>(info => Takebacksies.Card = info);
                TakebacksiesInjector.Register();
            }

            if (Plugin.Configs != null && Plugin.Configs.EnableSandbagSimulator.Value)
            {
                CustomCard.BuildCard<SandbagSimulator>(info => SandbagSimulator.Card = info);
            }
        }
    }
}
