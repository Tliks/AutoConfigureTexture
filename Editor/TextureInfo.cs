using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureInfo
    {
        public readonly Texture Texture;
        public readonly List<PropertyInfo> Properties;

        public readonly Type Type;
        public readonly TextureFormat Format;
        public readonly TextureImporterCompression Compression;
        public readonly int CompressionQuality;
        public readonly bool sRGBTexture;
        public readonly TextureImporterAlphaSource AlphaSource;
        public readonly bool AlphaIsTransparency;
        public readonly bool MipmapEnabled;
        public readonly bool isReadable;

        private Texture2D _readableTexture;
        public Texture2D ReadbleTexture2D
        {
            get
            {
                if (_readableTexture == null)
                {
                    _readableTexture = Utils.EnsureReadableTexture2D(Texture as Texture2D);;
                }
                return _readableTexture;
            }
        }

        private TextureUsage _primaryUsage;
        public TextureUsage PrimaryUsage
        {
            get
            {
                if (_primaryUsage == TextureUsage.Unknown)
                {
                    _primaryUsage = AssignPrimaryUsage();
                }
                return _primaryUsage;
            }
        }

        private bool? _hasAlpha = null;
        public bool HasAlpha
        {
            get
            {
                Profiler.BeginSample("HasAlpha");
                if (_hasAlpha == null)
                {
                    if (GraphicsFormatUtility.HasAlphaChannel(Format))
                    {
                        Profiler.BeginSample("HasAlphaImpl");
                        _hasAlpha = Utils.ContainsAlpha(Texture);
                        Profiler.EndSample();
                    }
                    else
                    {
                        _hasAlpha = false;
                    }
                }
                Profiler.EndSample();
                return _hasAlpha.Value;
            }
        }

        public TextureInfo(Texture texture)
        {
            Texture = texture;
            Properties = new List<PropertyInfo>();

            Type = default;
            Format = default;
            Compression = default;
            CompressionQuality = default;
            sRGBTexture = default;
            AlphaSource = default;
            AlphaIsTransparency = default;
            MipmapEnabled = default;
            isReadable = default;

            if (texture is Texture2D t)
            {
                Type = typeof(Texture2D);
                Format = t.format;
            }

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            if (importer is TextureImporter ti)
            {
                Compression = ti.textureCompression; 
                CompressionQuality = ti.compressionQuality;
                sRGBTexture = ti.sRGBTexture;
                AlphaSource = ti.alphaSource;
                AlphaIsTransparency = ti.alphaIsTransparency;
                MipmapEnabled = ti.mipmapEnabled;
                isReadable = ti.isReadable;
            }
        }

        public static IEnumerable<TextureInfo> Collect(GameObject root)
        {
            var mats = MaterialInfo.Collect(root);
            return Collect(mats);
        }

        public static IEnumerable<TextureInfo> Collect(IEnumerable<MaterialInfo> materialInfos)
        {
            var textureInfos = new Dictionary<Texture, TextureInfo>();

            foreach (var materialInfo in materialInfos)
            {
                var material = materialInfo.Material;
                var shader = material.shader;

                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                        continue;

                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    Texture texture = material.GetTexture(propertyName);
                    if (texture == null)
                        continue;

                    if (!textureInfos.TryGetValue(texture, out var textureInfo))
                    {
                        textureInfo = new TextureInfo(texture);
                        textureInfos[texture] = textureInfo;
                    }

                    var propertyInfo = new PropertyInfo(materialInfo, shader, propertyName, 0);
                    textureInfo.Properties.Add(propertyInfo);
                    materialInfo.TextureInfos.Add(textureInfo);
                }
            }

            foreach (var textureInfo in textureInfos.Values)
            {
                textureInfo.AssignPrimaryUsage();
            }

            return textureInfos.Values;
        }

        private static readonly TextureUsage[] s_usages = 
        {
            TextureUsage.MainTex,
            TextureUsage.NormalMap,
            TextureUsage.NormalMapSub,
            TextureUsage.AOMap,
            TextureUsage.MatCap,
            TextureUsage.Emission,
            TextureUsage.Others,
        };
        private TextureUsage AssignPrimaryUsage()
        {
            // 不明な使用用途は無視し既知の情報のみで判断
            // パターン1: lilToonのみで不明プロパティが含まれていた場合
            //            重要なプロパティは抑えてると思われるため不明プロパティは無視
            // パターン2: lilToon以外のみの場合
            //           _MainTexのみusageとして返るが、それ以外で不明プロパティのみの場合は何もしない
            // パターン3: lilToonとそれ以外が混じっている場合
            //           lilToonの情報のみで処理されるためlilToon以外で重要な使用プロパティだった場合顕劣化が想定されるが、エッジケースなのでここでは想定しない
            var usages = Properties
                .Select(info => ShaderSupport.GetTextureUsage(info.Shader, info.PropertyName))
                .Where(usage => usage != TextureUsage.Unknown);
                
            // 不明プロパティのみの場合は何もしない
            if (!usages.Any())
            {
                return TextureUsage.Unknown;
            }
            else
            {
                return GetPrimaryUsage(usages);;
            }   

            // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
            // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
            TextureUsage GetPrimaryUsage(IEnumerable<TextureUsage> usages)
            {
                foreach (var usage in s_usages)
                {
                    if (usages.Contains(usage))
                    {
                        return usage;
                    }
                }
                throw new InvalidOperationException();
            }
        }
    }

    public readonly struct PropertyInfo
    {
        public readonly MaterialInfo MaterialInfo;
        public readonly Shader Shader; 
        public readonly string PropertyName;
        public readonly int UVchannel;

        public PropertyInfo(MaterialInfo materialInfo, Shader shader, string propertyName, int uvchannel)
        {
            MaterialInfo = materialInfo;
            Shader = shader;
            PropertyName = propertyName;
            UVchannel = uvchannel;
        }
    }

    public class MaterialInfo
    {
        public readonly List<Renderer> Renderers = new();
        public readonly List<int> MaterialIndices = new();
        public readonly Material Material;
        public readonly HashSet<TextureInfo> TextureInfos = new();

        private MaterialInfo(Material material)
        {
            Material = material;
        }

        private void AddReference(Renderer renderer, int index)
        {
            Renderers.Add(renderer);
            MaterialIndices.Add(index);
        }

        public static IEnumerable<MaterialInfo> Collect(GameObject root)
        {
            var materialInfos = new Dictionary<Material, MaterialInfo>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null) continue;

                    if (!materialInfos.TryGetValue(material, out var materialInfo))
                    {
                        materialInfo = new MaterialInfo(material);
                        materialInfos[material] = materialInfo;
                    }

                    materialInfo.AddReference(renderer, index);
                }
            }

            return materialInfos.Values;
        }
    }

    public enum TextureUsage
    {
        Unknown,
        MainTex,
        NormalMap,
        NormalMapSub, // メインのNormalMapと区別
        AOMap,
        MatCap,
        Emission,
        Others,
    }

    [Flags]
    public enum TextureChannel
    {
        Unknown = 1 << 4,
        R = 1 << 0,
        G = 1 << 1,
        B = 1 << 2,
        A = 1 << 3,
        RG = R | G,
        RB = R | B,
        RA = R | A,
        GB = G | B,
        GA = G | A,
        BA = B | A,
        RGB = R | G | B,
        RGA = R | G | A,
        RBA = R | B | A,
        GBA = G | B | A, 
        RGBA = R | G | B | A,
    }
}