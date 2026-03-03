#if UNITY_EDITOR_WIN
#define VI_WINDOWS
#endif

using Strix.VirtualInspector.Editor.UI.Theme;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Strix.VirtualInspector.Editor.UI
{
    // ============================================================
    // STYLES
    // ============================================================

    static class VIContextIMGUIStyles
    {
        public static readonly GUIStyle WindowBorder;
        public static readonly GUIStyle Background;
        public static readonly GUIStyle Item;
        public static readonly GUIStyle ItemHover;

        static VIContextIMGUIStyles()
        {
            WindowBorder = new GUIStyle();
            WindowBorder.normal.background =
                MakeTex(VIThemeColors.ContextBorder);

            Background = new GUIStyle();
            Background.normal.background =
                MakeTex(VIThemeColors.ContextBackground);
            Background.padding = new RectOffset(6, 6, 6, 6);

            Item = new GUIStyle(EditorStyles.label);
            Item.fontSize = 12;
            Item.alignment = TextAnchor.MiddleLeft;
            Item.padding = new RectOffset(12, 12, 4, 4);
            Item.normal.textColor =
                VIThemeColors.ContextItemText;

            ItemHover = new GUIStyle(Item);
            ItemHover.normal.background =
                MakeTex(VIThemeColors.ContextItemHover);
        }


        static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }

    // ============================================================
    // DATA
    // ============================================================

    public class VIContextMenuItem
    {
        public string Label;
        public Action Action;
        public bool IsSeparator;

        public static VIContextMenuItem Item(string label, Action action)
            => new VIContextMenuItem { Label = label, Action = action };

        public static VIContextMenuItem Separator()
            => new VIContextMenuItem { IsSeparator = true };
    }

    // ============================================================
    // WINDOW
    // ============================================================

    public class VIHierarchyContextMenuWindow : EditorWindow
    {
        const float Width = 220f;
        const float ItemHeight = 22f;
        const float MinHeight = 80f;
        const float SeparatorHeight = 7f;

        static VIHierarchyContextMenuWindow _current;

        readonly List<VIContextMenuItem> _items = new();

        Vector2 _scroll;
        bool _useScroll;
        float _maxHeight;

        Vector2 _requestedScreenPos;
        Action _onClose;

        // ============================================================
        // FACTORY
        // ============================================================

        public static void ShowAtScreen(
            Vector2 screenPos,
            Action<VIHierarchyContextMenuWindow> build,
            Action onClose = null)
        {
            _current?.Close();

            var win = CreateInstance<VIHierarchyContextMenuWindow>();
            _current = win;

            win._onClose = onClose;
            win._requestedScreenPos = screenPos;

            build?.Invoke(win);

            Rect workArea = WorkAreaUtility.GetWorkAreaAt(screenPos);
            win._maxHeight = workArea.height * 0.7f;

            float startHeight = Mathf.Min(360f, win._maxHeight);


            Vector2 pos = ClampToWorkArea(screenPos, startHeight, workArea);
      
            win.ShowPopup();
            win.position = new Rect(pos, new Vector2(Width, startHeight));
            win.minSize = win.maxSize = new Vector2(Width, startHeight);
        }

        // ============================================================
        // API
        // ============================================================

        public void AddItem(string label, Action action)
            => _items.Add(VIContextMenuItem.Item(label, action));

        public void AddSeparator()
            => _items.Add(VIContextMenuItem.Separator());

        // ============================================================
        // LIFECYCLE
        // ============================================================

        void OnLostFocus()
        {
            Close();
        }

        void OnDisable()
        {
            if (_current == this)
                _current = null;

            _onClose?.Invoke();
        }

        // ============================================================
        // GUI
        // ============================================================

        void OnGUI()
        {
            wantsMouseMove = true;

            Rect full = new Rect(0, 0, position.width, position.height);

            GUI.BeginGroup(full, GUIContent.none, VIContextIMGUIStyles.WindowBorder);
            Rect inner = new Rect(1, 1, full.width - 2, full.height - 2);

            GUI.BeginGroup(inner, GUIContent.none, VIContextIMGUIStyles.Background);

            // ---------- measure content
            float contentHeight = 0f;
            foreach (var item in _items)
                contentHeight += item.IsSeparator ? SeparatorHeight : ItemHeight;

            contentHeight += VIContextIMGUIStyles.Background.padding.vertical;

            float desired = Mathf.Max(contentHeight, MinHeight);
            float clamped = Mathf.Min(desired, _maxHeight);
            _useScroll = desired > clamped + 0.5f;

            Rect view = new Rect(0, 0, inner.width, inner.height);
            Rect content = new Rect(0, 0, inner.width - (_useScroll ? 16 : 0), contentHeight);

            if (_useScroll)
                _scroll = GUI.BeginScrollView(view, _scroll, content);

            // ---------- draw items
            float y = 0f;

            foreach (var item in _items)
            {
                if (item.IsSeparator)
                {
                    y += 3;
                    EditorGUI.DrawRect(
                        new Rect(6, y, inner.width - 12, 1),
                        new Color(1f, 1f, 1f, 0.15f)
                    );
                    y += 4;
                    continue;
                }

                Rect r = new Rect(0, y, inner.width, ItemHeight);
                bool hover = r.Contains(Event.current.mousePosition);

                if (GUI.Button(r, item.Label,
                    hover ? VIContextIMGUIStyles.ItemHover : VIContextIMGUIStyles.Item))
                {
                    item.Action?.Invoke();
                    Close();
                }

                y += ItemHeight;
            }

            if (_useScroll)
                GUI.EndScrollView();

            GUI.EndGroup();
            GUI.EndGroup();

            // ---------- resize + clamp
            if (Event.current.type == EventType.Repaint)
            {
                if (Mathf.Abs(position.height - clamped) > 0.5f)
                {
                    Rect work = WorkAreaUtility.GetWorkAreaAt(_requestedScreenPos);
                    Vector2 pos = ClampToWorkArea(position.position, clamped, work);

                    position = new Rect(pos, new Vector2(Width, clamped));
                    minSize = maxSize = new Vector2(Width, clamped);
                }
            }
        }

        // ============================================================
        // POSITIONING
        // ============================================================

        static Vector2 ClampToWorkArea(Vector2 pos, float height, Rect work)
        {
            float y = pos.y;

            if (y + height > work.yMax)
                y = Mathf.Max(work.yMin, work.yMax - height);

            return new Vector2(pos.x, y);
        }
    }

    // ============================================================
    // WINDOWS WORK AREA (REAL OS)
    // ============================================================

#if VI_WINDOWS
    static class WorkAreaUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left, top, right, bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, int flags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int index);

        [DllImport("Shcore.dll")]
        static extern int GetDpiForMonitor(
            IntPtr hmonitor,
            int dpiType,
            out uint dpiX,
            out uint dpiY
        );

        const int LOGPIXELSX = 88;
        const int MDT_EFFECTIVE_DPI = 0;

        static float GetDpiScaleAtPoint(Vector2 screenPos)
        {
            POINT pt;
            pt.x = Mathf.RoundToInt(screenPos.x);
            pt.y = Mathf.RoundToInt(screenPos.y);

            IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            // --- Try modern API (Windows 8.1+) ---
            try
            {
                uint dpiX, dpiY;
                if (GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY) == 0)
                {
                    return dpiX / 96f;
                }
            }
            catch
            {
                // ignored → fallback
            }

            // --- Fallback: system DPI ---
            IntPtr hdc = GetDC(IntPtr.Zero);
            int dpi = GetDeviceCaps(hdc, LOGPIXELSX);
            ReleaseDC(IntPtr.Zero, hdc);

            return dpi / 96f;
        }


        public static Rect GetWorkAreaAt(Vector2 screenPos)
        {
            POINT pt;
            pt.x = Mathf.RoundToInt(screenPos.x);
            pt.y = Mathf.RoundToInt(screenPos.y);

            IntPtr mon = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            if (!GetMonitorInfo(mon, ref info))
            {
                return new Rect(0, 0,
                    Screen.currentResolution.width,
                    Screen.currentResolution.height);
            }

            float dpiScale = GetDpiScaleAtPoint(screenPos);

            return Rect.MinMaxRect(
                info.rcWork.left / dpiScale,
                info.rcWork.top / dpiScale,
                info.rcWork.right / dpiScale,
                info.rcWork.bottom / dpiScale
            );
        }

    }
#else
    static class WorkAreaUtility
    {
        public static Rect GetWorkAreaAt(Vector2 _)
            => new Rect(0, 0,
                Screen.currentResolution.width,
                Screen.currentResolution.height);
    }
#endif
}
