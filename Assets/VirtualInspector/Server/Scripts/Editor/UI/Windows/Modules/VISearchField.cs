using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VISearchField : TextField
    {
        public VISearchField(
            Action<string> onValueChanged,
            string value,
            string placeHolder = "Search...",
            bool isPassword = false
        ) : base()
        {
            name = "HierarchySearch";
            text = value;
            isPasswordField = isPassword;   

            AddToClassList("search-bar");
            label = string.Empty;
            style.backgroundColor = Color.clear;

            var placeholder = new Label(placeHolder);
            placeholder.style.position = Position.Absolute;
            placeholder.style.left = 8;
            placeholder.style.top = 9;
            placeholder.style.color = new Color(1f, 1f, 1f, 0.25f);
            placeholder.pickingMode = PickingMode.Ignore;
            placeholder.AddToClassList("placeHolder");
            Add(placeholder);

            VisualElement field = ElementAt(0);
            field.AddToClassList("vi-search--normal");
          //  field.RemoveFromClassList("unity-base-text-field__input");

            Color normalBorder = new Color32(0x2A, 0x2F, 0x3E, 0xFF);
            Color hoverBorder = new Color32(0x3C, 0x45, 0x5A, 0xFF);
            Color focusBorder = new Color32(0x4C, 0x8A, 0xFF, 0xFF);

            field.style.borderBottomColor = StyleKeyword.Null;
            field.style.borderLeftColor = StyleKeyword.Null;
            field.style.borderRightColor = StyleKeyword.Null;
            field.style.borderTopColor = StyleKeyword.Null;
            field.style.backgroundColor = StyleKeyword.Null;

            void SwitchState(string newState)
            {
                field.RemoveFromClassList("vi-search--normal");
                field.RemoveFromClassList("vi-search--hover");
                field.RemoveFromClassList("vi-search--focus");
                field.AddToClassList(newState);
            }


            RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!field.ClassListContains("vi-search--focus"))
                    SwitchState("vi-search--hover");
            });

            RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!field.ClassListContains("vi-search--focus"))
                    SwitchState("vi-search--normal");
            });

            RegisterCallback<FocusInEvent>(_ =>
            {
                SwitchState("vi-search--focus");
            });

            RegisterCallback<FocusOutEvent>(_ =>
            {
                SwitchState("vi-search--normal");
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
    }
}