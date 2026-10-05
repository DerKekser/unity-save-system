using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Kekser.SaveSystem.Data
{
    [Preserve]
    public class DataObject : IData
    {
        private Dictionary<string, IData> _data;

        public void Add(string key, IData data)
        {
            if (_data == null)
                _data = new Dictionary<string, IData>();

            _data[key] = data;
        }

        public IData Get(string key)
        {
            IData value = null;
            _data?.TryGetValue(key, out value);
            return value;
        }

        public T Get<T>(string key) where T : IData
        {
            IData value = null;
            _data?.TryGetValue(key, out value);
            return value is T typed ? typed : default;
        }

        /// <summary>Stores a value of any supported type (shorthand for Add(key, new DataElement(value))).</summary>
        public void SetValue(string key, object value)
        {
            Add(key, new DataElement(value));
        }

        /// <summary>Reads a value stored with <see cref="SetValue"/> or a DataElement; false if the key is missing.</summary>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (Get(key) is DataElement element)
            {
                try
                {
                    value = element.ToObject<T>();
                    return true;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Could not read \"{key}\" as {typeof(T).Name}: {e.Message}");
                }
            }
            value = default;
            return false;
        }

        /// <summary>Reads a value, or <paramref name="fallback"/> if the key is missing (e.g. in older saves).</summary>
        public T GetValue<T>(string key, T fallback = default)
        {
            return TryGetValue(key, out T value) ? value : fallback;
        }

        public void Remove(string key)
        {
            _data?.Remove(key);
        }

        public bool ContainsKey(string key)
        {
            return _data?.ContainsKey(key) ?? false;
        }

        public int Count()
        {
            return _data?.Count ?? 0;
        }

        public IEnumerator<KeyValuePair<string, IData>> GetEnumerator()
        {
            return _data?.GetEnumerator();
        }

        public IEnumerable<KeyValuePair<string, IData>> GetEnumerable()
        {
            return _data;
        }

        public IData this[string key]
        {
            get => Get(key);
            set => Add(key, value);
        }

        public void DataSerialize(SaveBuffer saveBuffer)
        {
            int count = _data?.Count ?? 0;
            saveBuffer.SaveInt(count);

            if (_data != null)
            {
                foreach (var kvp in _data)
                {
                    saveBuffer.SaveString(kvp.Key);
                    saveBuffer.SaveType(kvp.Value.GetType());
                    kvp.Value.DataSerialize(saveBuffer);
                }
            }
        }

        public void DataDeserialize(SaveBuffer saveBuffer)
        {
            _data = new Dictionary<string, IData>();

            int count = saveBuffer.LoadInt();
            for (int i = 0; i < count; i++)
            {
                string key = saveBuffer.LoadString();
                var type = saveBuffer.LoadType();
                var data = (IData)System.Activator.CreateInstance(type);
                data.DataDeserialize(saveBuffer);
                _data[key] = data;
            }
        }
    }
}
