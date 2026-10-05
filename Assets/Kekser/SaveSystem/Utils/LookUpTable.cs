using System;
using System.Collections.Generic;

namespace Kekser.SaveSystem.Utils
{
    public class LookUpTable
    {
        private DynamicArray _data = new DynamicArray();
        private List<string> _list = new List<string>();
        private Dictionary<string, int> _indices = new Dictionary<string, int>();
        
        public void Clear()
        {
            _list.Clear();
            _indices.Clear();
        }
        
        public int Add(string value)
        {
            if (_indices.TryGetValue(value, out int index))
                return index;
            
            _list.Add(value);
            _indices.Add(value, _list.Count - 1);
            return _list.Count - 1;
        }
        
        public string Get(int index)
        {
            return _list[index];
        }
        
        public byte[] PrependHeader(byte[] data)
        {
            _data.Data = null;
            _data.AddBytes(BitConverter.GetBytes(_list.Count));
            foreach (string value in _list)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
                _data.AddBytes(BitConverter.GetBytes(bytes.Length));
                _data.AddBytes(bytes);
            }
            _data.AddBytes(data);
            return _data.Data;
        }
        
        public byte[] RemoveHeader(byte[] data)
        {
            // The file's indices start at 0: anything left from an earlier save or load would shift them.
            Clear();
            _data.Data = data;
            int count = BitConverter.ToInt32(_data.RawData, 0);
            int offset = sizeof(int);
            for (int i = 0; i < count; i++)
            {
                int length = BitConverter.ToInt32(_data.RawData, offset);
                offset += sizeof(int);
                string value = System.Text.Encoding.UTF8.GetString(_data.GetBytes(offset, length));
                // Positional: older saves can hold the same string twice.
                if (!_indices.ContainsKey(value))
                    _indices.Add(value, _list.Count);
                _list.Add(value);
                offset += length;
            }
            return _data.GetBytes(offset, data.Length - offset);
        }
    }
}