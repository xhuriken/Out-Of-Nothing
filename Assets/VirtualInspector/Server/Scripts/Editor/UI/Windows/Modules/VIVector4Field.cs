using UnityEngine;
using UnityEngine.UIElements;
using static Strix.VirtualInspector.Editor.UI.VIFieldUtils;
#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif
namespace Strix.VirtualInspector.Editor.UI
{
    public class VIVector4Field : VisualElement, IVIUpdatableField
    {
        public Vector4 Value { get; private set; }
        public uint Key { get; set; }

        FloatField xField, yField, zField, wField;

        public VIVector4Field(string label, Vector4 value)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            Value = value;

            Add(CreateLabel(label));

            xField = CreateAxisField("X", value.x);
            yField = CreateAxisField("Y", value.y);
            zField = CreateAxisField("Z", value.z);
            wField = CreateAxisField("W", value.w);

            Add(xField);
            Add(yField);
            Add(zField);
            Add(wField);
        }

        public void UpdateValue(object value)
        {
            Vector4 vector4 = (Vector4)value;
            if (vector4.x != xField.value)
                xField.SetValueWithoutNotify(vector4.x);
            if (vector4.y != yField.value)
                yField.SetValueWithoutNotify(vector4.y);
            if (vector4.z != zField.value)
                zField.SetValueWithoutNotify(vector4.z);
            if (vector4.w != wField.value)
                wField.SetValueWithoutNotify(vector4.w);
        }
    }
}
