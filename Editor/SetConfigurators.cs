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

            MaterialArea materialArea = null;
            if (component.ResolutionReduction != Reduction.None)
                materialArea = new MaterialArea(component.transform);

            var infos = TextureInfo.Collect(component.gameObject);

            foreach (var info in infos)
            {
                var texture = info.Texture;
                List<PropertyInfo> properties = info.Properties;

                // Texture2D以外は現状何もしない
                if (texture is not Texture2D tex2d) continue;
                // 既存の設定をoverrideしないようにする
                if (exists.Contains(tex2d)) continue;

                // TextureConfiguratorを生成
                var go = new GameObject(tex2d.name);
                go.transform.SetParent(root.transform, false);
                var textureConfigurator = go.AddComponent<TextureConfigurator>();

                var textureSelector = new TextureSelector();
                textureSelector.Mode = TextureSelector.SelectMode.Relative;
                // 代表のプロパティ, Renderer, Material
                var property = properties.First();
                var materialInfo = property.MaterialInfo;
                textureSelector.RendererAsPath = materialInfo.Renderers.First();
                textureSelector.SlotAsPath = materialInfo.MaterialIndices.First();
                var propertyName = new net.rs64.TexTransTool.PropertyName(property.PropertyName);
                textureSelector.PropertyNameAsPath = propertyName;
                textureConfigurator.TargetTexture = textureSelector;

                // 解像度の変更を試す
                if (AdjustTextureResolution(tex2d, properties, component.ResolutionReduction, materialArea, out var resolution))
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.TextureSize = resolution;

                // MipMapのオフを試す
                if (RemoveMipMap(info, out var removeMipMap) && component.OptimizeMipMap)
                {
                    textureConfigurator.OverrideTextureSetting = true;
                }
                textureConfigurator.MipMap = !removeMipMap;

                // 圧縮形式の変更を試す
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
            
        internal static bool AdjustTextureResolution(Texture2D texture, List<PropertyInfo> propertyInfos, Reduction reduction, MaterialArea materialArea, out int resolution)
        {
            int width = texture.width;
            int height = texture.height;
            resolution = width;

            if (reduction == Reduction.None)
                return false;

            if (width != height)
            {
                Debug.LogWarning("width is not same as height");
                return false;
            }

            // 不明な使用用途は無視し既知の情報のみで判断
            // パターン1: lilToonのみで不明プロパティが含まれていた場合
            //            重要なプロパティは抑えてると思われるため不明プロパティは無視
            // パターン2: lilToon以外のみの場合
            //           _MainTexのみusageとして返るが、それ以外で不明プロパティのみの場合は何もしない
            // パターン3: lilToonとそれ以外が混じっている場合
            //           lilToonの情報のみで処理されるためlilToon以外で重要な使用プロパティだった場合顕劣化が想定されるが、エッジケースなのでここでは想定しない
            var usages = propertyInfos
                .Select(info => PropertyDictionary.GetTextureUsage(info.Shader, info.PropertyName))
                .OfType<TextureUsage>();
            // 不明プロパティのみの場合は何もしない
            if (!usages.Any())
                return false;

            var usage = GetPrimaryUsage(usages);

            if (reduction == Reduction.Low)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                        break;
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        if (resolution > 512) resolution /= 2;
                        break;
                    case TextureUsage.MatCap:
                        if (resolution > 256) resolution /= 2;
                        break;
                }
            }
            else if (reduction == Reduction.Normal)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        if (resolution > 512)
                        {
                            var materials = propertyInfos.Select(info => info.MaterialInfo.Material);
                            bool isUnderHips = materialArea.IsUnderHeight(materials, 0.5f);
                            if (isUnderHips)
                            {
                                resolution /= 2;
                            }
                        }
                        break;
                    case TextureUsage.Emission:
                        if (resolution > 512) resolution /= 2;
                        break;
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        if (resolution > 512)
                        {
                            resolution = Mathf.Max(resolution / 4, 512);
                        }
                        break;
                    case TextureUsage.MatCap:
                        if (resolution > 256) resolution /= 2;
                        break;
                }
            }
            else if (reduction == Reduction.High)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        if (resolution > 512) resolution /= 2;
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        if (resolution > 512)
                        {
                            resolution = Mathf.Max(resolution / 4, 512);
                        }
                        break;
                    case TextureUsage.MatCap:
                        if (resolution > 256) resolution /= 2;
                        break;
                }
            }
            else if (reduction == Reduction.Ultra)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        if (resolution > 512)
                        {
                            resolution = Mathf.Max(resolution / 4, 512);
                        }
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        if (resolution > 256)
                        {
                            resolution = Mathf.Max(resolution / 4, 256);
                        }
                        break;
                    case TextureUsage.MatCap:
                        if (resolution > 128)
                        {
                            resolution = Mathf.Max(resolution / 4, 128);
                        }
                        break;
                }
            }

            return resolution != width;
            
            // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
            // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
            TextureUsage GetPrimaryUsage(IEnumerable<TextureUsage> usages)
            {
                if (usages.Contains(TextureUsage.MainTex)){
                    return TextureUsage.MainTex;
                }
                else if (usages.Contains(TextureUsage.NormalMap)){
                    return TextureUsage.NormalMap;
                }
                else if (usages.Contains(TextureUsage.Emission)){
                    return TextureUsage.Emission;
                }
                else if (usages.Contains(TextureUsage.AOMap)){
                    return TextureUsage.AOMap;
                }
                else if (usages.Contains(TextureUsage.NormalMapSub)){
                    return TextureUsage.NormalMapSub;
                }
                else if (usages.Contains(TextureUsage.Others)){
                    return TextureUsage.Others;
                }
                else if (usages.Contains(TextureUsage.MatCap)){
                    return TextureUsage.MatCap;
                }
                else{
                    throw new InvalidOperationException();
                }
            }
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

        internal static bool RemoveMipMap(TextureInfo info, out bool shouldRemove)
        {
            // 頂点シェーダーとしてのみ用いられている場合MipMapをオフ
            shouldRemove = info.Properties.All(p => PropertyDictionary.IsVertexShader(p.Shader, p.PropertyName) == true);
            return shouldRemove;
        }

    }
}
