using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace Strix.VirtualInspector.Client.Editor
{
    [CustomEditor(typeof(VIClientParameter))]
    public class VIClientParameterEditor : UnityEditor.Editor
    {
        private string editorPassword = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Security", EditorStyles.boldLabel);

            editorPassword = EditorGUILayout.PasswordField("Password", editorPassword);

            if (GUILayout.Button("Generate Client Secret"))
            {
                GenerateSecret();
            }
        }

        private void GenerateSecret()
        {
            var param = (VIClientParameter)target;

            // Generate salt
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            // Compute storedSecret = SHA256(password + salt)
            byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes(editorPassword);
            byte[] combined = new byte[pwdBytes.Length + salt.Length];

            Buffer.BlockCopy(pwdBytes, 0, combined, 0, pwdBytes.Length);
            Buffer.BlockCopy(salt, 0, combined, pwdBytes.Length, salt.Length);

            byte[] storedSecret;
            using (var sha = SHA256.Create())
                storedSecret = sha.ComputeHash(combined);


            SerializedProperty saltProperty = serializedObject.FindProperty("salt");
            SerializedProperty storedSecretProperty = serializedObject.FindProperty("storedSecret");

            AssignByteArrayToSerializedProperty(saltProperty, salt);
            AssignByteArrayToSerializedProperty(storedSecretProperty, storedSecret);

            serializedObject.ApplyModifiedProperties();


            // Clear password from memory/editor
            editorPassword = "";

            EditorUtility.SetDirty(param);
            AssetDatabase.SaveAssets();

            Debug.Log("VIClientParameter: Client secret regenerated successfully.");
        }

        private void AssignByteArrayToSerializedProperty(SerializedProperty prop, byte[] data)
        {
            prop.arraySize = data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).intValue = data[i];
            }
        }
    }
}