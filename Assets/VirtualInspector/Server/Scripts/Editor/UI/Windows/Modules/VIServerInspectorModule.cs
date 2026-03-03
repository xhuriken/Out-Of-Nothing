using Strix.VirtualInspector.Server.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIServerInspectorModule : VisualElement
    {
        readonly ScrollView scrollView;
        VisualElement componenentView;
        ulong currentEntityId;

        readonly Dictionary<ulong, VIComponentCard> componentCards = new();

        VICheckbox EnableField;
        VISearchField NameField;
        VIDropdownField TagField;
        VIDropdownField LayerField;


        public VIServerInspectorModule()
        {
            name = "InspectorModule";
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;
            style.paddingBottom = 20;
            style.paddingLeft = 20;
            style.paddingRight = 20;
            style.paddingTop = 20;


            // ---------- SCROLL VIEW ----------
            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.name = "InspectorScrollView";
            scrollView.style.flexGrow = 1;

            
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            Add(scrollView);

            ShowNoSelection();


        }

        void ShowNoSelection()
        {
            scrollView.Clear();

            var label = new Label("No object selected");
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(0.6f, 0.6f, 0.6f);
            label.style.marginTop = 20;

            scrollView.Add(label);
        }

        void ShowUnavailable()
        {
            var label = new Label("Selected entity is not available");
            label.style.color = new Color(0.8f, 0.5f, 0.5f);
            label.style.marginTop = 20;
            scrollView.Add(label);
        }

        public void OnObjectSelected(ulong entityId)
        {
            if (currentEntityId == entityId)
                return;

            currentEntityId = entityId;

            componentCards.Clear();
            scrollView.Clear();

            if (entityId == 0)
            {
                ShowNoSelection();
                return;
            }

            if (!VIServerRuntime.AllObjects.TryGetValue(entityId, out var vobj) || vobj == null)
            {
                ShowUnavailable();
                return;
            }


            componenentView = new VisualElement();
            componenentView.style.paddingRight = 8;

            scrollView.Add(componenentView);

            BuildEntityHeader(entityId, vobj);
            BuildComponents(entityId, vobj);
        }

        void BuildComponents(ulong entityId, VIServerVirtualObject vobj)
        {
            var existing = new HashSet<ulong>(componentCards.Keys);

            foreach (var kv in vobj.Components)
            {
                var comp = kv.Value;
                ulong compId = comp.compId;

                if (!componentCards.TryGetValue(compId, out var card))
                {
                    // ---- NEW component ----
                    var title = !string.IsNullOrEmpty(comp.typeName)
                        ? Type.GetType(comp.typeName)?.Name ?? comp.typeName
                        : $"Component {compId}";

                    card = new VIComponentCard(title);
                    componentCards[compId] = card;
                    componenentView.Add(card);
                }

                // ---- UPDATE component ----
                card.Populate(entityId, comp);
                existing.Remove(compId);
            }

            // ---- REMOVED components ----
            foreach (var removedId in existing)
            {
                var card = componentCards[removedId];
                card.RemoveFromHierarchy();
                componentCards.Remove(removedId);
            }
        }

        public void Refresh()
        {
            if (currentEntityId == 0)
                return;

            if (!VIServerRuntime.AllObjects.TryGetValue(currentEntityId, out var vobj))
                return;

            if(NameField.value != vobj.Name)
                NameField.SetValueWithoutNotify(vobj.Name);


            var goMap = VIServerRuntime.S_Map;
            goMap.TryGetValue(currentEntityId, out var vgo);

            if (vgo != null)
            {
                if (EnableField.Value != vgo.ActiveSelf)
                    EnableField.Value = vgo.ActiveSelf;

                if (TagField.value != VIServerRuntime.Project.tags[vgo.TagId])
                    TagField.SetValueWithoutNotify(VIServerRuntime.Project.tags[vgo.TagId]);

                var layerIndex = Array.IndexOf(VIServerRuntime.Project.layersID, vgo.Layer);


                if (LayerField.value != VIServerRuntime.Project.layersName[layerIndex])
                    LayerField.SetValueWithoutNotify(VIServerRuntime.Project.layersName[layerIndex]);

               
            }


            BuildComponents(currentEntityId, vobj);
        }


        void BuildEntityHeader(ulong entityId, VIServerVirtualObject vobj)
        {
            var goMap = VIServerRuntime.S_Map;
            goMap.TryGetValue(entityId, out var vgo);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Column;

            // -------- Name + Enable --------
            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            if (vgo != null)
            {
                EnableField = new VICheckbox(vgo?.ActiveSelf ?? true);
                EnableField.Size = 32;
                EnableField.OnValueChanged += enabled =>
                {
                    if (vgo.ActiveSelf == enabled)
                        return;

                    vgo.ActiveSelf = enabled;

                    VIServerRuntime.UpdateGameObjectHeader(
                        vgo.EntityId,
                        vgo.Name,
                        VIServerRuntime.Project.tags[vgo.TagId],
                        vgo.Layer,
                        vgo.ActiveSelf
                    );
                };
            }

            NameField = new VISearchField(
                newName =>
                {
                    if (vgo != null)
                        VIServerRuntime.UpdateGameObjectHeader(
                            entityId,
                            newName,
                            VIServerRuntime.Project.tags[vgo.TagId],
                            vgo.Layer,
                            vgo.ActiveSelf
                        );
                },
                vgo?.Name ?? vobj.Name,
                string.Empty
            );

            NameField.style.flexGrow = 1;

            topRow.Add(EnableField);
            topRow.Add(NameField);
            componenentView.Add(topRow);

            // -------- Tag / Layer --------
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };

            if (vgo != null)
            {
                    TagField = new VIDropdownField(
                    VIServerRuntime.Project.tags.ToList(),
                    VIServerRuntime.Project.tags[vgo.TagId],
                    newTag =>
                    {
                        VIServerRuntime.UpdateGameObjectHeader(
                            entityId,
                            vgo.Name,
                            newTag,
                            vgo.Layer,
                            vgo.ActiveSelf
                        );
                    });

                var layerIndex = Array.IndexOf(VIServerRuntime.Project.layersID, vgo.Layer);
                LayerField = new VIDropdownField(
                    VIServerRuntime.Project.layersName.ToList(),
                    VIServerRuntime.Project.layersName[layerIndex],
                    newLayer =>
                    {
                        var idx = Array.IndexOf(VIServerRuntime.Project.layersName, newLayer);
                        VIServerRuntime.UpdateGameObjectHeader(
                            entityId,
                            vgo.Name,
                            VIServerRuntime.Project.tags[vgo.TagId],
                            VIServerRuntime.Project.layersID[idx],
                            vgo.ActiveSelf
                        );
                    });

                TagField.style.flexGrow = 1;
                LayerField.style.flexGrow = 1;
                LayerField.style.marginLeft = 8;

                row.Add(TagField);
                row.Add(LayerField);
            }

            componenentView.Add(row);
            componenentView.Add(CreateSeparator(8, 8));
        }



        public void Reset()
        {
            scrollView.Clear();
        }

        VisualElement CreateSeparator(float marginTop, float marginBottom)
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = marginTop;
            separator.style.marginBottom = marginBottom;

            Color color = new Color32(0x2A, 0x2F, 0x3E, 0xFF);

            separator.style.backgroundColor = color; // border-soft

            return separator;
        }
    }
}
