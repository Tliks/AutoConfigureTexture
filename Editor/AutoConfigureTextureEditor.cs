using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [CustomEditor(typeof(AutoConfigureTexture))]
    public class AutoConfigureTextureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            while(iterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(iterator);
            }
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/AutoConfigureTexture/Attach TextureConfigurator")]
        private static void SetTextureConfigurator(MenuCommand command)
        {
            var component = command.context as AutoConfigureTexture;
            var go = AttachConfigurators.Apply(component, component.transform);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            component.OptimizeTextureFormat = false;
            component.OptimizeMipMap = false;
            component.ResizeTexture = false;
            DestroyImmediate(component);
        }
    }
}