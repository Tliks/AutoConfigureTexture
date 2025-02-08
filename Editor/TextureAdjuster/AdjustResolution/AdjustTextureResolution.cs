using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{    
    internal class AdjustTextureResolution : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        private AutoConfigureTexture _config;
        private MaterialArea _materialArea;
        private Dictionary<TextureInfo, float> _intensities = new();

        public void Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config)
        {
            if (config.ResolutionReduction == Reduction.None)
                _shouldProcess = false;;
            _shouldProcess = true;

            _config = config;

            if (_shouldProcess)
            {   
                if (_config.UsePosition)
                {
                    _materialArea = new MaterialArea(root.transform);
                }

                if (config.UseGradient)
                {
                    _intensities = TextureGradientCalculator.CalculateGradientIntensityAsync(textureinfos.ToArray())
                        .Select((intensity, index) => (textureinfos.ElementAt(index), intensity))
                        .ToDictionary(x => x.Item1, x => x.Item2);
                }
            }
            return;
        }

        public bool Validate(TextureInfo info)
        {
            var texture = info.Texture as Texture2D;
            if (texture == null) return false;

            var usage = info.PrimaryUsage;
            if (usage == TextureUsage.Unknown)
                return false;

            return true;
        }

        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.TextureSize = info.Texture.width;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData<object> data)
        {
            configurator.OverrideTextureSetting = true;
            var resolution = (int)data.Data;
            configurator.TextureSize = resolution;
        }

        public bool Process(TextureInfo info, out AdjustData<object> data)
        {
            var texture = info.Texture as Texture2D;

            int width = texture.width;
            var resolution = width;

            var usage = info.PrimaryUsage;

            if (_intensities.TryGetValue(info, out var intensity))
            {
                Debug.Log($"Intensity {intensity} for {info.Texture.name}");
            }
            else
            {
                throw new Exception($"Intensity not found for texture: {info.Texture.name}");
            }

            var reduction = _config.ResolutionReduction;

            // | Reduction | MainTex    | NormalMap  | Emission   | AOMap     | NormalMapSub | Others    | MatCap    |
            // |-----------|------------|------------|------------|-----------|--------------|-----------|-----------|
            // | Low       |     -      |     -      |     -      |    -      |   1/2(512)   | 1/2(512)  | 1/2(256)  |
            // | Normal    | 1/2(512)H  | 1/2(512)H  | 1/2(512)   | 1/4(512)  |   1/4(512)   | 1/4(512)  | 1/4(256)  |
            // | High      | 1/2(512)   | 1/2(512)   | 1/4(512)   | 1/4(512)  |   1/4(512)   | 1/4(512)  | 1/4(256)  |
            // | Ultra     | 1/4(512)   | 1/4(512)   | 1/4(256)   | 1/4(256)  |   1/4(256)   | 1/4(256)  | 1/4(128)  |
            
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
                        TryReduceResolution(ref resolution, 1, 512, info);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 1, 256, info);
                        break;
                }
            }
            else if (reduction == Reduction.Normal)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolution(ref resolution, 0, 512, info);
                        break;
                    case TextureUsage.Emission:
                        TryReduceResolution(ref resolution, 1, 512, info);
                        break;
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 2, 512, info);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 2, 256, info);
                        break;
                }
            }
            else if (reduction == Reduction.High)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolution(ref resolution, 0, 512, info, 0.3f);
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 2, 512, info);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 2, 256, info);
                        break;
                }
            }
            else if (reduction == Reduction.Ultra)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolution(ref resolution, 1, 512, info);
                        break;
                    case TextureUsage.Emission:
                    case TextureUsage.AOMap:
                    case TextureUsage.NormalMapSub:
                    case TextureUsage.Others:
                        TryReduceResolution(ref resolution, 2, 256, info);
                        break;
                    case TextureUsage.MatCap:
                        TryReduceResolution(ref resolution, 2, 128, info);
                        break;
                }
            }

            data = new AdjustData<object>(resolution);
            return resolution != width;
        }

        private float GetUsagePririty(TextureUsage usage)
        {
            switch (usage)
            {   
                case TextureUsage.MainTex:
                case TextureUsage.NormalMap:
                    return 1.0f;
                case TextureUsage.Emission:
                    return 0.5f;
                case TextureUsage.AOMap:
                case TextureUsage.NormalMapSub:
                case TextureUsage.Others:
                    return 0.3f;
                case TextureUsage.MatCap:
                    return 0.2f;
                default:
                    throw new Exception("Unknown TextureUsage");
            }
        }

        private float GetReductionUsage(Reduction reduction)
        {
            switch (reduction)
            {
                case Reduction.Low:
                    return 1.0f;
                case Reduction.Normal:
                    return 0.8f;
                case Reduction.High:
                    return 0.5f;
                case Reduction.Ultra:
                    return 0.3f;
                default:
                    throw new Exception("Unknown Reduction");
            }
        }

        // 解像度が指定された最小値を下回らないようにしつつ、指定された除数で解像度を減少させます。
        // 現在の値が既に最小値を下回っている場合は現在の値を用います。
        private void TryReduceResolution(ref int currentValue, int baseStep, int minimum, TextureInfo info, float offset = 0f)
        {
            if (currentValue <= minimum)
                return;

            if (AdditionalDivisor(info, offset)) baseStep++;

            for (int i = 0; i < baseStep; i++)
            {
                currentValue = Mathf.Max(currentValue / 2, minimum);
            }
        }
        private bool AdditionalDivisor(TextureInfo info, float offset)
        {
            if (!_intensities.TryGetValue(info, out float identity)) throw new Exception();
            var reduction = Mathf.Lerp(1f, 0f, identity);
            return Mathf.RoundToInt(offset + reduction) != 0;
        }

        // テクスチャが使用されているマテリアルがアバターの下部でのみ使用されている条件を追加。
        // 靴などの目につきにくい箇所の解像度を下げることを意図しています。
        private void TryReduceResolutionIfUnderHeight(ref int currentValue, int divisor, int minimum, TextureInfo info, float thresholdRatio = 0.5f)
        {
            if (!Mathf.IsPowerOfTwo(divisor)) 
                throw new InvalidOperationException("divisor must be a power of two");

            if (currentValue <= minimum)
                return;
            
            var materials = info.Properties.Select(p => p.MaterialInfo.Material);
            if (_materialArea.IsUnderHeight(materials, 0.5f))
                currentValue = Mathf.Max(currentValue / divisor, minimum);
        }
    }
}