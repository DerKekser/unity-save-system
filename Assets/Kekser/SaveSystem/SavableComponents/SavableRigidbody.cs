using Kekser.SaveSystem.Attributes;
using Kekser.SaveSystem.Data;
using UnityEngine;

namespace Kekser.SaveSystem.SavableComponents
{
    [RequireComponent(typeof(Rigidbody))]
    public class SavableRigidbody : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
        
        [Save]
        public void Save(DataObject dataObject)
        {
            dataObject.Add("Velocity", new DataElement(_rigidbody.velocity));
            dataObject.Add("AngularVelocity", new DataElement(_rigidbody.angularVelocity));
        }

        [Load]
        public void Load(DataObject dataObject)
        {
            _rigidbody.velocity = dataObject.Get<DataElement>("Velocity").ToObject<Vector3>();
            _rigidbody.angularVelocity = dataObject.Get<DataElement>("AngularVelocity").ToObject<Vector3>();
        }
    }
}