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
        }

        private static void SaveSession()
        {
            SessionSettings.SetHostSession(SessionSettings.Current, broadcast: true);
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
