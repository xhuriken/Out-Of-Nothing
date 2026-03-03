using System;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIStringField : VisualElement, IVIUpdatableField, IVIUpdateLabelField
    {
        public string Value { get; private set; }
        public uint Key { get; set; }

        readonly VISearchField searchField;

        public VIStringField(
            string label,
            string value,
            Action<string> onChanged = null,
            bool isPassword = false    
        )
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            Value = value;

            var labelVE = VIFieldUtils.CreateLabel(label);
            Add(labelVE);

            searchField = new VISearchField(
                text =>
                {
                    Value = text;
                    onChanged?.Invoke(text);
                },
                value,
                string.Empty,
                isPassword       
            );

            searchField.style.flexGrow = 1;
            searchField.style.flexShrink = 1;
            searchField.style.minWidth = 0;
            searchField.style.marginBottom = 0;
            searchField.style.marginLeft = 3;
            searchField.style.marginRight = 3;
            searchField.AddToClassList("vi-string-field");

            Add(searchField);
        }

        public void SetLabelSize(float width)
        {
            var label = this.Q<Label>();
            if (label != null)
            {
                label.style.width = width;
                label.style.minWidth = width;
                label.style.maxWidth = width;
            }
        }

        public void SetValueWithoutNotify(string value)
        {
            Value = value;
            searchField.SetValueWithoutNotify(value);
        }

        public void UpdateValue(object value)
        {
            SetValueWithoutNotify((string)value);
        }
    }
}
