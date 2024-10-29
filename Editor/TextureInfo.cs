using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace com.aoyon.AutoConfigureTexture
{
    internal readonly struct TextureInfo
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

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            if (importer is not TextureImporter ti)
            {
                Debug.LogError($"invalid AssetImporter: {importer.GetType()}");
                return;
            }

            if (texture is Texture2D t)
            {
                Type = typeof(Texture2D);
                Format = t.format;

                Compression = ti.textureCompression; 
                CompressionQuality = ti.compressionQuality;
                sRGBTexture = ti.sRGBTexture;
                AlphaSource = ti.alphaSource;
                AlphaIsTransparency = ti.alphaIsTransparency;
                MipmapEnabled = ti.mipmapEnabled;
                isReadable = ti.isReadable;
            }
            else
            {
                Debug.LogWarning($"not supporting format: {texture.GetType()}");
            }
        }

        public void AddProperty(PropertyInfo propertyInfo)
        {
            Properties.Add(propertyInfo);
        }
    }

    internal readonly struct PropertyInfo
    {
        public readonly Material Material;
        public readonly Shader Shader; 
        public readonly string PropertyName;
        public PropertyInfo(Material material, Shader shader, string propertyName)
        {
            Material = material;
            Shader = shader;
            PropertyName = propertyName;
        }
    }
}