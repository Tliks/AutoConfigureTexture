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

        public void Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config)
        {
            if (config.ResolutionReduction == Reduction.None)
                _shouldProcess = false;;
            _shouldProcess = true;

            _config = config;

            if (_shouldProcess)
            {   
                _materialArea = new MaterialArea(root.transform);
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
            var propertyInfos = info.Properties;

            int width = texture.width;
            var resolution = width;
            
            data = new AdjustData<object>(resolution);

            var reduction = _config.ResolutionReduction;
            if (reduction == Reduction.None)
                return false;

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
                if (_materialArea.IsUnderHeight(materials, 0.5f))
                    currentValue = Mathf.Max(currentValue / divisor, minimum);
            }
        }
    }
}