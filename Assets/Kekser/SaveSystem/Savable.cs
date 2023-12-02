using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Kekser.SaveSystem
{
    [DisallowMultipleComponent]
    public sealed class Savable : MonoBehaviour
    {
        [SerializeField, GuidReadable]
        private string _guid;
        [SerializeField, GuidReadable]
        private string _prefabGuid;
        
        public Guid Guid => string.IsNullOrEmpty(_guid) ? Guid.NewGuid() : Guid.Parse(_guid);
        public Guid PrefabGuid => string.IsNullOrEmpty(_prefabGuid) ? Guid.NewGuid() : Guid.Parse(_prefabGuid);

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            
            if (gameObject.scene.name == null)
            {
                _guid = "";

                if (string.IsNullOrEmpty(_prefabGuid) || !Guid.TryParse(_prefabGuid, out _))
                {
                    _prefabGuid = Guid.NewGuid().ToString();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
                
                return;
            }
            
            Savable[] savables = FindObjectsOfType<Savable>(true);
            List<string> guids = new List<string>();
            for (int i = 0; i < savables.Length; i++)
                if (savables[i] != this)
                    guids.Add(savables[i]._guid);

            if (string.IsNullOrEmpty(_guid) || !Guid.TryParse(_guid, out _) || guids.Contains(_guid))
            {
                _guid = Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
            GameObject prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (prefab == null)
            {
                _prefabGuid = "";
                return;
            }
            Savable savable = prefab.GetComponentInChildren<Savable>(true);
            if (savable != null && savable._prefabGuid != _prefabGuid)
            {
                _prefabGuid = savable._prefabGuid;
                UnityEditor.EditorUtility.SetDirty(this);
            }

#endif
        }

        private void Awake()
        {
            if (gameObject.scene.name == null)
                return;
            
            Savable[] savables = FindObjectsOfType<Savable>(true);
            bool found = false;
            for (int i = 0; i < savables.Length; i++)
            {
                if (savables[i] == this) continue;
                if (savables[i].Guid != Guid) continue;
                found = true;
                break;
            }
            
            if (string.IsNullOrEmpty(_guid) || found || !Guid.TryParse(_guid, out _))
                _guid = Guid.NewGuid().ToString();
        }
    }
}