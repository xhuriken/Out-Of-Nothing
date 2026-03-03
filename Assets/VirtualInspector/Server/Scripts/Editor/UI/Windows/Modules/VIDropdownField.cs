using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Strix.VirtualInspector.Editor.UI
{
    public class VIDropdownField : VisualElement
    {
        static VIDropdownField _openedDropdown;

        // ---------- Static (icon cache) ----------

        static Texture2D ArrowIcon;

        static Texture2D GetArrowIcon()
        {
            if (ArrowIcon == null)
            {
                ArrowIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/VirtualInspector/Server/Scripts/Editor/UI/arrow.png"
                );
            }
            return ArrowIcon;
        }

        // ---------- State ----------

        readonly IReadOnlyList<string> _choices;
        public Action<string> _onValueChanged;

        string _value;
        bool _isOpen;

        Label _label;
        Image _arrow;

        public string value => _value;

        // ---------- Constructor ----------

        public VIDropdownField(
            IReadOnlyList<string> choices,
            string defaultValue,
            Action<string> onValueChanged
        )
        {
            _choices = choices;
            _onValueChanged = onValueChanged;
            _value = defaultValue;

            // Root style 
            AddToClassList("dropdown-field");
            style.height = 22;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.paddingLeft = 8;
            style.paddingRight = 6;

            // Label 
            _label = new Label(_value);
            _label.AddToClassList("dropdown-label");
            _label.style.flexGrow = 1;
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;

            // Arrow icon
            _arrow = new Image();
            _arrow.image = GetArrowIcon();
            _arrow.AddToClassList("dropdown-arrow");

            _arrow.style.rotate = new Rotate(90);

            Add(_label);
            Add(_arrow);

            // Interaction
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        // ---------- Interaction ----------

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            evt.StopImmediatePropagation();

            if (_openedDropdown == this && _isOpen)
            {
                ClosePopup();
                _openedDropdown = null;
                return;
            }

            if (_openedDropdown != null && _openedDropdown != this)
            {
                _openedDropdown.ClosePopup();
                _openedDropdown = null;
            }

            Vector2 panelPos = new Vector2(
                worldBound.xMin,
                worldBound.yMax
            );

            Vector2 guiPos = panelPos - panel.visualTree.worldBound.position;

         //   Vector2 screenPos = GUIUtility.GUIToScreenPoint(guiPos);
            Vector2 screenPos =
           GUIUtility.GUIToScreenPoint(evt.originalMousePosition);

           // EditorApplication.delayCall += () =>
            {
           //     ShowPopupAt(screenPos);
            };

            // 🔹 Anchor rect EN COORDONNÉES GUI (pas screen)
            Rect anchor = worldBound;

            UnityEditor.PopupWindow.Show(anchor, new VIDropdownPopup(_choices, _value, SetValue));

        }


        void ShowPopupAt(Vector2 screenPos)
        {
            _isOpen = true;
            _openedDropdown = this;
            UpdateArrowRotation();

            VIHierarchyContextMenuWindow.ShowAtScreen(
                screenPos,
                build: menu =>
                {
                    foreach (var c in _choices)
                    {
                        string label = (c == _value) ? $"✓  {c}" : c;

                        menu.AddItem(label, () =>
                        {
                            SetValue(c);
                        });
                    }
                },
                onClose: () =>
                {
                    _isOpen = false;
                    UpdateArrowRotation();

                    if (_openedDropdown == this)
                        _openedDropdown = null;
                }
            );
        }


        void ClosePopup()
        {
            _isOpen = false;
            UpdateArrowRotation();
        }

        // ---------- Public API (Unity-like) ----------

        public void SetValueWithoutNotify(string newValue)
        {
            if (_value == newValue)
                return;

            _value = newValue;
            _label.text = _value;

            _isOpen = false;
            UpdateArrowRotation();

            MarkDirtyRepaint();
        }

        public void SetValue(string newValue)
        {
            if (_value == newValue)
                return;

            _value = newValue;
            _label.text = _value;

            _isOpen = false;
            UpdateArrowRotation();

            _onValueChanged?.Invoke(_value);
            MarkDirtyRepaint();
        }

        // ---------- Visual ----------

        void UpdateArrowRotation()
        {
            _arrow.style.rotate = _isOpen
                ? new Rotate(270)
                : new Rotate(90);
        }

    }

    class VIDropdownPopup : PopupWindowContent
    {
        const float Width = 220f;
        const float ItemHeight = 22f;
        const float Padding = 6f;
        const float MaxHeight = 300f;
        const float ScrollbarWidth = 16f;

        readonly IReadOnlyList<string> _choices;
        readonly string _current;
        readonly Action<string> _onSelect;
        float _maxHeight = MaxHeight;

        Vector2 _scroll;

        public VIDropdownPopup(
            IReadOnlyList<string> choices,
            string current,
            Action<string> onSelect)
        {
            _choices = choices;
            _current = current;
            _onSelect = onSelect;
        }

        public override void OnOpen()
        {
            // Position écran du popup
            Vector2 screenPos = editorWindow.position.position;

            // Même logique que ton ancien popup
            Rect workArea = WorkAreaUtility.GetWorkAreaAt(screenPos);

            _maxHeight = workArea.height * 0.7f;
        }

        // ------------------------------------------------------------
        // WINDOW SIZE
        // ------------------------------------------------------------

        public override Vector2 GetWindowSize()
        {
            float contentHeight = GetContentHeight();

            bool needsScroll = contentHeight > _maxHeight;

            float height = needsScroll
                ? _maxHeight
                : contentHeight + 1f; // +1px anti scrollbar fantôme

            return new Vector2(Width, height);
        }

        float GetContentHeight()
        {
            return _choices.Count * ItemHeight + Padding * 2f;
        }

        // ------------------------------------------------------------
        // GUI
        // ------------------------------------------------------------

        public override void OnGUI(Rect rect)
        {
            Event e = Event.current;

            float contentHeight = GetContentHeight();

            // Inner rect calc d'abord
            Rect inner = new Rect(1, 1, rect.width - 2, rect.height - 2);

            bool useScroll = contentHeight > _maxHeight;

            // 🔑 GESTION SCROLLBAR CORRECTE
            var oldScrollbar = GUI.skin.verticalScrollbar;
            if (!useScroll)
                GUI.skin.verticalScrollbar = GUIStyle.none;

            try
            {
                // ===== Window border =====
                GUI.BeginGroup(rect, GUIContent.none, VIContextIMGUIStyles.WindowBorder);
                GUI.BeginGroup(inner, GUIContent.none, VIContextIMGUIStyles.Background);

                float y = Padding;

                if (!useScroll)
                {
                    // ---------- NO SCROLL ----------
                    for (int i = 0; i < _choices.Count; i++)
                        DrawItem(_choices[i], ref y, inner.width, e);
                }
                else
                {
                    // ---------- SCROLL ----------
                    Rect viewRect = new Rect(0, 0, inner.width, inner.height);
                    Rect contentRect = new Rect(
                        0,
                        0,
                        inner.width - ScrollbarWidth,
                        contentHeight
                    );

                    _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

                    for (int i = 0; i < _choices.Count; i++)
                        DrawItem(_choices[i], ref y, contentRect.width, e);

                    GUI.EndScrollView();
                }

                GUI.EndGroup();
                GUI.EndGroup();
            }
            finally
            {
                // 🔓 RESTORE SCROLLBAR
                GUI.skin.verticalScrollbar = oldScrollbar;
            }
        }


        // ------------------------------------------------------------
        // ITEM DRAW
        // ------------------------------------------------------------

        void DrawItem(string value, ref float y, float width, Event e)
        {
            bool selected = value == _current;
            string label = selected ? $"✓  {value}" : value;

            Rect r = new Rect(0, y, width, ItemHeight);
            bool hover = r.Contains(e.mousePosition);

            if (GUI.Button(
                r,
                label,
                hover
                    ? VIContextIMGUIStyles.ItemHover
                    : VIContextIMGUIStyles.Item))
            {
                _onSelect?.Invoke(value);
                editorWindow.Close();
            }

            y += ItemHeight;
        }
    }

}
