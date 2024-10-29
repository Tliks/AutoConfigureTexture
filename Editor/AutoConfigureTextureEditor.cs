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

        [MenuItem("CONTEXT/AutoConfigureTexture/Apply")]
        private static void CustomMenuOption(MenuCommand command)
        {
            var autoConfigureTexture = command.context as AutoConfigureTexture;
            var go = CompressTextureProcessor.SetConfigurators(autoConfigureTexture, autoConfigureTexture.transform);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            DestroyImmediate(autoConfigureTexture);
        }
    }
}