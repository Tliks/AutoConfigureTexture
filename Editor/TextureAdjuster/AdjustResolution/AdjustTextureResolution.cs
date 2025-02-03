using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;
using System.Threading.Tasks;

namespace com.aoyon.AutoConfigureTexture
{    
    public class AdjustTextureResolution : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        private Reduction _reduction;
        private bool _useGradient;
        private MaterialArea _materialArea;
        private Dictionary<TextureInfo, float> _intensities = new();

        public async Task Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config)
        {
            _reduction = config.ResolutionReduction;
            _useGradient = config.UseGradient;

            if (_reduction == Reduction.None)
                _shouldProcess = false;;
            _shouldProcess = true;

            if (_shouldProcess)
            {
                _materialArea = new MaterialArea(root.transform);

                _intensities = (await TextureGradientCalculator.CalculateGradientIntensityAsync(textureinfos.ToArray()))
                    .Select((intensity, index) => (textureinfos.ElementAt(index), intensity))
                    .ToDictionary(x => x.Item1, x => x.Item2);
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

            // | Reduction | MainTex    | NormalMap  | Emission   | AOMap     | NormalMapSub | Others    | MatCap    |
            // |-----------|------------|------------|------------|-----------|--------------|-----------|-----------|
            // | Low       |     -      |     -      |     -      |    -      |   1/2(512)   | 1/2(512)  | 1/2(256)  |
            // | Normal    | 1/2(512)H  | 1/2(512)H  | 1/2(512)   | 1/4(512)  |   1/4(512)   | 1/4(512)  | 1/4(256)  |
            // | High      | 1/2(512)   | 1/2(512)   | 1/4(512)   | 1/4(512)  |   1/4(512)   | 1/4(512)  | 1/4(256)  |
            // | Ultra     | 1/4(512)   | 1/4(512)   | 1/4(256)   | 1/4(256)  |   1/4(256)   | 1/4(256)  | 1/4(128)  |
            
            if (_reduction == Reduction.Low)
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
            else if (_reduction == Reduction.Normal)
            {
                switch (usage)
                {   
                    case TextureUsage.MainTex:
                    case TextureUsage.NormalMap:
                        TryReduceResolutionIfUnderHeight(ref resolution, 2, 512, info);
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
            else if (_reduction == Reduction.High)
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
            else if (_reduction == Reduction.Ultra)
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

            data = new AdjustData<object>(resolution);
            return resolution != width;
        }

        // 解像度が指定された最小値を下回らないようにしつつ、指定された除数で解像度を減少させます。
        // 現在の値が既に最小値を下回っている場合は現在の値を用います。
        private static void TryReduceResolution(ref int currentValue, int divisor, int minimum)
        {
            if (!Mathf.IsPowerOfTwo(divisor)) 
                throw new InvalidOperationException("divisor must be a power of two");

            if (currentValue <= minimum)
                return;

            currentValue = Mathf.Max(currentValue / divisor, minimum);
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