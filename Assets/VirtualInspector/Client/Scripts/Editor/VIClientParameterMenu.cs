#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Strix.VirtualInspector.Client.Editor
{
    public static class VIClientParameterMenu
    {
        private const string ResourcePath = "VirtualInspector/ProjectParameter";

        [MenuItem("Virtual Inspector/Project Parameter")]
        public static void OpenProjectParameter()
        {
            // Load from Resources (same as runtime)
            var param = Resources.Load<VIClientParameter>(ResourcePath);

            if (param == null)
            {
                EditorUtility.DisplayDialog(
                    "Virtual Inspector",
                    "ProjectParameter asset not found.\n\n" +
                    "Expected at:\nResources/VirtualInspector/ProjectParameter.asset",
                    "OK"
                );
                return;
            }

            // Select + ping in Project window
            Selection.activeObject = param;
            EditorGUIUtility.PingObject(param);
        }
    }
}
#endif
