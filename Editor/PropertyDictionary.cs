using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public enum TextureUsage
    {
        MainTex,
        NormalMap,
        NormalMapSub, // メインのNormalMapと区別
        AOMap,
        MatCap,
        Emission,
        Others,
        Unknown
    }

    public class PropertyDictionary
    {
        enum TextureChannel
        {
            None,
            R,
            G,
            B,
            A,
            RG,
            RB,
            RA,
            GB,
            GA,
            BA,
            RGB,
            RGA,
            RGBA,
        }

        struct PropertyData
        {
            public TextureChannel OpaqueChannel;
            public TextureChannel TransparentChannel;
            public TextureUsage TextureUsage;
            public bool IsVertex;

            internal PropertyData(TextureChannel baseChannel, TextureUsage textureUsage = TextureUsage.Others, bool isVertex = false)
            {
                OpaqueChannel = baseChannel;
                TransparentChannel = baseChannel;
                TextureUsage = textureUsage;
                IsVertex = isVertex;
            }

            internal PropertyData(TextureChannel opaqueChannel, TextureChannel transparentChannel, TextureUsage usage = TextureUsage.Others, bool isVertex = false)
            {
                OpaqueChannel = opaqueChannel;
                TransparentChannel = transparentChannel;
                TextureUsage = usage;
                IsVertex = isVertex;
            }

        }

        static Dictionary<string, PropertyData> lilToonProperty = new Dictionary<string, PropertyData>()
        {
            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor.html メインカラー
            { "_MainTex",                new PropertyData(TextureChannel.RGB, TextureChannel.RGBA, TextureUsage.MainTex) }, // RGB/RGBA
            { "_MainGradationTex",       new PropertyData(TextureChannel.RGB) },
            { "_MainColorAdjustMask",    new PropertyData(TextureChannel.R) },
            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor_layer.html 2ndカラー
            { "_Main2ndTex",             new PropertyData(TextureChannel.RGBA, TextureUsage.MainTex) }, //RGB/RGBA
            { "_Main2ndBlendMask",       new PropertyData(TextureChannel.R) },

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html 2ndカラー(Dissolve)
            { "_Main2ndDissolveMask",    new PropertyData(TextureChannel.R) }, 
            { "_Main2ndDissolveNoiseMask",new PropertyData(TextureChannel.R) },

            // 2ndと同じ 3rdカラー
            { "_Main3rdTex",             new PropertyData(TextureChannel.RGBA, TextureUsage.MainTex) }, // RGB/RGBA
            { "_Main3rdBlendMask",       new PropertyData(TextureChannel.R) }, 
            { "_Main3rdDissolveMask",    new PropertyData(TextureChannel.R) },
            { "_Main3rdDissolveNoiseMask",new PropertyData(TextureChannel.R) },

            // https://lilxyzw.github.io/lilToon/ja_JP/color/alphamask.html アルファマスク
            { "_AlphaMask",              new PropertyData(TextureChannel.R) },

            // https://lilxyzw.github.io/lilToon/ja_JP/color/shadow.html // 影
            { "_ShadowStrengthMask",     new PropertyData(TextureChannel.R) }, // マスクと強度
            { "_ShadowBorderMask",       new PropertyData(TextureChannel.RGB, TextureUsage.AOMap) }, // //AO Map？ 多分RGB
            { "_ShadowBlurMask",         new PropertyData(TextureChannel.RGB) }, // ぼかし量マスク
            { "_ShadowColorTex",         new PropertyData(TextureChannel.RGBA) }, // 影色1
            { "_Shadow2ndColorTex",      new PropertyData(TextureChannel.RGBA) }, // 影色2
            { "_Shadow3rdColorTex",      new PropertyData(TextureChannel.RGBA) }, // 影色3

            // https://lilxyzw.github.io/lilToon/ja_JP/color/rimshade.html // RimShade
            { "_RimShadeMask",           new PropertyData(TextureChannel.R) }, // lilMaterialPropertyでisTex:false

            // https://lilxyzw.github.io/lilToon/ja_JP/color/emission.html 発光
            { "_EmissionMap",            new PropertyData(TextureChannel.RGB, TextureUsage.Emission) }, // 多分RGB
            { "_EmissionBlendMask",      new PropertyData(TextureChannel.RGBA) }, // マスク RGBA
            { "_EmissionGradTex",        new PropertyData(TextureChannel.RGB) }, // 多分RGB
            // 1と同じ
            { "_Emission2ndMap",         new PropertyData(TextureChannel.RGB, TextureUsage.Emission) },
            { "_Emission2ndBlendMask",   new PropertyData(TextureChannel.RGBA) },
            { "_Emission2ndGradTex",     new PropertyData(TextureChannel.RGB) },

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/normal.html ノーマルマップ
            { "_BumpMap",                new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMap) }, // ノーマルマップ
            { "_Bump2ndMap",             new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMap) }, // ノーマルマップ2nd
            { "_Bump2ndScaleMask",       new PropertyData(TextureChannel.R) }, // R ノーマルマップ2nd マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/anisotropy.html ノーマルマップ(異方性反射)
            { "_AnisotropyTangentMap",   new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMapSub) }, //異方性反射
            { "_AisotropyScaleMask",    new PropertyData(TextureChannel.R) }, // マスク
            { "_AnisotropyShiftNoiseMask",new PropertyData(TextureChannel.R) }, // ノイズ

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/backlight.html 逆光
            { "_BacklightColorTex",      new PropertyData(TextureChannel.RGBA) }, // 多分RGBA 色/マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/reflection.html 光沢
            { "_SmoothnessTex",          new PropertyData(TextureChannel.R) }, // 滑らかさ
            { "_MetallicGlossMap",       new PropertyData(TextureChannel.R) }, // 金属度
            { "_ReflectionColorTex",     new PropertyData(TextureChannel.RGBA) }, // 多分RGBA
            { "_ReflectionCubeColor",    new PropertyData(TextureChannel.RGBA) }, // 多分RGBA? CubeMap Fallback 

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/matcap.html マットキャップ
            { "_MatCapTex",              new PropertyData(TextureChannel.RGB, TextureUsage.MatCap) }, // 多分RGB
            { "_MatCapBlendMask",        new PropertyData(TextureChannel.RGB) }, // RGB
            { "_MatCapBumpMap",          new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMapSub) },
            // 同じ
            { "_MatCap2ndTex",           new PropertyData(TextureChannel.RGB, TextureUsage.MatCap) }, 
            { "_MatCap2ndBlendMask",     new PropertyData(TextureChannel.RGB) },
            { "_MatCap2ndBumpMap",       new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMapSub) },

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/rimlight.html リムライト
            { "_RimColorTex",            new PropertyData(TextureChannel.RGBA) }, // 多分RGBA

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/glitter.html ラメ
            { "_GlitterColorTex",        new PropertyData(TextureChannel.None) }, // HDR
            { "_GlitterColorTex_UVMode", new PropertyData(TextureChannel.None) }, // HDR
            { "_GlitterShapeTex",        new PropertyData(TextureChannel.RGBA) }, // RGBA

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/outline.html 輪郭線
            { "_OutlineTex",             new PropertyData(TextureChannel.RGBA) }, // 多分RGBA
            { "_OutlineWidthMask",       new PropertyData(TextureChannel.R, isVertex: true) }, // マスクと太さ
            { "_OutlineVectorTex",       new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMapSub) },

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/parallax.html 視差マップ
            { "_ParallaxMap",            new PropertyData(TextureChannel.R) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/audiolink.html Audiolink
            { "_AudioLinkMask",          new PropertyData(TextureChannel.None) }, // 不明
            //{ "_AudioLinkMask_ScrollRotate",new PropertyData(TextureChannel.None) }, // 不明
            //{ "_AudioLinkMask_UVMode",   new PropertyData(TextureChannel.None) }, // 不明

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html Dissolve
            { "_DissolveMask",           new PropertyData(TextureChannel.R) }, // R
            { "_DissolveNoiseMask",      new PropertyData(TextureChannel.R) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/fur.html  ファー
            { "_FurNoiseMask",           new PropertyData(TextureChannel.R) }, // R ノイズ
            { "_FurMask",                new PropertyData(TextureChannel.R) }, // R
            { "_FurLengthMask",          new PropertyData(TextureChannel.R) }, // R
            { "_FurVectorTex",           new PropertyData(TextureChannel.RGBA, TextureUsage.NormalMapSub, isVertex: true) },

            //{ "_TriMask",                new PropertyData(TextureChannel.None) }  // 不明
        };

        private static PropertyData? GetPropertyData(Shader shader, string property)
        {
            Dictionary<string, PropertyData> shaderDictionary = null;

            if (CheckShader.IslilToon(shader))
                shaderDictionary = lilToonProperty;

            if (shaderDictionary != null && shaderDictionary.TryGetValue(property, out var channelValues))
            {
                return channelValues;
            }
            else
            {
                return null;
            }
        }

        public static int GetChannels(Shader shader, string property)
        {
            var propertydata = GetPropertyData(shader, property);
            if (propertydata is PropertyData data)
            {
                TextureChannel channel = Utils.IsOpaqueShader(shader) 
                    ? data.OpaqueChannel
                    : data.TransparentChannel;

                switch (channel)
                {
                    case TextureChannel.None: 
                        return -1;
                    case TextureChannel.R:
                    case TextureChannel.G:
                    case TextureChannel.B:
                    case TextureChannel.A: 
                        return 1;
                    case TextureChannel.RG:
                    case TextureChannel.RB:
                    case TextureChannel.RA:
                    case TextureChannel.GB:
                    case TextureChannel.GA:
                    case TextureChannel.BA: 
                        return 2;
                    case TextureChannel.RGB:
                    case TextureChannel.RGA: 
                        return 3;
                    case TextureChannel.RGBA: 
                        return 4;
                    default: 
                        return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        public static TextureUsage? GetTextureUsage(Shader shader, string property)
        {
            // _MainTexはUnityの予約語らしいので辞書を使わず返す
            if (property == "_MainTex") 
                return TextureUsage.MainTex; 
            var data = GetPropertyData(shader, property);
            return data?.TextureUsage;
        }

        public static bool? IsVertexShader(Shader shader, string property)
        {
            return GetPropertyData(shader, property)?.IsVertex;
        }
    }
}