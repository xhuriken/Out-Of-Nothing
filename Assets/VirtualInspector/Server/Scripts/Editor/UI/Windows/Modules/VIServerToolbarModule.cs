using Strix.VirtualInspector.Server.Editor;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIServerToolbarModule : VisualElement
    {
        bool isRunning;
        bool liveEdit = true;

        string serverPassword = "";

        Button startStopButton;
        Button liveEditButton;
        VIAuthStatusOverlay overlay;

        public event System.Action OnServerStarted;
        public event System.Action OnServerStopped;
        public event System.Action<bool> OnLiveEditChanged;

        public VIServerToolbarModule(VIAuthStatusOverlay vIAuthStatusOverlay)
        {
            overlay = vIAuthStatusOverlay;

            VIServerRuntime.OnAuthMessage += HandleAuthMessage;


            AddToClassList("vi-toolbar");

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.minHeight = 32;

            // ---------- SERVER PORT ----------
            Add(WrapToolbarField(
                new VIIntField(
                    "Server Port",
                    VIServerRuntime.ServerListenPort,
                    v => VIServerRuntime.ServerListenPort = v
                ),
                150
            ));

            // ---------- START / STOP ----------
            startStopButton = CreateToolbarButton("Start", ToggleServer);
            startStopButton.style.width = 60;
            startStopButton.style.minWidth = 60;
            Add(startStopButton);

            // ---------- FLEX SPACE ----------
            Add(new VisualElement { style = { flexGrow = 1 } });

            // ---------- PASSWORD ----------
            Add(WrapToolbarField(
                new VIStringField(
                    "Password",
                    serverPassword,
                    v =>
                    {
                        serverPassword = v;
                        VIServerRuntime.Password = v;
                    },
                    true
                ),
                180
            ));

            // ---------- CLIENT IP ----------
            Add(WrapToolbarField(
                new VIStringField(
                    "Client IP",
                    VIServerRuntime.ClientHost,
                    v => VIServerRuntime.ClientHost = v
                ),
                180
            ));

            // ---------- CLIENT PORT ----------
            Add(WrapToolbarField(
                new VIIntField(
                    "Client Port",
                    VIServerRuntime.ClientPort,
                    v => VIServerRuntime.ClientPort = v
                ),
                150
            ));

            // ---------- LIVE EDIT ----------
            liveEditButton = CreateLiveEditButton();
            Add(liveEditButton);

            RefreshState();

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            VIServerRuntime.OnAuthMessage -= HandleAuthMessage;
        }



        private void HandleAuthMessage(bool success, string message)
        {
            overlay?.Show(message, success);
        }

        // ======================================================
        // Helpers
        // ======================================================

        VisualElement WrapToolbarField(VisualElement field, float width)
        {
            if(field is IVIUpdateLabelField labelField)
            {
                labelField.SetLabelSize(72);
            }
            var wrapper = new VisualElement();
            wrapper.style.width = width;
            wrapper.style.minWidth = width;
            wrapper.style.maxWidth = width;
            wrapper.style.marginRight = 6;
            wrapper.style.alignSelf = Align.Center;

            wrapper.Add(field);
            return wrapper;
        }

        Button CreateToolbarButton(string label, System.Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("vi-toolbar-button");
            return btn;
        }

        Button CreateLiveEditButton()
        {
            var btn = new Button(() =>
            {
                liveEdit = !liveEdit;
                RefreshLiveEditButton();

                OnLiveEditChanged?.Invoke(liveEdit);
            });

            btn.AddToClassList("vi-toolbar-live-edit");
            return btn;
        }

        // ======================================================
        // State Refresh
        // ======================================================

        void RefreshState()
        {
            startStopButton.text = isRunning ? "Stop" : "Start";
            startStopButton.EnableInClassList("running", isRunning);
            RefreshLiveEditButton();
        }

        void RefreshLiveEditButton()
        {
            liveEditButton.text = "Client Sync";

            liveEditButton.EnableInClassList("on", liveEdit);
            liveEditButton.EnableInClassList("off", !liveEdit);


        }

        // ======================================================
        // Server Control
        // ======================================================

        void ToggleServer()
        {
            if (isRunning)
                StopServer();
            else
                StartServer();
        }

        void StartServer()
        {
            if (isRunning)
                return;

            isRunning = true;
            // VIServerRuntime.Start();
            RefreshState();
            OnServerStarted?.Invoke();


        }

        public void StopServer()
        {
            if (!isRunning)
                return;

            isRunning = false;
            // VIServerRuntime.Stop();
            RefreshState();
            OnServerStopped?.Invoke();

        }
    }
}
