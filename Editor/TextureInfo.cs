using UnityEngine.Experimental.Rendering;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureInfo
    {
        public readonly Texture Texture;
        public readonly List<PropertyInfo> Properties;

        public readonly Type Type;
        public readonly TextureFormat Format;
        public readonly TextureImporterType TextureImporterType;
        public readonly TextureImporterCompression Compression;
        public readonly int CompressionQuality;
        public readonly bool sRGBTexture;
        public readonly TextureImporterAlphaSource AlphaSource;
        public readonly bool AlphaIsTransparency;
        public readonly bool MipmapEnabled;
        public readonly bool isReadable;

        private Texture2D? _readableTexture;
        public Texture2D GetReadableTexture2D()
        {
            if (Texture is not Texture2D texture)
            {
                throw new InvalidOperationException($"Texture is not Texture2D: {Texture.name}");
            }
            if (_readableTexture == null)
            {
                _readableTexture = Utils.EnsureReadableTexture2D(texture);
            }
            return _readableTexture;
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
                        try
                        {
                            _hasAlpha = Utils.HasAlphaWithBinarization(Texture);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to check alpha channel: {e.Message}");
                            _hasAlpha = true;
                        }
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

            Type = texture.GetType();
            Format = default;
            TextureImporterType = default;
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
                TextureImporterType = ti.textureType;
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
            Profiler.BeginSample("TextureInfo.CollectImpl");
            var textureInfos = new Dictionary<Texture, TextureInfo>();

            foreach (var materialInfo in materialInfos)
            {
                var material = materialInfo.Material;
                var shader = material.shader;

                int propertyCount = shader.GetPropertyCount();
                for (int i = 0; i < propertyCount; i++)
                {
                    Profiler.BeginSample("ShaderUtil.GetPropertyType");
                    if (shader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                    {
                        Profiler.EndSample();
                        continue;
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("ShaderUtil.GetTexture");
                    var NameID = shader.GetPropertyNameId(i);
                    Texture texture = material.GetTexture(NameID);
                    if (texture == null)
                    {
                        Profiler.EndSample();
                        continue;
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("TextureInfo ctor");
                    if (!textureInfos.TryGetValue(texture, out var textureInfo))
                    {
                        textureInfo = new TextureInfo(texture);
                        textureInfos[texture] = textureInfo;
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("PropertyInfo ctor");
                    var propertyName = shader.GetPropertyName(i);
                    var propertyInfo = new PropertyInfo(materialInfo, shader, propertyName, 0);
                    textureInfo.Properties.Add(propertyInfo);
                    materialInfo.TextureInfos.Add(textureInfo);
                    Profiler.EndSample();
                }
            }

            Profiler.BeginSample("TextureInfo.AssignPrimaryUsage");
            foreach (var textureInfo in textureInfos.Values)
            {
                textureInfo.AssignPrimaryUsage();
            }
            Profiler.EndSample();
            Profiler.EndSample();

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