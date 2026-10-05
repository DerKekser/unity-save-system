using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kekser.SaveSystem.Data;
using UnityEngine;

namespace Kekser.SaveSystem.Attributes
{
    public static class SaveAttributeManager
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                                 | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            CacheTypes();
        }

        private static Dictionary<Type, FieldInfo[]> _cachedFields = new Dictionary<Type, FieldInfo[]>();
        private static Dictionary<Type, MethodInfo> _cachedSaveMethods = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> _cachedLoadMethods = new Dictionary<Type, MethodInfo>();

        private static List<Type> _cachedStaticTypes = new List<Type>();
        private static List<Type> _cachedNonStaticTypes = new List<Type>();
        private static HashSet<Type> _nonStaticTypeSet = new HashSet<Type>();

        private static bool _isCached = false;


        public static Type[] CachedStaticTypes => _cachedStaticTypes.ToArray();
        public static Type[] CachedNonStaticTypes => _cachedNonStaticTypes.ToArray();

        public static bool IsStatic(this Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }

        // Members are collected along the base classes too, so a component inherits its base class's savable
        // fields; the most derived [Save]/[Load] method wins.
        private static void CollectMembers(Type type, List<FieldInfo> fields, out MethodInfo save, out MethodInfo load)
        {
            save = null;
            load = null;
            for (Type t = type; t != null && t != typeof(object) && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                if (t.Assembly.GlobalAssemblyCache || t.Namespace != null && t.Namespace.StartsWith("UnityEngine"))
                    break;

                foreach (FieldInfo field in t.GetFields(MemberFlags))
                {
                    if (Attribute.IsDefined(field, typeof(SavableAttribute)))
                        fields.Add(field);
                }

                MethodInfo[] methods = t.GetMethods(MemberFlags);
                MethodInfo[] saveMethods = methods.Where(m => Attribute.IsDefined(m, typeof(SaveAttribute))).ToArray();
                MethodInfo[] loadMethods = methods.Where(m => Attribute.IsDefined(m, typeof(LoadAttribute))).ToArray();
                if (saveMethods.Length > 1)
                    Debug.LogError($"Multiple save methods found for type {t.Name}. This is not supported.");
                if (loadMethods.Length > 1)
                    Debug.LogError($"Multiple load methods found for type {t.Name}. This is not supported.");
                if (save == null && saveMethods.Length > 0)
                    save = saveMethods[0];
                if (load == null && loadMethods.Length > 0)
                    load = loadMethods[0];
            }
        }

        public static void CacheTypes()
        {
            if (_isCached)
                return;

            if (PrefabRegistry.Registry == null)
                Debug.LogWarning("PrefabRegistry not found (Tools/Save System/Update Prefabs creates it). " +
                                 "Saved prefab instances can't be spawned again on load.");

            string definedIn = typeof(SaveSystemAttribute).Assembly.GetName().Name;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GlobalAssemblyCache
                    || assembly.GetName().Name != definedIn
                    && assembly.GetReferencedAssemblies().All(assemblyName => assemblyName.Name != definedIn))
                    continue;

                foreach (Type type in GetLoadableTypes(assembly))
                {
                    List<FieldInfo> fieldList = new List<FieldInfo>();
                    CollectMembers(type, fieldList, out MethodInfo saveMethod, out MethodInfo loadMethod);

                    if (fieldList.Count == 0 && saveMethod == null && loadMethod == null)
                        continue;

                    if (fieldList.Count > 0 && !_cachedFields.ContainsKey(type))
                        _cachedFields.Add(type, fieldList.ToArray());
                    if (saveMethod != null && !_cachedSaveMethods.ContainsKey(type))
                        _cachedSaveMethods.Add(type, saveMethod);
                    if (loadMethod != null && !_cachedLoadMethods.ContainsKey(type))
                        _cachedLoadMethods.Add(type, loadMethod);
                    if (type.IsStatic())
                    {
                        if (!_cachedStaticTypes.Contains(type))
                            _cachedStaticTypes.Add(type);
                    }
                    else if (_nonStaticTypeSet.Add(type))
                        _cachedNonStaticTypes.Add(type);
                }
            }

            _isCached = true;
        }

        private static bool IsSavableComponent(Component component)
        {
            return component != null && _nonStaticTypeSet.Contains(component.GetType());
        }

        private static bool HasSavableComponents(GameObject go)
        {
            foreach (Component component in go.GetComponents<Component>())
                if (IsSavableComponent(component))
                    return true;
            return false;
        }

        private static bool HasSavableData(Transform transform)
        {
            if (HasSavableComponents(transform.gameObject))
                return true;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.TryGetComponent(out Savable _) && HasSavableData(child))
                    return true;
            }
            return false;
        }

        // Inactive parents count too (GetComponentInParent skips them before 2021.2).
        internal static bool HasSavableInParents(Transform transform)
        {
            for (Transform t = transform; t != null; t = t.parent)
                if (t.TryGetComponent(out Savable _))
                    return true;
            return false;
        }

        // Scene objects outside any Savable that carry savable components; they are matched by name.
        private static List<GameObject> FindLooseGameObjects()
        {
            List<GameObject> result = new List<GameObject>();
            foreach (GameObject go in SaveSystemUtils.FindAll<GameObject>())
            {
                if (go.scene.name == null || HasSavableInParents(go.transform))
                    continue;
                if (HasSavableComponents(go))
                    result.Add(go);
            }
            return result;
        }

        private static DataArray SaveFields(Type type, object target)
        {
            _cachedFields.TryGetValue(type, out FieldInfo[] fields);
            DataArray fieldArray = new DataArray();
            if (fields == null)
                return fieldArray;
            foreach (FieldInfo field in fields)
            {
                DataObject fieldObject = new DataObject();
                fieldObject.Add("Name", new DataElement(field.Name));
                fieldObject.Add("Value", new DataElement(field.GetValue(target)));
                fieldArray.Add(fieldObject);
            }
            return fieldArray;
        }

        private static void LoadFields(Type type, object target, DataArray fieldArray)
        {
            _cachedFields.TryGetValue(type, out FieldInfo[] fields);
            if (fields == null || fieldArray == null)
                return;
            for (int j = 0; j < fieldArray.Count(); j++)
            {
                DataObject fieldObject = fieldArray.Get<DataObject>(j);
                string fieldName = fieldObject.Get<DataElement>("Name").ToObject<string>();
                FieldInfo field = fields.FirstOrDefault(x => x.Name == fieldName);
                if (field == null)
                {
                    Debug.LogWarning($"Saved field {type.Name}.{fieldName} no longer exists, skipped.");
                    continue;
                }
                try
                {
                    field.SetValue(target, fieldObject.Get<DataElement>("Value").ToObject(field.FieldType));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not load field {type.Name}.{fieldName}: {e.Message}");
                }
            }
        }

        private static void InvokeMethod(MethodInfo method, object target, DataObject methodObject, Type type)
        {
            if (method == null || methodObject == null)
                return;
            try
            {
                method.Invoke(target, new object[] { methodObject });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogError($"{method.Name} of {type.Name} failed: {e.InnerException}", target as UnityEngine.Object);
            }
        }

        public static void Save(DataObject dataObject)
        {
            CacheTypes();

            DataArray staticData = new DataArray();
            foreach (Type type in _cachedStaticTypes)
            {
                DataObject staticObject = new DataObject();
                staticObject.Add("Type", new DataElement(type));
                staticObject.Add("Fields", SaveFields(type, null));

                _cachedSaveMethods.TryGetValue(type, out MethodInfo method);
                DataObject methodObject = new DataObject();
                InvokeMethod(method, null, methodObject, type);
                staticObject.Add("Method", methodObject);

                staticData.Add(staticObject);
            }
            dataObject.Add("Static", staticData);

            Savable[] savables = SaveSystemUtils.FindAll<Savable>();
            HashSet<Guid> savedGuids = new HashSet<Guid>();
            DataArray savablesData = new DataArray();
            foreach (Savable savable in savables)
            {
                if (savable.gameObject.scene.name == null)
                    continue;
                if (!savedGuids.Add(savable.Guid))
                {
                    Debug.LogWarning($"Savable {savable.name} shares its guid with another object; it gets a new one.", savable);
                    savable.SetGuid(Guid.NewGuid());
                    savedGuids.Add(savable.Guid);
                }

                DataObject savableObject = new DataObject();
                savableObject.Add("Guid", new DataElement(savable.Guid));
                savableObject.Add("PrefabGuid", new DataElement(savable.PrefabGuid));
                SaveGameObject(savableObject, savable.gameObject);
                savablesData.Add(savableObject);
            }
            dataObject.Add("Savables", savablesData);

            List<GameObject> gameObjectsToSave = FindLooseGameObjects();
            HashSet<string> duplicates = new HashSet<string>(gameObjectsToSave.GroupBy(go => go.name)
                .Where(g => g.Count() > 1).Select(g => g.Key));
            DataArray gameObjectsData = new DataArray();
            foreach (GameObject go in gameObjectsToSave)
            {
                if (duplicates.Contains(go.name))
                {
                    Debug.LogError($"Multiple GameObjects with name {go.name} found. This is not supported.", go);
                    continue;
                }

                DataObject gameObject = new DataObject();
                gameObject.Add("Name", new DataElement(go.name));
                SaveComponents(gameObject, go);
                gameObjectsData.Add(gameObject);
            }
            dataObject.Add("GameObjects", gameObjectsData);
        }

        private static void SaveComponents(DataObject dataObject, GameObject go)
        {
            DataArray components = new DataArray();
            foreach (Component savable in go.GetComponents<Component>())
            {
                if (!IsSavableComponent(savable))
                    continue;

                DataObject component = new DataObject();
                Type type = savable.GetType();
                component.Add("Type", new DataElement(type));
                component.Add("Fields", SaveFields(type, savable));

                _cachedSaveMethods.TryGetValue(type, out MethodInfo method);
                DataObject methodObject = new DataObject();
                InvokeMethod(method, savable, methodObject, type);
                component.Add("Method", methodObject);

                components.Add(component);
            }
            dataObject.Add("Components", components);
        }

        private static void SaveGameObject(DataObject dataObject, GameObject go)
        {
            dataObject.Add("Name", new DataElement(go.name));
            SaveComponents(dataObject, go);

            DataArray children = new DataArray();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.TryGetComponent(out Savable _)) continue;
                // Children without anything to save (colliders, runtime helpers) stay out of the file.
                if (!HasSavableData(child.transform)) continue;

                DataObject childObject = new DataObject();
                SaveGameObject(childObject, child);

                children.Add(childObject);
            }
            dataObject.Add("Children", children);
        }

        public static void Load(DataObject dataObject)
        {
            CacheTypes();

            DataArray staticData = dataObject.Get<DataArray>("Static");
            for (int i = 0; i < (staticData?.Count() ?? 0); i++)
            {
                DataObject staticObject = staticData.Get<DataObject>(i);

                Type type = staticObject.Get<DataElement>("Type").ToObject<Type>();
                if (type == null || !_cachedStaticTypes.Contains(type))
                {
                    Debug.LogWarning($"Saved static type {type?.Name ?? "(unknown)"} no longer has savable members, skipped.");
                    continue;
                }

                LoadFields(type, null, staticObject.Get<DataArray>("Fields"));
                _cachedLoadMethods.TryGetValue(type, out MethodInfo method);
                InvokeMethod(method, null, staticObject.Get<DataObject>("Method"), type);
            }

            Savable[] sceneSavables = SaveSystemUtils.FindAll<Savable>();
            Dictionary<Guid, Savable> sceneByGuid = new Dictionary<Guid, Savable>();
            foreach (Savable savable in sceneSavables)
            {
                if (savable.gameObject.scene.name != null && !sceneByGuid.ContainsKey(savable.Guid))
                    sceneByGuid.Add(savable.Guid, savable);
            }
            PrefabRegistry prefabRegistry = PrefabRegistry.Registry;
            Savable[] prefabs = prefabRegistry != null && prefabRegistry.Prefabs != null
                ? prefabRegistry.Prefabs.Where(p => p != null).ToArray()
                : new Savable[0];
            HashSet<Savable> loadedSavables = new HashSet<Savable>();
            DataArray savablesData = dataObject.Get<DataArray>("Savables");
            for (int i = 0; i < (savablesData?.Count() ?? 0); i++)
            {
                DataObject savableObject = savablesData.Get<DataObject>(i);

                Guid guid = savableObject.Get<DataElement>("Guid").ToObject<Guid>();
                Guid prefabGuid = savableObject.Get<DataElement>("PrefabGuid").ToObject<Guid>();

                if (!sceneByGuid.TryGetValue(guid, out Savable savable))
                {
                    Savable prefab = Array.Find(prefabs, s =>s.PrefabGuid == prefabGuid);
                    if (prefab == null)
                    {
                        Debug.LogError($"Prefab with guid {prefabGuid} not found!");
                        continue;
                    }

                    savable = GameObject.Instantiate(prefab);
                    // Keep the saved id, so the object stays the same one across further saves.
                    savable.SetGuid(guid);
                }

                LoadGameObject(savableObject, savable.gameObject);
                loadedSavables.Add(savable);
            }

            foreach (Savable savable in sceneSavables)
            {
                if (savable.gameObject.scene.name != null && !loadedSavables.Contains(savable))
                    GameObject.Destroy(savable.gameObject);
            }

            Dictionary<string, GameObject> byName = new Dictionary<string, GameObject>();
            HashSet<string> duplicates = new HashSet<string>();
            foreach (GameObject go in FindLooseGameObjects())
            {
                if (byName.ContainsKey(go.name))
                    duplicates.Add(go.name);
                else
                    byName.Add(go.name, go);
            }
            DataArray gameObjectsData = dataObject.Get<DataArray>("GameObjects");
            for (int i = 0; i < (gameObjectsData?.Count() ?? 0); i++)
            {
                DataObject gameObjectData = gameObjectsData.Get<DataObject>(i);

                string gameObjectName = gameObjectData.Get<DataElement>("Name").ToObject<string>();
                if (duplicates.Contains(gameObjectName))
                {
                    Debug.LogError($"Multiple GameObjects with name {gameObjectName} found. This is not supported.");
                    continue;
                }

                if (!byName.TryGetValue(gameObjectName, out GameObject go))
                {
                    Debug.LogError($"GameObject with name {gameObjectName} not found!");
                    continue;
                }

                LoadComponents(gameObjectData, go);
            }
        }

        private static void LoadComponents(DataObject dataObject, GameObject go)
        {
            List<Component> savables = new List<Component>(go.GetComponents<Component>());
            savables.RemoveAll(x => !IsSavableComponent(x));

            DataArray components = dataObject.Get<DataArray>("Components");
            for (int i = 0; i < (components?.Count() ?? 0); i++)
            {
                DataObject component = components.Get<DataObject>(i);
                Type type = component.Get<DataElement>("Type").ToObject<Type>();
                if (type == null || !_nonStaticTypeSet.Contains(type))
                {
                    Debug.LogWarning($"Saved component type {type?.Name ?? "(unknown)"} on {go.name} no longer has savable members, skipped.", go);
                    continue;
                }

                Component savable = savables.Find(x => x.GetType() == type);
                if (savable == null)
                {
                    Debug.LogError($"Component of type {type.Name} not found!", go);
                    continue;
                }
                savables.Remove(savable);

                LoadFields(type, savable, component.Get<DataArray>("Fields"));
                _cachedLoadMethods.TryGetValue(type, out MethodInfo method);
                InvokeMethod(method, savable, component.Get<DataObject>("Method"), type);
            }
        }

        private static void LoadGameObject(DataObject dataObject, GameObject go)
        {
            LoadComponents(dataObject, go);

            List<GameObject> childList = new List<GameObject>();
            for (int i = 0; i < go.transform.childCount; i++)
                childList.Add(go.transform.GetChild(i).gameObject);

            DataArray children = dataObject.Get<DataArray>("Children");
            for (int i = 0; i < (children?.Count() ?? 0); i++)
            {
                DataObject childObject = children.Get<DataObject>(i);

                string childName = childObject.Get<DataElement>("Name").ToObject<string>();
                GameObject child = childList.Find(x => x.name == childName);
                if (child == null)
                {
                    // Older saves list every child, also ones that held no data.
                    bool empty = (childObject.Get<DataArray>("Components")?.Count() ?? 0) == 0
                                 && (childObject.Get<DataArray>("Children")?.Count() ?? 0) == 0;
                    if (!empty)
                        Debug.LogError($"Child with name {childName} not found!", go);
                    continue;
                }
                childList.Remove(child);
                if (child.TryGetComponent(out Savable _)) continue;

                LoadGameObject(childObject, child);
            }
        }
    }
}
