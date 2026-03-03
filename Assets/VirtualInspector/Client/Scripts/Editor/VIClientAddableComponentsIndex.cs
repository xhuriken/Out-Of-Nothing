using System;
using System.Collections.Generic;
using UnityEngine;
namespace Strix.VirtualInspector.Client.Editor
{
    public static class VIAddableComponentsIndex
    {
        /// <summary>
        /// Builds a flat list of ComponentInfo:
        /// - From predefined XML (Resources), using Category + Component nodes (CategoryPath = XML hierarchy)
        /// - Then adds Scripts/* grouped by namespaces
        /// - Then adds UnityComponents/* grouped by namespaces for remaining Components not in XML or Scripts.
        /// Precedence: Predefined > Scripts > UnityComponents (no duplicates across groups).
        /// </summary>
        public static VIClientProjectManifest.ComponentInfo[] Build(string xmlPath = null)
        {
            var result = new List<VIClientProjectManifest.ComponentInfo>();

            // Trackers for dedup + precedence
            var predefined = new HashSet<string>(StringComparer.Ordinal); // FullTypeName from XML
            var added = new HashSet<string>(StringComparer.Ordinal); // Anything already emitted
            var scriptsAdded = new HashSet<string>(StringComparer.Ordinal); // Types emitted under Scripts/

            // 1) Load predefined hierarchy from Resources (if provided)
            if (!string.IsNullOrEmpty(xmlPath))
            {
                try
                {
                    string resourcePath = System.IO.Path.ChangeExtension(xmlPath, null); // Resources.Load: no extension
                    TextAsset xmlAsset = Resources.Load<TextAsset>(resourcePath);

                    if (xmlAsset != null)
                    {
                        var doc = System.Xml.Linq.XDocument.Parse(xmlAsset.text);
                        if (doc.Root != null)
                        {
                            TraverseXml(doc.Root, currentPath: "", result, predefined);

                            // Everything specified by XML is considered already added
                            foreach (var ci in result)
                            {
                                if (!string.IsNullOrEmpty(ci.FullTypeName))
                                    added.Add(ci.FullTypeName);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[AddableComponentsIndex] XML file is empty or invalid: {xmlPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AddableComponentsIndex] No XML file found in Resources at: {xmlPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddableComponentsIndex] Failed to read XML from Resources: {ex.Message}");
                }
            }

#if UNITY_EDITOR
            // 2) Scripts/*
            {
                foreach (var t in UnityEditor.TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                {
                    if (!IsEligibleScriptType(t)) continue;
                    var full = t.FullName;
                    if (string.IsNullOrEmpty(full)) continue;

                    if (predefined.Contains(full) || added.Contains(full)) continue;

                    string ns = t.Namespace ?? string.Empty;
                    string cat = BuildNamespaceCategory("Scripts", ns);

                    var ci = new VIClientProjectManifest.ComponentInfo
                    {
                        Name = t.Name,
                        FullTypeName = full,
                        IconPath = string.Empty,
                        CategoryPath = cat
                    };
                    result.Add(ci);

                    added.Add(full);
                    scriptsAdded.Add(full);
                }
            }

            // 3) UnityComponents/*
            {
                foreach (var t in UnityEditor.TypeCache.GetTypesDerivedFrom<Component>())
                {
                    if (!IsEligibleUnityComponentType(t)) continue;
                    var full = t.FullName;
                    if (string.IsNullOrEmpty(full)) continue;

                    if (predefined.Contains(full) || scriptsAdded.Contains(full) || added.Contains(full)) continue;

                    string ns = t.Namespace ?? string.Empty;
                    string cat = BuildNamespaceCategory("UnityComponents", ns);

                    var ci = new VIClientProjectManifest.ComponentInfo
                    {
                        Name = t.Name,
                        FullTypeName = full,
                        IconPath = string.Empty,
                        CategoryPath = cat
                    };
                    result.Add(ci);

                    added.Add(full);
                }
            }
#else
    Debug.LogWarning("[AddableComponentsIndex] Build(): Scripts/UnityComponents population requires UNITY_EDITOR TypeCache.");
#endif

            // 4) Sort for stable UI
            result.Sort((a, b) =>
            {
                int c = string.Compare(a.CategoryPath, b.CategoryPath, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            // Return as array instead of List
            return result.ToArray();
        }

        public static HashSet<string> GetPredefinedComponentsName(string xmlPath)
        {
            var predefined = new HashSet<string>(StringComparer.Ordinal);
            // 1) Load predefined hierarchy from Resources (if provided)
            if (!string.IsNullOrEmpty(xmlPath))
            {
                try
                {
                    string resourcePath = System.IO.Path.ChangeExtension(xmlPath, null); // Resources.Load: no extension
                    TextAsset xmlAsset = Resources.Load<TextAsset>(resourcePath);

                    if (xmlAsset != null)
                    {
                        var doc = System.Xml.Linq.XDocument.Parse(xmlAsset.text);
                        if (doc.Root != null)
                        {
                            TraverseXml(doc.Root, currentPath: "", predefined);
                        }
                        else
                        {
                            Debug.LogWarning($"[AddableComponentsIndex] XML file is empty or invalid: {xmlPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AddableComponentsIndex] No XML file found in Resources at: {xmlPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddableComponentsIndex] Failed to read XML from Resources: {ex.Message}");
                }
            }

            return predefined;
        }

        // ---------------- XML traversal ----------------

        private static void TraverseXml(System.Xml.Linq.XElement node, string currentPath, List<VIClientProjectManifest.ComponentInfo> dst, HashSet<string> predefined)
        {
            if (string.Equals(node.Name.LocalName, "Category", StringComparison.OrdinalIgnoreCase))
            {
                string name = node.Attribute("name")?.Value ?? "Group";
                string icon = node.Attribute("icon")?.Value ?? string.Empty;
                string path = AppendPath(currentPath, name);

                // Components under this category
                foreach (var comp in node.Elements("Component"))
                {
                    string compName = comp.Attribute("name")?.Value ?? "Unnamed";
                    string compType = comp.Attribute("type")?.Value ?? string.Empty;
                    string compIcon = comp.Attribute("icon")?.Value ?? icon; // child icon override, or inherit

                    if (string.IsNullOrEmpty(compType)) continue;

                    dst.Add(new VIClientProjectManifest.ComponentInfo
                    {
                        Name = compName,
                        FullTypeName = compType,
                        IconPath = compIcon,
                        CategoryPath = path
                    });

                    predefined.Add(compType);
                }

                // Recurse into sub-categories
                foreach (var sub in node.Elements("Category"))
                    TraverseXml(sub, path, dst, predefined);
            }
            else
            {
                foreach (var cat in node.Elements("Category"))
                    TraverseXml(cat, currentPath, dst, predefined);
            }
        }

        private static void TraverseXml(System.Xml.Linq.XElement node, string currentPath, HashSet<string> predefined)
        {
            if (string.Equals(node.Name.LocalName, "Category", StringComparison.OrdinalIgnoreCase))
            {
                string name = node.Attribute("name")?.Value ?? "Group";
                string icon = node.Attribute("icon")?.Value ?? string.Empty;
                string path = AppendPath(currentPath, name);

                // Components under this category
                foreach (var comp in node.Elements("Component"))
                {
                    string compName = comp.Attribute("name")?.Value ?? "Unnamed";
                    string compType = comp.Attribute("type")?.Value ?? string.Empty;
                    string compIcon = comp.Attribute("icon")?.Value ?? icon; // child icon override, or inherit

                    if (string.IsNullOrEmpty(compType)) continue;

                    predefined.Add(compType);
                }

                // Recurse into sub-categories
                foreach (var sub in node.Elements("Category"))
                    TraverseXml(sub, path, predefined);
            }
            else
            {
                foreach (var cat in node.Elements("Category"))
                    TraverseXml(cat, currentPath, predefined);
            }
        }

        private static string AppendPath(string basePath, string segment)
        {
            if (string.IsNullOrEmpty(basePath)) return segment ?? string.Empty;
            if (string.IsNullOrEmpty(segment)) return basePath;
            return basePath + "/" + segment;
        }

#if UNITY_EDITOR
        // ---------------- Type filters & helpers (Editor only) ----------------

        private static bool IsEligibleScriptType(Type t)
        {
            if (t == null || t.IsAbstract || !t.IsPublic) return false;
            string ns = t.Namespace ?? string.Empty;
            if (ns.StartsWith("UnityEngine", StringComparison.Ordinal)) return false;
            if (ns.StartsWith("UnityEditor", StringComparison.Ordinal)) return false;
            if (Attribute.IsDefined(t, typeof(ObsoleteAttribute), true)) return false;
            return true;
        }

        private static bool IsEligibleUnityComponentType(Type t)
        {
            if (t == null || t.IsAbstract || !t.IsPublic) return false;
            if (!typeof(Component).IsAssignableFrom(t)) return false;
            if (t == typeof(Transform)) return false;
            string ns = t.Namespace ?? string.Empty;
            if (ns.StartsWith("UnityEditor", StringComparison.Ordinal)) return false;
            if (Attribute.IsDefined(t, typeof(ObsoleteAttribute), true)) return false;
            return true;
        }

        private static string BuildNamespaceCategory(string root, string ns)
        {
            // If there's no namespace, keep only the root (e.g., "Scripts" or "UnityComponents")
            if (string.IsNullOrEmpty(ns))
                return root;

            // e.g. "UnityEngine.UI" -> "UnityComponents/UnityEngine/UI"
            return $"{root}/{ns.Replace('.', '/')}";
        }
#endif
    }
}