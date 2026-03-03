using UnityEngine.UIElements;
using static Strix.VirtualInspector.Editor.UI.VIFieldUtils;

#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIFloatField : VisualElement, IVIUpdatableField
    {
        public float Value => field.value;

        public uint Key { get; set; }

        readonly FloatField field;

        public VIFloatField(string label, float value)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // Label
            var labelVE = CreateLabel(label);
            Add(labelVE);

            // Field
            field = new FloatField();
            field.label = string.Empty;
            field.value = value;
            field.AddToClassList("vi-subfield");

            ForceFullWidth(field);
            StyleInput(field);

            Add(field);
        }

        public void UpdateValue(object value)
        {
            field.SetValueWithoutNotify((float)value);
        }
    }
}
