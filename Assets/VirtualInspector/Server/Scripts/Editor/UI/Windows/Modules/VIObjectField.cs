using Strix.VirtualInspector.Common;
using Strix.VirtualInspector.Server.Editor;
using System;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIObjectField : VisualElement, IVIUpdatableField
    {
        VIObjRef value;
        readonly Type expectedType;

        readonly Label valueLabel;

        public VIObjRef Value => value;

        public uint Key { get; set; }

        public VIObjectField(
            string label,
            VIObjRef value,
            Type expectedType
            )
        {
            this.value = value;
            this.expectedType = expectedType;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // -------- Label --------
            var labelVE = VIFieldUtils.CreateLabel(label);
            Add(labelVE);

            // -------- Field Container --------
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexGrow = 1;
            fieldContainer.style.flexShrink = 1;
            fieldContainer.style.minWidth = 0;
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.style.alignItems = Align.Center;
            Add(fieldContainer);

            // -------- Value Field (same style as others) --------
            var valueField = new VisualElement();
            valueField.AddToClassList("vi-subfield");
            valueField.style.flexGrow = 1;
            valueField.style.justifyContent = Justify.Center;

            valueLabel = new Label(BuildPrettyLabel(value));
            valueLabel.AddToClassList("vi-subfield__label");

            valueField.Add(valueLabel);
            fieldContainer.Add(valueField);
        }

        // ------------------------------------------------------
        // Pretty label 
        // ------------------------------------------------------
        static string BuildPrettyLabel(VIObjRef value)
        {
            if (value.entityId == 0)
                return "None";

            if (!VIServerRuntime.AllObjects.TryGetValue(value.entityId, out var vo) || vo == null)
                return "Not Available";

            string baseLabel = string.IsNullOrEmpty(vo.Name)
                ? $"Entity {vo.EntityId}"
                : vo.Name;

            bool isSceneObject = vo is VIServerVirtualGameObject;
            string prefix = isSceneObject ? "[Scene] " : "[Asset] ";

            if (value.entityId == value.compId)
                return prefix + baseLabel;

            if (vo.Components.TryGetValue(value.compId, out var comp) && comp != null)
            {
                string compName;

                if (!string.IsNullOrEmpty(comp.typeName))
                {
                    var t = Type.GetType(comp.typeName);
                    compName = t != null ? t.Name : comp.typeName;
                }
                else
                {
                    compName = $"Component {comp.compId}";
                }

                return $"{prefix}{baseLabel}/{compName}";
            }

            return prefix + baseLabel;
        }

        public void UpdateValue(object value)
        {
            VIObjRef vIObjRef = (VIObjRef)value;

            valueLabel.text = BuildPrettyLabel(vIObjRef);

        }
    }
}
