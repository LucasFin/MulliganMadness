using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;

namespace MulliganMadness.UI
{
    public static class MenuBuilder
    {
        public static void Build(GameObject menu)
        {
            MenuHandler.CreateText("Mulligan Madness", menu, out _, 60);
            MenuHandler.CreateText("Take All is once per player per game.", menu, out _, 30);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableTakeAll.Value,
                "Enable Take All button",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableTakeAll.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableAutoPickCurses.Value,
                "Enable auto-pick curses",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableAutoPickCurses.Value = value),
                40);

            MenuHandler.CreateText(
                "Curses: Forced Choice, Panic Pick, Leftmost Luck — mutually exclusive.",
                menu,
                out _,
                25);
        }
    }
}
