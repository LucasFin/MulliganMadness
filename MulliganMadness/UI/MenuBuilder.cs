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

            CreateSessionSlider(menu, "Take All (0=Off, 1=Once, 2=Multi, 3=Vote)",
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

            CreateSessionSlider(menu, "Vote timeout (seconds)",
                5f, 60f, SessionSettings.Current.VoteTimeoutSeconds,
                value =>
                {
                    SessionSettings.Current.VoteTimeoutSeconds = value;
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Take All inflicts a curse",
                SessionSettings.Current.TakeAllCurseCost,
                value =>
                {
                    SessionSettings.Current.TakeAllCurseCost = value;
                    SaveSession();
                }, canEdit);

            CreateSessionToggle(menu, "Mercy vote when far behind",
                SessionSettings.Current.EnableMercyVote,
                value =>
                {
                    SessionSettings.Current.EnableMercyVote = value;
                    SaveSession();
                },
                canEdit);

            CreateSessionSlider(menu, "Panic Pick timer (seconds)",
                1f, 10f, SessionSettings.Current.PanicTimerSeconds,
                value =>
                {
                    SessionSettings.Current.PanicTimerSeconds = value;
                    SaveSession();
                }, canEdit);

            if (canEdit)
            {
                MenuHandler.CreateButton("Chaos preset", menu, () => ApplyPreset(menu, SessionPresets.Chaos()), 32, false);
                MenuHandler.CreateButton("Competitive preset", menu, () => ApplyPreset(menu, SessionPresets.Competitive()), 32, false);
            }

            MenuHandler.CreateText(
                "Enable or disable MM cards and curses in Toggle Cards — not here.",
                menu,
                out _,
                24);

            MenuHandler.CreateText("Default look", menu, out _, 36);

            MenuHandler.CreateToggle(
                Plugin.Configs.DefaultAppearanceEnabled.Value,
                "Apply saved face & color each game",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.DefaultAppearanceEnabled.Value = value),
                35);

            MenuHandler.CreateButton("Save current face & color", menu, () =>
            {
                if (DefaultAppearance.TryCaptureFromLocal(out var notice))
                {
                    ShowMenuNotice(menu, notice);
                }
                else
                {
                    ShowMenuNotice(menu, notice ?? "Could not save appearance.");
                }
            }, 32, false);

            MenuHandler.CreateButton("Apply saved look now", menu, () =>
            {
                DefaultAppearance.TryApply(force: true);
                ShowMenuNotice(menu, "Applied saved face & color to local player.");
            }, 32, false);

            MenuHandler.CreateText(
                "In game: Tab opens stats (drag the top bar to move, left edge to resize). O hides the HUD.",
                menu,
                out _,
                24);
        }

        private static void ApplyPreset(GameObject menu, SessionSettingsData preset)
        {
            if (!SessionSettings.CanEditSession || preset == null) return;
            SessionSettings.SetHostSession(preset, broadcast: true);
            ShowMenuNotice(menu, $"Applied preset: {SessionRulesSummary.BuildOneLine(preset)}");
        }

        private static void SaveSession()
        {
            SessionSettings.SetHostSession(SessionSettings.Current, broadcast: true);
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
