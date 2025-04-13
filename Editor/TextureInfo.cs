using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace com.aoyon.AutoConfigureTexture
{
    public struct TextureInfo
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
                    ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);
                    if (propertyType != ShaderUtil.ShaderPropertyType.TexEnv)
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

                    materialInfo.AddTextureReference(textureInfo);
                }
            }

            return textureInfos.Values;
        }
    }

    public class TexrtureSessionData
    {
        public readonly TextureInfo Info;

        public TextureUsage PrimaryUsage;
        private Texture2D _readableTexture;
        public Texture2D ReadbleTexture2D
        {
            get
            {
                if (_readableTexture == null)
                {
                    _readableTexture = Utils.EnsureReadableTexture2D(Info.Texture as Texture2D);;
                }
                return _readableTexture;
            }
        }

        public TexrtureSessionData(TextureInfo info)
        {
            Info = info;
            PrimaryUsage = AssignPrimaryUsage();
        }

        private TextureUsage AssignPrimaryUsage()
        {
            // 不明な使用用途は無視し既知の情報のみで判断
            // パターン1: lilToonのみで不明プロパティが含まれていた場合
            //            重要なプロパティは抑えてると思われるため不明プロパティは無視
            // パターン2: lilToon以外のみの場合
            //           _MainTexのみusageとして返るが、それ以外で不明プロパティのみの場合は何もしない
            // パターン3: lilToonとそれ以外が混じっている場合
            //           lilToonの情報のみで処理されるためlilToon以外で重要な使用プロパティだった場合顕劣化が想定されるが、エッジケースなのでここでは想定しない
            var usages = Info.Properties
                .Select(info => ShaderSupport.GetTextureUsage(info.Shader, info.PropertyName))
                .OfType<TextureUsage>();
                
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

        public void AddReferenced(Renderer renderer, int index)
        {
            Renderers.Add(renderer);
            MaterialIndices.Add(index);
        }

        public void AddTextureReference(TextureInfo textureInfo)
        {
            TextureInfos.Add(textureInfo);
        }

        public static IEnumerable<MaterialInfo> Collect(IEnumerable<Renderer> renderers)
        {
            var materialInfos = new Dictionary<Material, MaterialInfo>();

            foreach (var renderer in renderers.ToHashSet())
            {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null) continue;

                    materialInfos.GetOrAdd(material, m => new(m)).AddReferenced(renderer, index);
                }
            }

            return materialInfos.Values;
        }
    }

    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
        {
            if (dictionary.TryGetValue(key, out TValue value))
                return value;
            value = factory(key);
            dictionary.Add(key, value);
            return value;
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

    public enum TextureChannel
    {
        Unknown,
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
        GBA,
        RGBA,
    }
}