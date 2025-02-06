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
        private SerializedProperty ResolutionReduction;
        private SerializedProperty UseGradient;
        private SerializedProperty IsPCOnly;
        private SerializedProperty Exclude;

        private void OnEnable()
        {
            OptimizeTextureFormat = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeTextureFormat));
            FormatMode = serializedObject.FindProperty(nameof(AutoConfigureTexture.FormatMode));
            MaintainCrunch = serializedObject.FindProperty(nameof(AutoConfigureTexture.MaintainCrunch));
            OptimizeMipMap = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeMipMap));
            ResolutionReduction = serializedObject.FindProperty(nameof(AutoConfigureTexture.ResolutionReduction));
            UseGradient = serializedObject.FindProperty(nameof(AutoConfigureTexture.UseGradient));
            IsPCOnly = serializedObject.FindProperty(nameof(AutoConfigureTexture.IsPCOnly));
            Exclude = serializedObject.FindProperty(nameof(AutoConfigureTexture.Exclude));
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
            PropertyField(ResolutionReduction);
            if (ResolutionReduction.enumValueIndex != (int)Reduction.None)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    PropertyField(UseGradient);
                }
            }
            PropertyField(IsPCOnly);
            PropertyField(Exclude);
            serializedObject.ApplyModifiedProperties();
        }

        private void PropertyField(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, L10n.G(property));
        }

        [MenuItem("CONTEXT/AutoConfigureTexture/Attach TextureConfigurator")]
        private async static void AttachTextureConfigurators(MenuCommand command)
        {
            var component = command.context as AutoConfigureTexture;
            var go = await SetTextureConfigurator.Apply(component, component.transform);
            Undo.RegisterCreatedObjectUndo(go, "Auto Configure Texture Setup");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            // overrideしないのでコンポーネントは残す
            //DestroyImmediate(component);
        }
    }
}