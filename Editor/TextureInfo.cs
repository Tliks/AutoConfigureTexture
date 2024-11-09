using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace com.aoyon.AutoConfigureTexture
{
    public readonly struct TextureInfo
    {
        public readonly Texture Texture;
        public readonly List<PropertyInfo> Properties;

        public readonly Type Type;
        public readonly TextureFormat Format;
        public readonly TextureImporterCompression Compression;
        public readonly int CompressionQuality;
        public readonly bool sRGBTexture;
        public readonly TextureImporterAlphaSource AlphaSource;
        public readonly bool AlphaIsTransparency;
        public readonly bool MipmapEnabled;
        public readonly bool isReadable;

        public TextureInfo(Texture texture)
        {
            Texture = texture;
            Properties = new List<PropertyInfo>();

            Type = default;
            Format = default;
            Compression = default;
            CompressionQuality = default;
            sRGBTexture = default;
            AlphaSource = default;
            AlphaIsTransparency = default;
            MipmapEnabled = default;
            isReadable = default;

            if (texture is Texture2D t)
            {
                Type = typeof(Texture2D);
                Format = t.format;
            }

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            if (importer is TextureImporter ti)
            {
                Compression = ti.textureCompression; 
                CompressionQuality = ti.compressionQuality;
                sRGBTexture = ti.sRGBTexture;
                AlphaSource = ti.alphaSource;
                AlphaIsTransparency = ti.alphaIsTransparency;
                MipmapEnabled = ti.mipmapEnabled;
                isReadable = ti.isReadable;
            }
        }

        public void AddProperty(PropertyInfo propertyInfo)
        {
            Properties.Add(propertyInfo);
        }

        internal static List<TextureInfo> Collect(GameObject root)
        {
            var textureInfos = new Dictionary<Texture, TextureInfo>();

            var materialInfos = MaterialInfo.Collect(root);
            foreach (var materialInfo in materialInfos)
            {
                var material = materialInfo.Material;
                var shader = material.shader;

                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);
                    if (propertyType != ShaderUtil.ShaderPropertyType.TexEnv)
                        continue;
                        
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);

                    Texture texture = material.GetTexture(propertyName);
                    if (texture == null)
                        continue;

                    if (!textureInfos.TryGetValue(texture, out var textureInfo))
                    {
                        textureInfo = new TextureInfo(texture);
                        textureInfos[texture] = textureInfo;
                    }

                    var propertyInfo = new PropertyInfo(materialInfo, shader, propertyName);
                    textureInfo.AddProperty(propertyInfo);
                }
            }

            return textureInfos.Values.ToList();
        }
    }

    public readonly struct PropertyInfo
    {
        public readonly MaterialInfo MaterialInfo;
        public readonly Shader Shader; 
        public readonly string PropertyName;
        public PropertyInfo(MaterialInfo materialInfo, Shader shader, string propertyName)
        {
            MaterialInfo = materialInfo;
            Shader = shader;
            PropertyName = propertyName;
        }
    }

    public class MaterialInfo
    {
        public readonly List<Renderer> Renderers = new();
        public readonly List<int> MaterialIndices = new();
        public readonly Material Material;
        private MaterialInfo(Material material)
        {
            Material = material;
        }

        private void AddReference(Renderer renderer, int index)
        {
            Renderers.Add(renderer);
            MaterialIndices.Add(index);
        }

        public static IEnumerable<MaterialInfo> Collect(GameObject root)
        {
            var materialInfos = new Dictionary<Material, MaterialInfo>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null) continue;

                    if (!materialInfos.TryGetValue(material, out var materialInfo))
                    {
                        materialInfo = new MaterialInfo(material);
                        materialInfos[material] = materialInfo;
                    }

                    materialInfo.AddReference(renderer, index);
                }
            }

            return materialInfos.Values;
        }
    }
}