using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIColorField : VisualElement, IVIUpdatableField
    {
        public Color Value => field.value;

        public uint Key { get; set; }

        readonly ColorField field;

        public VIColorField(
            string label,
            Color value,
            Action<Color> onChanged = null
        )
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // Label
            var labelVE = VIFieldUtils.CreateLabel(label);
            Add(labelVE);

            // Field
            field = new ColorField();
            field.label = string.Empty;
            field.value = value;

            field.AddToClassList("vi-color-field");

            // Layout
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.minWidth = 0;

            field.RegisterValueChangedCallback(evt =>
            {
                onChanged?.Invoke(evt.newValue);
            });

            Add(field);
        }

        public void UpdateValue(object value)
        {
            field.SetValueWithoutNotify((Color)value);
            //sthrow new NotImplementedException();
        }
    }
}
