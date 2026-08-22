using BepInEx.Configuration;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public class Configs
    {
        // Session defaults (host; persisted locally, synced to lobby)
        public ConfigEntry<TakeAllMode> DefaultTakeAllMode { get; }
        public ConfigEntry<int> DefaultTakeAllUsesPerPlayer { get; }
        public ConfigEntry<float> DefaultVoteThreshold { get; }
        public ConfigEntry<float> DefaultVoteTimeoutSeconds { get; }
        public ConfigEntry<bool> DefaultVoteConsumesUse { get; }
        public ConfigEntry<bool> DefaultTakeAllCurseCost { get; }
        public ConfigEntry<TakeAllCurseOnExisting> DefaultCurseOnExisting { get; }
        public ConfigEntry<bool> DefaultEnableMercyVote { get; }
        public ConfigEntry<int> DefaultMercyRoundDeficit { get; }
        public ConfigEntry<bool> DefaultMercyOncePerGame { get; }

        public ConfigEntry<bool> EnableAutoPickCurses { get; }
        public ConfigEntry<float> PanicTimerSeconds { get; }
        public ConfigEntry<bool> FixPristineHealth { get; }
        public ConfigEntry<bool> EnableThiefCard { get; }
        public ConfigEntry<bool> EnableTakebacksies { get; }
        public ConfigEntry<bool> EnableSandbagSimulator { get; }
        public ConfigEntry<bool> EnableJarOfDirt { get; }
        public ConfigEntry<bool> SandbagOncePerGame { get; }

        // Client-local stats UI
        public ConfigEntry<bool> EnableStatsHud { get; }
        public ConfigEntry<bool> StatsHudVisible { get; }
        public ConfigEntry<bool> StatsHudCollapsed { get; }
        public ConfigEntry<bool> StatsHudSimpleMode { get; }
        public ConfigEntry<bool> StatsHudUltraCompact { get; }
        public ConfigEntry<bool> StatsHudPeekMode { get; }
        public ConfigEntry<KeyCode> StatsHudPeekKey { get; }
        public ConfigEntry<bool> HideStatsHudDuringPick { get; }
        public ConfigEntry<bool> HideStatsHudDuringBattle { get; }
        public ConfigEntry<KeyCode> StatsHudToggleKey { get; }
        public ConfigEntry<float> StatsHudOpacity { get; }
        public ConfigEntry<float> StatsHudFontScale { get; }
        public ConfigEntry<float> StatsHudColorR { get; }
        public ConfigEntry<float> StatsHudColorG { get; }
        public ConfigEntry<float> StatsHudColorB { get; }
        public ConfigEntry<bool> EnableStatsTab { get; }
        public ConfigEntry<bool> EnableTabCompare { get; }
        public ConfigEntry<float> TabPanelWidth { get; }
        public ConfigEntry<float> TabPanelHeightFraction { get; }
        public ConfigEntry<float> TabPanelOpacity { get; }
        public ConfigEntry<bool> TabAnchorLeft { get; }
        public ConfigEntry<float> TabPosX { get; }
        public ConfigEntry<float> TabPosY { get; }
        public ConfigEntry<bool> ShowPing { get; }
        public ConfigEntry<bool> ShowPickDeltasOnHud { get; }
        public ConfigEntry<float> StatsPanelScale { get; }
        public ConfigEntry<float> StatsHudOffsetX { get; }
        public ConfigEntry<float> StatsHudOffsetY { get; }
        public ConfigEntry<float> StatsAccentR { get; }
        public ConfigEntry<float> StatsAccentG { get; }
        public ConfigEntry<float> StatsAccentB { get; }

        public ConfigEntry<bool> DefaultAppearanceEnabled { get; }
        public ConfigEntry<int> DefaultColorIndex { get; }
        public ConfigEntry<int> DefaultEyeId { get; }
        public ConfigEntry<int> DefaultMouthId { get; }
        public ConfigEntry<int> DefaultDetailId { get; }
        public ConfigEntry<int> DefaultDetail2Id { get; }
        public ConfigEntry<float> DefaultEyeOffsetX { get; }
        public ConfigEntry<float> DefaultEyeOffsetY { get; }
        public ConfigEntry<float> DefaultMouthOffsetX { get; }
        public ConfigEntry<float> DefaultMouthOffsetY { get; }
        public ConfigEntry<float> DefaultDetailOffsetX { get; }
        public ConfigEntry<float> DefaultDetailOffsetY { get; }
        public ConfigEntry<float> DefaultDetail2OffsetX { get; }
        public ConfigEntry<float> DefaultDetail2OffsetY { get; }
        public ConfigEntry<bool> AutoCloseTabDuringPick { get; }

        public Configs(ConfigFile config)
        {
            DefaultTakeAllMode = config.Bind(
                "Session Defaults",
                "TakeAllMode",
                TakeAllMode.OncePerGame,
                "0=Disabled, 1=Once per game, 2=Multi-use, 3=Vote");

            DefaultTakeAllUsesPerPlayer = config.Bind(
                "Session Defaults",
                "TakeAllUsesPerPlayer",
                1,
                "Take All uses per player per game (0-3).");

            DefaultVoteThreshold = config.Bind(
                "Session Defaults",
                "VoteThreshold",
                0.5f,
                "Fraction of voters who must accept (excluding requester).");

            DefaultVoteTimeoutSeconds = config.Bind(
                "Session Defaults",
                "VoteTimeoutSeconds",
                15f,
                "Seconds before a Take All vote expires.");

            DefaultVoteConsumesUse = config.Bind(
                "Session Defaults",
                "VoteConsumesUse",
                true,
                "Whether a passed vote consumes a Take All use.");

            DefaultTakeAllCurseCost = config.Bind(
                "Session Defaults",
                "TakeAllCurseCost",
                false,
                "Take All grants a random Mulligan Madness auto-pick curse afterward.");

            DefaultCurseOnExisting = config.Bind(
                "Session Defaults",
                "CurseOnExisting",
                TakeAllCurseOnExisting.ReplaceExisting,
                "0=Replace existing MM curse, 1=Skip curse if player already has one.");

            DefaultEnableMercyVote = config.Bind(
                "Session Defaults",
                "EnableMercyVote",
                false,
                "Auto-offer a Take All vote when a player is down by MercyRoundDeficit round wins.");

            DefaultMercyRoundDeficit = config.Bind(
                "Session Defaults",
                "MercyRoundDeficit",
                2,
                "Round-win deficit vs the leader before mercy vote can trigger.");

            DefaultMercyOncePerGame = config.Bind(
                "Session Defaults",
                "MercyOncePerGame",
                true,
                "Limit mercy vote to once per player per game.");

            FixPristineHealth = config.Bind(
                "Session Defaults",
                "FixPristineHealth",
                true,
                "Stop Pristine Perseverance from collapsing HP when a later card reduces health.");

            EnableAutoPickCurses = config.Bind(
                "Session Defaults",
                "EnableAutoPickCurses",
                true,
                "Register the auto-pick curse set (mutually exclusive with each other).");

            PanicTimerSeconds = config.Bind(
                "Session Defaults",
                "PanicTimerSeconds",
                3f,
                "How long Panic Pick waits before choosing for you.");

            EnableThiefCard = config.Bind(
                "Session Defaults",
                "EnableThief",
                true,
                "Allow the Thief legendary card in this session.");

            EnableTakebacksies = config.Bind(
                "Session Defaults",
                "EnableTakebacksies",
                true,
                "Allow Takebacksies and inject it for stolen-from players.");

            EnableSandbagSimulator = config.Bind(
                "Session Defaults",
                "EnableSandbagSimulator",
                true,
                "Allow the Sandbag Simulator legendary card in this session.");

            EnableJarOfDirt = config.Bind(
                "Session Defaults",
                "EnableJarOfDirt",
                true,
                "Allow the Jar of Dirt unique card in this session.");

            SandbagOncePerGame = config.Bind(
                "Session Defaults",
                "SandbagOncePerGame",
                true,
                "Limit Sandbag Simulator to once per game per player.");

            // Legacy migration: old "Take All.Enabled" key
            var legacyTakeAll = config.Bind("Take All", "Enabled", true, "Deprecated - use Session Defaults.TakeAllMode.");
            if (!legacyTakeAll.Value && DefaultTakeAllMode.Value != TakeAllMode.Disabled)
            {
                DefaultTakeAllMode.Value = TakeAllMode.Disabled;
            }

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

            StatsHudCollapsed = config.Bind(
                "Stats HUD",
                "Collapsed",
                false,
                "Show the HUD as a minimal strip instead of the full panel.");

            StatsHudSimpleMode = config.Bind(
                "Stats HUD",
                "SimpleMode",
                true,
                "Show a compact stat list instead of every stat.");

            StatsHudUltraCompact = config.Bind(
                "Stats HUD",
                "UltraCompact",
                false,
                "Narrow HUD with fewer lines.");

            StatsHudPeekMode = config.Bind(
                "Stats HUD",
                "PeekMode",
                false,
                "Only show the HUD while holding the peek key.");

            StatsHudPeekKey = config.Bind(
                "Stats HUD",
                "PeekKey",
                KeyCode.LeftAlt,
                "Hold to show HUD when PeekMode is enabled.");

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
                0f,
                "Background opacity for the HUD panel (0 = text only).");
            if (Mathf.Abs(StatsHudOpacity.Value - 0.78f) < 0.001f)
            {
                StatsHudOpacity.Value = 0f;
            }

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

            EnableTabCompare = config.Bind(
                "Stats Tab",
                "EnableCompare",
                true,
                "Compare mode inside the Tab overlay (C toggles, [ ] switches opponent).");

            TabPanelWidth = config.Bind(
                "Stats Tab",
                "PanelWidth",
                480f,
                "Tab overlay width in pixels (before scale).");

            TabPanelHeightFraction = config.Bind(
                "Stats Tab",
                "PanelHeightFraction",
                0.82f,
                "Tab overlay height as a fraction of screen height.");

            TabPanelOpacity = config.Bind(
                "Stats Tab",
                "PanelOpacity",
                0.94f,
                "Tab overlay background opacity.");

            TabAnchorLeft = config.Bind(
                "Stats Tab",
                "AnchorLeft",
                false,
                "Deprecated - drag the Tab panel in-game.");

            TabPosX = config.Bind(
                "Stats Tab",
                "PosX",
                -1f,
                "Tab overlay X in canvas pixels. Negative = default centered.");

            TabPosY = config.Bind(
                "Stats Tab",
                "PosY",
                -1f,
                "Tab overlay Y in canvas pixels. Negative = default centered.");

            ShowPing = config.Bind(
                "Stats Tab",
                "ShowPing",
                true,
                "Show Photon ping next to player names in the Tab overlay and HUD (online games).");

            ShowPickDeltasOnHud = config.Bind(
                "Stats HUD",
                "ShowPickDeltas",
                true,
                "During picks, show card deltas on the bottom-left HUD for whoever is picking (hover a card to preview).");

            StatsPanelScale = config.Bind("Stats Layout", "PanelScale", 1f, "Global UI scale multiplier.");
            StatsHudOffsetX = config.Bind("Stats Layout", "HudOffsetX", 14f, "HUD distance from left edge.");
            StatsHudOffsetY = config.Bind("Stats Layout", "HudOffsetY", 14f, "HUD distance from bottom edge.");
            StatsAccentR = config.Bind("Stats Layout", "AccentR", 0.35f, "Accent color red.");
            StatsAccentG = config.Bind("Stats Layout", "AccentG", 0.82f, "Accent color green.");
            StatsAccentB = config.Bind("Stats Layout", "AccentB", 0.72f, "Accent color blue.");

            AutoCloseTabDuringPick = config.Bind(
                "Stats Tab",
                "AutoCloseDuringPick",
                false,
                "Close the Tab overlay when a card pick starts so cards stay clickable. Tab still toggles it open again.");

            DefaultAppearanceEnabled = config.Bind(
                "Default Appearance",
                "Enabled",
                false,
                "Apply your saved face and color at the start of each game (you can still change in character select).");

            DefaultColorIndex = config.Bind(
                "Default Appearance",
                "ColorIndex",
                0,
                "Player color palette index from the character-select color wheel.");

            DefaultEyeId = config.Bind("Default Appearance", "EyeId", 0, "Default eyes part ID.");
            DefaultMouthId = config.Bind("Default Appearance", "MouthId", 0, "Default mouth part ID.");
            DefaultDetailId = config.Bind("Default Appearance", "DetailId", 0, "Default detail part ID.");
            DefaultDetail2Id = config.Bind("Default Appearance", "Detail2Id", 0, "Default second detail part ID.");
            DefaultEyeOffsetX = config.Bind("Default Appearance", "EyeOffsetX", 0f, "Default eyes X offset.");
            DefaultEyeOffsetY = config.Bind("Default Appearance", "EyeOffsetY", 0f, "Default eyes Y offset.");
            DefaultMouthOffsetX = config.Bind("Default Appearance", "MouthOffsetX", 0f, "Default mouth X offset.");
            DefaultMouthOffsetY = config.Bind("Default Appearance", "MouthOffsetY", 0f, "Default mouth Y offset.");
            DefaultDetailOffsetX = config.Bind("Default Appearance", "DetailOffsetX", 0f, "Default detail X offset.");
            DefaultDetailOffsetY = config.Bind("Default Appearance", "DetailOffsetY", 0f, "Default detail Y offset.");
            DefaultDetail2OffsetX = config.Bind("Default Appearance", "Detail2OffsetX", 0f, "Default detail 2 X offset.");
            DefaultDetail2OffsetY = config.Bind("Default Appearance", "Detail2OffsetY", 0f, "Default detail 2 Y offset.");
        }
    }
}
