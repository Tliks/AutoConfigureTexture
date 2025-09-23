using com.aoyon.AutoConfigureTexture.Analyzer;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture.Adjuster
{    
    internal class AdjustTextureResolution : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        private AutoConfigureTexture _config = null!;

        public void Init(GameObject root, AutoConfigureTexture config)
        {
            if (config.ResolutionReduction == Reduction.None)
                _shouldProcess = false;;
            _shouldProcess = true;

            _config = config;
            return;
        }

        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.TextureSize = info.Texture2D.width;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData data)
        {
            configurator.OverrideTextureSetting = true;
            var resolution = data.GetData<int>();
            configurator.TextureSize = resolution;
        }

        public bool Process(TextureInfo info, TextureAnalyzer analyzer, [NotNullWhen(true)] out AdjustData? data)
        {
            data = null;

            var propertyInfos = info.Properties;

            int width = info.Texture2D.width;
            var resolution = width;
            
            var reduction = _config.ResolutionReduction;
            if (reduction == Reduction.None)
                return false;

            var usage = analyzer.PrimaryUsage(info);
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

            data = AdjustData.Create(resolution);
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

                if (analyzer.IsAllDrawingCoordinatesUnderHeight(info, thresholdRatio))
                    currentValue = Mathf.Max(currentValue / divisor, minimum);
            }
        }
    }
}