using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kekser.SaveSystem.Utils;

namespace Kekser.SaveSystem
{
    public class LookUpSaveBuffer : SaveBuffer
    {
        public LookUpSaveBuffer() : base()
        {
        }
        
        public LookUpSaveBuffer(byte[] data) : base(data)
        {
        }
        
        public override byte[] Data
        {
            get => _lookUpTable.PrependHeader(base.Data);
            set => base.Data = _lookUpTable.RemoveHeader(value);
        }
    }
    
    public class SaveBuffer
    {
        protected static LookUpTable _lookUpTable = new LookUpTable();
        
        public SaveBuffer()
        {
        }
        
        public SaveBuffer(byte[] data)
        {
            Data = data;
        }
        
        private int _offset = 0;
        
        private DynamicArray _data = new DynamicArray();

        public virtual byte[] Data
        {
            get => _data.Data;
            set
            {
                _data.Data = value;
                _offset = 0;
            }
        }
        
        public void SaveBytes(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                SaveInt(0);
                return;
            }
            
            SaveInt(value.Length);
            _data.AddBytes(value);
        }
        public void SaveInt(int value)
        {
            _data.AddBytes(BitConverter.GetBytes(value));
        }
        public void SaveFloat(float value)
        {
            _data.AddBytes(BitConverter.GetBytes(value));
        }
        public void SaveBool(bool value)
        {
            _data.AddBytes(BitConverter.GetBytes(value));
        }
        public void SaveString(string value)
        {
            SaveInt(_lookUpTable.Add(value));
        }
        public void SaveVector2(UnityEngine.Vector2 value)
        {
            SaveFloat(value.x);
            SaveFloat(value.y);
        }
        public void SaveVector3(UnityEngine.Vector3 value)
        {
            SaveFloat(value.x);
            SaveFloat(value.y);
            SaveFloat(value.z);
        }
        public void SaveVector4(UnityEngine.Vector4 value)
        {
            SaveFloat(value.x);
            SaveFloat(value.y);
            SaveFloat(value.z);
            SaveFloat(value.w);
        }
        public void SaveQuaternion(UnityEngine.Quaternion value)
        {
            SaveFloat(value.x);
            SaveFloat(value.y);
            SaveFloat(value.z);
            SaveFloat(value.w);
        }
        
        public void SaveColor(UnityEngine.Color value)
        {
            SaveFloat(value.r);
            SaveFloat(value.g);
            SaveFloat(value.b);
            SaveFloat(value.a);
        }
        
        public void SaveType(Type type)
        {
            SaveString(type.AssemblyQualifiedName);
        }
        
        public void SaveGuid(Guid guid)
        {
            SaveBytes(guid.ToByteArray());
        }
        
        public void SaveGameObject(UnityEngine.GameObject gameObject)
        {
            PrefabRegistry prefabRegistry = PrefabRegistry.Registry;
            if (prefabRegistry == null)
                throw new Exception("PrefabRegistry not found! Save/Load system will not work!");

            Savable goSavable = gameObject.GetComponentInParent<Savable>(true);
            if (goSavable == null)
            {
                UnityEngine.Debug.LogError($"GameObject {gameObject.name} is not savable!");
                return;
            }
            
            Savable savable = Array.Find(prefabRegistry.Prefabs, p => p.PrefabGuid == goSavable.PrefabGuid);
            if (savable == null)
            {
                UnityEngine.Debug.LogError($"GameObject {gameObject.name} is not registered in PrefabRegistry!");
                return;
            }
            
            SaveGuid(savable.PrefabGuid);
        }
        
        public void SaveList(IList list)
        {
            Type type = list.GetType().GetGenericArguments()[0];
            SaveType(type);
            SaveInt(list.Count);
            for (int i = 0; i < list.Count; i++)
                Save(list[i]);
        }
        
