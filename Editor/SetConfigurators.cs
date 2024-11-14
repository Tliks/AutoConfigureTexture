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
                .Select(c => c.TargetTexture.GetTexture()) // TTTInternal
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

                AssignPrimaryUsage(info);

                // 解像度の変更を試す
                if (AdjustTextureResolution(info, component.ResolutionReduction, materialArea, out var resolution))
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
                if (AdjustTextureFormat(info, component, out var format))
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

        public static void AssignPrimaryUsage(TextureInfo info)
        {
            // 不明な使用用途は無視し既知の情報のみで判断
            // パターン1: lilToonのみで不明プロパティが含まれていた場合
            //            重要なプロパティは抑えてると思われるため不明プロパティは無視
            // パターン2: lilToon以外のみの場合
            //           _MainTexのみusageとして返るが、それ以外で不明プロパティのみの場合は何もしない
            // パターン3: lilToonとそれ以外が混じっている場合
            //           lilToonの情報のみで処理されるためlilToon以外で重要な使用プロパティだった場合顕劣化が想定されるが、エッジケースなのでここでは想定しない
            var usages = info.Properties
                .Select(info => PropertyDictionary.GetTextureUsage(info.Shader, info.PropertyName))
                .OfType<TextureUsage>();
                
            // 不明プロパティのみの場合は何もしない
            if (!usages.Any())
            {
                info.PrimaryUsage = TextureUsage.Unknown;
            }
            else
            {
                info.PrimaryUsage = GetPrimaryUsage(usages);;
            }   

            return;

            // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
            // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
            TextureUsage GetPrimaryUsage(IEnumerable<TextureUsage> usages)
            {
                foreach (var usage in new[] { 
                    TextureUsage.MainTex, 
                    TextureUsage.NormalMap, 
                    TextureUsage.Emission, 
                    TextureUsage.AOMap, 
                    TextureUsage.NormalMapSub, 
                    TextureUsage.Others, 
                    TextureUsage.MatCap 
                })
                {
                    if (usages.Contains(usage))
                    {
                        return usage;
                    }
                }
                throw new InvalidOperationException();
            }
        }

        internal static bool AdjustTextureResolution(TextureInfo info, Reduction reduction, MaterialArea materialArea, out int resolution)
        {
            var texture = info.Texture as Texture2D;
            var propertyInfos = info.Properties;

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

            var usage = info.PrimaryUsage;
            if (usage == TextureUsage.Unknown)
                return false;

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
                        TryReduceResolution(ref resolution, 2, 512);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 2, 256);
                        break;
                }
            }
            else if (reduction == Reduction.Normal)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolutionIfUnderHeight(ref resolution, 2, 512);
                        break;
                    case TextureUsage.Emission:
                        TryReduceResolution(ref resolution, 2, 512);
                        break;
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 4, 512);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 4, 256);
                        break;
                }
            }
            else if (reduction == Reduction.High)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolution(ref resolution, 2, 512);
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 4, 512);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 4, 256);
                        break;
                }
            }
            else if (reduction == Reduction.Ultra)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolution(ref resolution, 4, 512);
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 4, 256);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 4, 128);
                        break;
                }
            }

            return resolution != width;

            // 解像度が指定された最小値を下回らないようにしつつ、指定された除数で解像度を減少させます。
            // 現在の値が既に最小値を下回っている場合は現在の値を用います。
            static void TryReduceResolution(ref int currentValue, int divisor, int minimum)
            {
                if (!Mathf.IsPowerOfTwo(divisor)) 
                    throw new InvalidOperationException("divisor must be a power of two");

                if (currentValue <= minimum)
                    return;

                currentValue = Mathf.Max(currentValue / divisor, minimum);
            }

            // テクスチャが使用されているマテリアルがアバターの下部でのみ使用されている条件を追加。
            // 靴などの目につきにくい箇所の解像度を下げることを意図しています。
            void TryReduceResolutionIfUnderHeight(ref int currentValue, int divisor, int minimum, float thresholdRatio = 0.5f)
            {
                if (!Mathf.IsPowerOfTwo(divisor)) 
                    throw new InvalidOperationException("divisor must be a power of two");

                if (currentValue <= minimum)
                    return;

                var materials = propertyInfos.Select(info => info.MaterialInfo.Material);
                if (materialArea.IsUnderHeight(materials, 0.5f))
                    currentValue = Mathf.Max(currentValue / divisor, minimum);
            }
      
        }

        // Todo: MSDFなど圧縮してはいけないテクスチャを除外できていない
        internal static bool AdjustTextureFormat(TextureInfo info, AutoConfigureTexture component, out TextureFormat format)
        {
            var tex = info.Texture as Texture2D;

            var current = info.Format;
            format = current;

            if (!component.OptimizeTextureFormat) return false;

            if (current == TextureFormat.DXT5Crunched || current == TextureFormat.DXT1Crunched){
                return false;
            }

            var mode = component.FormatMode;
            var currentBPP = MathHelper.FormatToBPP(current);

            var channels = info.Properties
                .Select(propertyInfo => PropertyDictionary.GetChannels(propertyInfo.Shader, propertyInfo.PropertyName));

            int maxChannel = channels.All(c => c != -1)
                ? channels.Max()
                : 4; // 不明な使用用途が一つでもあった場合は4チャンネルとして処理
        
            switch(maxChannel)
            {
                case 4:
                    // ノーマルマップ(4チャンネル)にBC5が入っている場合への対症療法
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else if (Utils.HasAlpha(tex))
                    {
                        if (mode == FormatMode.HighQuality){
                            format = TextureFormat.BC7;
                        }
                        else if (mode == FormatMode.Balanced){
                            format = info.PrimaryUsage == TextureUsage.MainTex 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT5;
                        }
                        else if (mode == FormatMode.LowDownloadSize){
                            format = TextureFormat.DXT5;
                        }
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
                        if (mode == FormatMode.HighQuality){
                            format = currentBPP >= 8d 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT1;
                            //Debug.LogWarning($"Conversion: {tex.name} {current} format with {currentBPP}bpp to {format} format with {MathHelper.FormatToBPP(format)}bpp");
                        }
                        else if (mode == FormatMode.Balanced){
                            /*
                            format = currentBPP >= 8  && info.PrimaryUsage == TextureUsage.MainTex
                                ? TextureFormat.BC7 
                                : TextureFormat.DXT1;
                            */
                            format = TextureFormat.DXT1;
                        }
                        else if (mode == FormatMode.LowDownloadSize){
                            format = TextureFormat.DXT1;
                        }
                    }
                    break;
                case 2:
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else
                    {
                        if (mode == FormatMode.HighQuality){
                            format = currentBPP >= 8d 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT1;
                        }
                        else{
                            format = TextureFormat.DXT1;
                        }
                    }
                    break;
                case 1:
                    format = TextureFormat.BC4;
                    break;
                default:
                    throw new InvalidOperationException();
            }

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
