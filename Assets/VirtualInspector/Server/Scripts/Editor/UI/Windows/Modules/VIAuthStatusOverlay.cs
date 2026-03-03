using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIAuthStatusOverlay : VisualElement
    {
        const float Duration = 5f;

        double startTime;
        bool active;

        public VIAuthStatusOverlay()
        {
            AddToClassList("vi-auth-overlay");
            style.display = DisplayStyle.None;

            pickingMode = PickingMode.Ignore;
        }

        public void Show(string message, bool success)
        {
            Clear();

            AddToClassList(success ? "success" : "error");
            RemoveFromClassList(success ? "error" : "success");

            var label = new Label(message);
            label.AddToClassList("vi-auth-label");
            Add(label);

            startTime = EditorApplication.timeSinceStartup;
            active = true;

            style.opacity = 1f;
            style.display = DisplayStyle.Flex;
            // Animation / fade
            schedule.Execute(UpdateFade).Every(16); // ~60fps
        }

        void UpdateFade()
        {
            if (!active)
                return;

            float elapsed = (float)(EditorApplication.timeSinceStartup - startTime);
            float t = Mathf.Clamp01(elapsed / Duration);

            style.opacity = 1f - t;

            if (t >= 1f)
            {
                active = false;
                style.display = DisplayStyle.None;
                Clear();
            }
        }
    }
}
