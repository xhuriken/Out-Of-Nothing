using System;
using UnityEngine.UIElements;
#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif
namespace Strix.VirtualInspector.Editor.UI
{

    public class VITogglelessIntField : VisualElement
    {
        public int Value => field.value;

        readonly IntegerField field;

        public VITogglelessIntField(int value, Action<int> onChanged)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            field = new IntegerField();
            field.label = string.Empty;
            field.value = value;

            field.AddToClassList("vi-subfield");

            // Compact size field
            field.style.width = 42;
            field.style.minWidth = 42;

            // Style internal input
            field.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var input = field.Q("unity-text-input");
                if (input != null)
                    input.AddToClassList("vi-subfield__input");
            });

            field.RegisterValueChangedCallback(evt =>
            {
                onChanged?.Invoke(evt.newValue);
            });

            Add(field);
        }

        public void SetValueWithoutNotify(int value)
        {
            field.SetValueWithoutNotify(value);
        }
    }
}
