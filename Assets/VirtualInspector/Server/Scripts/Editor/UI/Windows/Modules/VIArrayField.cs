using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIArrayField<T> : VisualElement, IVIUpdatableField
    {
        // ---------------- DATA ----------------
        List<T> values;
        readonly Func<int, T, VisualElement> elementFactory;

        // ---------------- UI ----------------
        readonly ListView listView;
        readonly VITogglelessIntField sizeField;
        readonly VisualElement arrow;

        readonly List<IVIUpdatableField> elementFields = new();

        bool expanded = true;

        // ---------------- API ----------------
        public IReadOnlyList<T> Values => values;
        public uint Key { get; set; }

        public Action<List<T>> OnStructureChanged;

        // ---------------- CTOR ----------------
        public VIArrayField(
            string label,
            List<T> values,
            Func<int, T, VisualElement> elementFactory
        )
        {
            this.values = values ?? new List<T>();
            this.elementFactory = elementFactory;

            AddToClassList("vi-array");

            // ===== HEADER =====
            var header = new VisualElement();
            header.AddToClassList("vi-array-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            Add(header);

            // Fold arrow
            arrow = new VisualElement();
            arrow.AddToClassList("vi-array-fold");
            arrow.style.rotate = new Rotate(new Angle(expanded ? 90 : 0));
            arrow.RegisterCallback<MouseDownEvent>(_ => ToggleFold());
            header.Add(arrow);

            // Label
            var labelVE = new Label(label);
            labelVE.AddToClassList("vi-array-label");
            header.Add(labelVE);

            sizeField = new VITogglelessIntField(
                this.values.Count,
                newSize =>
                {
                }
            );
            header.Add(sizeField);

            // ===== LIST VIEW =====
            listView = new ListView
            {
                itemsSource = this.values,
                reorderable = false,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = 26,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None
            };

            listView.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("vi-array-row");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var handle = new Label("≡");
                handle.AddToClassList("vi-array-handle");
                row.Add(handle);

                var content = new VisualElement { name = "vi-array-content" };
                content.AddToClassList("vi-array-content");
                content.style.flexGrow = 1;
                row.Add(content);

                return row;
            };

            listView.bindItem = (ve, index) =>
            {
                var content = ve.Q<VisualElement>("vi-array-content");
                content.Clear();

                var field = elementFactory(index, values[index]);

                field.SetEnabled(false);
                field.AddToClassList("vi-readonly");

                content.Add(field);

                if (index >= elementFields.Count)
                    elementFields.Add(field as IVIUpdatableField);
                else
                    elementFields[index] = field as IVIUpdatableField;
            };

            Add(listView);

            Refresh();
        }

        // ---------------- UI ACTIONS ----------------

        void ToggleFold()
        {
            expanded = !expanded;
            arrow.style.rotate = new Rotate(new Angle(expanded ? 90 : 0));
            listView.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
        }

   
        // ---------------- INTERNAL ----------------

        void Refresh()
        {
            sizeField.SetValueWithoutNotify(values.Count);
            elementFields.Clear();
            listView.RefreshItems();
        }

        // ---------------- IVIUpdatableField ----------------

        public void UpdateValue(object value)
        {
            if (value is not T[] newValues)
                return;

            if (newValues.Length != values.Count)
            {
                values = newValues.ToList();
                Refresh();
                return;
            }

            for (int i = 0; i < newValues.Length; i++)
            {
                values[i] = newValues[i];

                if (i < elementFields.Count && elementFields[i] != null)
                    elementFields[i].UpdateValue(newValues[i]);
            }
        }
    }
}
