using System.Collections.Generic;

namespace Kekser.SaveSystem.Data
{
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
            _data.TryGetValue(key, out var value);
            return value;
        }

        public T Get<T>(string key) where T : IData
        {
            _data.TryGetValue(key, out var value);
            return (T)value;
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
