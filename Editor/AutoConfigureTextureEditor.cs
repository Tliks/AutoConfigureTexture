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
            // overrideしないのでコンポーネントは残す
            //DestroyImmediate(component);
        }

        [MenuItem("CONTEXT/AutoConfigureTexture/Attach AtlasTexture")]
        private static void AttachAtlasTextureContext(MenuCommand command)
        {
            var component = command.context as AutoConfigureTexture;
            var go = AttachAtlasTexture.Apply(component, component.transform);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            // overrideしないのでコンポーネントは残す
            //DestroyImmediate(component);
        }

    }
}