using UnityEditor;
using UnityEngine;

namespace FPSParkour.EditorTools
{
    public class AssetAuthoring
    {
        readonly SerializedObject serialized;

        public AssetAuthoring(Object target) => serialized = new SerializedObject(target);

        public AssetAuthoring Ref(string field, Object value) => Apply(field, p => p.objectReferenceValue = value);
        public AssetAuthoring Str(string field, string value) => Apply(field, p => p.stringValue = value);
        public AssetAuthoring Int(string field, int value) => Apply(field, p => p.intValue = value);
        public AssetAuthoring Float(string field, float value) => Apply(field, p => p.floatValue = value);
        public AssetAuthoring Bool(string field, bool value) => Apply(field, p => p.boolValue = value);
        public AssetAuthoring Enum(string field, int value) => Apply(field, p => p.enumValueIndex = value);
        public AssetAuthoring Mask(string field, int value) => Apply(field, p => p.intValue = value);

        public AssetAuthoring Refs(string field, params Object[] values)
        {
            return Apply(field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });
        }

        public AssetAuthoring Vectors(string field, params Vector3[] values)
        {
            return Apply(field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).vector3Value = values[i];
            });
        }

        public AssetAuthoring Enums(string field, params int[] values)
        {
            return Apply(field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).enumValueIndex = values[i];
            });
        }

        public AssetAuthoring Apply(string field, System.Action<SerializedProperty> write)
        {
            SerializedProperty property = serialized.FindProperty(field);

            if (property == null)
                Debug.LogWarning($"No serialized field '{field}' on {serialized.targetObject.GetType().Name}");
            else
                write(property);

            return this;
        }

        public void Save() => serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
