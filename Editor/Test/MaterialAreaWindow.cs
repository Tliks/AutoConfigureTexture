using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Test
{
    public class MaterialAreaWindow : EditorWindow
    {
        [SerializeField] private Transform _rootTransform;
        [SerializeField, Range(0f, 1f)] private float _heightThreshold = 0.5f;
        [SerializeField] private Material[] _targetMaterials;

        private SerializedObject _serializedObject;
        private SerializedProperty _rootTransformProperty;
        private SerializedProperty _heightThresholdProperty;
        private SerializedProperty _targetMaterialsProperty;

        [MenuItem("Tools/AutoConfigureTexture/Material Area Window")]
        public static void ShowWindow()
        {
            GetWindow<MaterialAreaWindow>("Material Area Window");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _rootTransformProperty = _serializedObject.FindProperty("_rootTransform");
            _heightThresholdProperty = _serializedObject.FindProperty("_heightThreshold");
            _targetMaterialsProperty = _serializedObject.FindProperty("_targetMaterials");
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            EditorGUILayout.PropertyField(_rootTransformProperty, new GUIContent("Root Transform"));
            EditorGUILayout.PropertyField(_heightThresholdProperty, new GUIContent("Height Threshold"));
            EditorGUILayout.PropertyField(_targetMaterialsProperty, new GUIContent("Target Materials"), true);

            _serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Check Materials"))
            {
                if (_rootTransform == null)
                {
                    Debug.LogError("Root Transform is not set.");
                    return;
                }

                if (_targetMaterials == null || _targetMaterials.Length == 0)
                {
                    Debug.LogError("Target Materials is not set.");
                    return;
                }

                var materialArea = new MaterialArea(_rootTransform);

                if (materialArea.IsUnderHeight(_targetMaterials, _heightThreshold))
                {
                    Debug.Log($"指定されたマテリアルは全て閾値以下です。");
                }
                else
                {
                    Debug.Log($"指定されたマテリアルの中に閾値を超えるものがあります。");
                }
            }
        }
    }
}