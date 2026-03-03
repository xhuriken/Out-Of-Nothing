using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace Strix.VirtualInspector.Editor.UI
{
    public class VIServerHierarchyModule : VisualElement
    {
        VITreeView treeView;

        public enum Tab
        {
            Scene,
            Assets
        }

        Tab currentTab = Tab.Scene;
        TextField searchField;
        VisualElement tabs;
        VisualElement _overlayLayer;

        readonly ScrollView scrollView;

        public event Action<VITreeView.TreeViewItem> OnItemSelected;


        public VIServerHierarchyModule()
        {
            name = "HierarchyModule";
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            // ---------------- HEADER ----------------
            var header = new VisualElement();
            header.style.flexShrink = 0;
            header.style.paddingTop = 6;
            header.style.paddingBottom = 6;
            header.style.flexDirection = FlexDirection.Column;

            Add(header);

            // ---------------- TABS ----------------
            tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.marginBottom = 6;

            var sceneTab  = CreateTab("Scene",  Tab.Scene);
            var assetsTab = CreateTab("Assets", Tab.Assets);

            tabs.Add(sceneTab);
            tabs.Add(assetsTab);
            header.Add(tabs);

            UpdateTabs(sceneTab, assetsTab);

            // ---------------- SEARCH ----------------
            
            searchField = new VISearchField((searchText) => {
                treeView.SearchText = searchText;
                treeView.Reload();
            }, string.Empty);

            header.Add(searchField);


            // ---------------- TREEVIEW ----------------
            treeView = new VITreeView();
            treeView.style.flexGrow = 1;


            treeView.OnItemSelected += item =>
            {
                OnItemSelected?.Invoke(item);
            };

            Add(treeView);

            style.paddingBottom = 20;
            style.paddingLeft = 20;
            style.paddingRight = 20;
            style.paddingTop = 20;
        }


        VisualElement GetWindowRoot()
        {
            VisualElement ve = this;
            while (ve.parent != null)
                ve = ve.parent;
            return ve;
        }

        public void Refresh()
        {
            treeView.Reload();
        }

        public void Reset()
        {
            searchField.value = string.Empty;
            treeView.Reload();
        }

        // ---------------- TAB FACTORY ----------------
        VisualElement CreateTab(string title, Tab tab)
        {
            var tabEl = new Label(title);
        
            tabEl.style.unityTextAlign = TextAnchor.MiddleCenter;
            tabEl.AddToClassList("tab");
            tabEl.AddToClassList("tab-active");

            tabEl.RegisterCallback<MouseDownEvent>(_ =>
            {
                currentTab = tab;
                OnTabChanged();
            });

            return tabEl;
        }

        // ---------------- TAB STATE ----------------
        void OnTabChanged()
        {
            // reset search
                searchField.SetValueWithoutNotify(string.Empty);

            treeView.tab = currentTab;
            treeView.Reload();

            UpdateTabs(tabs.ElementAt(0), tabs.ElementAt(1));
        }

        void UpdateTabs(VisualElement scene, VisualElement assets)
        {
            scene.EnableInClassList("tab-active", currentTab == Tab.Scene);
            scene.EnableInClassList("tab", currentTab != Tab.Scene);
            assets.EnableInClassList("tab-active", currentTab == Tab.Assets);
            assets.EnableInClassList("tab", currentTab != Tab.Assets);
        }

        static Rect GetSafePopupRect(Vector2 screenPos)
        {
            const float safeMargin = 20f;

            var screen = GUIUtility.GUIToScreenPoint(screenPos);

            float x = Mathf.Clamp(
                screen.x,
                safeMargin,
                Screen.currentResolution.width - safeMargin
            );

            float y = Mathf.Clamp(
                screen.y,
                safeMargin,
                Screen.currentResolution.height - safeMargin
            );

            return new Rect(x, y, 1, 1);
        }
    }
}
