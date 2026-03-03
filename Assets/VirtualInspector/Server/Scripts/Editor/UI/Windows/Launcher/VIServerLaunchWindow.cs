using Strix.VirtualInspector.Editor.UI.Theme;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    [InitializeOnLoad]
    public class VIServerLauncherWindow : EditorWindow
    {
        private const string SendFeedbackUrl =
    "https://strixcorporation.com/#contact";


        private const string ProUpgradeUrl =
    "https://strixcorporation.com/#pricing";

        private const string VERSION = "1.0.0";
     
        private const string PREF_STARTUP_MODE = "VI_Launcher_Mode";
        private const string PREF_LAST_VERSION = "VI_LastVersion";

        private enum StartupMode { Always, Never, OnlyWhenNewVersion }

        static VIServerLauncherWindow()
        {
            EditorApplication.delayCall += TryShowOnStartup;
        }

        [MenuItem("Virtual Inspector/Launcher")]
        public static void Open()
        {
            OpenWindow(false);
        }

        private static void TryShowOnStartup()
        {
            StartupMode mode = (StartupMode)EditorPrefs.GetInt(PREF_STARTUP_MODE, (int)StartupMode.Always);

            bool shouldOpen =
                mode == StartupMode.Always ||
                (mode == StartupMode.OnlyWhenNewVersion &&
                    EditorPrefs.GetString(PREF_LAST_VERSION, "") != VERSION);

            if (shouldOpen)
                OpenWindow(true);
        }

        public static void OpenWindow(bool auto)
        {
            var window = GetWindow<VIServerLauncherWindow>();
            window.titleContent = new GUIContent("Virtual Inspector Launcher");
            window.minSize = new Vector2(850, 520);
            window.Show();

            if (auto)
                EditorPrefs.SetString(PREF_LAST_VERSION, VERSION);
        }


        void CreateGUI()
        {
            var root = rootVisualElement;

            VIThemeManager.ApplyTheme(rootVisualElement);



            root.Clear();

            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            // ==================================================
            // HEADER
            // ==================================================
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 20;
            header.style.paddingRight = 20;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;

            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.15f, 0.15f, 0.15f);

            var logo = new VisualElement();
            logo.style.width = 32;
            logo.style.height = 32;
            logo.style.backgroundImage =
                EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image as Texture2D;
            logo.style.marginRight = 10;

            var title = new Label("Virtual Inspector");
            title.AddToClassList("vi-title");

            header.Add(logo);
            header.Add(title);
            root.Add(header);

            // ==================================================
            // MIDDLE — SPLIT VIEW
            // ==================================================
            var split = new TwoPaneSplitView(0, 220, TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1;
            root.Add(split);

            // ---------------- LEFT : ACTIONS ----------------
            var leftPanel = new VisualElement();
            leftPanel.style.flexDirection = FlexDirection.Column;

            leftPanel.style.paddingLeft = 16;
            leftPanel.style.paddingRight = 16;
            leftPanel.style.paddingTop = 16;
            leftPanel.style.paddingBottom = 16;

            //leftPanel.style.gap = 8;

            leftPanel.Add(new VIButtonField(
                "Open Virtual Inspector",
                () => VIServerEditorWindow.Open()
            ));

            leftPanel.Add(new VIButtonField(
                "Documentation",
                () => Application.OpenURL("https://strix-corporation.gitbook.io/virtual-inspector-docs/")
            ));

            //leftPanel.Add(new VIButtonField(
            //    "Discord",
            //    () => Application.OpenURL("https://discord.gg/XXXXX")
            //));

            var feedbackButton = new VIButtonField(
          "Send Feedback",
          () => Application.OpenURL(SendFeedbackUrl)
      );
            feedbackButton.tooltip = "Send us your feedback, suggestions or issues to help improve Virtual Inspector";
            leftPanel.Add(feedbackButton);

            var upgradeButton = new VIButtonField(
    "Upgrade to Pro",
    () => Application.OpenURL(ProUpgradeUrl),
    "vi-button-field-ProUpgrade",
    "vi-toolbar-button-ProUpgrade"
);
            upgradeButton.tooltip = "Unlock full runtime editing and advanced workflows";

            leftPanel.Add(upgradeButton);


            leftPanel.Add(new VIButtonField(
                "Website",
                () => Application.OpenURL("https://strixcorporation.com/")
            ));

            split.Add(leftPanel);

            // ---------------- RIGHT : CONTENT ----------------
            var rightPanel = new VisualElement();
            rightPanel.style.flexGrow = 1;
            rightPanel.style.paddingLeft = 16;
            rightPanel.style.paddingRight = 16;
            rightPanel.style.paddingTop = 16;
            rightPanel.style.paddingBottom = 16;

            // Presentation
            var presentationTitle = new Label("Overview");
            presentationTitle.AddToClassList("vi-section-title");

            var presentationText = new Label(
                 "Virtual Inspector (Free) allows you to inspect and monitor your Unity runtime " +
                 "in real time.\n\n" +
                 "This edition is designed for indies, prototyping and evaluation workflows, \n" +
                 "with a read-only inspection experience across runtime builds.\n\n" +
                 "Upgrade to the Pro edition to unlock runtime editing, hierarchy actions, component actions, and more."
             );
            presentationText.AddToClassList("vi-paragraph");

            rightPanel.Add(presentationTitle);
            rightPanel.Add(presentationText);

            AddSpacer(rightPanel, 16);

            

            VisualElement changelogParent = new VisualElement();



            changelogParent.style.height = 220;
            changelogParent.AddToClassList("hierarchy");

            changelogParent.style.flexGrow = 1;
            changelogParent.style.flexDirection = FlexDirection.Column;

            ScrollView changelog = new ScrollView();
            changelogParent.Add(changelog);
            // Changelog
            var changelogTitle = new Label("Changelog");
            changelogTitle.AddToClassList("vi-section-title");
            rightPanel.Add(changelogTitle);
            changelog.Clear();

            changelog.Add(new Label("Free Edition — Feature Overview"));
            changelog.Add(new Label(""));

            changelog.Add(new Label("• Live runtime hierarchy"));
            changelog.Add(new Label("  Browse scenes and GameObjects from any runtime build"));
            changelog.Add(new Label(""));

            changelog.Add(new Label("• Object & component inspection"));
            changelog.Add(new Label("  Read-only inspection of GameObjects and Components"));
            changelog.Add(new Label(""));

            changelog.Add(new Label("• Runtime variable monitoring"));
            changelog.Add(new Label("  Observe live values without modifying runtime state"));
            changelog.Add(new Label(""));

            changelog.Add(new Label("  Works with development or production builds (read-only)"));
            changelog.Add(new Label(""));

            changelog.Add(new Label("• LAN connection only"));
            changelog.Add(new Label("  Editor and runtime must be on the same local network"));
            changelog.Add(new Label(""));

            rightPanel.Add(changelogParent);


            split.Add(rightPanel);

            // ==================================================
            // FOOTER
            // ==================================================
            var footer = new VisualElement();
            footer.style.height = 32;
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            footer.style.paddingLeft = 12;
            footer.style.paddingRight = 12;

            // LEFT
            var footerLabel = new Label("© Strix Corporation");
            footerLabel.AddToClassList("vi-footer");
            footerLabel.style.flexShrink = 0;
            footer.Add(footerLabel);

            // RIGHT
            List<string> array = new()
            {
                "Always",
                "Never",
                "Only When New Version"
            };

            var dropdown = new VIDropdownField(
                array,
                array[EditorPrefs.GetInt(PREF_STARTUP_MODE, (int)StartupMode.Always)],
                newValue =>
                {
                    EditorPrefs.SetInt(PREF_STARTUP_MODE, array.IndexOf(newValue));
                }
            );

            dropdown.style.flexShrink = 0;
            footer.Add(dropdown);




            root.Add(footer);
        }

        // --------------------------------------------------
        static void AddSpacer(VisualElement parent, float height)
        {
            var ve = new VisualElement();
            ve.style.height = height;
            parent.Add(ve);
        }
    }
}
