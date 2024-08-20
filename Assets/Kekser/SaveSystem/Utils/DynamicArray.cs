using System;

namespace Kekser.SaveSystem.Utils
{
    public class DynamicArray
    {
        private int _capacity = 0;

        private int _length = 0;

        private byte[] _data;

        public DynamicArray()
        {
            _capacity = 1024;
            _data = new byte[1024];
        }

        public DynamicArray(int capacity)
        {
            _capacity = capacity;
            _data = new byte[capacity];
        }

        private void EnsureCapacity(int requiredLength)
        {
            if (requiredLength > _data.Length)
            {
                int newLength = _data.Length;
                while (newLength < requiredLength)
                    newLength <<= 1;

                byte[] newData = new byte[newLength];
                Buffer.BlockCopy(_data, 0, newData, 0, _length);
                _data = newData;
            }
        }

        public byte[] RawData => _data;

        public byte[] Data
        {
            get
            {
                byte[] result = new byte[_length];
                Buffer.BlockCopy(_data, 0, result, 0, _length);
                return result;
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    _data = new byte[_capacity];
                    _length = 0;
                }
                else
                {
                    EnsureCapacity(value.Length);
                    Buffer.BlockCopy(value, 0, _data, 0, value.Length);
                    _length = value.Length;
                }
            }
        }
        
        public void AddBytes(byte[] value)
        {
            EnsureCapacity(_length + value.Length);
            Buffer.BlockCopy(value, 0, _data, _length, value.Length);
            _length += value.Length;
        }
        
        public byte[] GetBytes(int index, int length)
        {
            byte[] value = new byte[length];
            Buffer.BlockCopy(_data, index, value, 0, length);
            return value;
        }
    }
}