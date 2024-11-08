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
            if (component == null || (!component.OptimizeTextureFormat && !component.OptimizeMipMap && component.ResolutionReduction == Reduction.None))
                return null;

            // 既にTextureConfiguratorを設定しているテクスチャを取得
            var exists = component.GetComponentsInChildren<TextureConfigurator>()
                .Select(c => c.TargetTexture.SelectTexture)
                .Where(t => t != null)
                .ToHashSet();

            var root = new GameObject("Auto Configure Texture");
            root.transform.SetParent(paerent);
        
            var materials = Utils.CollectMaterials(component.gameObject);
            var infos = TextureInfo.Collect(materials);

            AdjustTextureResolution adjuster = new AdjustTextureResolution(component.transform);;

            foreach (var info in infos)
            {
                var texture = info.Texture;

                // Texture2D以外は現状何もしない
                if (texture is not Texture2D tex2d) continue;
                // 既存の設定をoverrideしないようにする
                if (exists.Contains(tex2d)) continue;

                // TextureConfiguratorを生成
                var go = new GameObject(tex2d.name);
                go.transform.SetParent(root.transform, false);
                var textureConfigurator = go.AddComponent<TextureConfigurator>();
                var textureSelector = new TextureSelector(){ SelectTexture = tex2d};
                textureConfigurator.TargetTexture = textureSelector;

                // 解像度の変更を試す
                if (adjuster.Apply(info, component.ResolutionReduction, out var resolution))
                {
                    textureConfigurator.OverrideTextureSetting = true;
                    textureConfigurator.TextureSize = resolution;
                }

                // MipMapのオフを試す
                if (component.OptimizeMipMap && RemoveMipMap(info, out var removeMipMap))
                {
                    textureConfigurator.OverrideTextureSetting = true;
                    textureConfigurator.MipMap = !removeMipMap;
                }

                // 圧縮形式の変更を試す
                if (component.OptimizeTextureFormat && AdjustTextureFormat(info, tex2d, out var format))
                {
                    textureConfigurator.OverrideCompression = true;
                    var compressionSetting = textureConfigurator.CompressionSetting;
                    compressionSetting.UseOverride = true;
                    compressionSetting.OverrideTextureFormat = format;
                }
            }

            Undo.RegisterCreatedObjectUndo(root, "Auto Configure Texture Setup");

            return root;
        }

        internal static bool RemoveMipMap(TextureInfo info, out bool shouldRemove)
        {
            // 頂点シェーダーとしてのみ用いられている場合MipMapをオフ
            shouldRemove = info.Properties.All(p => PropertyDictionary.IsVertexShader(p.Shader, p.PropertyName) == true);
            return shouldRemove;
        }
        
        // Todo: MSDFなど圧縮してはいけないテクスチャを除外できていない
        internal static bool AdjustTextureFormat(TextureInfo info, Texture2D tex, out TextureFormat format)
        {
            var current = info.Format;

            var channels = info.Properties
                .Select(propertyInfo => PropertyDictionary.GetChannels(propertyInfo.Shader, propertyInfo.PropertyName));

            int maxChannel = channels.All(c => c != -1)
                ? channels.Max()
                : 4; // 不明な使用用途が一つでもあった場合は4チャンネルとして処理
            
            switch(maxChannel)
            {
                case 4:
                    // ノーマルマップ(4チャンネル)にBC5が入っている場合への対症療法
                    if (current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else if (Utils.HasAlpha(info))
                    {
                        var texture = Utils.EnsureReadableTexture2D(tex);
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
                    throw new InvalidOperationException();
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

    }
}
