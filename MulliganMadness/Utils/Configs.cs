using BepInEx.Configuration;

namespace MulliganMadness.Utils
{
    public class Configs
    {
        public ConfigEntry<bool> EnableTakeAll { get; }
        public ConfigEntry<bool> EnableAutoPickCurses { get; }
        public ConfigEntry<float> PanicTimerSeconds { get; }

        public Configs(ConfigFile config)
        {
            EnableTakeAll = config.Bind(
                "Take All",
                "Enabled",
                true,
                "When enabled, each player gets one Take All during card pick, usable once per game.");

            EnableAutoPickCurses = config.Bind(
                "Curses",
                "EnableAutoPickCurses",
                true,
                "Register the auto-pick curse set (mutually exclusive with each other).");

            PanicTimerSeconds = config.Bind(
                "Curses",
                "PanicTimerSeconds",
                3f,
                "How long Panic Pick waits before choosing for you.");
        }
    }
}
