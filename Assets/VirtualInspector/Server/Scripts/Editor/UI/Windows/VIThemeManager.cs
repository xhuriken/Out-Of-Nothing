using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI.Theme
{
    public enum VITheme
    {
        Dark = 0,
        Light = 1
    }

    public static class VIThemeManager
    {
        const string RootClass = "vi-root";
        const string PrefKeyTheme = "VI.CurrentTheme";

        const string CoreStylePath =
            "Assets/VirtualInspector/Server/Scripts/Editor/UI/Styles/VIStyles.uss";

        const string DarkThemePath =
            "Assets/VirtualInspector/Server/Scripts/Editor/UI/Styles/VIDarkTheme.uss";

        const string LightThemePath =
            "Assets/VirtualInspector/Server/Scripts/Editor/UI/Styles/VILightTheme.uss";

        static StyleSheet _coreStyle;
        static StyleSheet _darkTheme;
        static StyleSheet _lightTheme;

        // ------------------------------------------------------
        // CURRENT THEME (GLOBAL STATE)
        // ------------------------------------------------------

        public static VITheme CurrentTheme { get; private set; }

        static VIThemeManager()
        {
            LoadThemeFromPrefs();
        }

        // ------------------------------------------------------
        // PUBLIC API
        // ------------------------------------------------------

        /// <summary>
        /// Set the current theme and save it.
        /// Does NOT automatically reapply it.
        /// </summary>
        public static void SetTheme(VITheme theme)
        {
            if (CurrentTheme == theme)
                return;

            CurrentTheme = theme;
            SaveThemeToPrefs();

            // Apply theme
            switch (CurrentTheme)
            {
                case VITheme.Dark:
                    VIThemeColors.ApplyDark();
                    break;

                case VITheme.Light:
                    VIThemeColors.ApplyLight();
                    break;
            }
        }

        /// <summary>
        /// Apply the current theme to a root VisualElement
        /// </summary>
        public static void ApplyTheme(VisualElement root)
        {
            ApplyTheme(root, CurrentTheme);
        }

        /// <summary>
        /// Apply a specific theme and make it current
        /// </summary>
        public static void ApplyTheme(VisualElement root, VITheme theme)
        {
            if (root == null)
            {
                Debug.LogWarning("[VIThemeManager] Root VisualElement is null");
                return;
            }

            SetTheme(theme);
            EnsureStylesLoaded();

            // Core (design system)
            AddStyleOnce(root, _coreStyle);

            // Root class
            if (!root.ClassListContains(RootClass))
                root.AddToClassList(RootClass);

            // Remove previous theme
            RemoveThemeStyles(root);

            // Apply theme
            switch (CurrentTheme)
            {
                case VITheme.Dark:
                    AddStyleOnce(root, _darkTheme);
                    VIThemeColors.ApplyDark();
                    break;

                case VITheme.Light:
                    AddStyleOnce(root, _lightTheme);
                    VIThemeColors.ApplyLight();
                    break;
            }
        }

        // ------------------------------------------------------
        // INTERNALS
        // ------------------------------------------------------

        static void LoadThemeFromPrefs()
        {
            int value = EditorPrefs.GetInt(PrefKeyTheme, (int)VITheme.Dark);
            CurrentTheme = (VITheme)Mathf.Clamp(value, 0, System.Enum.GetValues(typeof(VITheme)).Length - 1);
        }

        static void SaveThemeToPrefs()
        {
            EditorPrefs.SetInt(PrefKeyTheme, (int)CurrentTheme);
        }

        static void EnsureStylesLoaded()
        {
            if (_coreStyle == null)
                _coreStyle = Load(CoreStylePath);

            if (_darkTheme == null)
                _darkTheme = Load(DarkThemePath);

            if (_lightTheme == null)
                _lightTheme = Load(LightThemePath);
        }

        static StyleSheet Load(string path)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null)
                Debug.LogError($"[VIThemeManager] Failed to load stylesheet: {path}");
            return sheet;
        }

        static void AddStyleOnce(VisualElement root, StyleSheet sheet)
        {
            if (sheet == null)
                return;

            if (!root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);
        }

        static void RemoveThemeStyles(VisualElement root)
        {
            if (_darkTheme != null)
                root.styleSheets.Remove(_darkTheme);

            if (_lightTheme != null)
                root.styleSheets.Remove(_lightTheme);
        }
    }

    public static class VIThemeColors
    {
        // ---------- Context Menu ----------

        public static Color ContextBorder;
        public static Color ContextBackground;
        public static Color ContextItemText;
        public static Color ContextItemHover;
        public static Color ContextSeparator;

        // ---------- Apply Theme ----------

        public static void ApplyDark()
        {
            ContextBorder = new Color(1f, 1f, 1f, 0.18f);
            ContextBackground = new Color(0.10f, 0.12f, 0.17f);
            ContextItemText = new Color(0.88f, 0.90f, 0.92f);
            ContextItemHover = new Color(0.16f, 0.20f, 0.28f);
            ContextSeparator = new Color(1f, 1f, 1f, 0.15f);
        }

        public static void ApplyLight()
        {
            ContextBorder = new Color(1f, 1f, 1f, 0.08f);
            ContextBackground = new Color(0.19f, 0.19f, 0.19f);
            ContextItemText = new Color(0.90f, 0.90f, 0.90f);
            ContextItemHover = new Color(0.25f, 0.45f, 0.65f);
            ContextSeparator = new Color(1f, 1f, 1f, 0.10f);
        }
    }
}
