using System;
using UnityEngine.Scripting;

namespace Kekser.SaveSystem.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    [Preserve]
    public class LoadAttribute : SaveSystemAttribute
    {
        
    }
}