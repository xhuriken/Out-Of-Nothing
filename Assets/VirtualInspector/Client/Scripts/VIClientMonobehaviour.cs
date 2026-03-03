using UnityEngine;

namespace Strix.VirtualInspector.Client
{
    /// <summary>
    /// Entry point MonoBehaviour for the VI client runtime.
    /// Ensures there is only one active instance and persists across scene loads.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class VIClientMonobehaviour : MonoBehaviour
    {
        private static VIClientMonobehaviour _instance;

        private void Awake()
        {
            // Singleton guard: destroy any extra instances
            if (_instance != null && _instance != this)
            {
#if UNITY_EDITOR
                Destroy(this);
#else
                Destroy(gameObject);
#endif
                return;
            }

            _instance = this;

            DontDestroyOnLoad(gameObject);

            VIClientRuntime.OnCollectSubtree += VIClientRuntime_OnCollectSubtree;
            VIClientRuntime.OnVIGameObjectAdded += VIClientRuntime_OnVIGameObjectAdded;
            VIClientRuntime.OnReceiveParams += VIClientRuntime_OnReceiveParams;

            VIClientRuntime.Awake();
        }

        private void VIClientRuntime_OnReceiveParams(Object obj)
        {

            {
                if (obj is TMPro.TextMeshProUGUI tmp)
                {
                    tmp.ForceMeshUpdate(true);
                    tmp.SetAllDirty();
                    tmp.UpdateVertexData();
                }
            }

            {
                if (obj is TMPro.TextMeshPro tmp)
                {
                    tmp.ForceMeshUpdate(true);
                    tmp.SetAllDirty();
                    tmp.UpdateVertexData();
                }
            }


        }

        private void VIClientRuntime_OnVIGameObjectAdded(GameObject go, ulong entityId)
        {

            var hook = go.GetComponent<VIClientDestroyHook>();
            if (hook == null)
                hook = go.AddComponent<VIClientDestroyHook>();

            hook.EntityId = entityId;
        }

        private void VIClientRuntime_OnCollectSubtree(GameObject go)
        {
            var watcher = go.GetComponent<VIHierarchyWatcher>();
            if (watcher == null)
                go.AddComponent<VIHierarchyWatcher>();
        }

        private void Start()
        {
            if (_instance != this)
                return;

            VIClientRuntime.Start();
        }

        private void Update()
        {
            if (_instance != this)
                return;

            VIClientRuntime.Update();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                VIClientRuntime.OnDestroy();
                _instance = null;
                VIClientRuntime.OnCollectSubtree -= VIClientRuntime_OnCollectSubtree;
                VIClientRuntime.OnVIGameObjectAdded -= VIClientRuntime_OnVIGameObjectAdded;
                VIClientRuntime.OnReceiveParams -= VIClientRuntime_OnReceiveParams;
            }
        }
    }
}
