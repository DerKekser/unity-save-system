using System;
using Kekser.SaveSystem.Utils;
using UnityEngine;
using UnityEngine.Scripting;

namespace Kekser.SaveSystem.Data
{
    [Preserve]
    public class DataElement : IData
    {
        // Either a value (encoded when the file is written, against that file's string table) or the bytes read from
        // a file (decoded against the string table of the file they came from).
        private object _value;
        private bool _hasValue;
        private byte[] _bytes;
        private LookUpTable _table;

        public DataElement()
        {
        }
        
        public DataElement(object data)
        {
            if (data is IData)
            {
                Debug.LogError("DataElement can't be initialized with IData");
                return;
            }
            
            _value = data;
            _hasValue = true;
        }

        private SaveBuffer Encode(LookUpTable table)
        {
            SaveBuffer buffer = new SaveBuffer { Table = table };
            if (_hasValue)
                buffer.Save(_value);
            return buffer;
        }
        
        // Every read starts at the beginning (an element can be read more than once) and returns a copy.
        public object ToObject(Type type)
        {
            if (_bytes != null)
                return new SaveBuffer(_bytes) { Table = _table }.Load(type);
            LookUpTable table = new LookUpTable();
            return new SaveBuffer(Encode(table).Data) { Table = table }.Load(type);
        }
        
        public T ToObject<T>()
        {
            return (T)ToObject(typeof(T));
        }

        public void DataSerialize(SaveBuffer saveBuffer)
        {
            saveBuffer.SaveBytes(_bytes ?? Encode(saveBuffer.Table).Data);
        }

        public void DataDeserialize(SaveBuffer saveBuffer)
        {
            _value = null;
            _hasValue = false;
            _bytes = saveBuffer.LoadBytes();
            _table = saveBuffer.Table;
        }
    }
}