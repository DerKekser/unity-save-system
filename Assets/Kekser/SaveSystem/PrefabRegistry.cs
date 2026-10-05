using UnityEngine;

namespace Kekser.SaveSystem
{
    public class PrefabRegistry : ScriptableObject
    {
        private static PrefabRegistry _prefabRegistry;
        // A missing registry is looked up again next time (it may have been created since).
        public static PrefabRegistry Registry
        {
            get
            {
                if (_prefabRegistry == null)
                    _prefabRegistry = Resources.Load<PrefabRegistry>("PrefabRegistry");
                return _prefabRegistry;
            }
        }
        
        [SerializeField]
        private Savable[] _prefabs;

        public Savable[] Prefabs
        {
            get => _prefabs;
#if UNITY_EDITOR
            set => _prefabs = value;
#endif
        }
    }
}