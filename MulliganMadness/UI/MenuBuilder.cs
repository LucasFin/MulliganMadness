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
            MenuHandler.CreateText("Mulligan Madness", menu, out _, 60);
            MenuHandler.CreateText("Take All is once per player per game.", menu, out _, 30);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableTakeAll.Value,
                "Enable Take All button",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableTakeAll.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.FixPristineHealth.Value,
                "Fix Pristine Perseverance HP collapse",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.FixPristineHealth.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableAutoPickCurses.Value,
                "Enable auto-pick curses",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableAutoPickCurses.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableThiefCard.Value,
                "Enable Thief card",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableThiefCard.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableTakebacksies.Value,
                "Enable Takebacksies card",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableTakebacksies.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableSandbagSimulator.Value,
                "Enable Sandbag Simulator card",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableSandbagSimulator.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableJarOfDirt.Value,
                "Enable Jar of Dirt card",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableJarOfDirt.Value = value),
                40);

            MenuHandler.CreateText(
                "Curses: Forced Choice, Panic Pick, Leftmost Luck — mutually exclusive.",
                menu,
                out _,
                25);

            MenuHandler.CreateText("Stats (replaces Infoholic + TabInfo)", menu, out _, 40);

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
                "Enable compact compare panel",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableCompactCompare.Value = value),
                40);

            MenuHandler.CreateToggle(
                Plugin.Configs.EnableCardHoverPreview.Value,
                "Preview hovered card stat changes",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.EnableCardHoverPreview.Value = value),
                40);

            MenuHandler.CreateText("Layout (also in BepInEx config)", menu, out _, 28);

            MenuHandler.CreateToggle(
                Plugin.Configs.StatsHudSimpleMode.Value,
                "HUD simple mode",
                menu,
                (UnityAction<bool>)(value => Plugin.Configs.StatsHudSimpleMode.Value = value),
                35);

            Slider scaleSlider;
            MenuHandler.CreateSlider("Panel scale", menu, 35, 0.75f, 1.35f, Plugin.Configs.StatsPanelScale.Value,
                value => Plugin.Configs.StatsPanelScale.Value = value, out scaleSlider);

            Slider opacitySlider;
            MenuHandler.CreateSlider("HUD opacity", menu, 35, 0.35f, 1f, Plugin.Configs.StatsHudOpacity.Value,
                value => Plugin.Configs.StatsHudOpacity.Value = value, out opacitySlider);

            Slider fontSlider;
            MenuHandler.CreateSlider("HUD font scale", menu, 35, 0.8f, 1.4f, Plugin.Configs.StatsHudFontScale.Value,
                value => Plugin.Configs.StatsHudFontScale.Value = value, out fontSlider);
        }
    }
}
