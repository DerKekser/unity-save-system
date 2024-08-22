using System;
using UnityEngine.Scripting;

namespace Kekser.SaveSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    [Preserve]
    public class SavableAttribute : SaveSystemAttribute
    {
        
    }
}