        public void SaveDictionary(IDictionary dictionary)
        {
            Type keyType = dictionary.GetType().GetGenericArguments()[0];
            Type valueType = dictionary.GetType().GetGenericArguments()[1];
            SaveType(keyType);
            SaveType(valueType);
            SaveInt(dictionary.Count);
            foreach (object key in dictionary.Keys)
            {
                Save(key);
                Save(dictionary[key]);
            }
        }
        
        public void SaveArray(Array array)
        {
            Type type = array.GetType().GetElementType();
            SaveType(type);
            SaveInt(array.Length);
            foreach (object obj in array)
                Save(obj);
        }
        
        public void SaveClassOrStruct(object obj)
        {
            Type type = obj.GetType();
            SaveType(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                Save(field.GetValue(obj));
        }
        
        public void Save(object obj)
        {
            if (obj is byte[] bytes)
                SaveBytes(bytes);
            else if (obj is int i)
                SaveInt(i);
            else if (obj is float f)
                SaveFloat(f);
            else if (obj is bool b)
                SaveBool(b);
            else if (obj is string s)
                SaveString(s);
            else if (obj is UnityEngine.Vector2 v2)
                SaveVector2(v2);
            else if (obj is UnityEngine.Vector3 v3)
                SaveVector3(v3);
            else if (obj is UnityEngine.Vector4 v4)
                SaveVector4(v4);
            else if (obj is UnityEngine.Quaternion q)
                SaveQuaternion(q);
            else if (obj is UnityEngine.Color c)
                SaveColor(c);
            else if (obj is Type type)
                SaveType(type);
            else if (obj is Guid guid)
                SaveGuid(guid);
            else if (obj is UnityEngine.GameObject gameObject)
                SaveGameObject(gameObject);
            else if (obj is IList list)
                SaveList(list);
            else if (obj is IDictionary dictionary)
                SaveDictionary(dictionary);
            else if (obj.GetType().IsArray)
                SaveArray((Array)obj);
            else if (obj.GetType().IsEnum)
                SaveInt((int)obj);
            else if (obj.GetType().IsClass)
                SaveClassOrStruct(obj);
            else if (obj.GetType().IsValueType && !obj.GetType().IsPrimitive && !obj.GetType().IsEnum && !obj.GetType().IsEquivalentTo(typeof(decimal)))
                SaveClassOrStruct(obj);
            else
                UnityEngine.Debug.LogError($"Type {obj.GetType()} is not supported!");
        }
        
        public byte[] LoadBytes()
        {
            int length = LoadInt();
            byte[] value = _data.GetBytes(_offset, length);
            _offset += length;
            return value;
        }
        public int LoadInt()
        {
            int value = BitConverter.ToInt32(_data.RawData, _offset);
            _offset += sizeof(int);
            return value;
        }
        public float LoadFloat()
        {
            float value = BitConverter.ToSingle(_data.RawData, _offset);
            _offset += sizeof(float);
            return value;
        }
        public bool LoadBool()
        {
            bool value = BitConverter.ToBoolean(_data.RawData, _offset);
            _offset += sizeof(bool);
            return value;
        }
        public string LoadString()
        {
            return _lookUpTable.Get(LoadInt());
        }
        public UnityEngine.Vector2 LoadVector2()
        {
            UnityEngine.Vector2 value = new UnityEngine.Vector2();
            value.x = LoadFloat();
            value.y = LoadFloat();
            return value;
        }
        public UnityEngine.Vector3 LoadVector3()
        {
            UnityEngine.Vector3 value = new UnityEngine.Vector3();
            value.x = LoadFloat();
            value.y = LoadFloat();
            value.z = LoadFloat();
            return value;
        }
        public UnityEngine.Vector4 LoadVector4()
        {
            UnityEngine.Vector4 value = new UnityEngine.Vector4();
            value.x = LoadFloat();
            value.y = LoadFloat();
            value.z = LoadFloat();
            value.w = LoadFloat();
            return value;
        }
        public UnityEngine.Quaternion LoadQuaternion()
        {
            UnityEngine.Quaternion value = new UnityEngine.Quaternion();
            value.x = LoadFloat();
            value.y = LoadFloat();
            value.z = LoadFloat();
            value.w = LoadFloat();
            return value;
        }
        
        public UnityEngine.Color LoadColor()
        {
            UnityEngine.Color value = new UnityEngine.Color();
            value.r = LoadFloat();
            value.g = LoadFloat();
            value.b = LoadFloat();
            value.a = LoadFloat();
            return value;
        }
        
        public Type LoadType()
        {
            return Type.GetType(LoadString());
        }
        
        public Guid LoadGuid()
        {
            return new Guid(LoadBytes());
        }
        
        public UnityEngine.GameObject LoadGameObject()
        {
            PrefabRegistry prefabRegistry = PrefabRegistry.Registry;
            if (prefabRegistry == null)
                throw new Exception("PrefabRegistry not found! Save/Load system will not work!");
            
            Guid guid = LoadGuid();
            
            Savable savable = Array.Find(prefabRegistry.Prefabs, p => p.PrefabGuid == guid);
            if (savable == null)
            {
                UnityEngine.Debug.LogError($"Prefab with guid {guid} not found!");
                return null;
            }
            
            return savable.gameObject;
        }
        
        public IList LoadList()
        {
            Type type = LoadType();
            int count = LoadInt();
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
            for (int i = 0; i < count; i++)
                list.Add(Load(type));
            return list;
        }
        
        public List<T> LoadList<T>()
        {
            return (List<T>)LoadList();
        }

        public IDictionary LoadDictionary()
        {
            Type keyType = LoadType();
            Type valueType = LoadType();
            int count = LoadInt();
            IDictionary dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
            for (int i = 0; i < count; i++)
                dictionary.Add(Load(keyType), Load(valueType));
            return dictionary;
        }
        
        public Array LoadArray()
        {
            Type type = LoadType();
            int count = LoadInt();
            Array array = Array.CreateInstance(type, count);
            for (int i = 0; i < count; i++)
                array.SetValue(Load(type), i);
            return array;
        }
        
        public T[] LoadArray<T>()
        {
            return (T[])LoadArray();
        }
        
        public object LoadClassOrStruct()
        {
            Type type = LoadType();
            object obj = Activator.CreateInstance(type);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
                fields[i].SetValue(obj, Load(fields[i].FieldType));
            return obj;
        }
        
        public T LoadClassOrStruct<T>()
        {
            return (T)LoadClassOrStruct();
        }

        public object Load(Type type)
        {
            if (type == typeof(byte[]))
                return LoadBytes();
            if (type == typeof(int))
                return LoadInt();
            if (type == typeof(float))
                return LoadFloat();
            if (type == typeof(bool))
                return LoadBool();
            if (type == typeof(string))
                return LoadString();
            if (type == typeof(UnityEngine.Vector2))
                return LoadVector2();
            if (type == typeof(UnityEngine.Vector3))
                return LoadVector3();
            if (type == typeof(UnityEngine.Vector4))
                return LoadVector4();
            if (type == typeof(UnityEngine.Quaternion))
                return LoadQuaternion();
            if (type == typeof(UnityEngine.Color))
                return LoadColor();
            if (type == typeof(Type))
                return LoadType();
            if (type == typeof(Guid))
                return LoadGuid();
            if (type == typeof(UnityEngine.GameObject))
                return LoadGameObject();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return LoadList();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return LoadDictionary();
            if (type.IsArray)
                return LoadArray();
            if (type.IsEnum)
                return LoadInt();
            if (type.IsClass)
                return LoadClassOrStruct();
            if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && !type.IsEquivalentTo(typeof(decimal)))
                return LoadClassOrStruct();
            UnityEngine.Debug.LogError($"Type {type} is not supported!");
            return null;
        }

        public T Load<T>()
        {
            return (T)Load(typeof(T));
        }
    }
}