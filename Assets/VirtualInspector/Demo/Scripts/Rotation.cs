using UnityEngine;
namespace Strix.VirtualInspector.Demo
{
    public class Rotation : MonoBehaviour
    {
        public float time = 0.0f;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(new Vector3(0, 30, 0) * Time.deltaTime);
            time+=Time.deltaTime;
        }
    }

}
