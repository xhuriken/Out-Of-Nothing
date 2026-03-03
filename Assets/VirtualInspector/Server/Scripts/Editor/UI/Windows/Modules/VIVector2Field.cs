using UnityEngine;
using UnityEngine.UIElements;
using static Strix.VirtualInspector.Editor.UI.VIFieldUtils;
#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif
namespace Strix.VirtualInspector.Editor.UI
{
    public class VIVector2Field : VisualElement, IVIUpdatableField
    {
        public Vector2 Value { get; private set; }
        public uint Key { get; set; }

        FloatField xField, yField;

        public VIVector2Field(string label, Vector2 value)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            Value = value;

            Add(CreateLabel(label));

            xField = CreateAxisField("X", value.x);
            yField = CreateAxisField("Y", value.y);

            Add(xField);
            Add(yField);
        }

        public void UpdateValue(object value)
        {
            Vector2 vector2 = (Vector2)value;
            if (vector2.x != xField.value)
                xField.SetValueWithoutNotify(vector2.x);
            if (vector2.y != yField.value)
                yField.SetValueWithoutNotify(vector2.y);
        }
    }
}
