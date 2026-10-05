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
            transform.SetPositionAndRotation(dataObject.GetValue("Position", transform.position),
                dataObject.GetValue("Rotation", transform.rotation));
            // A rigidbody keeps its old pose until the next physics step otherwise.
            if (TryGetComponent(out Rigidbody body))
            {
                bool sleeping = body.IsSleeping();
                body.position = transform.position;
                body.rotation = transform.rotation;
                if (sleeping)
                    body.Sleep();
            }
        }
    }
}
