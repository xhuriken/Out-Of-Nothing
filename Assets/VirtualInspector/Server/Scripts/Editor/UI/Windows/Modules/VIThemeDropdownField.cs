using System;
using System.Collections.Generic;
using Strix.VirtualInspector.Editor.UI.Theme;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIThemeDropdownField : VIDropdownField
    {
        static readonly List<string> ThemeLabels = new()
        {
            "Dark",
            "Light"
        };

        public event Action<VITheme> OnThemeChanged;

        public VIThemeDropdownField(string label = "Theme")
            : base(ThemeLabels, VIThemeManager.CurrentTheme.ToString(), null)
        {
            _onValueChanged = OnValueChanged;
        }

        void OnValueChanged(string newValue)
        {
            var theme = GetThemeFromLabel(newValue);

            // Update global state
            VIThemeManager.SetTheme(theme);

            // Notify external listeners
            OnThemeChanged?.Invoke(theme);
        }

        // --------------------------------------------------
        // Helpers
        // --------------------------------------------------

        static int GetIndexFromTheme(VITheme theme)
        {
            return theme switch
            {
                VITheme.Dark => 0,
                VITheme.Light => 1,
                _ => 0
            };
        }

        static VITheme GetThemeFromLabel(string label)
        {
            return label switch
            {
                "Light" => VITheme.Light,
                _ => VITheme.Dark
            };
        }
    }
}
