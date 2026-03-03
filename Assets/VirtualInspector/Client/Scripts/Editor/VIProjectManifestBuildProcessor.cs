using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Strix.VirtualInspector.Client.Editor
{
    /// <summary>
    /// Automatically rebuilds the Project Manifest when making a Unity build,
    /// but only if VIClientParameter.BuildManifestOnBuildProject is enabled.
    /// </summary>
    public class VIProjectManifestBuildProcessor : IPreprocessBuildWithReport
    {
        // Order does not matter much here
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Load parameters
            var parameters = VIClientParameter.GetParameter();
            if (parameters == null)
            {
                Debug.LogWarning("[VI] Could not find ProjectParameter in Resources. Manifest will NOT be built automatically.");
                return;
            }

            if (!parameters.BuildManifestOnBuildProject)
            {
                Debug.Log("[VI] Auto-build manifest disabled. (ProjectParameter.BuildManifestOnBuildProject = false)");
                return;
            }

            Debug.Log("[VI] Automatically rebuilding Virtual Inspector Project Manifest before build…");

            // Call your existing menu method
            VIClientProjectManifestBuilder.BuildProjectManifest();

            Debug.Log("[VI] Project Manifest rebuilt successfully before build.");
        }
    }
}
