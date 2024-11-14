using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [CustomEditor(typeof(AutoConfigureTexture))]
    public class AutoConfigureTextureEditor : Editor
    {
        private SerializedProperty OptimizeTextureFormat;
        private SerializedProperty FormatMode;
        private SerializedProperty OptimizeMipMap;
        private SerializedProperty ResolutionReduction;

        private void OnEnable()
        {
            OptimizeTextureFormat = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeTextureFormat));
            FormatMode = serializedObject.FindProperty(nameof(AutoConfigureTexture.FormatMode));
            OptimizeMipMap = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeMipMap));
            ResolutionReduction = serializedObject.FindProperty(nameof(AutoConfigureTexture.ResolutionReduction));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(OptimizeTextureFormat);
            if (OptimizeTextureFormat.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(FormatMode);
                }
            }
            EditorGUILayout.PropertyField(OptimizeMipMap);
            EditorGUILayout.PropertyField(ResolutionReduction);
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
    }
}