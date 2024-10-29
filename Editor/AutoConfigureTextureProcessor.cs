using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{    
    public class CompressTextureProcessor
    {
        public static GameObject SetConfigurators(AutoConfigureTexture component, Transform paerent)
        {
            if (component == null || (component.OptimizeTextureFormat == false && component.OptimizeMipMap == false && component.ResizeTexture == false))
                return null;

            var infos = CollectTextureInfos(component.gameObject);

            var root = new GameObject("Auto Configure Texture");
            root.transform.SetParent(paerent);

            foreach (var info in infos)
            {
                Texture teture = info.Texture;
                List<PropertyInfo> properties = info.Properties;

                var go = new GameObject(teture.name);
                go.transform.SetParent(root.transform, false);
                var textureConfigurator = go.AddComponent<TextureConfigurator>();
                var textureSelector = new TextureSelector(){ SelectTexture = (Texture2D)teture};
                textureConfigurator.TargetTexture = textureSelector;

                if (AdjustTextureResolution(teture, properties, out var resolution) && component.ResizeTexture)
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.TextureSize = resolution;

                if (RemoveMipMap(info, out var removeMipMap) && component.OptimizeMipMap)
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.MipMap = !removeMipMap;

                if (AdjustTextureFormat(info, out var format) && component.OptimizeTextureFormat)
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
            
        internal static List<TextureInfo> CollectTextureInfos(GameObject obj)
        {
            var textureInfos = new Dictionary<Texture, TextureInfo>();

            var materials = obj.GetComponentsInChildren<Renderer>(true)
                               .SelectMany(renderer => renderer.sharedMaterials)
                               .Where(m => m != null)
                               .ToHashSet();

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

        internal static bool AdjustTextureResolution(Texture texture, List<PropertyInfo> propertyInfos, out int resolution)
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

        internal static bool AdjustTextureFormat(TextureInfo info, out TextureFormat format)
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
                    else if (HasAlpha(info))
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

        private static bool HasAlpha(TextureInfo info, float alphaThreshold = 0.99f)
        {
            Texture2D texture = info.Texture as Texture2D;
            if (!info.isReadable)
            {
                texture = CreateReadabeTexture2D(texture);
            }

            byte[] rawTextureData = texture.GetRawTextureData();
            int alphaThresholdByte = (int)(alphaThreshold * 255);

            int length = rawTextureData.Length / 4;
            bool hasAlpha = false;

            for (int i = 0; i < length; i++)
            {
                if (rawTextureData[i * 4 + 3] < alphaThresholdByte)
                {
                    hasAlpha = true;
                    break;
                }
            }

            return hasAlpha;
        }

        private static Texture2D CreateReadabeTexture2D(Texture2D texture2d)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                        texture2d.width,
                        texture2d.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(texture2d, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height);
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTextur2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readableTextur2D;
        }

    }
}
