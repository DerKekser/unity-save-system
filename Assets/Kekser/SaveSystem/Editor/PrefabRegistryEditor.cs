#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kekser.SaveSystem
{
    [CustomEditor(typeof(PrefabRegistry))]
    public class PrefabRegistryEditor : Editor
    {
        /// <summary>Creates or refreshes Resources/PrefabRegistry with every prefab that has a Savable.</summary>
        [MenuItem("Tools/Save System/Update Prefabs")]
        public static void UpdatePrefabs()
        {
            PrefabRegistry prefabRegistry = (PrefabRegistry) AssetDatabase.LoadAssetAtPath("Assets/Resources/PrefabRegistry.asset", typeof(PrefabRegistry));
            if (prefabRegistry == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                
                prefabRegistry = CreateInstance<PrefabRegistry>();
                AssetDatabase.CreateAsset(prefabRegistry, "Assets/Resources/PrefabRegistry.asset");
            }
            prefabRegistry.Prefabs = GetPrefabs();
            EditorUtility.SetDirty(prefabRegistry);
            AssetDatabase.SaveAssets();
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            PrefabRegistry prefabRegistry = (PrefabRegistry) target;
            if (GUILayout.Button("Update Prefabs"))
            {
                prefabRegistry.Prefabs = GetPrefabs();
                EditorUtility.SetDirty(prefabRegistry);
            }
        }

        private static Savable[] GetPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            List<Savable> prefabs = new List<Savable>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Savable savable = go != null ? go.GetComponentInChildren<Savable>(true) : null;
                if (savable != null)
                    prefabs.Add(savable);
            }

            return prefabs.ToArray();
        }
    }
}
#endif