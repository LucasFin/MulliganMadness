using BepInEx.Configuration;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public class Configs
    {
        public ConfigEntry<bool> EnableTakeAll { get; }
        public ConfigEntry<bool> EnableAutoPickCurses { get; }
        public ConfigEntry<float> PanicTimerSeconds { get; }
        public ConfigEntry<bool> FixPristineHealth { get; }
        public ConfigEntry<bool> EnableThiefCard { get; }
        public ConfigEntry<bool> EnableTakebacksies { get; }
        public ConfigEntry<bool> EnableSandbagSimulator { get; }
        public ConfigEntry<bool> EnableJarOfDirt { get; }
        public ConfigEntry<bool> SandbagOncePerGame { get; }

        public ConfigEntry<bool> EnableStatsHud { get; }
        public ConfigEntry<bool> StatsHudVisible { get; }
        public ConfigEntry<bool> StatsHudSimpleMode { get; }
        public ConfigEntry<bool> HideStatsHudDuringPick { get; }
        public ConfigEntry<bool> HideStatsHudDuringBattle { get; }
        public ConfigEntry<KeyCode> StatsHudToggleKey { get; }
        public ConfigEntry<float> StatsHudOpacity { get; }
        public ConfigEntry<float> StatsHudFontScale { get; }
        public ConfigEntry<float> StatsHudColorR { get; }
        public ConfigEntry<float> StatsHudColorG { get; }
        public ConfigEntry<float> StatsHudColorB { get; }
        public ConfigEntry<bool> EnableStatsTab { get; }
        public ConfigEntry<bool> EnableCompactCompare { get; }
        public ConfigEntry<int> CompactCompareMaxPlayers { get; }
        public ConfigEntry<bool> EnableCardHoverPreview { get; }
        public ConfigEntry<float> StatsPanelScale { get; }
        public ConfigEntry<float> StatsHudOffsetX { get; }
        public ConfigEntry<float> StatsHudOffsetY { get; }
        public ConfigEntry<float> CompareOffsetX { get; }
        public ConfigEntry<float> CompareOffsetY { get; }
        public ConfigEntry<float> StatsAccentR { get; }
        public ConfigEntry<float> StatsAccentG { get; }
        public ConfigEntry<float> StatsAccentB { get; }

        public Configs(ConfigFile config)
        {
            EnableTakeAll = config.Bind(
                "Take All",
                "Enabled",
                true,
                "When enabled, each player gets one Take All during card pick, usable once per game.");

            FixPristineHealth = config.Bind(
                "Fixes",
                "FixPristineHealth",
                true,
                "Stop Pristine Perseverance from collapsing HP when a later card reduces health.");

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

            EnableThiefCard = config.Bind(
                "Cards",
                "EnableThief",
                true,
                "Register the Thief legendary card.");

            EnableTakebacksies = config.Bind(
                "Cards",
                "EnableTakebacksies",
                true,
                "Register Takebacksies and inject it for stolen-from players.");

            EnableSandbagSimulator = config.Bind(
                "Cards",
                "EnableSandbagSimulator",
                true,
                "Register the Sandbag Simulator legendary card.");

            EnableJarOfDirt = config.Bind(
                "Cards",
                "EnableJarOfDirt",
                true,
                "Register the Jar of Dirt unique card (Nulls become treasures).");

            SandbagOncePerGame = config.Bind(
                "Cards",
                "SandbagOncePerGame",
                true,
                "Limit Sandbag Simulator to once per game per player.");

            EnableStatsHud = config.Bind(
                "Stats HUD",
                "Enabled",
                true,
                "Always-on stats panel (replaces Infoholic).");

            StatsHudVisible = config.Bind(
                "Stats HUD",
                "Visible",
                true,
                "Whether the HUD is currently shown (toggle in-game with StatsHudToggleKey).");

            StatsHudSimpleMode = config.Bind(
                "Stats HUD",
                "SimpleMode",
                true,
                "Show a compact stat list instead of every stat.");

            HideStatsHudDuringPick = config.Bind(
                "Stats HUD",
                "HideDuringPick",
                false,
                "Hide the always-on HUD during card pick.");

            HideStatsHudDuringBattle = config.Bind(
                "Stats HUD",
                "HideDuringBattle",
                false,
                "Hide the always-on HUD during battle rounds.");

            StatsHudToggleKey = config.Bind(
                "Stats HUD",
                "ToggleKey",
                KeyCode.O,
                "Toggle the always-on HUD visibility.");

            StatsHudOpacity = config.Bind(
                "Stats HUD",
                "Opacity",
                0.78f,
                "Background opacity for the HUD panel.");

            StatsHudFontScale = config.Bind(
                "Stats HUD",
                "FontScale",
                1f,
                "Text scale multiplier (auto-scales with resolution too).");

            StatsHudColorR = config.Bind("Stats HUD", "ColorR", 1f, "HUD text red channel.");
            StatsHudColorG = config.Bind("Stats HUD", "ColorG", 1f, "HUD text green channel.");
            StatsHudColorB = config.Bind("Stats HUD", "ColorB", 1f, "HUD text blue channel.");

            EnableStatsTab = config.Bind(
                "Stats Tab",
                "Enabled",
                true,
                "Tab overlay for all players (replaces TabInfo). Press Tab in-game.");

            EnableCompactCompare = config.Bind(
                "Stats Compare",
                "Enabled",
                true,
                "Compact multi-player compare panel with pin/reset baseline.");

            CompactCompareMaxPlayers = config.Bind(
                "Stats Compare",
                "MaxPlayers",
                4,
                "How many player columns to show (2-4).");

            EnableCardHoverPreview = config.Bind(
                "Stats Preview",
                "Enabled",
                true,
                "During your pick, show how hovered cards would change your stats.");

            StatsPanelScale = config.Bind("Stats Layout", "PanelScale", 1f, "Global UI scale multiplier.");
            StatsHudOffsetX = config.Bind("Stats Layout", "HudOffsetX", 14f, "HUD distance from left edge.");
            StatsHudOffsetY = config.Bind("Stats Layout", "HudOffsetY", 14f, "HUD distance from bottom edge.");
            CompareOffsetX = config.Bind("Stats Layout", "CompareOffsetX", -14f, "Compare panel distance from right edge.");
            CompareOffsetY = config.Bind("Stats Layout", "CompareOffsetY", -14f, "Compare panel distance from top edge.");
            StatsAccentR = config.Bind("Stats Layout", "AccentR", 0.35f, "Accent color red.");
            StatsAccentG = config.Bind("Stats Layout", "AccentG", 0.82f, "Accent color green.");
            StatsAccentB = config.Bind("Stats Layout", "AccentB", 0.72f, "Accent color blue.");
        }
    }
}
