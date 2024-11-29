using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [CustomEditor(typeof(AutoConfigureTexture))]
    public class AutoConfigureTextureEditor : Editor
    {
        private SerializedProperty OptimizeTextureFormat;
        private SerializedProperty FormatMode;
        private SerializedProperty MaintainCrunch;
        private SerializedProperty OptimizeMipMap;
        private SerializedProperty OptimizeMaterial;
        private SerializedProperty ResolutionReduction;

        private void OnEnable()
        {
            OptimizeTextureFormat = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeTextureFormat));
            FormatMode = serializedObject.FindProperty(nameof(AutoConfigureTexture.FormatMode));
            MaintainCrunch = serializedObject.FindProperty(nameof(AutoConfigureTexture.MaintainCrunch));
            OptimizeMipMap = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeMipMap));
            OptimizeMaterial = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeMaterial));
            ResolutionReduction = serializedObject.FindProperty(nameof(AutoConfigureTexture.ResolutionReduction));
        }

        public override void OnInspectorGUI()
        {
            L10n.SelectLanguageGUI();
            serializedObject.Update();
            PropertyField(OptimizeTextureFormat);
            if (OptimizeTextureFormat.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    PropertyField(FormatMode);
                    PropertyField(MaintainCrunch);
                }
            }
            PropertyField(OptimizeMipMap);
            PropertyField(OptimizeMaterial);
            PropertyField(ResolutionReduction);
            serializedObject.ApplyModifiedProperties();
        }

        private void PropertyField(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, L10n.G(property));
        }

        [MenuItem("CONTEXT/AutoConfigureTexture/Attach TextureConfigurator")]
        private static void SetTextureConfigurator(MenuCommand command)
        {
            var component = command.context as AutoConfigureTexture;
            var go = AttachConfigurators.Apply(component, component.transform);
            Undo.RegisterCreatedObjectUndo(go, "Auto Configure Texture Setup");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            // overrideしないのでコンポーネントは残す
            //DestroyImmediate(component);
        }
    }
}