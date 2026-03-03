using Strix.VirtualInspector.Editor.UI.Theme;
using Strix.VirtualInspector.Server.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIServerEditorWindow : EditorWindow
    {

        VIServerToolbarModule toolbar;
        VIServerHierarchyModule hierarchy;
        VIServerInspectorModule inspector;

        bool isRunning;

        [MenuItem("Virtual Inspector/Inspector")]
        public static void Open()
        {
            var w = GetWindow<VIServerEditorWindow>();
            w.titleContent = new GUIContent("Virtual Inspector");
            w.minSize = new Vector2(600, 400);
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            ShutdownServerIfNeeded();

        }

        void OnBeforeAssemblyReload()
        {
            ShutdownServerIfNeeded();
        }

        void OnDestroy()
        {
            ShutdownServerIfNeeded();
        }

        void ShutdownServerIfNeeded()
        {
            if (!isRunning)
                return;

            isRunning = false;

            if (toolbar != null)
                toolbar.StopServer();
            else
                VIServerRuntime.OnDestroy();
        }

        ulong entityId = 0;
        void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;
            VIThemeManager.ApplyTheme(rootVisualElement);

             // ---------- Auth Overlay ----------
             var authOverlay = new VIAuthStatusOverlay();

            // ---------- Toolbar ----------
            toolbar = new VIServerToolbarModule(authOverlay);
            toolbar.OnServerStarted += () =>
            {
                isRunning = true;
                VIServerRuntime.Awake(VIServerRuntime.ServerListenPort, VIServerRuntime.ClientHost, VIServerRuntime.ClientPort, EditorApplication.timeSinceStartup);
            };
            toolbar.OnServerStopped += () =>
            {
                isRunning = false;
                VIServerRuntime.OnDestroy();

                hierarchy.Reset();
                inspector.Reset();
            };

            toolbar.OnLiveEditChanged += (enabled) =>
            {
                VIServerRuntime.SendLiveEdit(enabled);
            };


            rootVisualElement.Add(toolbar);

            // ---------- SplitView ----------
            var splitView = new TwoPaneSplitView(
                0,
                300,
                TwoPaneSplitViewOrientation.Horizontal
            );
            splitView.style.flexGrow = 1;

            // ---------- Modules ----------
            hierarchy = new VIServerHierarchyModule();
            inspector = new VIServerInspectorModule();

            hierarchy.OnItemSelected += item =>
            {
                if (item.entityId != 0)
                {
                    VIServerRuntime.UnselectEntity(entityId);
                    entityId = item.entityId;
                    VIServerRuntime.SelectEntity(entityId);
                    inspector.OnObjectSelected(item.entityId);
                }
                else
                    inspector.OnObjectSelected(0);
            };

            splitView.Add(hierarchy);
            splitView.Add(inspector);

            rootVisualElement.Add(splitView);

            var footer = new VisualElement();
            footer.style.height = 32;
            footer.style.minHeight = 32;
            

            footer.style.justifyContent = Justify.Center;
            footer.style.alignItems = Align.FlexEnd;
            footer.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f);

            VIThemeDropdownField vIThemeDropdownField = new VIThemeDropdownField();
            vIThemeDropdownField.OnThemeChanged += (theme) =>
            {
                VIThemeManager.SetTheme(theme);
                VIThemeManager.ApplyTheme(rootVisualElement);
            };
            vIThemeDropdownField.style.marginRight = 8;

            footer.Add(vIThemeDropdownField);

            rootVisualElement.Add(footer);


            rootVisualElement.Add(authOverlay);
        }
        void OnEditorUpdate()
        {
            // Server stopped unexpectedly
            if (!VIServerRuntime.IsListening && isRunning)
            {
                toolbar.StopServer();

            



                Debug.LogWarning(
                    "[Virtual Inspector] Server has stopped listening. Closing connection."
                );

                return;
            }

            // Server running
            if (isRunning && VIServerRuntime.IsListening)
            {
                VIServerRuntime.Update(EditorApplication.timeSinceStartup);

                if (VIServerRuntime.IsDirty)
                {
                    hierarchy.Refresh();

                    VIServerRuntime.ClearDirtyFlag();
                }
            }

            if(inspector != null && entityId != 0)
                inspector.Refresh();

        }

    }
}
