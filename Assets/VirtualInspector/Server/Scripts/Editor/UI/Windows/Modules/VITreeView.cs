using Strix.VirtualInspector.Common;
using Strix.VirtualInspector.Server.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Strix.VirtualInspector.Editor.UI
{
#if UNITY_2022_2_OR_NEWER
    public class VITreeView : TreeView
    {
        public string SearchText { get; set; }
        public event Action<TreeViewItem> OnItemSelected;
        public Action<MouseDownEvent, TreeViewItem> OnContextMenu;
        public event Action<TreeViewItem> OnItemDoubleClicked;

        public Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab tab { get; set; } = Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Scene;

        public class TreeViewItem
        {
            public int id;
            public string name;
            public IconType icon;
            public string otherIcon = string.Empty;
            public List<TreeViewItem> children = new();
            public VICommonComponentNode ComponentNode { get; set; }
            // ---- NEW ----
            public ulong sceneId;
            public ulong entityId;
            public ulong compId;

            public bool IsScene => entityId == 0 && sceneId != 0;
            public bool IsEntity => entityId != 0;
        }

        class ArrowState
        {
            public int id;
            public IVisualElementScheduledItem anim;
        }


        public VITreeView() : base()
        {


            var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/VirtualInspector/Server/Scripts/Editor/UI//VIHierarchyItem.uxml"
            );


            name = "HierarchyTree";
            makeItem = () =>
            {
                var ve = itemTemplate.CloneTree();

                var container = ve.ElementAt(0);
                var arrow = container.Q<VisualElement>("arrow");

                arrow.RegisterCallback<MouseDownEvent>(OnArrowClick);

                ve.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
                    {
                        if (ve.userData is TreeViewItem item)
                        {
                            OnItemDoubleClicked?.Invoke(item);
                            evt.StopPropagation();
                        }
                    }
                });


                // Init state
                if (arrow.userData == null || arrow.userData is not ArrowState)
                    arrow.userData = new ArrowState();

                return ve;
            };

            AddToClassList("hierarchy");

            //   AddToClassList("treeview");

            bindItem = (ve, index) =>
            {
                var container = ve.ElementAt(0);
                var arrow = container.Q<VisualElement>("arrow");
                var icon = container.Q<VisualElement>("icon");
                var label = container.Q<Label>("label");

                icon.style.flexShrink = 0;
                icon.style.flexGrow = 0;
                icon.style.width = 16;
                icon.style.height = 16;

                arrow.style.flexShrink = 0;
                arrow.style.width = 12;

                label.style.flexGrow = 1;
                label.style.flexShrink = 1;
                label.style.minWidth = 0; // ⭐ CRUCIAL
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Hidden;
                label.style.textOverflow = TextOverflow.Ellipsis;

                container.style.flexDirection = FlexDirection.Row;
                container.style.alignItems = Align.Center;
                container.style.minWidth = 0;


                int depth = GetDepthForIndex(index);
                container.Q<VisualElement>("indent").style.marginLeft = depth * 8;

                var data = GetItemDataForIndex<TreeViewItem>(index);

                ve.userData = data;
                EnsureContextMenuHook(ve);

                ve.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == (int)MouseButton.RightMouse)
                    {
                        SetSelection(index);
                    }
                });
                ve.pickingMode = PickingMode.Position;

                int id = GetIdForIndex(index);

                // Label
                label.text = data.name;



                // Icon
                icon.style.backgroundImage = new StyleBackground(GetIcon(data.icon, data.otherIcon));

                bool isSelected = selectedIndices.Contains(index);

                if (isSelected)
                {
                    //label.style.color = Color.white;
                    icon.style.opacity = 1f;
                }
                else
                {
                    //  label.style.color = new Color(0.62f, 0.62f, 0.62f); // #A0A0A0
                    icon.style.opacity = 0.8f;
                }



                bool hasChildren = GetChildrenIdsForIndex(index).Count() > 0;
                bool expanded = IsExpanded(id);

                if (hasChildren)
                {
                    arrow.style.display = DisplayStyle.Flex;
                    arrow.style.backgroundImage = new StyleBackground(GetArrowIcon(expanded));
                }
                else
                {
                    arrow.style.display = DisplayStyle.None;              
                    arrow.style.backgroundImage = new StyleBackground();
                }

                var st = (ArrowState)arrow.userData;
                st.id = id;

                if (st.anim != null)
                {
                    st.anim.Pause();
                    st.anim = null;
                }

                arrow.style.rotate = expanded
                    ? new Rotate(new Angle(90f))
                    : new Rotate(new Angle(0f));

            };

            selectionChanged += OnSelectionChangedInternal;
            InstallEmptySpaceContextMenu();

            Reload();
        }

        void OnArrowClick(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            evt.StopPropagation();

            var arrow = (VisualElement)evt.currentTarget;
            if (arrow.userData is not ArrowState st)
                return;

            int id = st.id;

            bool expanded = IsExpanded(id);

            if (expanded)
            {
                _expandedCache.Remove(id);
                CollapseItem(id);
            }
            else
            {
                _expandedCache.Add(id);
                ExpandItem(id);
            }

            AnimateArrowRotation(arrow, st, !expanded);
        }

        void AnimateArrowRotation(VisualElement arrow, ArrowState st, bool expanded)
        {
            float start = arrow.style.rotate.value.angle.value;
            float end = expanded ? 90f : 0f;
            const float duration = 0.12f;

            if (st.anim != null)
            {
                st.anim.Pause();
                st.anim = null;
            }

            float t = 0f;

            st.anim = arrow.schedule.Execute(() =>
            {
                t += 1f / 60f;
                float progress = Mathf.Clamp01(t / duration);

                float angle = Mathf.Lerp(start, end, progress);
                arrow.style.rotate = new Rotate(new Angle(angle));

                if (progress >= 1f)
                {
                    st.anim?.Pause();
                    st.anim = null;
                }
            }).Every(0);
        }


        void InstallEmptySpaceContextMenu()
        {
            var sv = this.Q<ScrollView>();
            if (sv == null)
                return;

            var viewport = sv.Q<VisualElement>("unity-content-viewport")
                           ?? sv.contentContainer?.parent;

            if (viewport == null)
                return;

            viewport.pickingMode = PickingMode.Position;

            viewport.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.RightMouse)
                    return;

                evt.StopPropagation();

                OnContextMenu?.Invoke(
                    evt,
                    new TreeViewItem
                    {
                        name = "Root",
                        sceneId = 0,
                        entityId = 0
                    }
                );
            });
        }


        void OnSelectionChangedInternal(IEnumerable<object> selected)
        {
            foreach (var obj in selected)
            {
                if (obj is TreeViewItem item)
                {
                    OnItemSelected?.Invoke(item);
                    break; // single selection
                }
            }
        }


        readonly HashSet<int> _expandedCache = new HashSet<int>();
        float _scrollY = 0f;
        void RestoreExpandedAfterReload()
        {
            foreach (var id in _expandedCache)
                ExpandItem(id);
        }

        void SaveScrollPosition()
        {
            var sv = GetScrollView();
            if (sv != null)
                _scrollY = sv.scrollOffset.y;
        }

        void RestoreScrollPosition()
        {
            var sv = GetScrollView();
            if (sv != null)
                sv.scrollOffset = new Vector2(0, _scrollY);
        }

        ScrollView GetScrollView()
        {
            // Unity 2022+ / Unity 6
            return this.Q<ScrollView>();
        }

        public void Reload()
        {
            bool isSearch = !string.IsNullOrEmpty(SearchText);

            // --- SAVE STATES ---
            SaveScrollPosition();

            if (!isSearch)
                SyncExpandedFromCurrentTree(); // ton cache expand

            // --- REBUILD DATA ---
            var root = BuildRoot();
            var children = GetChildren(root, SearchText);

            if (isSearch)
                children = PrepareFlatNodes(children);

            SetRootItems(BuildTree(children));
            Rebuild();

            // --- RESTORE STATES ---
            if (!isSearch)
                RestoreExpandedAfterReload();

            RestoreScrollPosition();
        }

        void SyncExpandedFromCurrentTree()
        {
            _expandedCache.Clear();

            // itemsCount n'est pas public partout => on le récupère via viewController si dispo,
            // sinon on skip (le cache restera au dernier état user, ce qui marche déjà très bien).
            var vc = viewController;
            if (vc == null) return;

            int count = vc.GetItemsCount();
            for (int i = 0; i < count; i++)
            {
                int id = GetIdForIndex(i);
                if (IsExpanded(id))
                    _expandedCache.Add(id);
            }
        }


        public List<TreeViewItem> GetChildren(TreeViewItem root, string search)
        {
            if (string.IsNullOrEmpty(search))
                return root.children;

            var result = new List<TreeViewItem>();

            string lowered = search.ToLowerInvariant();

            FlattenAndFilter(root, lowered, result);

            return result;
        }

        void FlattenAndFilter(TreeViewItem node, string search, List<TreeViewItem> result)
        {
            foreach (var child in node.children)
            {
                if (child.name != null &&
                    child.name.ToLowerInvariant().Contains(search))
                {
                    result.Add(child);
                }

                if (child.children != null && child.children.Count > 0)
                    FlattenAndFilter(child, search, result);
            }
        }

        protected virtual List<TreeViewItem> PrepareFlatNodes(List<TreeViewItem> nodes)
        {
            var flat = new List<TreeViewItem>();

            foreach (var n in nodes)
            {
                flat.Add(new TreeViewItem
                {
                    name = n.name,
                    icon = n.icon,
                    sceneId = n.sceneId,
                    entityId = n.entityId,
                    children = new List<TreeViewItem>()
                });
            }

            return flat;
        }

        public virtual TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                name = "Root",
                icon = IconType.Scene
            };

            if (tab == Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Scene)
            {
                BuildSceneTree(root);
            }
            else if (tab == Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Assets)
            {
                BuildSceneAssetsTree(root);
            }


            return root;
        }

        private void BuildSceneAssetsTree(TreeViewItem root)
        {
            var allObjects = VIServerRuntime.AllObjects;
            if (allObjects == null || allObjects.Count == 0)
                return;

            // Collect only non-GameObject entities (assets from scene manifests)
            List<VIServerVirtualObject> flatAssets = new List<VIServerVirtualObject>();

            foreach (var vobj in allObjects.Values)
            {
                if (vobj is VIServerVirtualGameObject)
                    continue;

                flatAssets.Add(vobj);
            }

            if (flatAssets.Count == 0)
                return;

            // Sort alphabetically by display name
            flatAssets.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            // Add every asset at depth 0
            foreach (var vobj in flatAssets)
            {

                root.children.Add(new TreeViewItem
                {
                    entityId = vobj.EntityId,
                    name = vobj.Name,
                    icon = IconType.GameObject
                });
            }
        }


        void BuildSceneTree(TreeViewItem root)
        {
            var goMap = VIServerRuntime.S_Map;
            if (goMap == null || goMap.Count == 0)
                return;


            // ---------- Scene lookup ----------
            var sceneLookup = new Dictionary<ulong, VIProjectPacket.SceneData>();
            var projectScenes = VIServerRuntime.Project.scenes ?? Array.Empty<VIProjectPacket.SceneData>();
            foreach (var s in projectScenes)
                sceneLookup[s.id] = s;

            var presentSceneIds = goMap.Values
                .Select(v => v.SceneId)
                .Where(id => id != 0UL)
                .Distinct()
                .ToList();

            if (presentSceneIds.Count == 0)
                return;

            // ---------- Scene ordering ----------
            presentSceneIds.Sort((a, b) =>
            {
                string GetSceneName(ulong id)
                {
                    if (sceneLookup.TryGetValue(id, out var sd))
                    {
                        if (!string.IsNullOrEmpty(sd.name)) return sd.name;
                        if (!string.IsNullOrEmpty(sd.path)) return sd.path;
                    }
                    return $"Scene {id}";
                }

                return string.Compare(GetSceneName(a), GetSceneName(b), StringComparison.OrdinalIgnoreCase);
            });

            foreach (var sceneId in presentSceneIds)
            {
                // ---------- Objects in scene ----------
                var vgosInSceneList = goMap.Values
                    .Where(v => v.SceneId == sceneId)
                    .ToList();

                if (vgosInSceneList.Count == 0)
                    continue;

                var vgosInScene = vgosInSceneList.ToDictionary(v => v.EntityId, v => v);

                // ---------- Scene label ----------
                string sceneLabel;
                if (sceneLookup.TryGetValue(sceneId, out var sd))
                {
                    sceneLabel = !string.IsNullOrEmpty(sd.name)
                        ? sd.name
                        : !string.IsNullOrEmpty(sd.path)
                            ? sd.path
                            : $"Scene {sceneId}";
                }
                else
                {
                    sceneLabel = $"Scene {sceneId}";
                }

                var sceneNode = new TreeViewItem
                {
                    name = sceneLabel,
                    icon = IconType.Scene,
                    sceneId = sd.id,
                    entityId = 0

                };

                // ---------- Children map ----------
                var childrenMap = new Dictionary<ulong, List<ulong>>();
                var parentCandidateIds = vgosInScene.Keys.ToHashSet();

                foreach (var vgo in vgosInSceneList)
                {
                    ulong parentId =
                        (vgo.HasParent && parentCandidateIds.Contains(vgo.ParentId))
                            ? vgo.ParentId
                            : 0UL;

                    if (!childrenMap.TryGetValue(parentId, out var list))
                        childrenMap[parentId] = list = new List<ulong>();

                    list.Add(vgo.EntityId);
                }

                // ---------- Sort by sibling index ----------
                foreach (var kv in childrenMap)
                {
                    kv.Value.Sort((a, b) =>
                    {
                        vgosInScene.TryGetValue(a, out var va);
                        vgosInScene.TryGetValue(b, out var vb);

                        int ia = va != null ? va.SiblingIndex : int.MaxValue;
                        int ib = vb != null ? vb.SiblingIndex : int.MaxValue;

                        return ia.CompareTo(ib);
                    });
                }

                // ---------- Roots ----------
                var childIds = new HashSet<ulong>(
                    childrenMap.Where(kv => kv.Key != 0UL).SelectMany(kv => kv.Value)
                );

                var roots = vgosInSceneList
                    .Select(v => v.EntityId)
                    .Where(id => !childIds.Contains(id))
                    .ToList();

                roots.Sort((a, b) =>
                {
                    vgosInScene.TryGetValue(a, out var va);
                    vgosInScene.TryGetValue(b, out var vb);

                    int ia = va != null ? va.SiblingIndex : int.MaxValue;
                    int ib = vb != null ? vb.SiblingIndex : int.MaxValue;

                    return ia.CompareTo(ib);
                });

                // ---------- Recursive build ----------
                foreach (var rootEntity in roots)
                    AddNodeRecursive(sceneNode, rootEntity, vgosInScene, childrenMap);

                if (sceneNode.children.Count > 0)
                    root.children.Add(sceneNode);
            }
        }

        void AddNodeRecursive(
        TreeViewItem parentNode,
        ulong entityId,
        Dictionary<ulong, VIServerVirtualGameObject> vgos,
        Dictionary<ulong, List<ulong>> childrenMap
    )
        {
            if (!vgos.TryGetValue(entityId, out var vgo))
                return;

            string name = string.IsNullOrEmpty(vgo.Name)
                ? entityId.ToString()
                : vgo.Name;

            var node = new TreeViewItem
            {
                name = name,
                icon = IconType.GameObject,
                sceneId = vgo.SceneId,
                entityId = vgo.EntityId

            };

            parentNode.children.Add(node);

            if (childrenMap.TryGetValue(entityId, out var children))
            {
                foreach (var child in children)
                    AddNodeRecursive(node, child, vgos, childrenMap);
            }
        }


        private TreeViewItemData<TreeViewItem> BuildNode(TreeViewItem item, ref int id)
        {
            var children = new List<TreeViewItemData<TreeViewItem>>();

            foreach (var c in item.children)
                children.Add(BuildNode(c, ref id));

            int idForTree = GetItemId(item);
            return new TreeViewItemData<TreeViewItem>(idForTree, item, children);
        }

        protected virtual int GetItemId(TreeViewItem item)
        {
            return (int)(item.IsScene ? item.sceneId : item.entityId);
        }

        IList<TreeViewItemData<TreeViewItem>> BuildTree(List<TreeViewItem> items)
        {
            var result = new List<TreeViewItemData<TreeViewItem>>();
            int id = 1;

            foreach (var item in items)
                result.Add(BuildNode(item, ref id));

            return result;
        }


        float GetCurrentAngle(VisualElement arrow)
        {
            // resolvedStyle.rotate.angle.value returns 0 unless set earlier.
            var rot = arrow.style.rotate;
            if (rot.keyword == StyleKeyword.Undefined || rot.keyword == StyleKeyword.Auto)
                return 0f;

            return rot.value.angle.value;
        }

        void AnimateArrowRotation(VisualElement arrow, bool expanded)
        {
            float start = arrow.style.rotate.value.angle.value;
            float end = expanded ? 90f : 0f;
            const float duration = 0.12f;

            // --- STOP PREVIOUS ANIMATION ---
            if (arrow.userData is IVisualElementScheduledItem running)
                running.Pause(); // stop the old tween

            float t = 0f;


            // create new animation
            var anim = arrow.schedule.Execute(() =>
            {
                t += 1f / 60f;
                // approx 60fps
                float progress = Mathf.Clamp01(t / duration);

                float angle = Mathf.Lerp(start, end, progress);
                Debug.Log(angle);
                arrow.style.rotate = new Rotate(new Angle(angle));

                if (progress >= 1f)
                {

                    IVisualElementScheduledItem a = arrow.userData as IVisualElementScheduledItem;

                    a.Pause();       // stop animation
                    arrow.userData = null; // clear reference
                }

            }).Every(0);

            // store this animation so we can stop it next time
            arrow.userData = anim;
        }




        // ---------- DEPTH UTILITY ----------
        int GetDepthForIndex(int index)
        {
            int id = GetIdForIndex(index);
            int depth = 0;

            int parent = viewController.GetParentId(id);
            while (parent != -1)
            {
                depth++;
                parent = viewController.GetParentId(parent);
            }

            return depth;
        }

        Texture2D GetArrowIcon(bool expanded)
        {
            var custom = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/VirtualInspector/Server/Scripts/Editor/UI/arrow.png"
            );

            if (custom != null)
                return custom; 

            var tex = EditorGUIUtility.IconContent("ArrowNavigationRight")?.image as Texture2D;

            if (tex != null)
                return tex;

            return Texture2D.whiteTexture;
        }


        // ---------- ICONS ----------
        Texture2D GetIcon(IconType type, string otherIcon)
        {
            string unityIcon = type switch
            {
                IconType.Scene => "SceneAsset Icon",
                IconType.Folder => "Folder Icon",
                IconType.GameObject => "GameObject Icon",
                _ => "DefaultAsset Icon"
            };

            if (string.IsNullOrEmpty(otherIcon))
                return EditorGUIUtility.IconContent(unityIcon).image as Texture2D;
            else
            {
                GUIContent content = EditorGUIUtility.IconContent(otherIcon);
                if (content != null && content.image != null)
                    return content.image as Texture2D;
                else
                    return EditorGUIUtility.IconContent(unityIcon).image as Texture2D;
            }
        }

        public enum IconType { Scene, Folder, GameObject }

        void EnsureContextMenuHook(VisualElement ve)
        {

            ve.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.RightMouse)
                    return;

                evt.StopPropagation();

                if (ve.userData is TreeViewItem item)
                {
                    OnContextMenu?.Invoke(evt, item);
                }
            });
        }

    }
