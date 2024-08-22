using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Kekser.SaveSystem.Data
{
    [Preserve]
    public class DataElement : IData
    {
        private SaveBuffer _data;

        public DataElement()
        {
            _data = new SaveBuffer();
        }
        
        public DataElement(object data)
        {
            if (data is IData)
            {
                Debug.LogError("DataElement can't be initialized with IData");
                return;
            }
            
            _data = new SaveBuffer();
            _data.Save(data);
        }
        
        public object ToObject(Type type)
        {
            return _data.Load(type);
        }
        
        public T ToObject<T>()
        {
            return _data.Load<T>();
        }

        public void DataSerialize(SaveBuffer saveBuffer)
        {
            saveBuffer.SaveBytes(_data.Data);
        }

        public void DataDeserialize(SaveBuffer saveBuffer)
        {
            _data = new SaveBuffer(saveBuffer.LoadBytes());
        }
    }
}