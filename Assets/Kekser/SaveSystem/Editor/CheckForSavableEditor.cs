#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Kekser.SaveSystem.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kekser.SaveSystem
{
    [InitializeOnLoad]
    public static class CheckForSavableEditor
    {
        static CheckForSavableEditor()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }
        
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (Application.isPlaying)
                return;
            
            CheckForSavables();
        }
        
        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (Application.isPlaying)
                return;
            
            for (int i = 0; i < stream.length; i++)
            {
                ObjectChangeKind type = stream.GetEventType(i);

                switch (type)
                {
                    case ObjectChangeKind.ChangeScene:
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
                        CheckForSavables();
                        break;
                }
            }
        }
        
        [MenuItem("Tools/Save System/Check for Savables")]
        private static void CheckForSavables()
        {
            if (Application.isPlaying)
                return;
            
            SaveAttributeManager.CacheTypes();
            List<Type> types = new List<Type>(SaveAttributeManager.CachedNonStaticTypes);
            
            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
            List<GameObject> gameObjectsToSave = new List<GameObject>();
            foreach (GameObject go in gameObjects)
            {
                if (go.GetComponentInParent<Savable>() != null)
                    continue;
                
                List<Component> savableComponents = new List<Component>(go.GetComponents<Component>());
                savableComponents.RemoveAll(x => x == null || !types.Contains(x.GetType()));
                if (savableComponents.Count > 0)
                    gameObjectsToSave.Add(go);
            }

            foreach (GameObject go in gameObjectsToSave)
            {
                Debug.LogError($"GameObject {go.name} has savable components but no Savable component. " +
                                 $"Add a Savable component to the GameObject or one of its parents. " +
                                    $"Otherwise the object may not be saved properly.", go);
            }
        }
    }
}
#endif