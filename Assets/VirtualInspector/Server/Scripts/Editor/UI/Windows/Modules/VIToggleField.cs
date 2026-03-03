using System;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIToggleField : VisualElement, IVIUpdatableField
    {
        public bool Value => toggle.Value;

        public uint Key { get; set; }

        readonly VICheckbox toggle;

        public VIToggleField(
            string label,
            bool value,
            Action<bool> onChanged = null
        )
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // Label
            var labelVE = VIFieldUtils.CreateLabel(label);
            Add(labelVE);

            // Toggle
            toggle = new VICheckbox(value);
            toggle.Size = 20;
            toggle.style.top = 5;

            toggle.style.marginLeft = 2;

            toggle.OnValueChanged += value =>
            {
                onChanged?.Invoke(value);
            };

            Add(toggle);
        }

        public void UpdateValue(object value)
        {
            toggle.Value = (bool)value;
        }
    }
}
