using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Strix.VirtualInspector.Server.Editor
{
    public abstract class VIServerVirtualObjectDrawer
    {
        public abstract HashSet<string> Keywords { get; }

        private static Dictionary<string, Type> _byName;

        static VIServerVirtualObjectDrawer()
        {
            _byName = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t => t != null
                            && !t.IsAbstract
                            && typeof(VIServerVirtualObjectDrawer).IsAssignableFrom(t))
                .Select(t => (type: t, attr: t.GetCustomAttribute<VIServerVirtualObjectNameAttribute>()))
                .Where(x => x.attr != null)
                .ToDictionary(x => x.attr.Name, x => x.type, StringComparer.OrdinalIgnoreCase);
        }


        public static bool TryGetKeywords(string virtualName, out HashSet<string> keywords)
        {
            keywords = null;
            if (string.IsNullOrWhiteSpace(virtualName)) return false;

            if (_byName != null && _byName.TryGetValue(virtualName, out var type))
            {
                var drawer = (VIServerVirtualObjectDrawer)Activator.CreateInstance(type);
                keywords = drawer.Keywords;
                return true;
            }
            return false;
        }

        public static bool Exists(string virtualName) =>
            _byName != null && _byName.ContainsKey(virtualName);

        public static bool HasKeyword(string virtualName, string keyword)
        {
            if (string.IsNullOrWhiteSpace(virtualName) || string.IsNullOrWhiteSpace(keyword))
                return false;

            return TryGetKeywords(virtualName, out var set) && set.Contains(keyword);
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class VIServerVirtualObjectNameAttribute : Attribute
    {
        public string Name { get; }
        public VIServerVirtualObjectNameAttribute(string name) => Name = name;
    }
}