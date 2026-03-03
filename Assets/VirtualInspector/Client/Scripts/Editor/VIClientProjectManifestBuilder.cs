using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace Strix.VirtualInspector.Client.Editor
{

    [InitializeOnLoad]
    public static class VIClientProjectManifestBuilder
    {
        const string kResourcesRoot = "Assets/Resources/VirtualInspector";
        const string kSceneManifestFolder = kResourcesRoot + "/SceneManifests";
        const string kProjectManifestPath = kResourcesRoot + "/ProjectManifest.asset";

        static readonly Type[] AllowedTypes =
        {
            typeof(Material), typeof(Texture2D), typeof(Texture3D), typeof(Cubemap),
            typeof(Sprite), typeof(Shader), typeof(Mesh), typeof(AudioClip),
            typeof(AnimationClip), typeof(ComputeShader), typeof(ScriptableObject),
            typeof(GameObject)
        };

        static VIClientProjectManifestBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        static void OnEditorReady()
        {
            EditorApplication.delayCall -= OnEditorReady;

            var manifest = AssetDatabase.LoadAssetAtPath<VIClientProjectManifest>(kProjectManifestPath);

            if (!manifest)
            {
                BuildProjectManifest();
            }
        }

        [MenuItem("Virtual Inspector/Build Project Manifest")]
        public static void BuildProjectManifest()
        {
            EnsureFolders();
            var manifest = LoadOrCreateProjectManifest();

            BuildTagsAndLayers(manifest);
            BuildScenes(manifest);
            BuildComponents(manifest);

           // BuildSceneManifests(manifest);

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();

            Debug.Log("[VI] Project & Scene manifests built successfully.");
        }

        // --------------------------------------------------------------------
        // Folder Setup
        // --------------------------------------------------------------------
        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder(kResourcesRoot))
                AssetDatabase.CreateFolder("Assets/Resources", "VirtualInspector");
        }

        static VIClientProjectManifest LoadOrCreateProjectManifest()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<VIClientProjectManifest>(kProjectManifestPath);

            if (!manifest)
            {
                manifest = ScriptableObject.CreateInstance<VIClientProjectManifest>();
                AssetDatabase.CreateAsset(manifest, kProjectManifestPath);
            }

            return manifest;
        }

        // --------------------------------------------------------------------
        // Tags & Layers
        // --------------------------------------------------------------------
        static void BuildTagsAndLayers(VIClientProjectManifest manifest)
        {
            manifest.tags = InternalEditorUtility.tags;

            List<string> layers = new();
            List<int> layerIds = new();

            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    layers.Add(name);
                    layerIds.Add(i);
                }
            }

            manifest.layersName = layers.ToArray();
            manifest.layersID = layerIds.ToArray();
        }

        // --------------------------------------------------------------------
        // Scenes
        // --------------------------------------------------------------------
        static void BuildScenes(VIClientProjectManifest manifest)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");

            var list = new List<VIClientProjectManifest.SceneInfo>();

            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                list.Add(new VIClientProjectManifest.SceneInfo
                {
                    id = Hash(path),
                    guid = guid,
                    path = path,
                    name = Path.GetFileNameWithoutExtension(path),
                    buildIndex = GetSceneBuildIndex(path)
                });
            }

            manifest.sceneEntries = list.ToArray();
        }

        static int GetSceneBuildIndex(string path)
        {
            var arr = EditorBuildSettings.scenes;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i].path == path)
                    return i;
            return -1;
        }

        static ulong Hash(string s)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;

            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(s))
            {
                h ^= b;
                h *= prime;
            }

            return h;
        }

        // --------------------------------------------------------------------
        // Components
        // --------------------------------------------------------------------
        static void BuildComponents(VIClientProjectManifest manifest)
        {
            try
            {
                var comps = VIAddableComponentsIndex.Build(manifest.xmlPath) as VIClientProjectManifest.ComponentInfo[];
                manifest.componentsRoot = comps ?? Array.Empty<VIClientProjectManifest.ComponentInfo>();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VI] Failed loading components: " + e.Message);
                manifest.componentsRoot = Array.Empty<VIClientProjectManifest.ComponentInfo>();
            }
        }

        //// --------------------------------------------------------------------
        //// Scene Manifests
        //// --------------------------------------------------------------------
        //static void BuildSceneManifests(VIClientProjectManifest manifest)
        //{
        //    foreach (var scene in manifest.sceneEntries)
        //    {
        //        BuildSingleSceneManifest(scene);
        //    }
        //}

        //static void BuildSingleSceneManifest(VIClientProjectManifest.SceneInfo scene)
        //{
        //    string assetPath = $"{kSceneManifestFolder}/{scene.name}Manifest.asset";


        //    // list all deps
        //    var deps = GetDepsUltraFast(scene.path);
        //   // var deps2 = AssetDatabase.GetDependencies(scene.path, true);

        //   // Debug.Log($"[VI] Scene '{scene.name}' dependencies found: {deps.Length} (ultrafast), {deps2.Length} (standard)");
        //    var entries = new List<VIClientSceneManifest.AssetEntry>();

        //    var obj = AssetDatabase.LoadAssetAtPath<VIClientSceneManifest>(assetPath);
        //    if (!obj)
        //    {
        //        obj = ScriptableObject.CreateInstance<VIClientSceneManifest>();
        //        AssetDatabase.CreateAsset(obj, assetPath);
        //    }


        //    foreach (var dep in deps)
        //    {

        //        if (!dep.StartsWith("Assets/")) continue;
        //        if (dep.EndsWith(".cs")) continue;
        //        if (dep.Contains("/Editor/")) continue;

        //        var asset = AssetDatabase.LoadMainAssetAtPath(dep);
        //        if (!asset) continue;

        //        Type t = asset.GetType();
        //        if (!AllowedTypes.Contains(t)) continue;

        //        entries.Add(new VIClientSceneManifest.AssetEntry
        //        {
        //            asset = asset,
        //            path = dep,
        //            typeName = t.FullName
        //        });
        //    }

        //    obj.assets = entries.ToArray();

        //    Debug.Log($"[VI] Scene '{scene.name}' dependencies included: {entries.Count}");

        //    //   EditorUtility.UnloadUnusedAssetsImmediate();
        //    if (obj == null)
        //    {
        //        Debug.LogError($"[VI] Failed to create scene manifest for scene: {scene.name}");
        //        return;
        //    }
        //    EditorUtility.SetDirty(obj);
        //   // AssetDatabase.SaveAssets();

        //    Debug.Log($"[VI] Scene manifest built: {scene.name} ({entries.Count} assets)");


        //}




    }

}
