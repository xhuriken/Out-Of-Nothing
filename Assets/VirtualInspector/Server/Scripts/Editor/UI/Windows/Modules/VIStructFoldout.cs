using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIStructFoldout : VisualElement
    {
        readonly VisualElement header;
        readonly VisualElement arrow;
        readonly Label titleLabel;
        readonly VisualElement content;

        bool expanded = true;

        public VIStructFoldout(string title, bool defaultExpanded = true)
        {
            expanded = defaultExpanded;

            AddToClassList("vi-struct");

            style.flexDirection = FlexDirection.Column;

            // ================= HEADER =================
            header = new VisualElement();
            header.AddToClassList("vi-struct-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            Add(header);

            // Arrow
            arrow = new VisualElement();
            arrow.AddToClassList("vi-struct-arrow");
            arrow.style.rotate = new Rotate(new Angle(expanded ? 90 : 0));
            header.Add(arrow);

            // Label
            titleLabel = new Label(title);
            titleLabel.AddToClassList("vi-struct-title");
            header.Add(titleLabel);

            // Click → toggle
            header.RegisterCallback<MouseDownEvent>(_ => Toggle());

            // ================= CONTENT =================
            content = new VisualElement();
            content.AddToClassList("vi-struct-content");
            content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            Add(content);
        }

        void Toggle()
        {
            expanded = !expanded;

            arrow.style.rotate = new Rotate(new Angle(expanded ? 90 : 0));
            content.style.display = expanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public VisualElement Content => content;

        public bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                arrow.style.rotate = new Rotate(new Angle(expanded ? 90 : 0));
                content.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
