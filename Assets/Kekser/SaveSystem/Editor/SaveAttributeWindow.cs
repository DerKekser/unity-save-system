#if UNITY_EDITOR
using System;
using Kekser.SaveSystem.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kekser.SaveSystem
{
    public class SaveAttributeWindow : EditorWindow
    {
        [MenuItem("Tools/Save System/Attributes")]
        private static void ShowWindow()
        {
            var window = GetWindow<SaveAttributeWindow>();
            window.titleContent = new GUIContent("Save Attributes");
            window._savePath = Application.persistentDataPath + "/save.sav";
            window.Show();
        }
        
        private string _savePath;
        private Vector2 _scrollPos;

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("You must be in play mode to use this window!", MessageType.Warning);
                return;
            }

            _savePath = EditorGUILayout.TextField("Save Path", _savePath);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
                SaveLoadManager.Save(_savePath);
            if (GUILayout.Button("Load"))
                SaveLoadManager.Load(_savePath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField("Static");
            foreach (Type type in SaveAttributeManager.CachedStaticTypes)
            {
                EditorGUILayout.LabelField(type.Name);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Non-Static");
            foreach (Type type in SaveAttributeManager.CachedNonStaticTypes)
            {
                EditorGUILayout.LabelField(type.Name);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif