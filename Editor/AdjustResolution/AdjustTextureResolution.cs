using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace com.aoyon.AutoConfigureTexture
{    
    public class AdjustTextureResolution
    {
        private TextureArea _textureArea;
        public AdjustTextureResolution(Transform root)
        {
            _textureArea = new TextureArea(root);
        }

        internal bool Apply(TextureInfo info, Reduction reduction, out int resolution)
        {
            var texture = info.Texture;
            var propertyInfos = info.Properties;

            int width = texture.width;
            int height = texture.height;
            resolution = width;

            // TTTが対応してるか不明
            if (width != height)
            {
                return false;
            }

            if (reduction == Reduction.None)
                return false;

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
        }

        // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
        // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
        private static TextureUsage GetPrimaryUsage(IEnumerable<TextureUsage> usages)
        {
            foreach (var usage in new[] {
                TextureUsage.MainTex,
                TextureUsage.NormalMap,
                TextureUsage.Emission,
                TextureUsage.AOMap,
                TextureUsage.NormalMapSub,
                TextureUsage.Others,
                TextureUsage.MatCap })
            {
                if (usages.Contains(usage))
                {
                    return usage;
                }
            }
            throw new InvalidOperationException();
        }

        // 解像度が指定された最小値を下回らないようにしつつ、指定された除数で解像度を減少させます。
        // 現在の値が既に最小値を下回っている場合は現在の値を用います。
        private static void TryReduceResolution(ref int currentValue, int divisor, int minimum)
        {
            if ((divisor & (divisor - 1)) != 0) 
                throw new InvalidOperationException("divisor must be a power of two");

            if (currentValue <= minimum)
                return;

            currentValue = Mathf.Max(currentValue / divisor, minimum);
        }

        // テクスチャが使用されているマテリアルがアバターの下部でのみ使用されている条件を追加。
        // 靴などの目につきにくい箇所の解像度を下げることを意図しています。
        private void TryReduceResolutionIfUnderHeight(ref int currentValue, int divisor, int minimum, TextureInfo info, float thresholdRatio = 0.5f)
        {
            if ((divisor & (divisor - 1)) != 0) 
                throw new InvalidOperationException("divisor must be a power of two");

            if (currentValue <= minimum)
                return;

            if (_textureArea.IsUnderHeight(info, 0.5f))
                currentValue = Mathf.Max(currentValue / divisor, minimum);
        }
    }
}