#else
        // LEGACY fallback for Unity 2021 where UIElements.TreeView is internal.
    public class VITreeView : VisualElement
    {
        public string SearchText { get; set; }

        public event Action<TreeViewItem> OnItemSelected;
        public Action<MouseDownEvent, TreeViewItem> OnContextMenu;
        public event Action<TreeViewItem> OnItemDoubleClicked;

        public Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab tab { get; set; } = Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Scene;

    readonly List<TreeViewItem> _flat = new();                  // index -> item
    readonly Dictionary<int, TreeViewItem> _byId = new();       // id -> item
    readonly Dictionary<int, int> _indexById = new();

    public class TreeViewItem
        {
            public int id;
            public string name;
            public IconType icon;
            public string otherIcon = string.Empty;
            public List<TreeViewItem> children = new();
            public VICommonComponentNode ComponentNode { get; set; }

            public ulong sceneId;
            public ulong entityId;
            public ulong compId;

            public bool IsScene => entityId == 0 && sceneId != 0;
            public bool IsEntity => entityId != 0;
        }

        // ------------------------------------------------------------
        // Internal state
        // ------------------------------------------------------------

        readonly ScrollView _scroll;
        readonly Dictionary<int, bool> _expanded = new();   // itemId -> expanded
        int _selectedItemId = 0;

        // cached UI rows (id -> row root)
        readonly Dictionary<int, VisualElement> _rowById = new();

        // optional item template (same uxml as your modern TreeView item)
        readonly VisualTreeAsset _itemTemplate;

        // ------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------

        public VITreeView()
        {
            name = "HierarchyTree";
            AddToClassList("hierarchy");

            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            _itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/VirtualInspector/Server/Scripts/Editor/UI//VIHierarchyItem.uxml"
            );

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1;
            Add(_scroll);

            // Context menu on empty space (the scroll viewport)
            RegisterEmptySpaceContextMenu();
        }

        // ------------------------------------------------------------
        // Public API (same name as modern)
        // ------------------------------------------------------------

        public void Reload()
        {
            var root = BuildRoot();

            var children = GetChildren(root, SearchText);

            if (!string.IsNullOrEmpty(SearchText))
                children = PrepareFlatNodes(children);

            // Render
            RebuildUI(children);
        }

    // ======================================================
    // COMPATIBILITY PATCH (IMGUI → UI Toolkit)
    // ======================================================

    // Ancien code attendait un seul index sélectionné

    // ======================================================
    // Compatibility API (so external code doesn't change)
    // ======================================================

    public int selectedIndex
    {
        get
        {
            if (_selectedItemId == 0) return -1;
            return _indexById.TryGetValue(_selectedItemId, out var idx) ? idx : -1;
        }
    }

    // Optional: keep same name as UITK TreeView
    public List<int> selectedIndices
    {
        get
        {
            int idx = selectedIndex;
            return idx >= 0 ? new List<int> { idx } : new List<int>();
        }
    }

    public int GetIdForIndex(int index)
    {
        if (index < 0 || index >= _flat.Count) return 0;
        return GetItemId(_flat[index]);
    }

    public T GetItemDataForIndex<T>(int index)
    {
        if (index < 0 || index >= _flat.Count)
            return default;

        object obj = _flat[index];

        // In practice VIServerObjectSelectorTreeView uses TreeViewItem
        if (obj is T t)
            return t;

        return default;
    }

    public int GetIndexForId(int id)
    {
        return _indexById.TryGetValue(id, out var idx) ? idx : -1;
    }


    public void Clear()
        {
            _scroll.Clear();
            _rowById.Clear();
            _expanded.Clear();
            _selectedItemId = 0;
        }

        // ------------------------------------------------------------
        // Virtual hooks (must stay to keep overrides compiling)
        // ------------------------------------------------------------

        public virtual TreeViewItem BuildRoot()
        {
            // Default same as your modern implementation
            var root = new TreeViewItem { name = "Root", icon = IconType.Scene };

            if (tab == Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Scene) BuildSceneTree(root);
            else if (tab == Strix.VirtualInspector.Editor.UI.VIServerHierarchyModule.Tab.Assets) BuildSceneAssetsTree(root);

            return root;
        }

        protected virtual List<TreeViewItem> PrepareFlatNodes(List<TreeViewItem> nodes)
        {
            // Same as your modern PrepareFlatNodes
            var flat = new List<TreeViewItem>();
            foreach (var n in nodes)
            {
                flat.Add(new TreeViewItem
                {
                    name = n.name,
                    icon = n.icon,
                    otherIcon = n.otherIcon,
                    sceneId = n.sceneId,
                    entityId = n.entityId,
                    compId = n.compId,
                    ComponentNode = n.ComponentNode,
                    children = new List<TreeViewItem>()
                });
            }
            return flat;
        }

        protected virtual int GetItemId(TreeViewItem item)
        {
            // Same logic as your modern GetItemId
            return (int)(item.IsScene ? item.sceneId : item.entityId);
        }

        // ------------------------------------------------------------
        // Search behavior (same as modern)
        // ------------------------------------------------------------

        public List<TreeViewItem> GetChildren(TreeViewItem root, string search)
        {
            if (string.IsNullOrEmpty(search))
                return root.children;

            var result = new List<TreeViewItem>();
            string lowered = search.ToLowerInvariant();
            FlattenAndFilter(root, lowered, result);
            return result;
        }

        void FlattenAndFilter(TreeViewItem node, string search, List<TreeViewItem> result)
        {
            foreach (var child in node.children)
            {
                if (!string.IsNullOrEmpty(child.name) &&
                    child.name.ToLowerInvariant().Contains(search))
                {
                    result.Add(child);
                }

                if (child.children != null && child.children.Count > 0)
                    FlattenAndFilter(child, search, result);
            }
        }

    // ------------------------------------------------------------
    // UI build (VisualElement rows)
    // ------------------------------------------------------------

        void RebuildUI(List<TreeViewItem> roots)
        {
            _scroll.Clear();
            _rowById.Clear();

            _flat.Clear();
            _byId.Clear();
            _indexById.Clear();

            foreach (var item in roots)
                AddRowRecursive(item, depth: 0);
        }

        void AddRowRecursive(TreeViewItem item, int depth)
        {
            int itemId = GetItemId(item);

            // cache index <-> item
            int index = _flat.Count;
            _flat.Add(item);
            _byId[itemId] = item;
            _indexById[itemId] = index;

            bool hasChildren = item.children != null && item.children.Count > 0;
            bool expanded = hasChildren && GetExpanded(itemId, defaultValue: false);

            var row = CreateRow(item, itemId, depth, hasChildren, expanded);
            _scroll.Add(row);
            _rowById[itemId] = row;

            if (!hasChildren || !expanded)
                return;

            foreach (var child in item.children)
                AddRowRecursive(child, depth + 1);
        }


        VisualElement CreateRow(TreeViewItem item, int itemId, int depth, bool hasChildren, bool expanded)
        {
            VisualElement ve;

            if (_itemTemplate != null)
            {
                ve = _itemTemplate.CloneTree();
            }
            else
            {
                // hard fallback if uxml missing
                ve = new VisualElement();
                ve.style.flexDirection = FlexDirection.Row;
                ve.style.height = 22;
                var indent = new VisualElement { name = "indent" };
                var arrow = new VisualElement { name = "arrow" };
                var icon = new VisualElement { name = "icon" };
                var label = new Label { name = "label" };
                ve.Add(indent); ve.Add(arrow); ve.Add(icon); ve.Add(label);
            }

            var container = ve.ElementAt(0); // your template root container
            container.style.flexDirection = FlexDirection.Row;

            var indentVe = container.Q<VisualElement>("indent");
            var arrowVe = container.Q<VisualElement>("arrow");
            var iconVe = container.Q<VisualElement>("icon");
            var labelVe = container.Q<Label>("label");

            iconVe.style.flexShrink = 0;
            iconVe.style.flexGrow = 0;
            iconVe.style.width = 16;
            iconVe.style.height = 16;

            arrowVe.style.flexShrink = 0;
            arrowVe.style.width = 12;

            labelVe.style.flexGrow = 1;
            labelVe.style.flexShrink = 1;
            labelVe.style.minWidth = 0; // ⭐ CRUCIAL
            labelVe.style.whiteSpace = WhiteSpace.NoWrap;
            labelVe.style.overflow = Overflow.Hidden;
            labelVe.style.textOverflow = TextOverflow.Ellipsis;

            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.minWidth = 0;

            if (indentVe != null)
                indentVe.style.marginLeft = depth * 8;

            if (labelVe != null)
                labelVe.text = item.name ?? string.Empty;

            if (iconVe != null)
                iconVe.style.backgroundImage = new StyleBackground(GetIcon(item.icon, item.otherIcon));

            // selection styling: you already do it with USS in modern; here keep basic state
            bool isSelected = (itemId == _selectedItemId);
            if (isSelected)
            {
                if (labelVe != null) labelVe.style.color = Color.white;
                if (iconVe != null) iconVe.style.opacity = 1f;
                container.AddToClassList("unity-tree-view__item--selected"); // optional: your USS may target this
            }
            else
            {
                if (labelVe != null) labelVe.style.color = new Color(0.62f, 0.62f, 0.62f);
                if (iconVe != null) iconVe.style.opacity = 0.8f;
                container.RemoveFromClassList("unity-tree-view__item--selected");
            }

            // arrow
            if (arrowVe != null)
            {
                arrowVe.pickingMode = PickingMode.Position;
                if (hasChildren)
                {
                    arrowVe.style.display = DisplayStyle.Flex;
                    arrowVe.style.backgroundImage = new StyleBackground(GetArrowIcon(expanded));
                    arrowVe.style.rotate = expanded
                        ? new Rotate(new Angle(90f))
                        : new Rotate(new Angle(0f));
                }
                else
                {
                    arrowVe.style.display = DisplayStyle.None;
                    arrowVe.style.backgroundImage = new StyleBackground();
                }

                // Left click on arrow toggles expand/collapse (only if has children)
                arrowVe.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button != (int)MouseButton.LeftMouse)
                        return;

                    if (item.children == null || item.children.Count == 0)
                        return;

                    evt.StopImmediatePropagation();
                    evt.PreventDefault();

                    bool nowExpanded = !GetExpanded(itemId, defaultValue: false);
                    SetExpanded(itemId, nowExpanded);

                    // IMPORTANT: rebuild next frame
                    schedule.Execute(Reload);
                });
            }

            // click / double-click / right-click on row
            container.pickingMode = PickingMode.Position;
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Double click (left)
                if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
                {
                    _selectedItemId = itemId;
                    OnItemDoubleClicked?.Invoke(item);
                    Reload();
                    evt.StopPropagation();
                    return;
                }

                // Left = select
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    _selectedItemId = itemId;
                    OnItemSelected?.Invoke(item);
                    Reload();
                    evt.StopPropagation();
                    return;
                }

                // Right = select + context menu
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    _selectedItemId = itemId;
                    Reload(); // so selection visuals update before menu

                    OnContextMenu?.Invoke(evt, item);
                    evt.StopPropagation();
                    return;
                }
            });

            return ve;
        }

        bool GetExpanded(int itemId, bool defaultValue)
        {
            if (_expanded.TryGetValue(itemId, out var v)) return v;
            return defaultValue;
        }

        void SetExpanded(int itemId, bool expanded)
        {
            _expanded[itemId] = expanded;
        }

        void RegisterEmptySpaceContextMenu()
        {
            // ScrollView internals vary; simplest: listen on ScrollView itself + its viewport if found.
            _scroll.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.RightMouse)
                    return;

                // only when clicking empty space (not a row)
                if (evt.target is VisualElement target && target != null)
                {
                    // If right click is on a row/container, it will be stopped by row callback.
                    OnContextMenu?.Invoke(evt, new TreeViewItem { name = "Root", sceneId = 0, entityId = 0 });
                    evt.StopPropagation();
                }
            });

            _scroll.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
              //  evt.menu.ClearItems();
                OnContextMenu?.Invoke(new MouseDownEvent(), new TreeViewItem { name = "Root", sceneId = 0, entityId = 0 });
            });
        }

        // ------------------------------------------------------------
        // Data builders (same code as your modern build)
        // ------------------------------------------------------------

        void BuildSceneAssetsTree(TreeViewItem root)
        {
            var allObjects = VIServerRuntime.AllObjects;
            if (allObjects == null || allObjects.Count == 0)
                return;

            List<VIServerVirtualObject> flatAssets = new List<VIServerVirtualObject>();

            foreach (var vobj in allObjects.Values)
            {
                if (vobj is VIServerVirtualGameObject)
                    continue;

                flatAssets.Add(vobj);
            }

            if (flatAssets.Count == 0)
                return;

            flatAssets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            foreach (var vobj in flatAssets)
            {
                root.children.Add(new TreeViewItem
                {
                    entityId = vobj.EntityId,
                    name = vobj.Name,
                    icon = IconType.GameObject
                });
            }
        }

        void BuildSceneTree(TreeViewItem root)
        {
            var goMap = VIServerRuntime.S_Map;
            if (goMap == null || goMap.Count == 0)
                return;

            var sceneLookup = new Dictionary<ulong, VIProjectPacket.SceneData>();
            var projectScenes = VIServerRuntime.Project.scenes ?? Array.Empty<VIProjectPacket.SceneData>();
            foreach (var s in projectScenes)
                sceneLookup[s.id] = s;

            var presentSceneIds = goMap.Values
                .Select(v => v.SceneId)
                .Where(id => id != 0UL)
                .Distinct()
                .ToList();

            if (presentSceneIds.Count == 0)
                return;

            presentSceneIds.Sort((a, b) =>
            {
                string GetSceneName(ulong id)
                {
                    if (sceneLookup.TryGetValue(id, out var sd))
                    {
                        if (!string.IsNullOrEmpty(sd.name)) return sd.name;
                        if (!string.IsNullOrEmpty(sd.path)) return sd.path;
                    }
                    return $"Scene {id}";
                }
                return string.Compare(GetSceneName(a), GetSceneName(b), StringComparison.OrdinalIgnoreCase);
            });

            foreach (var sceneId in presentSceneIds)
            {
                var vgosInSceneList = goMap.Values
                    .Where(v => v.SceneId == sceneId)
                    .ToList();

                if (vgosInSceneList.Count == 0)
                    continue;

                var vgosInScene = vgosInSceneList.ToDictionary(v => v.EntityId, v => v);

                string sceneLabel;
                ulong sdId = sceneId;
                if (sceneLookup.TryGetValue(sceneId, out var sd))
                {
                    sdId = sd.id;
                    sceneLabel = !string.IsNullOrEmpty(sd.name)
                        ? sd.name
                        : !string.IsNullOrEmpty(sd.path)
                            ? sd.path
                            : $"Scene {sceneId}";
                }
                else
                {
                    sceneLabel = $"Scene {sceneId}";
                }

                var sceneNode = new TreeViewItem
                {
                    name = sceneLabel,
                    icon = IconType.Scene,
                    sceneId = sdId,
                    entityId = 0
                };

                var childrenMap = new Dictionary<ulong, List<ulong>>();
                var parentCandidateIds = vgosInScene.Keys.ToHashSet();

                foreach (var vgo in vgosInSceneList)
                {
                    ulong parentId =
                        (vgo.HasParent && parentCandidateIds.Contains(vgo.ParentId))
                            ? vgo.ParentId
                            : 0UL;

                    if (!childrenMap.TryGetValue(parentId, out var list))
                        childrenMap[parentId] = list = new List<ulong>();

                    list.Add(vgo.EntityId);
                }

                foreach (var kv in childrenMap)
                {
                    kv.Value.Sort((a, b) =>
                    {
                        vgosInScene.TryGetValue(a, out var va);
                        vgosInScene.TryGetValue(b, out var vb);

                        int ia = va != null ? va.SiblingIndex : int.MaxValue;
                        int ib = vb != null ? vb.SiblingIndex : int.MaxValue;

                        return ia.CompareTo(ib);
                    });
                }

                var childIds = new HashSet<ulong>(
                    childrenMap.Where(kv => kv.Key != 0UL).SelectMany(kv => kv.Value)
                );

                var roots = vgosInSceneList
                    .Select(v => v.EntityId)
                    .Where(id => !childIds.Contains(id))
                    .ToList();

                roots.Sort((a, b) =>
                {
                    vgosInScene.TryGetValue(a, out var va);
                    vgosInScene.TryGetValue(b, out var vb);

                    int ia = va != null ? va.SiblingIndex : int.MaxValue;
                    int ib = vb != null ? vb.SiblingIndex : int.MaxValue;

                    return ia.CompareTo(ib);
                });

                foreach (var rootEntity in roots)
                    AddNodeRecursive(sceneNode, rootEntity, vgosInScene, childrenMap);

                if (sceneNode.children.Count > 0)
                    root.children.Add(sceneNode);
            }
        }

        void AddNodeRecursive(
            TreeViewItem parentNode,
            ulong entityId,
            Dictionary<ulong, VIServerVirtualGameObject> vgos,
            Dictionary<ulong, List<ulong>> childrenMap
        )
        {
            if (!vgos.TryGetValue(entityId, out var vgo))
                return;

            string name = string.IsNullOrEmpty(vgo.Name) ? entityId.ToString() : vgo.Name;

            var node = new TreeViewItem
            {
                name = name,
                icon = IconType.GameObject,
                sceneId = vgo.SceneId,
                entityId = vgo.EntityId
            };

            parentNode.children.Add(node);

            if (childrenMap.TryGetValue(entityId, out var children))
            {
                foreach (var child in children)
                    AddNodeRecursive(node, child, vgos, childrenMap);
            }
        }

        // ------------------------------------------------------------
        // Icons (same as your modern code)
        // ------------------------------------------------------------

        Texture2D GetArrowIcon(bool expanded)
        {
            var custom = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/VirtualInspector/Server/Scripts/Editor/UI/arrow.png"
            );

            if (custom != null)
                return custom;

            var tex = EditorGUIUtility.IconContent("ArrowNavigationRight")?.image as Texture2D;
            if (tex != null) return tex;

            return Texture2D.whiteTexture;
        }

        Texture2D GetIcon(IconType type, string otherIcon)
        {
            string unityIcon = type switch
            {
                IconType.Scene => "SceneAsset Icon",
                IconType.Folder => "Folder Icon",
                IconType.GameObject => "GameObject Icon",
                _ => "DefaultAsset Icon"
            };

            if (string.IsNullOrEmpty(otherIcon))
                return EditorGUIUtility.IconContent(unityIcon).image as Texture2D;

            GUIContent content = EditorGUIUtility.IconContent(otherIcon);
            if (content != null && content.image != null)
                return content.image as Texture2D;

            return EditorGUIUtility.IconContent(unityIcon).image as Texture2D;
        }

        public enum IconType { Scene, Folder, GameObject }


    }
#endif
}

