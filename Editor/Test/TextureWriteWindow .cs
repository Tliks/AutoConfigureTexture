using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Test
{
    public class TextureWriteWindow : EditorWindow
    {
        [SerializeField] private float _edgeThreshold = 0.5f;
        [SerializeField] private SkinnedMeshRenderer _targetRenderer;
        [SerializeField] private Texture2D _targetTexture;
        private IslandHandler _islandHandler;

        private SerializedObject _serializedObject;
        private SerializedProperty _edgeThresholdProperty;
        private SerializedProperty _targetRendererProperty;
        private SerializedProperty _targetTextureProperty;

        [MenuItem("Tools/AutoConfigureTexture/TextureWriteWindow")]
        public static void ShowWindow()
        {
            GetWindow<TextureWriteWindow>("TextureWriteWindow");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _edgeThresholdProperty = _serializedObject.FindProperty(nameof(_edgeThreshold));
            _targetRendererProperty = _serializedObject.FindProperty(nameof(_targetRenderer));
            _targetTextureProperty = _serializedObject.FindProperty(nameof(_targetTexture));
            _islandHandler = new IslandHandler();
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            //EditorGUILayout.PropertyField(_edgeThresholdProperty);
            EditorGUILayout.PropertyField(_targetRendererProperty);
            EditorGUILayout.PropertyField(_targetTextureProperty);

            _serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Check Texture"))
            {
                if (_targetTexture == null)
                {
                    Debug.LogError("Target Texture is not set.");
                    return;
                }

                var textureinfo = new TextureInfo(_targetTexture);
                var texture = textureinfo.AssignReadbleTexture2D();

                var islands = _islandHandler.GetIslands(_targetRenderer.sharedMesh, 0, 0);

                TextureWrite.AnalyzeEdgeStrength(texture, islands);
            }
        }
    }
}