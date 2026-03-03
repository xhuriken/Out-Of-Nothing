using System.Collections.Generic;
using UnityEngine;

namespace Strix.VirtualInspector.Demo
{
    public enum WeaponType { None, Sword, Bow, Staff, Gun }
    [System.Flags] public enum AbilityFlags { None = 0, Dash = 1 << 0, DoubleJump = 1 << 1, Shield = 1 << 2, Invisibility = 1 << 3 }

    public class DemoSO : ScriptableObject
    {
        public string id;
        public int value;
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
    public struct MyStruct2
    {
        [SerializeField] private Vector3 c;
        public Vector3 d;
        [SerializeField] private Vector3 e;
    }

    [System.Serializable]
    public struct NestedStruct
    {
        public int i;
        public float f;
        public Color color;
        public Quaternion rot;
        public Vector2 uv;
    }

    [System.Serializable]
    public class MyClass
    {
        [SerializeField] private Vector3 a;
        public Vector3 b;
    }

    [System.Serializable]
    public class MyClass2
    {
        [SerializeField] private Vector3 c;
        public Vector3 d;
        [SerializeField] private Vector3 e;
        [SerializeField] private Vector3[] vecArray;
    }

    [System.Serializable]
    public class MyClass3
    {
        [SerializeField] private MyClass3 a;
        public Vector4 d;
    }

    [System.Serializable]
    public class CustomData
    {
        public string name;
        public int level;
        public Vector3 position;
        public MyStruct inner;
        public NestedStruct nested;
        public WeaponType weapon;
        public AbilityFlags flags;
    }

    [ExecuteInEditMode]
    public class Player : MonoBehaviour
    {
        [SerializeField] public UnityEngine.Object player;
        public MyStruct m1;
        public Vector3 a;
        public Vector3 C
        {
            get => a;
            set => a = value;
        }
        [SerializeField] private MyStruct2 m2;
        public MyClass m3;
        [SerializeField] private MyClass2 m4;


        public int k;

        public int testInt;
        public float testFloat;
        public bool testBool;
        public string testString;
        public Color testColor;
        public Vector2 testV2;
        public Vector3 testV3;
        public Vector4 testV4;
        public Quaternion testQuat;

        [SerializeField] private int privateInt = 42;
        [SerializeField] private Vector3 privateVec3 = Vector3.one;

        public WeaponType weaponType;
        [SerializeField] private AbilityFlags abilities = AbilityFlags.Dash | AbilityFlags.DoubleJump;

        public UnityEngine.Object anyObject;
        public Transform tr;
        public BoxCollider boxCollider;
        public Material materialRef;
        public Texture2D textureRef;
        public GameObject goRef;
        public DemoSO demoSo;

        public int[] arrInt = { 1, 2, 3 };
        public float[] arrFloat = { 0.1f, 2.5f };
        public bool[] arrBool = { true, false, true };
        public string[] arrString = { "a", "b", "c" };
        public Vector2[] arrV2 = { new Vector2(1, 2), new Vector2(3, 4) };
        public Vector3[] arrV3 = { Vector3.zero, Vector3.one, new Vector3(1, 2, 3) };
        public Vector4[] arrV4 = { new Vector4(1, 2, 3, 4) };
        public Quaternion[] arrQuat = { Quaternion.identity, Quaternion.Euler(0, 90, 0) };
        public Color[] arrColor = { Color.white, Color.red };

        public MyStruct[] arrStruct =
        {
        new MyStruct { b = new Vector3(1, 2, 3), C = new Vector3(9, 9, 9) },
        new MyStruct { b = Vector3.one,          C = new Vector3(1, 0, 0)   },
    };

        public MyClass[] arrClass =
        {
        new MyClass { b = new Vector3(10, 0, 0) },
        new MyClass { b = new Vector3(0, 10, 0) },
        new MyClass { b = new Vector3(0, 0, 10) },
    };

        public WeaponType[] arrWeapon = { WeaponType.None, WeaponType.Bow, WeaponType.Gun };
        public AbilityFlags[] arrFlags = { AbilityFlags.None, AbilityFlags.Dash | AbilityFlags.Shield };

        public UnityEngine.Object[] arrObjects;
        public Transform[] arrTransforms;
        public BoxCollider[] arrBoxColliders;
        public Material[] arrMaterials;
        public Texture2D[] arrTextures;
        public GameObject[] arrGameObjects;
        public DemoSO[] arrDemoSOs;

        public List<int> listInt = new List<int> { 10, 20, 30 };
        public List<float> listFloat = new List<float> { 1f, 2f, 3.5f };
        public List<bool> listBool = new List<bool> { true, false };
        public List<string> listString = new List<string> { "hello", "world" };
        public List<Vector2> listV2 = new List<Vector2> { new Vector2(5, 6) };
        public List<Vector3> listV3 = new List<Vector3> { new Vector3(7, 8, 9) };
        public List<Vector4> listV4 = new List<Vector4> { new Vector4(1, 1, 1, 1) };
        public List<Quaternion> listQuat = new List<Quaternion> { Quaternion.Euler(10, 20, 30) };
        public List<Color> listColor = new List<Color> { new Color(0.2f, 0.5f, 0.7f, 1f) };

        public List<WeaponType> listWeapon = new List<WeaponType> { WeaponType.Sword, WeaponType.Staff };
        public List<AbilityFlags> listFlags = new List<AbilityFlags> { AbilityFlags.Dash, AbilityFlags.Invisibility | AbilityFlags.Shield };

        public List<UnityEngine.Object> listObjects = new List<UnityEngine.Object>();
        public List<Transform> listTransforms = new List<Transform>();
        public List<BoxCollider> listBoxColliders = new List<BoxCollider>();
        public List<Material> listMaterials = new List<Material>();
        public List<Texture2D> listTextures = new List<Texture2D>();
        public List<GameObject> listGameObjects = new List<GameObject>();
        public List<DemoSO> listDemoSOs = new List<DemoSO>();

        public MyStruct structField;
        [SerializeField] private MyStruct2 structField2;
        public NestedStruct nestedStruct;

        public CustomData customRef;
        public List<CustomData> listCustom = new List<CustomData>();
        public CustomData[] arrCustom;
    }

}

