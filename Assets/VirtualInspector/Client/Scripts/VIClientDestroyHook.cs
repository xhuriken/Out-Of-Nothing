using UnityEngine;

namespace Strix.VirtualInspector.Client
{
    internal sealed class VIClientDestroyHook : MonoBehaviour
    {
        public ulong EntityId;

        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;
            VIClientRuntime.OnGameObjectDestroyed(EntityId, gameObject);
        }
    }
}