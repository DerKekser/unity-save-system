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

#if UNITY_6000_0_OR_NEWER
        private Vector3 Velocity { get => _rigidbody.linearVelocity; set => _rigidbody.linearVelocity = value; }
#else
        private Vector3 Velocity { get => _rigidbody.velocity; set => _rigidbody.velocity = value; }
#endif
        
        [Save]
        public void Save(DataObject dataObject)
        {
            dataObject.Add("Velocity", new DataElement(Velocity));
            dataObject.Add("AngularVelocity", new DataElement(_rigidbody.angularVelocity));
            dataObject.SetValue("IsKinematic", _rigidbody.isKinematic);
            dataObject.SetValue("Sleeping", _rigidbody.IsSleeping());
        }

        [Load]
        public void Load(DataObject dataObject)
        {
            _rigidbody.isKinematic = dataObject.GetValue("IsKinematic", _rigidbody.isKinematic);
            if (!_rigidbody.isKinematic)
            {
                Velocity = dataObject.GetValue("Velocity", Vector3.zero);
                _rigidbody.angularVelocity = dataObject.GetValue("AngularVelocity", Vector3.zero);
            }
            // A body that rested when saved stays at rest instead of settling (and sliding) again.
            if (dataObject.GetValue("Sleeping", false))
                _rigidbody.Sleep();
        }
    }
}
