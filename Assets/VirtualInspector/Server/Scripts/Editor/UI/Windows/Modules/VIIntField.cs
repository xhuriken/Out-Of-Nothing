using System;
using UnityEngine.UIElements;
using static Strix.VirtualInspector.Editor.UI.VIFieldUtils;

#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIIntField : VisualElement, IVIUpdatableField, IVIUpdateLabelField
    {
        public int Value => field.value;

        readonly IntegerField field;
        public uint Key { get; set; }

        public VIIntField(string label, int value, Action<int> onChanged = null)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            var labelVE = CreateLabel(label);
            Add(labelVE);

            field = new IntegerField();
            field.label = string.Empty;
            field.value = value;
            field.AddToClassList("vi-subfield");

            ForceFullWidth(field);
            StyleInput(field);

            field.RegisterValueChangedCallback(e =>
            {
                onChanged?.Invoke(e.newValue);
            });

            Add(field);
        }

        public void SetLabelSize(float size)
        {
            var label = this.Q<Label>();
            label.style.width = size;
            label.style.minWidth = size;
            label.style.maxWidth = size;
        }

        public void SetValueWithoutNotify(int value)
        {
            field.SetValueWithoutNotify(value);
        }

        public void UpdateValue(object value)
        {
            SetValueWithoutNotify((int)value);
        }
    }
}
