using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VICheckbox : VisualElement
    {
        bool value;

        readonly VisualElement box;
        readonly VisualElement icon;

        static Texture2D checkIcon;

        public bool Value
        {
            get => value;
            set
            {
                this.value = value;
                Refresh();
            }
        }

        public float Size 
        { 
            set
            {
                box.style.width = value;
                box.style.height = value;
            }
        }

        public event Action<bool> OnValueChanged;

        public VICheckbox(bool defaultValue = true)
        {
            value = defaultValue;

            AddToClassList("vi-checkbox");

            // Load icon once
            if (checkIcon == null)
            {
                 checkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                     "Assets/VirtualInspector/Server/Scripts/Editor/UI/check.png"
                 );
            }

            box = new VisualElement();
            box.AddToClassList("vi-checkbox-box");
            Add(box);

            icon = new VisualElement();
            icon.AddToClassList("vi-checkbox-icon");
            icon.style.backgroundImage = new StyleBackground(checkIcon);
            box.Add(icon);

            RegisterCallback<MouseDownEvent>(evt =>
            {
                Value = !Value;
                OnValueChanged?.Invoke(Value);
                evt.StopPropagation();
            });

            Refresh();
        }

        void Refresh()
        {
            icon.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
