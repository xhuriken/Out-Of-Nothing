using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIPasswordField : TextField, IVIUpdateLabelField
    {
        public VIPasswordField(
            Action<string> onValueChanged,
            string value,
            string placeHolder = "Password"
        )
        {
            name = "PasswordField";
            isPasswordField = true;             
            text = value;
            label = string.Empty;

            AddToClassList("search-bar");        
            style.backgroundColor = Color.clear;

            // Placeholder
            var placeholder = new Label(placeHolder);
            placeholder.style.position = Position.Absolute;
            placeholder.style.left = 8;
            placeholder.style.top = 9;
            placeholder.style.color = new Color(1f, 1f, 1f, 0.25f);
            placeholder.pickingMode = PickingMode.Ignore;
            placeholder.AddToClassList("placeHolder");
            Add(placeholder);

            VisualElement field = ElementAt(0);

            Color normalBorder = new Color32(0x2A, 0x2F, 0x3E, 0xFF);
            Color hoverBorder = new Color32(0x3C, 0x45, 0x5A, 0xFF);
            Color focusBorder = new Color32(0x4C, 0x8A, 0xFF, 0xFF);

            bool isFocused = false;

            void SetBorder(Color c)
            {
                field.style.borderBottomColor = c;
                field.style.borderLeftColor = c;
                field.style.borderRightColor = c;
                field.style.borderTopColor = c;
            }

            SetBorder(normalBorder);

            RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!isFocused) SetBorder(hoverBorder);
            });

            RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!isFocused) SetBorder(normalBorder);
            });

            RegisterCallback<FocusInEvent>(_ =>
            {
                isFocused = true;
                SetBorder(focusBorder);
            });

            RegisterCallback<FocusOutEvent>(_ =>
            {
                isFocused = false;
                SetBorder(normalBorder);
            });

            this.RegisterValueChangedCallback(evt =>
            {
                placeholder.style.display =
                    string.IsNullOrEmpty(evt.newValue)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;

                onValueChanged?.Invoke(evt.newValue);
            });
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
    }
}
