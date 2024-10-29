using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public class PropertyDictionary
    {
        /// <summary>
        ///  lilToonにテクスチャを割り当てるプロパティに関する辞書
        ///  key: property
        ///  value: R, G, B, A, isVertexOperation
        /// </summary>
        /// 
        static Dictionary<string, (bool?, bool?, bool?, bool?, bool)> lilToonProperty = new Dictionary<string, (bool?, bool?, bool?, bool?, bool)>
        {
            //{ "_BaseMap",                (null, null, null, null, false) }, // 不明
            //{ "_BaseColorMap",           (null, null, null, null, false) }, // 不明

            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor.html メインカラー
            { "_MainTex",                (true, true, true, true, false) }, // RGBA A? 描画モードに応じて変更すべきかも
            { "_MainGradationTex",       (true, true, true, false, false) }, // RGB
            { "_MainColorAdjustMask",    (true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/color/maincolor_layer.html 2ndカラー
            { "_Main2ndTex",             (true, true, true, true, false) }, // RGBA A?
            { "_Main2ndBlendMask",       (true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html 2ndカラー(Dissolve)
            { "_Main2ndDissolveMask",    (true, false, false, false, false) }, // R
            { "_Main2ndDissolveNoiseMask",(true, false, false, false, false) }, // R

            // 2ndと同じ 3rdカラー
            { "_Main3rdTex",             (true, true, true, true, false) }, // RGBA A?
            { "_Main3rdBlendMask",       (true, false, false, false, false) }, // R
            { "_Main3rdDissolveMask",    (true, false, false, false, false) }, // R
            { "_Main3rdDissolveNoiseMask",(true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/color/alphamask.html アルファマスク
            { "_AlphaMask",              (true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/color/shadow.html // 影
            { "_ShadowStrengthMask",     (true, false, false, false, false) }, // R マスクと強度
            { "_ShadowBorderMask",       (true, true, true, false, false) }, // //RGB AO Map？
            { "_ShadowBlurMask",         (true, true, true, false, false) }, // //RGB ぼかし量マスク
            { "_ShadowColorTex",         (true, true, true, true, false) }, // RGBA 影色1・2・3
            { "_Shadow2ndColorTex",      (true, true, true, true, false) }, // RGBA 影色1・2・3
            { "_Shadow3rdColorTex",      (true, true, true, true, false) }, // RGBA 影色1・2・3

            // https://lilxyzw.github.io/lilToon/ja_JP/color/rimshade.html // RimShade
            { "_RimShadeMask",           (true, false, false, false, false) }, // R 色 / マスク lilMaterialPropertyでisTex:false

            // https://lilxyzw.github.io/lilToon/ja_JP/color/emission.html 発光
            { "_EmissionMap",            (true, true, true, true, false) }, // RGBA 色 / マスク?
            { "_EmissionBlendMask",      (true, false, false, false, false) }, // 多分R 不明 UIではマスク？
            { "_EmissionGradTex",        (true, true, true, false, false) }, // 多分RGB
            // 1同じ
            { "_Emission2ndMap",         (true, true, true, true, false) }, // RGBA
            { "_Emission2ndBlendMask",   (true, false, false, false, false) }, // 多分R
            { "_Emission2ndGradTex",     (true, true, true, false, false) }, // 多分RGB

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/normal.html ノーマルマップ
            { "_BumpMap",                (true, true, true, true, false) }, // 多分RGBA ノーマルマップ
            { "_Bump2ndMap",             (true, true, true, true, false) }, // 多分RGBA ノーマルマップ2nd
            { "_Bump2ndScaleMask",       (true, false, false, false, false) }, // R ノーマルマップ2nd マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/anisotropy.html ノーマルマップ(異方性反射)
            { "_AnisotropyTangentMap",   (true, true, true, true, false) }, // 多分RGBA 異方性反射 ノーマルマップ
            { "_AnisotropyScaleMask",    (true, false, false, false, false) }, // R マスク
            { "_AnisotropyShiftNoiseMask",(true, false, false, false, false) }, // R ノイズ

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/backlight.html 逆光
            { "_BacklightColorTex",      (true, true, true, true, false) }, // 多分RGBA 色/マスク

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/reflection.html 光沢
            { "_SmoothnessTex",          (true, false, false, false, false) }, // R 滑らかさ
            { "_MetallicGlossMap",       (true, false, false, false, false) }, // R 金属度
            { "_ReflectionColorTex",     (true, true, true, true, false) }, // 多分RGBA
            { "_ReflectionCubeColor",    (true, true, true, true, false) }, // 多分RGBA? CubeMap Fallback 

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/matcap.html マットキャップ
            { "_MatCapTex",              (true, true, true, false, false) }, // 多分RGB
            { "_MatCapBlendMask",        (true, true, true, false, false) }, // RGB
            { "_MatCapBumpMap",          (true, true, true, true, false) }, // 多分RGBA ノーマルマップ
            // 同じ
            { "_MatCap2ndTex",           (true, true, true, false, false) }, // 多分RGB
            { "_MatCap2ndBlendMask",     (true, true, true, false, false) }, // RGB
            { "_MatCap2ndBumpMap",       (true, true, true, true, false) }, // 多分RGBA ノーマルマップ

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/rimlight.html リムライト
            { "_RimColorTex",            (true, false, false, false, false) }, // RGBA?

            // https://lilxyzw.github.io/lilToon/ja_JP/reflections/glitter.html ラメ
            { "_GlitterColorTex",        (null, null, null, null, false) }, // HDR
            { "_GlitterColorTex_UVMode", (null, null, null, null, false) }, // HDR
            { "_GlitterShapeTex",        (true, true, true, true, false) }, // RGBA

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/outline.html 輪郭線
            { "_OutlineTex",             (true, true, true, true, false) }, // 多分RGBA
            { "_OutlineWidthMask",       (true, false, false, false, true) }, // R マスクと太さ
            { "_OutlineVectorTex",       (true, true, true, true, false) }, // 多分RGBA ノーマルマップ

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/parallax.html 視差マップ
            { "_ParallaxMap",            (true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/audiolink.html Audiolink
            { "_AudioLinkMask",          (null, null, null, null, false) }, // 不明
            //{ "_AudioLinkMask_ScrollRotate",(null, null, null, null, false) }, // 不明
            //{ "_AudioLinkMask_UVMode",   (null, null, null, null, false) }, // 不明

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/dissolve.html Dissolve
            { "_DissolveMask",           (true, false, false, false, false) }, // R
            { "_DissolveNoiseMask",      (true, false, false, false, false) }, // R

            // https://lilxyzw.github.io/lilToon/ja_JP/advanced/fur.html  ファー
            { "_FurNoiseMask",           (true, false, false, false, false) }, // R ノイズ
            { "_FurMask",                (true, false, false, false, false) }, // R
            { "_FurLengthMask",          (true, false, false, false, false) }, // R
            { "_FurVectorTex",           (true, true, true, true, false) }, // 多分RGBA ノーマルマップ

            //{ "_TriMask",                (null, null, null, null, false) }  // 不明
        };

        public static int GetChannels(Shader shader, string property)
        {
            Dictionary<string, (bool?, bool?, bool?, bool?, bool)> shaderDictionary = null;

            if (SerachShader.IsLilToonShader(shader))
                shaderDictionary = lilToonProperty;

            if (shaderDictionary != null && shaderDictionary.TryGetValue(property, out var channelValues))
            {
                int count = 0;
                if (channelValues.Item1 == true) count++;
                if (channelValues.Item2 == true) count++;
                if (channelValues.Item3 == true) count++;
                if (channelValues.Item4 == true) count++;
                return count;
            }
            return 0;
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
            Dictionary<string, (bool?, bool?, bool?, bool?, bool)> shaderDictionary = null;

            if (SerachShader.IsLilToonShader(shader))
                shaderDictionary = lilToonProperty;

            if (shaderDictionary != null && shaderDictionary.TryGetValue(property, out var channelValues))
            {
                return channelValues.Item5;
            }
            else
            {
                return false;
            }
        }
        
    }
}