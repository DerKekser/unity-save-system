using System.Collections.Generic;

namespace Game.Scripts.SaveSystem.Data
{
    public class DataArray : IData
    {
        private List<IData> _data;

        public void Add(IData data)
        {
            if (_data == null)
                _data = new List<IData>();

            _data.Add(data);
        }

        public IData Get(int index)
        {
            return _data[index];
        }

        public T Get<T>(int index) where T : IData
        {
            return (T)_data[index];
        }

        public void Remove(IData data)
        {
            _data?.Remove(data);
        }

        public void RemoveAt(int index)
        {
            _data?.RemoveAt(index);
        }

        public int Count()
        {
            return _data?.Count ?? 0;
        }

        public IEnumerator<IData> GetEnumerator()
        {
            return _data?.GetEnumerator();
        }

        public IData this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public void DataSerialize(SaveBuffer saveBuffer)
        {
            int count = _data?.Count ?? 0;
            saveBuffer.SaveInt(count);

            if (_data != null)
            {
                foreach (var data in _data)
                {
                    saveBuffer.SaveType(data.GetType());
                    data.DataSerialize(saveBuffer);
                }
            }
        }

        public void DataDeserialize(SaveBuffer saveBuffer)
        {
            _data = new List<IData>();

            int count = saveBuffer.LoadInt();
            for (int i = 0; i < count; i++)
            {
                var type = saveBuffer.LoadType();
                var data = (IData)System.Activator.CreateInstance(type);
                data.DataDeserialize(saveBuffer);
                _data.Add(data);
            }
        }
    }
}