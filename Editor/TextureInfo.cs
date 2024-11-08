using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureInfo
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

        private Texture2D _readableTexture;

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

        internal void AddProperty(PropertyInfo propertyInfo)
        {
            Properties.Add(propertyInfo);
        }

        // todo 何とかしてUVチャンネルを取得する
        public static List<TextureInfo> Collect(IEnumerable<Material> materials)
        {
            var textureInfos = new Dictionary<Texture, TextureInfo>();

            foreach (Material material in materials)
            {
                Shader shader = material.shader;

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

                    // UVチャンネルは仮
                    var propertyInfo = new PropertyInfo(material, shader, propertyName, 0);
                    textureInfo.AddProperty(propertyInfo);
                }
            }

            return textureInfos.Values.ToList();
        }

        public Texture2D AssignReadbleTexture2D()
        {
            if (_readableTexture != null) {
                return _readableTexture;
            }
            else {
                _readableTexture = Utils.EnsureReadableTexture2D(Texture as Texture2D);
                return _readableTexture;
            } 
        }
    }

    public readonly struct PropertyInfo
    {
        public readonly Material Material;
        public readonly Shader Shader; 
        public readonly string PropertyName;
        public readonly int UVchannel;
        public PropertyInfo(Material material, Shader shader, string propertyName, int uvchannel)
        {
            Material = material;
            Shader = shader;
            PropertyName = propertyName;
            UVchannel = uvchannel;
        }
    }
}