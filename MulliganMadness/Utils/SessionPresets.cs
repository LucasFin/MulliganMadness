namespace MulliganMadness.Utils
{
    internal static class SessionPresets
    {
        internal static SessionSettingsData Chaos()
        {
            return new SessionSettingsData
            {
                TakeAllMode = TakeAllMode.Vote,
                TakeAllUsesPerPlayer = 2,
                VoteThreshold = 0.5f,
                VoteTimeoutSeconds = 12f,
                VoteConsumesUse = false,
                TakeAllCurseCost = true,
                CurseOnExisting = TakeAllCurseOnExisting.ReplaceExisting,
                EnableMercyVote = true,
                MercyRoundDeficit = 2,
                MercyOncePerGame = true,
                FixPristineHealth = true,
                EnableAutoPickCurses = true,
                PanicTimerSeconds = 2f,
                EnableThiefCard = true,
                EnableTakebacksies = true,
                EnableSandbagSimulator = true,
                EnableJarOfDirt = true,
                SandbagOncePerGame = false
            };
        }

        internal static SessionSettingsData Competitive()
        {
            return new SessionSettingsData
            {
                TakeAllMode = TakeAllMode.Disabled,
                TakeAllUsesPerPlayer = 0,
                VoteThreshold = 0.5f,
                VoteTimeoutSeconds = 15f,
                VoteConsumesUse = true,
                TakeAllCurseCost = false,
                CurseOnExisting = TakeAllCurseOnExisting.SkipCurse,
                EnableMercyVote = false,
                MercyRoundDeficit = 3,
                MercyOncePerGame = true,
                FixPristineHealth = true,
                EnableAutoPickCurses = false,
                PanicTimerSeconds = 3f,
                EnableThiefCard = true,
                EnableTakebacksies = true,
                EnableSandbagSimulator = true,
                EnableJarOfDirt = true,
                SandbagOncePerGame = true
            };
        }
    }
}
