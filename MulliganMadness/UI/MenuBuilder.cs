using MulliganMadness.Utils;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    public static class MenuBuilder
    {
        public static void Build(GameObject menu)
        {
            var canEdit = SessionSettings.CanEditSession;
            var hostNote = canEdit
                ? "Match rules apply to everyone in the lobby (host)."
                : "Match rules are set by the host.";

            MenuHandler.CreateText("Mulligan Madness", menu, out _, 60);
            MenuHandler.CreateText(hostNote, menu, out _, 28);
            MenuHandler.CreateText("Match rules (host)", menu, out _, 40);

            CreateSessionSlider(menu, "Take All mode (0=Off,1=Once,2=Multi,3=Vote)",
                0f, 3f, (int)SessionSettings.Current.TakeAllMode,
                value =>
                {
                    SessionSettings.Current.TakeAllMode = (TakeAllMode)Mathf.RoundToInt(value);
                    SaveSession();
                }, canEdit);

            CreateSessionSlider(menu, "Take All uses per player",
                0f, 3f, SessionSettings.Current.TakeAllUsesPerPlayer,
                value =>
                {
                    SessionSettings.Current.TakeAllUsesPerPlayer = Mathf.RoundToInt(value);
                    SaveSession();
                }, canEdit);

            CreateSessionSlider(menu, "Vote threshold (fraction yes)",
                0.25f, 1f, SessionSettings.Current.VoteThreshold,
                value =>
                {
                    SessionSettings.Current.VoteThreshold = value;
                    SaveSession();
                }, canEdit);

            CreateSessionSlider(menu, "Vote timeout (seconds)",
                5f, 60f, SessionSettings.Current.VoteTimeoutSeconds,
                value =>
                {
                    SessionSettings.Current.VoteTimeoutSeconds = value;
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Vote consumes Take All use",
                SessionSettings.Current.VoteConsumesUse,
                value =>
                {
                    SessionSettings.Current.VoteConsumesUse = value;
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Take All inflicts a curse",
                SessionSettings.Current.TakeAllCurseCost,
                value => SetTakeAllCurseCost(value, menu, canEdit),
                canEdit);

            CreateSessionSlider(menu, "Curse if already owned (0=Replace,1=Skip)",
                0f, 1f, (int)SessionSettings.Current.CurseOnExisting,
                value =>
                {
                    SessionSettings.Current.CurseOnExisting = (TakeAllCurseOnExisting)Mathf.RoundToInt(value);
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Enable auto-pick curses",
                SessionSettings.Current.EnableAutoPickCurses,
                value => SetAutoPickCurses(value, menu, canEdit),
                canEdit);

            CreateSessionSlider(menu, "Panic Pick timer (seconds)",
                1f, 10f, SessionSettings.Current.PanicTimerSeconds,
                value =>
                {
                    SessionSettings.Current.PanicTimerSeconds = value;
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Enable Thief card",
                SessionSettings.Current.EnableThiefCard,
                value => { SessionSettings.Current.EnableThiefCard = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Enable Takebacksies card",
                SessionSettings.Current.EnableTakebacksies,
                value => { SessionSettings.Current.EnableTakebacksies = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Enable Sandbag Simulator",
                SessionSettings.Current.EnableSandbagSimulator,
                value => { SessionSettings.Current.EnableSandbagSimulator = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Sandbag once per game",
                SessionSettings.Current.SandbagOncePerGame,
                value => { SessionSettings.Current.SandbagOncePerGame = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Enable Jar of Dirt",
                SessionSettings.Current.EnableJarOfDirt,
                value => { SessionSettings.Current.EnableJarOfDirt = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Fix Pristine Perseverance HP collapse",
                SessionSettings.Current.FixPristineHealth,
                value => { SessionSettings.Current.FixPristineHealth = value; SaveSession(); }, canEdit);

            CreateSessionToggle(menu, "Enable mercy Take All vote",
                SessionSettings.Current.EnableMercyVote,
                value =>
                {
                    SessionSettings.Current.EnableMercyVote = value;
                    SaveSession();
                },
                canEdit);

            CreateSessionSlider(menu, "Mercy round deficit vs leader",
                1f, 6f, SessionSettings.Current.MercyRoundDeficit,
                value =>
                {
                    SessionSettings.Current.MercyRoundDeficit = Mathf.RoundToInt(value);
                    SaveSession();
                },
                canEdit,
                wholeNumbers: true);

            CreateSessionToggle(menu, "Mercy vote once per player",
                SessionSettings.Current.MercyOncePerGame,
                value =>
                {
                    SessionSettings.Current.MercyOncePerGame = value;
                    SaveSession();
                },
                canEdit);

            if (canEdit)
            {
                MenuHandler.CreateText("Host presets", menu, out _, 32);
                MenuHandler.CreateButton("Apply Chaos preset", menu, () => ApplyPreset(menu, SessionPresets.Chaos()), 32, false);
                MenuHandler.CreateButton("Apply Competitive preset", menu, () => ApplyPreset(menu, SessionPresets.Competitive()), 32, false);
            }

            MenuHandler.CreateText(
                "Curses: Forced Choice, Panic Pick, Leftmost Luck — mutually exclusive.",
                menu,
                out _,
                25);

            MenuHandler.CreateText("Stats & UI (personal — not synced)", menu, out _, 40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableStatsHud.Value,
                "Enable always-on stats HUD",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableStatsHud.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableStatsTab.Value,
                "Enable Tab overlay (Tab key)",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableStatsTab.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableCompactCompare.Value,
                "Enable compare panel (shown with Tab overlay)",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableCompactCompare.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableCardHoverPreview.Value,
                "Preview hovered card stat changes",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableCardHoverPreview.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.StatsHudSimpleMode.Value,
                "HUD simple mode",
                menu,
                (UnityAction<bool>)(value =>
                {
                    Plugin.Configs.StatsHudSimpleMode.Value = value;
                    StatsController.Instance?.RebuildHud();
                }),
                35);

            MenuHandler.CreateToggle(
                Plugin.Configs.StatsHudUltraCompact.Value,
                "HUD ultra-compact width",
                menu,
                (UnityAction<bool>)(value =>
                {
                    Plugin.Configs.StatsHudUltraCompact.Value = value;
                    StatsController.Instance?.RebuildHud();
                }),
                35);

            MenuHandler.CreateToggle(
                Plugin.Configs.StatsHudPeekMode.Value,
                "HUD peek mode (hold Alt)",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.StatsHudPeekMode.Value = value),
                35);

            MenuHandler.CreateToggle(
                Plugin.Configs.HideStatsHudDuringPick.Value,
                "Hide HUD during card pick",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.HideStatsHudDuringPick.Value = value),
                35);

            MenuHandler.CreateToggle(
                Plugin.Configs.HideStatsHudDuringBattle.Value,
                "Hide HUD during battle",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.HideStatsHudDuringBattle.Value = value),
                35);

            Slider scaleSlider;
            MenuHandler.CreateSlider("Panel scale", menu, 35, 0.75f, 1.35f, Plugin.Configs.StatsPanelScale.Value,
                value =>
                {
                    Plugin.Configs.StatsPanelScale.Value = value;
                    StatsController.Instance?.RebuildHud();
                }, out scaleSlider);

            Slider opacitySlider;
            MenuHandler.CreateSlider("HUD opacity", menu, 35, 0.08f, 0.85f, Plugin.Configs.StatsHudOpacity.Value,
                value => Plugin.Configs.StatsHudOpacity.Value = value, out opacitySlider);

            Slider fontSlider;
            MenuHandler.CreateSlider("HUD font scale", menu, 35, 0.8f, 1.4f, Plugin.Configs.StatsHudFontScale.Value,
                value => Plugin.Configs.StatsHudFontScale.Value = value, out fontSlider);

            Slider offsetXSlider;
            MenuHandler.CreateSlider("HUD offset X", menu, 35, 0f, 120f, Plugin.Configs.StatsHudOffsetX.Value,
                value =>
                {
                    Plugin.Configs.StatsHudOffsetX.Value = value;
                    StatsController.Instance?.RebuildHud();
                }, out offsetXSlider);

            Slider offsetYSlider;
            MenuHandler.CreateSlider("HUD offset Y", menu, 35, 0f, 120f, Plugin.Configs.StatsHudOffsetY.Value,
                value =>
                {
                    Plugin.Configs.StatsHudOffsetY.Value = value;
                    StatsController.Instance?.RebuildHud();
                }, out offsetYSlider);

            Slider accentRSlider;
            MenuHandler.CreateSlider("Accent color R", menu, 30, 0f, 1f, Plugin.Configs.StatsAccentR.Value,
                value => Plugin.Configs.StatsAccentR.Value = value, out accentRSlider);

            Slider accentGSlider;
            MenuHandler.CreateSlider("Accent color G", menu, 30, 0f, 1f, Plugin.Configs.StatsAccentG.Value,
                value => Plugin.Configs.StatsAccentG.Value = value, out accentGSlider);

            Slider accentBSlider;
            MenuHandler.CreateSlider("Accent color B", menu, 30, 0f, 1f, Plugin.Configs.StatsAccentB.Value,
                value => Plugin.Configs.StatsAccentB.Value = value, out accentBSlider);

            Slider textRSlider;
            MenuHandler.CreateSlider("HUD text color R", menu, 30, 0f, 1f, Plugin.Configs.StatsHudColorR.Value,
                value => Plugin.Configs.StatsHudColorR.Value = value, out textRSlider);

            Slider textGSlider;
            MenuHandler.CreateSlider("HUD text color G", menu, 30, 0f, 1f, Plugin.Configs.StatsHudColorG.Value,
                value => Plugin.Configs.StatsHudColorG.Value = value, out textGSlider);

            Slider textBSlider;
            MenuHandler.CreateSlider("HUD text color B", menu, 30, 0f, 1f, Plugin.Configs.StatsHudColorB.Value,
                value => Plugin.Configs.StatsHudColorB.Value = value, out textBSlider);

            Slider toggleKeySlider;
            MenuHandler.CreateSlider(
                $"HUD toggle key ({HudToggleKeyOptions.LabelAt(HudToggleKeyOptions.IndexOf(Plugin.Configs.StatsHudToggleKey.Value))})",
                menu,
                30,
                0f,
                HudToggleKeyOptions.MaxIndex,
                HudToggleKeyOptions.IndexOf(Plugin.Configs.StatsHudToggleKey.Value),
                value =>
                {
                    Plugin.Configs.StatsHudToggleKey.Value = HudToggleKeyOptions.KeyAt(Mathf.RoundToInt(value));
                },
                out toggleKeySlider,
                wholeNumbers: true);

            MenuHandler.CreateButton("Reset HUD layout", menu, () =>
            {
                Plugin.Configs.StatsHudOffsetX.Value = 14f;
                Plugin.Configs.StatsHudOffsetY.Value = 14f;
                Plugin.Configs.StatsPanelScale.Value = 1f;
                Plugin.Configs.StatsHudCollapsed.Value = false;
                StatsController.Instance?.RebuildHud();
            }, 35, false);
        }

        private static void ApplyPreset(GameObject menu, SessionSettingsData preset)
        {
            if (!SessionSettings.CanEditSession || preset == null) return;
            NormalizeCurseSettings(null);
            SessionSettings.SetHostSession(preset, broadcast: true);
            ShowMenuNotice(menu, $"Applied preset: {SessionRulesSummary.BuildOneLine(preset)}");
        }

        private static void SaveSession()
        {
            NormalizeCurseSettings(null);
            SessionSettings.SetHostSession(SessionSettings.Current, broadcast: true);
        }

        private static void SetTakeAllCurseCost(bool value, GameObject menu, bool canEdit)
        {
            if (!canEdit) return;
            if (value && !SessionSettings.Current.EnableAutoPickCurses)
            {
                SessionSettings.Current.TakeAllCurseCost = false;
                ShowMenuNotice(menu, "Take All curse cost requires auto-pick curses enabled.");
                return;
            }

            SessionSettings.Current.TakeAllCurseCost = value;
            SaveSession();
        }

        private static void SetAutoPickCurses(bool value, GameObject menu, bool canEdit)
        {
            if (!canEdit) return;
            SessionSettings.Current.EnableAutoPickCurses = value;
            if (!value && SessionSettings.Current.TakeAllCurseCost)
            {
                SessionSettings.Current.TakeAllCurseCost = false;
                ShowMenuNotice(menu, "Disabled Take All curse cost because curses are off.");
            }

            SaveSession();
        }

        private static void NormalizeCurseSettings(GameObject menu)
        {
            if (!SessionSettings.Current.TakeAllCurseCost || SessionSettings.Current.EnableAutoPickCurses) return;
            SessionSettings.Current.TakeAllCurseCost = false;
            if (menu != null) ShowMenuNotice(menu, "Take All curse cost requires auto-pick curses enabled.");
        }

        private static void ShowMenuNotice(GameObject menu, string message)
        {
            MenuHandler.CreateText(message, menu, out _, 24, false);
        }

        private static void CreateSessionToggle(GameObject menu, string label, bool value, UnityAction<bool> onChange, bool enabled)
        {
            MenuHandler.CreateToggle(value, label, menu, onChange, 40);
            if (!enabled) DisableLastToggle(menu);
        }

        private static void CreateSessionSlider(GameObject menu, string label, float min, float max, float value, UnityAction<float> onChange, bool enabled, bool wholeNumbers = false)
        {
            MenuHandler.CreateSlider(label, menu, 35, min, max, value, onChange, out var slider, wholeNumbers);
            if (!enabled && slider != null) slider.interactable = false;
        }

        private static void DisableLastToggle(GameObject menu)
        {
            var toggles = menu.GetComponentsInChildren<Toggle>(true);
            if (toggles == null || toggles.Length == 0) return;
            toggles[toggles.Length - 1].interactable = false;
        }
    }
}
