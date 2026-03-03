using Strix.VirtualInspector.Common;
using Strix.VirtualInspector.Server.Editor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace Strix.VirtualInspector.Editor.UI
{
    public interface IVIUpdatableField
    {
        uint Key { get; set; }
        void UpdateValue(object value);
    }

    public interface IVIUpdateLabelField
    {
        void SetLabelSize(float size);
    }

    public class VIComponentCard : VisualElement
    {
        protected VisualElement header;
        protected VisualElement body;
        protected VisualElement foldArrow;
        protected VICheckbox enableToggle;

        bool isExpanded = true;

        static readonly Dictionary<(ulong entityId, ulong compId), Dictionary<uint, (string className, string name)>> s_NameCache
    = new();


        // cache fields (property-level diff)
        readonly Dictionary<uint, IVIUpdatableField> fields = new();

        // cache struct foldouts (path-level)
        readonly Dictionary<string, VIStructFoldout> structFoldouts = new();



        public VIComponentCard(string title, Texture2D icon = null)
        {
            AddToClassList("vi-component-card");
            style.flexDirection = FlexDirection.Column;

            // ================= HEADER =================
            header = new VisualElement();
            header.AddToClassList("vi-component-header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            Add(header);

            // ---------- Fold Arrow ----------
            foldArrow = new VisualElement();
            foldArrow.AddToClassList("vi-component-fold");
            foldArrow.style.rotate = new Rotate(new Angle(90)); // expanded
            header.Add(foldArrow);

            foldArrow.RegisterCallback<MouseDownEvent>(evt =>
            {
                evt.StopPropagation();
                ToggleFold();
            });

            // ---------- Enable Toggle ----------




            enableToggle = new VICheckbox(true);
            enableToggle.Size = 20;
            enableToggle.style.marginTop = 8;
            enableToggle.AddToClassList("vi-component-toggle");

            enableToggle.SetEnabled(false);
            enableToggle.AddToClassList("vi-readonly");


            // ---------- Icon ----------
            if (icon != null)
            {
                var iconVE = new VisualElement();
                iconVE.AddToClassList("vi-component-icon");
                iconVE.style.backgroundImage = new StyleBackground(icon);
                header.Add(iconVE);
            }

            // ---------- Title ----------
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("vi-component-title");
            header.Add(titleLabel);

          

            // Click on header toggles fold
            header.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.target == enableToggle && enableToggle.style.opacity == 1.0f)
                    return;

                ToggleFold();
            });

            // ================= BODY =================
            body = new VisualElement();
            body.AddToClassList("vi-component-body");
            body.style.flexDirection = FlexDirection.Column;
            Add(body);
        }

        public void Populate(ulong entityId, VIServerVirtualComponent component)
        {
            if (component == null || component.properties == null)
                return;

            VIServerRuntime.AllObjects.TryGetValue(entityId, out var vobj);

            var existing = new HashSet<uint>(fields.Keys);

            var goMap = VIServerRuntime.S_Map;
            goMap.TryGetValue(entityId, out var vgo);

            if (vgo != null && TryGetBehaviourEnabled(entityId, component.compId, out uint enableKey, out bool value))
            {      
                if (enableToggle.style.opacity == 0.0f)
                {
                    enableToggle.OnValueChanged += (value) =>
                    {
                        if (TryGetProperty(vobj.EntityId, component.compId, enableKey, out var prop))
                        {
                            prop.boolValue = value;
                        }
                    };
                    var spacer = new VisualElement();
                    spacer.style.flexGrow = 1;

                    header.Add(spacer);
                }
                enableToggle.style.opacity = 1.0f;
                if (enableToggle.Value != value)
                {
                    enableToggle.Value = value;
                }
            }

            foreach (var kv in component.properties)
            {



                uint key = kv.Key;
                var prop = kv.Value;

                VICommonParamNameRegistry.TryGetAllByKey(entityId, component.compId, out var nameMap);

                if (nameMap != null && nameMap.TryGetValue(key, out var info) && !ShouldShowParam(info.className, info.name))
                    continue;

                if (!fields.TryGetValue(key, out var field))
                {
                    string fullPath = ResolveFullPath(entityId, component.compId, key);

                    VisualElement parent = Body;

                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        var parts = fullPath.Split('.');
                        if (parts.Length > 1)
                        {
                            parent = GetOrCreateStructPath(
                                parts.Take(parts.Length - 1).ToArray(),
                                Body
                            );
                        }
                    }

                    var ve = CreateFieldForProperty(vobj, component, key, prop);

                    if (ve is IVIUpdatableField updatable)
                    {
                        fields[key] = updatable;

                        ApplyReadOnly(ve);

                        parent.Add(ve);
                    }
                }

                else
                {
                    // ---- UPDATE ----
                    field.UpdateValue(GetPropertyValue(prop));
                }

                existing.Remove(key);
            }

            // ---- REMOVE ----
            foreach (var removedKey in existing)
            {
                var ve = fields[removedKey] as VisualElement;
                ve.RemoveFromHierarchy();
                fields.Remove(removedKey);
            }
        }

        static void ApplyReadOnly(VisualElement ve)
        {
            ve.SetEnabled(false);
            ve.AddToClassList("vi-readonly");
        }
        private static bool ShouldShowParam(string className, string paramName)
        {
            if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(paramName))
                return true;

            if (!VIServerVirtualObjectDrawer.Exists(className))
                return true;

            return VIServerVirtualObjectDrawer.HasKeyword(className, paramName);
        }

        private static bool TryGetBehaviourEnabled(ulong entityId, ulong compId, out uint key, out bool value)
        {
            key = 0;
            value = false;

            if (VICommonParamNameRegistry.TryGetByName(entityId, compId, "enabled", out var info))
            {
                key = info.key;
                if (TryGetProperty(entityId, compId, key, out var prop))
                {
                    value = prop.boolValue;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetProperty(ulong entityId, ulong compId, uint key, out VIServerVirtualProperty prop)
        {
            if (VIServerRuntime.GetComponent(entityId, compId, out var comp))
                return comp.properties.TryGetValue(key, out prop);

            prop = null;
            return false;
        }

        VisualElement GetOrCreateStructPath(
    string[] segments,
    VisualElement root
)
        {
            VisualElement parent = root;
            string currentPath = string.Empty;

            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];
                currentPath = string.IsNullOrEmpty(currentPath)
                    ? seg
                    : currentPath + "." + seg;

                if (!structFoldouts.TryGetValue(currentPath, out var foldout))
                {
                    foldout = new VIStructFoldout(seg);
                    structFoldouts[currentPath] = foldout;
                    parent.Add(foldout);
                }

                parent = foldout.Content;
            }

            return parent;
        }


        string ResolveFullPath(ulong entityId, ulong compId, uint key)
        {
            if (VICommonParamNameRegistry.TryGetAllByKey(entityId, compId, out var map) &&
                map != null &&
                map.TryGetValue(key, out var info))
            {
                return info.name;
            }

            return null;
        }

        string ResolveLabel(ulong entityId, ulong compId, uint key)
        {
            VICommonParamNameRegistry.TryGetAllByKey(entityId, compId, out var nameMap);

            if (nameMap != null && nameMap.TryGetValue(key, out var info) && !string.IsNullOrEmpty(info.name))
            {
                int lastDot = info.name.LastIndexOf('.');
                return lastDot >= 0 ? info.name.Substring(lastDot + 1) : info.name;
            }

            return $"Param_{key}";
        }

        VisualElement CreateFieldForProperty(
    VIServerVirtualObject vo,
    VIServerVirtualComponent component,
    uint key,
    VIServerVirtualProperty prop
)
        {
            string label = ResolveLabel(vo.EntityId, component.compId, key); 

            switch (prop.type)
            {
                case VICommonProtocol.ParamType.INT32:
                    return new VIIntField(label, prop.intValue);

                case VICommonProtocol.ParamType.FLOAT:
                    return new VIFloatField(label, prop.floatValue);

                case VICommonProtocol.ParamType.BOOL:
                    return new VIToggleField(label, prop.boolValue);

                case VICommonProtocol.ParamType.STRING:
                    return new VIStringField(label, prop.stringValue ?? string.Empty);

                case VICommonProtocol.ParamType.VEC2:
                    return new VIVector2Field(label, prop.vector2Value);

                case VICommonProtocol.ParamType.VEC3:
                    return new VIVector3Field(label, prop.vector3Value);

                case VICommonProtocol.ParamType.VEC4:
                    return new VIVector4Field(label, prop.vector4Value);

                case VICommonProtocol.ParamType.COLOR:
                    return new VIColorField(label, prop.colorValue);

                case VICommonProtocol.ParamType.QUAT:
                    return new VIVector3Field(label, prop.quatValue.eulerAngles);
                    case VICommonProtocol.ParamType.OBJECT:
                        return new VIObjectField(label, prop.objRef, typeof(UnityEngine.Object));

                case VICommonProtocol.ParamType.ARRAY:
                    return CreateArrayField(vo, component, key, prop, label);

                default:
                    return CreateUnsupportedField(label);
            }
        }


        object GetPropertyValue(VIServerVirtualProperty prop)
        {
            switch (prop.type)
            {
                case VICommonProtocol.ParamType.INT32: return prop.intValue;
                case VICommonProtocol.ParamType.FLOAT: return prop.floatValue;
                case VICommonProtocol.ParamType.BOOL: return prop.boolValue;
                case VICommonProtocol.ParamType.STRING: return prop.stringValue;
                case VICommonProtocol.ParamType.VEC2: return prop.vector2Value;
                case VICommonProtocol.ParamType.VEC3: return prop.vector3Value;
                case VICommonProtocol.ParamType.VEC4: return prop.vector4Value;
                case VICommonProtocol.ParamType.QUAT: return prop.quatValue.eulerAngles;
                case VICommonProtocol.ParamType.COLOR: return prop.colorValue;
                case VICommonProtocol.ParamType.OBJECT: return prop.objRef;
                case VICommonProtocol.ParamType.ARRAY: return prop.arrayValue;
            }

            return null;
        }

        VisualElement CreateArrayField(VIServerVirtualObject vo, VIServerVirtualComponent component, uint key, VIServerVirtualProperty prop, string label)
        {
            // ---------------- INT ----------------
            if (prop.arrayValue is int[])
            {
                VIArrayField<int> field = null;

                field = CreateArray(label, (int[])prop.arrayValue, (i, v) =>
                    new VIIntField("", v, newValue =>
                    {
                        var arr = prop.arrayValue as int[];
                        if (arr == null || i < 0 || i >= arr.Length)
                        {
                            prop.arrayValue = field.Values.ToArray();
                            return;
                        }

                        arr[i] = newValue;
                        prop.arrayValue = arr;
                    })
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };
                return field;
            }

            // ---------------- FLOAT ----------------
            if (prop.arrayValue is float[])
            {
                VIArrayField<float> field = null;

                field = CreateArray(label, (float[])prop.arrayValue, (i, v) =>
                    new VIFloatField("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- BOOL ----------------
            if (prop.arrayValue is bool[])
            {
                VIArrayField<bool> field = null;

                field = CreateArray(label, (bool[])prop.arrayValue, (i, v) =>
                    new VIToggleField("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- STRING ----------------
            if (prop.arrayValue is string[])
            {
                VIArrayField<string> field = null;

                field = CreateArray(label, (string[])prop.arrayValue, (i, v) =>
                    new VIStringField("", v ?? "")
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- VECTOR2 ----------------
            if (prop.arrayValue is Vector2[])
            {
                VIArrayField<Vector2> field = null;

                field = CreateArray(label, (Vector2[])prop.arrayValue, (i, v) =>
                    new VIVector2Field("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- VECTOR3 ----------------
            if (prop.arrayValue is Vector3[])
            {
                VIArrayField<Vector3> field = null;

                field = CreateArray(label, (Vector3[])prop.arrayValue, (i, v) =>
                    new VIVector3Field("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- VECTOR4 ----------------
            if (prop.arrayValue is Vector4[])
            {
                VIArrayField<Vector4> field = null;

                field = CreateArray(label, (Vector4[])prop.arrayValue, (i, v) =>
                    new VIVector4Field("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- COLOR ----------------
            if (prop.arrayValue is Color[])
            {
                VIArrayField<Color> field = null;

                field = CreateArray(label, (Color[])prop.arrayValue, (i, v) =>
                    new VIColorField("", v)
                );

                field.OnStructureChanged = list =>
                {
                    prop.arrayValue = list.ToArray();
                };

                return field;
            }

            // ---------------- FALLBACK ----------------
            return CreateUnsupportedField(label);
        }


        VIArrayField<T> CreateArray<T>(
     string label,
     T[] array,
     Func<int, T, VisualElement> elementFactory
 )
        {
            return new VIArrayField<T>(
                label,
                new List<T>(array),
                elementFactory
            );
        }



        VisualElement CreateUnsupportedField(string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            row.Add(VIFieldUtils.CreateLabel(label));

            var info = new Label("Field not supported");
            info.style.fontSize = 11;
            info.style.unityFontStyleAndWeight = FontStyle.Italic;
            info.style.color = new Color(0.6f, 0.6f, 0.6f);

            row.Add(info);
            return row;
        }



        // ================= API =================

        void ToggleFold()
        {
            isExpanded = !isExpanded;

            body.style.display = isExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            foldArrow.style.rotate = new Rotate(
                new Angle(isExpanded ? 90 : 0)
            );
        }

        public VisualElement Body => body;

        public bool Enabled
        {
            get => enableToggle.Value;
            set => enableToggle.Value = value;
        }
    }
}
