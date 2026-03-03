using System.Collections.Generic;
using UnityEngine;

namespace Strix.VirtualInspector.Demo
{
    public class PlayerUnsupported : MonoBehaviour
    {
        public enum WeaponType { None, Sword, Bow, Staff, Gun }
        [System.Flags]
        public enum AbilityFlags
        {
            None = 0,
            Dash = 1 << 0,
            DoubleJump = 1 << 1,
            Shield = 1 << 2,
            Invisibility = 1 << 3
        }



        [System.Serializable]
        public struct MyStruct
        {
            [SerializeField] private Vector3 a;
            public Vector3 b;

            public Vector3 C
            {
                get => a;
                set => a = value;
            }
        }

        [System.Serializable]
        public class MyClass
        {
            [SerializeField] private Vector3 a;
            public Vector3 b;
        }

        // ----------------------------------------------------
        // ENUM FIELDS (scalar enums) — ignored during leaf scanning
        // ----------------------------------------------------
        public WeaponType weaponEnum = WeaponType.Bow;
        [SerializeField] private AbilityFlags flagsEnum = AbilityFlags.Dash | AbilityFlags.Shield;

        // ----------------------------------------------------
        // UNSUPPORTED ARRAYS (struct / class / enum arrays)
        // ----------------------------------------------------
        public MyStruct[] arrStruct =
        {
            new MyStruct { b = new Vector3(1,2,3), C = new Vector3(9,9,9) },
            new MyStruct { b = Vector3.one,        C = new Vector3(1,0,0) },
        };

        public MyClass[] arrClass =
        {
            new MyClass { b = new Vector3(10,0,0) },
            new MyClass { b = new Vector3(0,10,0) },
        };

        public WeaponType[] arrEnum =
        {
            WeaponType.None,
            WeaponType.Gun
        };

        public Object[] arrayObjects;
        public Transform[] arrayTrs;

        // NOTE:
        // Leaf arrays (int[], float[], Vector3[], etc.) are not included here
        // because they would be captured by leaf enumeration.
        // This test focuses only on non-leaf arrays.

        // ----------------------------------------------------
        // LIST FIELDS — all lists are ignored (any element type)
        // ----------------------------------------------------

        // Lists of leaf types
        public List<int> listInt = new List<int> { 1, 2, 3 };
        public List<float> listFloat = new List<float> { 0.5f, 2.5f };
        public List<bool> listBool = new List<bool> { true, false };
        public List<string> listString = new List<string> { "a", "b" };
        public List<Vector2> listV2 = new List<Vector2> { new Vector2(1, 2) };
        public List<Vector3> listV3 = new List<Vector3> { new Vector3(3, 4, 5) };
        public List<Vector4> listV4 = new List<Vector4> { new Vector4(1, 1, 1, 1) };
        public List<Color> listColor = new List<Color> { Color.red, Color.green };
        public List<Quaternion> listQuat = new List<Quaternion> { Quaternion.identity };

        // Lists of UnityEngine.Object references
        public List<UnityEngine.Object> listObjects = new List<UnityEngine.Object>();
        public List<Transform> listTransforms = new List<Transform>();
        public List<BoxCollider> listBoxColliders = new List<BoxCollider>();
        public List<Material> listMaterials = new List<Material>();
        public List<Texture2D> listTextures = new List<Texture2D>();
        public List<GameObject> listGameObjects = new List<GameObject>();

        // Lists of custom struct/class types
        public List<MyStruct> listStruct = new List<MyStruct>
        {
            new MyStruct { b = new Vector3(7,8,9), C = new Vector3(0.1f, 0.2f, 0.3f) }
        };

        public List<MyClass> listClass = new List<MyClass>
        {
            new MyClass { b = new Vector3(0,0,10) }
        };

        // Lists of enums
        public List<WeaponType> listWeapon = new List<WeaponType>
        {
            WeaponType.Sword,
            WeaponType.Staff
        };

        public List<AbilityFlags> listFlags = new List<AbilityFlags>
        {
            AbilityFlags.Dash,
            AbilityFlags.Invisibility | AbilityFlags.Shield
        };

        // ----------------------------------------------------
        // Additional non-serialized or unsupported fields 
        // ----------------------------------------------------

        // Dictionaries are not handled because Unity does not serialize them.
        public Dictionary<string, int> notSerializedDict = new Dictionary<string, int>
        {
            ["score"] = 100
        };
    }
}
