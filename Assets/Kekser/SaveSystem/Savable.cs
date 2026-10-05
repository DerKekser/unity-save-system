using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kekser.SaveSystem
{
    [DisallowMultipleComponent]
    public sealed class Savable : MonoBehaviour
    {
        // Live scene savables by guid, for the duplicate check in Awake (instead of a scene search per object).
        private static readonly Dictionary<Guid, Savable> _live = new Dictionary<Guid, Savable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _live.Clear();

        [SerializeField, GuidReadable]
        private string _guid;
        [SerializeField, GuidReadable]
        private string _prefabGuid;

        // An empty or invalid guid is replaced once and kept, so every read returns the same value.
        public Guid Guid
        {
            get
            {
                if (!Guid.TryParse(_guid, out Guid guid))
                {
                    guid = Guid.NewGuid();
                    _guid = guid.ToString();
                }
                return guid;
            }
        }

        public Guid PrefabGuid
        {
            get
            {
                if (!Guid.TryParse(_prefabGuid, out Guid guid))
                {
                    guid = Guid.NewGuid();
                    _prefabGuid = guid.ToString();
                }
                return guid;
            }
        }

        /// <summary>
        /// Sets a fixed guid, e.g. one derived from the object's name, so generated scenes keep their ids across
        /// rebuilds. In the editor, record the change (SerializedObject or Undo) so the scene stores it.
        /// </summary>
        public void SetGuid(Guid guid)
        {
            Unregister();
            _guid = guid.ToString();
            if (Application.isPlaying && gameObject.scene.name != null)
                Register();
        }

        private void Reset()
        {
            _guid = "";
            _prefabGuid = "";

            OnValidate();
        }

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

            Savable[] savables = SaveSystemUtils.FindAll<Savable>();
            HashSet<string> guids = new HashSet<string>();
            for (int i = 0; i < savables.Length; i++)
                if (savables[i] != this)
                    guids.Add(savables[i]._guid);

            if (string.IsNullOrEmpty(_guid) || !Guid.TryParse(_guid, out _) || guids.Contains(_guid))
            {
                _guid = Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
            Savable prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(this);
            if (prefab == null)
            {
                _prefabGuid = "";
                return;
            }
            if (prefab._prefabGuid != _prefabGuid)
            {
                _prefabGuid = prefab._prefabGuid;
                UnityEditor.EditorUtility.SetDirty(this);
            }

#endif
        }

        private void Awake()
        {
            if (gameObject.scene.name == null)
                return;

            // A copy of a scene object (Instantiate) arrives with the original's guid: give it its own.
            if (!Guid.TryParse(_guid, out Guid guid)
                || _live.TryGetValue(guid, out Savable other) && other != null && other != this)
                _guid = Guid.NewGuid().ToString();
            Register();
        }

        private void OnDestroy() => Unregister();

        private void Register() => _live[Guid] = this;

        private void Unregister()
        {
            if (Guid.TryParse(_guid, out Guid guid) && _live.TryGetValue(guid, out Savable savable) && savable == this)
                _live.Remove(guid);
        }
    }

    internal static class SaveSystemUtils
    {
        public static T[] FindAll<T>() where T : UnityEngine.Object
        {
#if UNITY_6000_4_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
#elif UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>(true);
#endif
        }
    }
}
