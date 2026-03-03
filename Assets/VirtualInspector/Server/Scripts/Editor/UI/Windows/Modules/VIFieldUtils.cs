using UnityEngine.UIElements;
#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace Strix.VirtualInspector.Editor.UI
{
    public static class VIFieldUtils
    {
        public static Label CreateLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("vi-field-label");

            label.style.width = 200;
            label.style.minWidth = 200;
            label.style.maxWidth = 200;

            return label;
        }

        public static FloatField CreateAxisField(string axis, float value)
        {
            var field = new FloatField();
            field.label = string.Empty;
            field.value = value;
            field.AddToClassList("vi-subfield");

            ForceFullWidth(field);
            StyleInput(field);

            var axisLabel = new Label(axis);
            axisLabel.AddToClassList("vi-axis");
            field.Insert(0, axisLabel);

            return field;
        }

        public static void ForceFullWidth(VisualElement field)
        {
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.minWidth = 0;
        }

        public static void StyleInput(VisualElement field)
        {
            field.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var input = field.Q("unity-text-input");
                if (input != null)
                    input.AddToClassList("vi-subfield__input");
            });
        }
    }
}
