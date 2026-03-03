using UnityEngine;
using UnityEngine.SceneManagement;

namespace Strix.VirtualInspector.Client
{
    /// <summary>
    /// Hook component used to notify VIClientRuntime when hierarchy / scene / name changes.
    /// </summary>
    internal sealed class VIHierarchyWatcher : MonoBehaviour
    {
        private string _lastName;
        private Scene _lastScene;

        private void Awake()
        {
            _lastName = gameObject.name;
            _lastScene = gameObject.scene;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            _lastName = gameObject.name;
            _lastScene = gameObject.scene;

            VIClientRuntime.OnActiveStateChanged(transform);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            if (this != null && gameObject != null)
                VIClientRuntime.OnActiveStateChanged(transform);
        }

        private void OnTransformParentChanged()
        {
            if (!Application.isPlaying)
                return;

            VIClientRuntime.OnHierarchyChanged(transform);
        }

        private void OnTransformChildrenChanged()
        {
            if (!Application.isPlaying)
                return;

            VIClientRuntime.OnHierarchyChanged(transform);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            // Detect GameObject name change at runtime
            if (_lastName != gameObject.name)
            {
                _lastName = gameObject.name;
                VIClientRuntime.OnGameObjectRenamed(transform);
            }

            // Detect scene change (MoveGameObjectToScene, etc.)
            if (_lastScene != gameObject.scene)
            {
                _lastScene = gameObject.scene;
                VIClientRuntime.OnSceneChanged(transform);
            }
        }
    }
}
