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
        private SerializedProperty IsPCOnly;
        private SerializedProperty Exclude;

        private void OnEnable()
        {
            OptimizeTextureFormat = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeTextureFormat));
            FormatMode = serializedObject.FindProperty(nameof(AutoConfigureTexture.FormatMode));
            MaintainCrunch = serializedObject.FindProperty(nameof(AutoConfigureTexture.MaintainCrunch));
            OptimizeMipMap = serializedObject.FindProperty(nameof(AutoConfigureTexture.OptimizeMipMap));
            ResolutionReduction = serializedObject.FindProperty(nameof(AutoConfigureTexture.ResolutionReduction));
            IsPCOnly = serializedObject.FindProperty(nameof(AutoConfigureTexture.IsPCOnly));
            Exclude = serializedObject.FindProperty(nameof(AutoConfigureTexture.Exclude));
        }

        public override void OnInspectorGUI()
        {
            Localization.DrawLanguageSwitcher();
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
            PropertyField(IsPCOnly);
            PropertyField(Exclude);
            serializedObject.ApplyModifiedProperties();
        }

        private void PropertyField(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property, $"AutoConfigureTexture:prop:{property.name}".LG());
        }

        [MenuItem("CONTEXT/AutoConfigureTexture/Attach TextureConfigurator")]
        private static void AttachTextureConfigurators(MenuCommand command)
        {
            var component = command.context as AutoConfigureTexture;
            var go = SetTextureConfigurator.Apply(component);
            Undo.RegisterCreatedObjectUndo(go, "Auto Configure Texture Setup");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            // overrideしないのでコンポーネントは残す
            //DestroyImmediate(component);
        }
    }
}