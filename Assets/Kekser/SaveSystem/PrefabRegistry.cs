using UnityEngine;

namespace Kekser.SaveSystem
{
    public class PrefabRegistry : ScriptableObject
    {
        private static PrefabRegistry _prefabRegistry;
        private static bool _isLoaded;
        public static PrefabRegistry Registry
        {
            get
            {
                if (!_isLoaded)
                {
                    _prefabRegistry = Resources.Load<PrefabRegistry>("PrefabRegistry");
                    _isLoaded = true;
                }
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