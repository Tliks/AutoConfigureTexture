using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransCore.Island;
using net.rs64.TexTransTool;
using net.rs64.TexTransTool.IslandSelector;
using net.rs64.TexTransTool.TextureAtlas;
using net.rs64.TexTransTool.TextureAtlas.IslandFineTuner;
using VRC.SDK3.Avatars.Components;
using net.rs64.TexTransCore;

namespace com.aoyon.AutoConfigureTexture
{    
    public class AttachAtlasTexture
    {
        public static GameObject Apply(AutoConfigureTexture component, Transform parent)
        {
            if (!component.ResizeIslands) return null;

            var descriptor = parent.GetComponentInParent<VRCAvatarDescriptor>();
            if (descriptor == null) throw new InvalidOperationException();
            var rootPos = descriptor.transform.position;
            var viewPos = descriptor.ViewPosition;
            var worldViewPos = rootPos + viewPos;
            
            var root = new GameObject("Auto Atlas Texture");
            root.transform.SetParent(parent);

            var materialInfos = MaterialInfo.Collect(component.gameObject);

            foreach (var materialInfo in materialInfos)
            {
                var material = materialInfo.Material;
                var mainTexture = Utils.GetMainTexture(material);
                if (mainTexture == null) continue;

                var go = new GameObject(material.name);
                go.transform.SetParent(root.transform, false);
                var atlasTexture = go.AddComponent<AtlasTexture>();
                atlasTexture.SelectMatList.Add(new AtlasTexture.MatSelector(){
                    Material = material,
                    MaterialFineTuningValue = 1.0f,
                });

                var atlasSetting = atlasTexture.AtlasSetting;
                atlasSetting.TextureFineTuning.Clear();
                atlasSetting.TextureIndividualFineTuning ??= new(); // -v0.8.4への対策

                var texsize = mainTexture.width;
                atlasSetting.AtlasTextureSize = texsize;
                atlasSetting.HeightDenominator = 2;
                //atlasSetting.ForceSizePriority = true;
                atlasSetting.IslandFineTuners.Add(new AutoResizeIslands(texsize, worldViewPos.y * 0.5f, 0.5f));}

            Undo.RegisterCreatedObjectUndo(root, "AttachAtlasTexture");
            return root;
        }
    }

    internal class AutoResizeIslands : IIslandFineTuner
    {
        public bool Enable = true;
        private readonly int _texsize;
        private readonly float _thresholdHeight;
        private readonly float _sizePriority;

        public AutoResizeIslands(int texsize, float thresholdHeight, float sizePriority)
        {
            _texsize = texsize;
            _thresholdHeight = thresholdHeight;
            _sizePriority = sizePriority;
        }

        public void LookAtCalling(ILookingObject lookingObject) {}

        // target materialに関する配列のみ返ってくる
        public void IslandFineTuning(float[] sizePriority, Island[] islands, IslandDescription[] islandDescriptions, IReplaceTracking replaceTracking)
        {
            if (!Enable) return;

            List<float> UVRatios = new();

            for (var i = 0; sizePriority.Length > i; i += 1)
            {
                var island = islands[i];
                var description = islandDescriptions[i];

                var uvratio = CalUVRatio(island, description) / 10000;
                UVRatios.Add(uvratio);
                uvratio = Mathf.Clamp01(uvratio);

                sizePriority[i] = Mathf.Lerp(1.0f, 0.5f, uvratio);
            }

            var renderer = islandDescriptions.First().Renderer;

            return;

            bool IsUnderHeight(Island island, IslandDescription description)
            {
                foreach (var tri in island.triangles)
                {
                    foreach (var vi in tri)
                    {
                        var vert = description.Position[vi];
                        if (vert.y < _thresholdHeight) 
                            return false;
                    }
                }
                return true;
            }

            float CalUVRatio(Island island, IslandDescription description)
            {
                float uvarea = default;
                float area = default;

                var texsizeoffset = _texsize * _texsize / 10000;

                foreach (var tri in island.triangles)
                {
                    var u1 = description.UV[tri.zero];
                    var u2 = description.UV[tri.one];
                    var u3 = description.UV[tri.two];
                    uvarea += Utils.CalculateArea(u1, u2, u3) * texsizeoffset;

                    var v1 = description.Position[tri.zero];
                    var v2 = description.Position[tri.one];
                    var v3 = description.Position[tri.two];
                    area += Utils.CalculateArea(v1, v2, v3);
                }

                // uvareaは外れ値を取りにくいので、inifinityの場合はareaが過剰に小さい
                return uvarea / area;
            }
        }
    }

    [CustomPropertyDrawer(typeof(AutoResizeIslands))]
    internal class AutoResizeIslandsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = 18f;
            EditorGUI.LabelField(position, "Auto Resize Islands (Auto Configure Texture)");
            position.y += 18;

            var enableAutoResize = property.FindPropertyRelative(nameof(AutoResizeIslands.Enable));
            EditorGUI.PropertyField(position, enableAutoResize, new GUIContent("Enable"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 2;
        }
    }
}   
