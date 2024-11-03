using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public class PropertyDictionary
    {
        public enum TextureChannel
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
        
        /// <summary>
        ///  lilToonにテクスチャを割り当てるプロパティに関する辞書
        ///  key: property
        ///  value: チャンネル(不透明), チャンネル(透明), isVertexOperation
        /// </summary>
        /// 
        static Dictionary<string, (TextureChannel, TextureChannel, bool)> lilToonProperty = new Dictionary<string, (TextureChannel, TextureChannel, bool)>()
        {
            //{ "_BaseMap",                (TextureChannel.None, TextureChannel.None, false) }, // Dummy
            //{ "_BaseColorMap",           (TextureChannel.None, TextureChannel.None, false) }, // Dummy

            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor.html メインカラー
            { "_MainTex",                (TextureChannel.RGB, TextureChannel.RGBA, false) }, // RGB/RGBA
            { "_MainGradationTex",       (TextureChannel.RGB, TextureChannel.RGB, false) },
            { "_MainColorAdjustMask",    (TextureChannel.R, TextureChannel.R, false) },
            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor_layer.html 2ndカラー
            { "_Main2ndTex",             (TextureChannel.RGB, TextureChannel.RGBA, false) }, //RGB/RGBA
            { "_Main2ndBlendMask",       (TextureChannel.R, TextureChannel.R, false) },

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html 2ndカラー(Dissolve)
            { "_Main2ndDissolveMask",    (TextureChannel.R, TextureChannel.R, false) }, 
            { "_Main2ndDissolveNoiseMask",(TextureChannel.R, TextureChannel.R, false) },

            // 2ndと同じ 3rdカラー
            { "_Main3rdTex",             (TextureChannel.RGB, TextureChannel.RGBA, false) }, // RGB/RGBA
            { "_Main3rdBlendMask",       (TextureChannel.R, TextureChannel.R, false) }, 
            { "_Main3rdDissolveMask",    (TextureChannel.R, TextureChannel.R, false) },
            { "_Main3rdDissolveNoiseMask",(TextureChannel.R, TextureChannel.R, false) },

            // https://lilxyzw.github.io/lilToon/ja_JP/color/alphamask.html アルファマスク
            { "_AlphaMask",              (TextureChannel.R, TextureChannel.R, false) },

            // https://lilxyzw.github.io/lilToon/ja_JP/color/shadow.html // 影
            { "_ShadowStrengthMask",     (TextureChannel.R, TextureChannel.R, false) }, // マスクと強度
            { "_ShadowBorderMask",       (TextureChannel.RGB, TextureChannel.RGB, false) }, // //AO Map？ 多分RGB
            { "_ShadowBlurMask",         (TextureChannel.RGB, TextureChannel.RGB, false) }, // ぼかし量マスク
            { "_ShadowColorTex",         (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 影色1
            { "_Shadow2ndColorTex",      (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 影色2
            { "_Shadow3rdColorTex",      (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 影色3

            // https://lilxyzw.github.io/lilToon/ja_JP/color/rimshade.html // RimShade
            { "_RimShadeMask",           (TextureChannel.R, TextureChannel.R, false) }, // lilMaterialPropertyでisTex:false

            // https://lilxyzw.github.io/lilToon/ja_JP/color/emission.html 発光
            { "_EmissionMap",            (TextureChannel.RGB, TextureChannel.RGB, false) }, // 多分RGB
            { "_EmissionBlendMask",      (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // マスク RGBA
            { "_EmissionGradTex",        (TextureChannel.RGB, TextureChannel.RGB, false) }, // 多分RGB
            // 1と同じ
            { "_Emission2ndMap",         (TextureChannel.RGB, TextureChannel.RGB, false) },
            { "_Emission2ndBlendMask",   (TextureChannel.RGBA, TextureChannel.RGBA, false) },
            { "_Emission2ndGradTex",     (TextureChannel.RGB, TextureChannel.RGB, false) },

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/normal.html ノーマルマップ
            { "_BumpMap",                (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // ノーマルマップ
            { "_Bump2ndMap",             (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // ノーマルマップ2nd
            { "_Bump2ndScaleMask",       (TextureChannel.R, TextureChannel.R, false) }, // R ノーマルマップ2nd マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/anisotropy.html ノーマルマップ(異方性反射)
            { "_AnisotropyTangentMap",   (TextureChannel.RGBA, TextureChannel.RGBA, false) }, //異方性反射 ノーマルマップ
            { "_AisotropyScaleMask",    (TextureChannel.R, TextureChannel.R, false) }, // マスク
            { "_AnisotropyShiftNoiseMask",(TextureChannel.R, TextureChannel.R, false) }, // ノイズ

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/backlight.html 逆光
            { "_BacklightColorTex",      (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 多分RGBA 色/マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/reflection.html 光沢
            { "_SmoothnessTex",          (TextureChannel.R, TextureChannel.R, false) }, // 滑らかさ
            { "_MetallicGlossMap",       (TextureChannel.R, TextureChannel.R, false) }, // 金属度
            { "_ReflectionColorTex",     (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 多分RGBA
            { "_ReflectionCubeColor",    (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 多分RGBA? CubeMap Fallback 

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/matcap.html マットキャップ
            { "_MatCapTex",              (TextureChannel.RGB, TextureChannel.RGB, false) }, // 多分RGB
            { "_MatCapBlendMask",        (TextureChannel.RGB, TextureChannel.RGB, false) }, // RGB
            { "_MatCapBumpMap",          (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // ノーマルマップ
            // 同じ
            { "_MatCap2ndTex",           (TextureChannel.RGB, TextureChannel.RGB, false) }, 
            { "_MatCap2ndBlendMask",     (TextureChannel.RGB, TextureChannel.RGB, false) },
            { "_MatCap2ndBumpMap",       (TextureChannel.RGBA, TextureChannel.RGBA, false) },

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/rimlight.html リムライト
            { "_RimColorTex",            (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 多分RGBA

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/glitter.html ラメ
            { "_GlitterColorTex",        (TextureChannel.None, TextureChannel.None, false) }, // HDR
            { "_GlitterColorTex_UVMode", (TextureChannel.None, TextureChannel.None, false) }, // HDR
            { "_GlitterShapeTex",        (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // RGBA

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/outline.html 輪郭線
            { "_OutlineTex",             (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // 多分RGBA
            { "_OutlineWidthMask",       (TextureChannel.R, TextureChannel.R, true) }, // マスクと太さ
            { "_OutlineVectorTex",       (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // ノーマルマップ

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/parallax.html 視差マップ
            { "_ParallaxMap",            (TextureChannel.R, TextureChannel.R, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/audiolink.html Audiolink
            { "_AudioLinkMask",          (TextureChannel.None, TextureChannel.None, false) }, // 不明
            //{ "_AudioLinkMask_ScrollRotate",(TextureChannel.None, TextureChannel.None, false) }, // 不明
            //{ "_AudioLinkMask_UVMode",   (TextureChannel.None, TextureChannel.None, false) }, // 不明

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html Dissolve
            { "_DissolveMask",           (TextureChannel.R, TextureChannel.R, false) }, // R
            { "_DissolveNoiseMask",      (TextureChannel.R, TextureChannel.R, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/fur.html  ファー
            { "_FurNoiseMask",           (TextureChannel.R, TextureChannel.R, false) }, // R ノイズ
            { "_FurMask",                (TextureChannel.R, TextureChannel.R, false) }, // R
            { "_FurLengthMask",          (TextureChannel.R, TextureChannel.R, false) }, // R
            { "_FurVectorTex",           (TextureChannel.RGBA, TextureChannel.RGBA, false) }, // ノーマルマップ

            //{ "_TriMask",                (TextureChannel.None, TextureChannel.None, false) }  // 不明
        };

        public static int GetChannels(Shader shader, string property)
        {
            Dictionary<string, (TextureChannel, TextureChannel, bool)> shaderDictionary = null;

            if (SerachShader.IsLilToonShader(shader))
                shaderDictionary = lilToonProperty;

            if (shaderDictionary != null && shaderDictionary.TryGetValue(property, out var channelValues))
            {
                TextureChannel channel;
                var isOpaque = Utils.IsOpaqueShader(shader);
                if (isOpaque)
                {
                    channel = channelValues.Item1;
                }
                else
                {
                    channel = channelValues.Item2;
                }

                switch (channel)
                {
                    case TextureChannel.None: 
                        return 0;
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
                        return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        /*
        public static bool? UsesRChannel(string property)
        {
            return lilToonProperty.TryGetValue(property, out var channelValues) ? channelValues.Item1 : null;
        }

        public static bool? UsesGChannel(string property)
        {
            return lilToonProperty.TryGetValue(property, out var channelValues) ? channelValues.Item2 : null;
        }

        public static bool? UsesBChannel(string property)
        {
            return lilToonProperty.TryGetValue(property, out var channelValues) ? channelValues.Item3 : null;
        }

        public static bool? UsesAChannel(string property)
        {
            return lilToonProperty.TryGetValue(property, out var channelValues) ? channelValues.Item4 : null;
        }
        */

        public static bool IsVertexShader(Shader shader, string property)
        {
            Dictionary<string, (TextureChannel, TextureChannel, bool)> shaderDictionary = null;

            if (SerachShader.IsLilToonShader(shader))
                shaderDictionary = lilToonProperty;

            if (shaderDictionary != null && shaderDictionary.TryGetValue(property, out var channelValues))
            {
                return channelValues.Item3;
            }
            else
            {
                return false;
            }
        }
        
    }
}