using Kekser.SaveSystem.Attributes;
using Kekser.SaveSystem.Data;
using UnityEngine;

namespace Kekser.SaveSystem.SavableComponents
{
    public class SavableTransform : MonoBehaviour
    {
        [Save]
        public void Save(DataObject dataObject)
        {
            dataObject.Add("Position", new DataElement(transform.position));
            dataObject.Add("Rotation", new DataElement(transform.rotation));
        }

        [Load]
        public void Load(DataObject dataObject)
        {
            transform.position = dataObject.Get<DataElement>("Position").ToObject<Vector3>();
            transform.rotation = dataObject.Get<DataElement>("Rotation").ToObject<Quaternion>();
        }
    }
}