using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIButtonField : VisualElement
    {
        public Button Button { get; }

        public VIButtonField(string label, System.Action onClick, string btField = "vi-button-field", string toolBarBt = "vi-toolbar-button")
        {
            style.marginTop = 8;
            AddToClassList(btField);

            Button = new Button(onClick)
            {
                text = label
            };

            Button.AddToClassList(toolBarBt);
          
            Button.style.marginLeft = 0;
            Button.style.marginRight = 8;
            Add(Button);
        }
    }
}
