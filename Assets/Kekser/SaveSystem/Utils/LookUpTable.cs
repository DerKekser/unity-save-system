using System;
using System.Collections.Generic;

namespace Kekser.SaveSystem.Utils
{
    public class LookUpTable
    {
        private DynamicArray _data = new DynamicArray();
        private List<string> _list = new List<string>();
        
        public void Clear()
        {
            _list.Clear();
        }
        
        public int Add(string value)
        {
            if (_list.Contains(value))
                return _list.IndexOf(value);
            
            _list.Add(value);
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
            _data.Data = data;
            int count = BitConverter.ToInt32(_data.RawData, 0);
            int offset = sizeof(int);
            for (int i = 0; i < count; i++)
            {
                int length = BitConverter.ToInt32(_data.RawData, offset);
                offset += sizeof(int);
                string value = System.Text.Encoding.UTF8.GetString(_data.GetBytes(offset, length));
                _list.Add(value);
                offset += length;
            }
            return _data.GetBytes(offset, _data.RawData.Length - offset);
        }
    }
}