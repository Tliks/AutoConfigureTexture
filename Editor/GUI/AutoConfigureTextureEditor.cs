using com.aoyon.AutoConfigureTexture.Build;
using com.aoyon.AutoConfigureTexture.Processor;
using nadena.dev.ndmf.runtime;

namespace com.aoyon.AutoConfigureTexture.GUI;

[CustomEditor(typeof(AutoConfigureTexture))]
public class AutoConfigureTextureEditor : Editor
{
    private SerializedProperty Quality = null!;
    void OnEnable()
    {
        Quality = serializedObject.FindProperty(nameof(AutoConfigureTexture.Quality));
    }

    public override void OnInspectorGUI()
    {
        Localization.DrawLanguageSwitcher();
        serializedObject.Update();

        DrawQuality();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawQuality()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("軽量化 ←");
            EditorGUILayout.Slider(Quality, 0.0f, 1.0f, GUIContent.none);
            GUILayout.Label("→ 品質維持");
        }
    }

    [MenuItem("CONTEXT/AutoConfigureTexture/Attach TextureConfigurator")]
    private static void AttachTextureConfigurators(MenuCommand command)
    {
        var component = (AutoConfigureTexture)command.context;
        var collector = new TextureInfoCollector();
        var root = RuntimeUtil.FindAvatarInParents(component.transform);
        if (root == null) return;
        var textureInfos = collector.Execute(root.gameObject);
        var decider = new TextureScaleDecider();
        var results = decider.Decide(textureInfos.ToList(), Mathf.Clamp01(component.Quality));
        foreach (var result in results)
        {
            Debug.Log(result.ToString());
        }
    }
}
