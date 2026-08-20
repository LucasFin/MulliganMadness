using UnityEngine;

namespace MulliganMadness.UI
{
    internal static class HudToggleKeyOptions
    {
        private static readonly KeyCode[] Keys =
        {
            KeyCode.O,
            KeyCode.H,
            KeyCode.J,
            KeyCode.K,
            KeyCode.L,
            KeyCode.LeftAlt,
            KeyCode.RightAlt,
            KeyCode.F1,
            KeyCode.F2
        };

        private static readonly string[] Labels =
        {
            "O",
            "H",
            "J",
            "K",
            "L",
            "Left Alt",
            "Right Alt",
            "F1",
            "F2"
        };

        internal static int IndexOf(KeyCode key)
        {
            for (var i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == key) return i;
            }

            return 0;
        }

        internal static KeyCode KeyAt(int index)
        {
            if (index < 0 || index >= Keys.Length) return Keys[0];
            return Keys[index];
        }

        internal static string LabelAt(int index)
        {
            if (index < 0 || index >= Labels.Length) return Labels[0];
            return Labels[index];
        }

        internal static int MaxIndex => Keys.Length - 1;
    }
}
