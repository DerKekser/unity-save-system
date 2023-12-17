using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Kekser.SaveSystem.Data;
using UnityEngine;

namespace Kekser.SaveSystem.Attributes
{
    public static class SaveAttributeManager
    {
        private const string AssembliesToIgnoreRegex = @"^Unity\.|^UnityEngine\.|^mscorlib|^System\.|^Mono\.";
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Debug.Log("SaveAttributeManager initialized!");
            CacheTypes();
        }
        
        private static Dictionary<Type, FieldInfo[]> _cachedFields = new Dictionary<Type, FieldInfo[]>();
        private static Dictionary<Type, MethodInfo> _cachedSaveMethods = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> _cachedLoadMethods = new Dictionary<Type, MethodInfo>();
        
        private static List<Type> _cachedStaticTypes = new List<Type>();
        private static List<Type> _cachedNonStaticTypes = new List<Type>();

        private static bool _isCached = false;
        
        private static PrefabRegistry _prefabRegistry;
        
        public static Type[] CachedStaticTypes => _cachedStaticTypes.ToArray();
        public static Type[] CachedNonStaticTypes => _cachedNonStaticTypes.ToArray();
        
        private static bool TryAddToDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                return false;
            dictionary.Add(key, value);
            return true;
        }
        
        public static bool IsStatic(this Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        public static void CacheTypes()
        {
            if (_isCached)
                return;
            
            _prefabRegistry = PrefabRegistry.Registry;
            if (_prefabRegistry == null)
                throw new Exception("PrefabRegistry not found! Save/Load system will not work!");
            
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Dictionary<Type, FieldInfo[]> checkedFields = new Dictionary<Type, FieldInfo[]>();
            Dictionary<Type, MethodInfo[]> checkedMethods = new Dictionary<Type, MethodInfo[]>();
            
            foreach (Assembly assembly in assemblies)
            {
                if (Regex.IsMatch(assembly.FullName, AssembliesToIgnoreRegex))
                    continue;
                
                Type[] types = assembly.GetTypes();
                
                foreach (Type type in types)
                {
                    FieldInfo[] fields;
                    MethodInfo[] methods;

                    if(!checkedFields.TryGetValue(type, out fields))
                    {
                        fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        checkedFields.Add(type, fields);
                    }
                    if(!checkedMethods.TryGetValue(type, out methods))
                    {
                        methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        checkedMethods.Add(type, methods);
                    }
                    
                    List<FieldInfo> fieldList = new List<FieldInfo>();
                    List<MethodInfo> saveMethodList = new List<MethodInfo>();
                    List<MethodInfo> loadMethodList = new List<MethodInfo>();

                    foreach (FieldInfo field in fields)
                    {
                        if (Attribute.IsDefined(field, typeof(SavableAttribute)))
                            fieldList.Add(field);
                    }
                    
                    foreach (MethodInfo method in methods)
                    {
                        if (Attribute.IsDefined(method, typeof(SaveAttribute)))
                            saveMethodList.Add(method);
                        if (Attribute.IsDefined(method, typeof(LoadAttribute)))
                            loadMethodList.Add(method);
                    }
                    
                    if (fieldList.Count > 0 || saveMethodList.Count > 0 || loadMethodList.Count > 0)
                    {
                        if (saveMethodList.Count > 1)
                            Debug.LogError($"Multiple save methods found for type {type.Name}. This is not supported.");
                        if (loadMethodList.Count > 1)
                            Debug.LogError($"Multiple load methods found for type {type.Name}. This is not supported.");
                        
                        if (fieldList.Count > 0)
                            _cachedFields.TryAddToDictionary(type, fieldList.ToArray());
                        if (saveMethodList.Count > 0)
                            _cachedSaveMethods.TryAddToDictionary(type, saveMethodList.First());
                        if (loadMethodList.Count > 0)
                            _cachedLoadMethods.TryAddToDictionary(type, loadMethodList.First());
                        if (type.IsStatic() && !_cachedStaticTypes.Contains(type))
                            _cachedStaticTypes.Add(type);
                        else if (!type.IsStatic() && !_cachedNonStaticTypes.Contains(type))
                            _cachedNonStaticTypes.Add(type);
                    }
                    
                }
            }
            
            _isCached = true;
        }
        
        public static void Save(DataObject dataObject)
        {
            CacheTypes();
            
            DataArray staticData = new DataArray();
            foreach (Type type in _cachedStaticTypes)
            {
                DataObject staticObject = new DataObject();
                
                _cachedFields.TryGetValue(type, out FieldInfo[] fields);
                fields ??= new FieldInfo[0];
                _cachedSaveMethods.TryGetValue(type, out MethodInfo method);

                staticObject.Add("Type", new DataElement(type));
                
                DataArray fieldArray = new DataArray();
                foreach (FieldInfo field in fields)
                {
                    DataObject fieldObject = new DataObject();
                    fieldObject.Add("Name", new DataElement(field.Name));
                    fieldObject.Add("Value", new DataElement(field.GetValue(null)));
                    fieldArray.Add(fieldObject);
                }
                staticObject.Add("Fields", fieldArray);
                
                DataObject methodObject = new DataObject();
                method?.Invoke(null, new object[] {methodObject});
                staticObject.Add("Method", methodObject);
                
                staticData.Add(staticObject);
            }
            dataObject.Add("Static", staticData);
            
            Savable[] savables = GameObject.FindObjectsOfType<Savable>(true);
            DataArray savablesData = new DataArray();
            foreach (Savable savable in savables)
            {
                DataObject savableObject = new DataObject();
                savableObject.Add("Guid", new DataElement(savable.Guid));
                savableObject.Add("PrefabGuid", new DataElement(savable.PrefabGuid));
                SaveGameObject(savableObject, savable.gameObject);
                savablesData.Add(savableObject);
            }
            dataObject.Add("Savables", savablesData);
            
            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
            List<GameObject> gameObjectsToSave = new List<GameObject>();
            foreach (GameObject go in gameObjects)
            {
                if (go.GetComponentInParent<Savable>() != null)
                    continue;
                
                List<Component> savableComponents = new List<Component>(go.GetComponents<Component>());
                savableComponents.RemoveAll(x => x == null || !_cachedNonStaticTypes.Contains(x.GetType()));
                if (savableComponents.Count > 0)
                    gameObjectsToSave.Add(go);
            }
            DataArray gameObjectsData = new DataArray();
            foreach (GameObject go in gameObjectsToSave)
            {
                if (gameObjectsToSave.Count(x => x.name == go.name) > 1)
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
            List<Component> savables = new List<Component>(go.GetComponents<Component>());
            savables.RemoveAll(x => x == null || !_cachedNonStaticTypes.Contains(x.GetType()));
            
            DataArray components = new DataArray();
            for (int i = 0; i < savables.Count; i++)
            {
                DataObject component = new DataObject();
                Type type = savables[i].GetType();
                
                _cachedFields.TryGetValue(type, out FieldInfo[] fields);
                fields ??= new FieldInfo[0];
                _cachedSaveMethods.TryGetValue(type, out MethodInfo method);
                
                component.Add("Type", new DataElement(type));
                
                DataArray fieldArray = new DataArray();
                foreach (FieldInfo field in fields)
                {
                    DataObject fieldObject = new DataObject();
                    fieldObject.Add("Name", new DataElement(field.Name));
                    fieldObject.Add("Value", new DataElement(field.GetValue(savables[i])));
                    fieldArray.Add(fieldObject);
                }
                component.Add("Fields", fieldArray);
                
                DataObject methodObject = new DataObject();
                method?.Invoke(savables[i], new object[] {methodObject});
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
            for (int i = 0; i < staticData.Count(); i++)
            {
                DataObject staticObject = staticData.Get<DataObject>(i);

                Type type = staticObject.Get<DataElement>("Type").ToObject<Type>();
                if (!_cachedStaticTypes.Contains(type))
                {
                    Debug.LogError($"Type {type.Name} is not cached. This should not happen.");
                    continue;
                }
                
                _cachedFields.TryGetValue(type, out FieldInfo[] fields);
                fields ??= new FieldInfo[0];
                _cachedLoadMethods.TryGetValue(type, out MethodInfo method);
                
                DataArray fieldArray = staticObject.Get<DataArray>("Fields");
                for (int j = 0; j < fieldArray.Count(); j++)
                {
                    DataObject fieldObject = fieldArray.Get<DataObject>(j);
                    string fieldName = fieldObject.Get<DataElement>("Name").ToObject<string>();
                    FieldInfo field = fields.First(x => x.Name == fieldName);
                    field.SetValue(null, fieldObject.Get<DataElement>("Value").ToObject(field.FieldType));
                }
                
                DataObject methodObject = staticObject.Get<DataObject>("Method");
                method?.Invoke(null, new object[] {methodObject});
            }
            
            Savable[] sceneSavables = GameObject.FindObjectsOfType<Savable>(true);
            List<Savable> loadedSavables = new List<Savable>();
            DataArray savablesData = dataObject.Get<DataArray>("Savables");
            for (int i = 0; i < savablesData.Count(); i++)
            {
                DataObject savableObject = savablesData.Get<DataObject>(i);
                
                Guid guid = savableObject.Get<DataElement>("Guid").ToObject<Guid>();
                Guid prefabGuid = savableObject.Get<DataElement>("PrefabGuid").ToObject<Guid>();
                
                Savable savable = Array.Find(sceneSavables, s => s.Guid == guid);
                if (savable == null)
                {
                    Savable prefab = Array.Find(_prefabRegistry.Prefabs, s => s.PrefabGuid == prefabGuid);
                    if (prefab == null)
                    {
                        Debug.LogError($"Prefab with guid {prefabGuid} not found!");
                        continue;
                    }

                    savable = GameObject.Instantiate(prefab);
                }
                
                LoadGameObject(savableObject, savable.gameObject);
                loadedSavables.Add(savable);
            }
            
            foreach (Savable savable in sceneSavables)
            {
                if (!loadedSavables.Contains(savable))
                    GameObject.Destroy(savable.gameObject);
            }
            
            GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>(true);
            List<GameObject> gameObjectsToSave = new List<GameObject>();
            foreach (GameObject go in gameObjects)
            {
                if (go.GetComponentInParent<Savable>() != null)
                    continue;
                
                List<Component> savableComponents = new List<Component>(go.GetComponents<Component>());
                savableComponents.RemoveAll(x => x == null || !_cachedNonStaticTypes.Contains(x.GetType()));
                if (savableComponents.Count > 0)
                    gameObjectsToSave.Add(go);
            }
            DataArray gameObjectsData = dataObject.Get<DataArray>("GameObjects");
            for (int i = 0; i < gameObjectsData.Count(); i++)
            {
                DataObject gameObjectData = gameObjectsData.Get<DataObject>(i);
                
                string gameObjectName = gameObjectData.Get<DataElement>("Name").ToObject<string>();
                if (gameObjectsToSave.Count(x => x.name == gameObjectName) > 1)
                {
                    Debug.LogError($"Multiple GameObjects with name {gameObjectName} found. This is not supported.");
                    continue;
                }
                
                GameObject go = gameObjectsToSave.Find( g => g.name == gameObjectName);
                if (go == null)
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
            savables.RemoveAll(x => x == null || !_cachedNonStaticTypes.Contains(x.GetType()));
            
            DataArray components = dataObject.Get<DataArray>("Components");
            for (int i = 0; i < components.Count(); i++)
            {
                DataObject component = components.Get<DataObject>(i);
                Type type = component.Get<DataElement>("Type").ToObject<Type>();
                if (!_cachedNonStaticTypes.Contains(type))
                {
                    Debug.LogError($"Type {type.Name} is not cached. This should not happen.");
                    continue;
                }
                
                Component savable = savables.Find(x => x.GetType() == type);
                if (savable == null)
                {
                    Debug.LogError($"Component of type {type.Name} not found!");
                    continue;
                }
                savables.Remove(savable);
                
                _cachedFields.TryGetValue(type, out FieldInfo[] fields);
                fields ??= new FieldInfo[0];
                _cachedLoadMethods.TryGetValue(type, out MethodInfo method);
                
                DataArray fieldData = component.Get<DataArray>("Fields");
                for (int j = 0; j < fieldData.Count(); j++)
                {
                    DataObject fieldObject = fieldData.Get<DataObject>(j);
                    string fieldName = fieldObject.Get<DataElement>("Name").ToObject<string>();
                    FieldInfo field = fields.First(x => x.Name == fieldName);
                    field.SetValue(savable, fieldObject.Get<DataElement>("Value").ToObject(field.FieldType));
                }
                
                DataObject methodObject = component.Get<DataObject>("Method");
                method?.Invoke(savable, new object[] {methodObject});
            }
        }

        private static void LoadGameObject(DataObject dataObject, GameObject go)
        {
            LoadComponents(dataObject, go);
            
            List<GameObject> childList = new List<GameObject>();
            for (int i = 0; i < go.transform.childCount; i++)
                childList.Add(go.transform.GetChild(i).gameObject);

            DataArray children = dataObject.Get<DataArray>("Children");
            for (int i = 0; i < children.Count(); i++)
            {
                DataObject childObject = children.Get<DataObject>(i);

                string childName = childObject.Get<DataElement>("Name").ToObject<string>();
                GameObject child = childList.Find(x => x.name == childName);
                if (child == null)
                {
                    Debug.LogError($"Child with name {childName} not found!", go);
                    continue;
                }
                childList.Remove(child);
                if (child.TryGetComponent(out Savable _)) continue;
                
                LoadGameObject(childObject, child);
            }
        }

        private static string UniqueGameObjectName(GameObject go)
        {
            string name = go.name;
            if (go.transform.parent == null) return name;
            return $"{UniqueGameObjectName(go.transform.parent.gameObject)}/{name}";
        }
    }
}