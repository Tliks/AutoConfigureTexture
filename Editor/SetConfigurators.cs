using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{    
    public class AttachConfigurators
    {
        public static GameObject Apply(AutoConfigureTexture component, Transform paerent)
        {
            if (component == null || (component.OptimizeTextureFormat == false && component.OptimizeMipMap == false && component.ResizeTexture == false))
                return null;

            var materials = Utils.CollectMaterials(component.gameObject);
            var infos = CollectTextureInfos(materials);

            var root = new GameObject("Auto Configure Texture");
            root.transform.SetParent(paerent);

            foreach (var info in infos)
            {
                var texture = info.Texture;
                List<PropertyInfo> properties = info.Properties;

                if (!(texture is Texture2D tex2d))
                {
                    continue;
                }

                var go = new GameObject(tex2d.name);
                go.transform.SetParent(root.transform, false);
                var textureConfigurator = go.AddComponent<TextureConfigurator>();
                var textureSelector = new TextureSelector(){ SelectTexture = tex2d};
                textureConfigurator.TargetTexture = textureSelector;

                if (AdjustTextureResolution(tex2d, properties, out var resolution) && component.ResizeTexture)
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.TextureSize = resolution;

                if (RemoveMipMap(info, out var removeMipMap) && component.OptimizeMipMap)
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.MipMap = !removeMipMap;

                if (AdjustTextureFormat(info, tex2d, out var format) && component.OptimizeTextureFormat)
                {
                    textureConfigurator.OverrideCompression = true;
                }
                var compressionSetting = textureConfigurator.CompressionSetting;
                compressionSetting.UseOverride = true;
                compressionSetting.OverrideTextureFormat = format;
            }

            Undo.RegisterCreatedObjectUndo(root, "Auto Configure Texture Setup");

            return root;
        }
            
        internal static List<TextureInfo> CollectTextureInfos(IEnumerable<Material> materials)
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

                    var propertyInfo = new PropertyInfo(material, shader, propertyName);
                    textureInfo.AddProperty(propertyInfo);
                }
            }

            return textureInfos.Values.ToList();
        }

        internal static bool AdjustTextureResolution(Texture2D texture, List<PropertyInfo> propertyInfos, out int resolution)
        {
            int width = texture.width;
            int height = texture.height;
            resolution = width;
            if (width != height)
            {
                Debug.LogWarning("width is not same as height");
                return false;
            }

            if (propertyInfos.Any(info => info.PropertyName == "_MainTex"))
            {
                if (resolution > 512) resolution /= 2;
            }
            else
            {
                if (resolution > 512) resolution = 512;
            }
            
            return resolution != width;
        }

        internal static bool AdjustTextureFormat(TextureInfo info, Texture2D tex, out TextureFormat format)
        {
            var current = info.Format;

            int maxChannel = info.Properties
                .Select(propertyInfo => PropertyDictionary.GetChannels(propertyInfo.Shader, propertyInfo.PropertyName))
                .Max();
            
            switch(maxChannel)
            {
                case 4:
                    // ノーマルマップ(4チャンネル)にBC5が入っている場合への対症療法
                    if (current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else if (Utils.HasAlpha(tex))
                    {
                        format = TextureFormat.BC7;
                    }
                    else
                    {
                        goto case 3;
                    }
                    break;
                case 3:
                    if (current == TextureFormat.BC7)
                    {
                        format = current;
                    }
                    else
                    {
                        format = TextureFormat.DXT1;
                    }
                    break;
                case 2:
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else
                    {
                        format = TextureFormat.DXT1;
                    }
                    break;
                case 1:
                    format = TextureFormat.BC4;
                    break;
                default:
                    goto case 4;
            }

            var currentBPP = MathHelper.FormatToBPP(current);
            var BPP = MathHelper.FormatToBPP(format);
            if (BPP > currentBPP)
            {
                Debug.LogWarning($"Conversion cancelled: {nameof(current)} format with {currentBPP}bpp to {nameof(format)} format with {BPP}bpp");
                format = current;
            }

            return current != format;
        }
        internal static bool RemoveMipMap(TextureInfo info, out bool shouldRemove)
        {
            shouldRemove = info.Properties.All(p => PropertyDictionary.IsVertexShader(p.Shader, p.PropertyName));
            return shouldRemove;
        }

    }
}
