#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Kekser.SaveSystem
{
    [CustomPropertyDrawer(typeof(GuidReadableAttribute))]
    public class GuidReadablePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use CustomSceneAssetAttribute with string.");
                return;
            }
            
            string guid = property.stringValue;
            
            if (string.IsNullOrEmpty(guid))
            {
                EditorGUI.LabelField(position, label.text, "No GUID");
                return;
            }
            
            EditorGUI.LabelField(position, label.text, guid);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
    }
}
#endif