using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Strix.VirtualInspector.Editor.UI.VIFieldUtils;
#if !UNITY_2022_2_OR_NEWER
using UnityEditor.UIElements;
#endif
namespace Strix.VirtualInspector.Editor.UI
{
    public class VIVector3Field : VisualElement, IVIUpdatableField
    {
        public Vector3 Value { get; private set; }

        public uint Key { get; set; }

        readonly FloatField xField;
        readonly FloatField yField;
        readonly FloatField zField;

        public VIVector3Field(
            string label,
            Vector3 initialValue
        )
        {
            AddToClassList("vi-vector3-field");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            Value = initialValue;

            // -------- Label --------
            Add(CreateLabel(label));

            // -------- X --------
            xField = CreateAxisField("X", initialValue.x);
            Add(xField);

            // -------- Y --------
            yField = CreateAxisField("Y", initialValue.y);
            Add(yField);

            // -------- Z --------
            zField = CreateAxisField("Z", initialValue.z);
            Add(zField);
        }

        FloatField CreateAxisField(string axis, float value)
        {
            var field = new FloatField();
            field.label = string.Empty;
            field.value = value;

            field.AddToClassList("vi-subfield");

            // Force layout stability
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.minWidth = 0;

            // Axis badge (muted, centered, fixed width)
            var axisLabel = new Label(axis);
            axisLabel.AddToClassList("vi-axis");
            field.Insert(0, axisLabel);
            EnableAxisDrag(axisLabel, field);


            // Style the internal input too (Unity 2021)
            field.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                var input = field.Q("unity-text-input");
                if (input != null)
                    input.AddToClassList("vi-subfield__input");
            });

            return field;
        }

        void EnableAxisDrag(
            Label axisLabel,
            FloatField field,
            float sensitivity = 0.02f
        )
        {
            bool dragging = false;
            Vector2 startMouse = Vector2.zero;
            float startValue = 0;

            axisLabel.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                dragging = true;
                startMouse = evt.mousePosition;
                startValue = field.value;

                axisLabel.CaptureMouse();
                evt.StopImmediatePropagation();
            });

            axisLabel.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!dragging)
                    return;

                float delta = evt.mousePosition.x - startMouse.x;

                float speed = sensitivity;
                if (evt.shiftKey)
                    speed *= 0.1f; 

                field.value = startValue + delta * speed;
                evt.StopImmediatePropagation();
            });

            axisLabel.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (!dragging)
                    return;

                dragging = false;
                axisLabel.ReleaseMouse();
                evt.StopImmediatePropagation();
            });
        }

        void WarpMouseIfNeeded(Vector2 mousePos)
        {
            const float margin = 10f;

            var screen = GUIUtility.GUIToScreenPoint(mousePos);

            float x = screen.x;
            float y = screen.y;

            bool warped = false;

            if (x < margin)
            {
                x = Screen.currentResolution.width - margin;
                warped = true;
            }
            else if (x > Screen.currentResolution.width - margin)
            {
                x = margin;
                warped = true;
            }

            if (y < margin)
            {
                y = Screen.currentResolution.height - margin;
                warped = true;
            }
            else if (y > Screen.currentResolution.height - margin)
            {
                y = margin;
                warped = true;
            }

            if (warped)
                EditorGUIUtility.SetWantsMouseJumping(1);
        }

        public void UpdateValue(object value)
        {
            Vector3 vector3 = (Vector3)value;
            if (vector3.x != xField.value)
                xField.SetValueWithoutNotify(vector3.x);
            if (vector3.y != yField.value)
                yField.SetValueWithoutNotify(vector3.y);
            if (vector3.z != zField.value)
                zField.SetValueWithoutNotify(vector3.z);
        }
    }
}
