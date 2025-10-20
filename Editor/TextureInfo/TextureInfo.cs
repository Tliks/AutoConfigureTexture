namespace com.aoyon.AutoConfigureTexture;

/// <summary>
/// Texture2D以外を対象としない
/// </summary>
internal class TextureInfo
{
    public readonly Texture2D Texture2D;
    public readonly TextureFormat Format;

    private readonly List<PropertyInfo> _referencedProperties = new();
    public IReadOnlyList<PropertyInfo> ReferencedProperties => _referencedProperties;

    public readonly TextureImportedInfo? ImportedInfo;

    private Texture2D? _readableTexture = null;
    public Texture2D ReadableTexture => EnsureReadableTexture2D();

    public TextureInfo(Texture2D texture)
    {
        Texture2D = texture;
        Format = texture.format;
        _referencedProperties = new List<PropertyInfo>();

        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        if (importer is TextureImporter ti)
        {
            ImportedInfo = new TextureImportedInfo(ti);
        }
    }

    public void AddPropertyInfo(PropertyInfo propertyInfo)
    {
        _referencedProperties.Add(propertyInfo);
    }

    private Texture2D EnsureReadableTexture2D()
    {
        if (_readableTexture == null)
        {
            _readableTexture = TextureUtility.EnsureReadableTexture2D(Texture2D);
        }
        return _readableTexture;
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"TextureInfo: {Texture2D.name}");
        sb.Append("  ReferencedProperties: ");
        foreach (var property in _referencedProperties)
        {
            sb.Append($"{property}, ");
        }
        return sb.ToString();
    }
}

internal record struct PropertyInfo(MaterialInfo MaterialInfo, Shader Shader, string PropertyName, int UVchannel)
{
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"PropertyInfo: {PropertyName}");
        sb.Append($"  MaterialInfo: {MaterialInfo}");
        sb.Append($"  Shader: {Shader.name}");
        sb.Append($"  UVchannel: {UVchannel}");
        return sb.ToString();
    }
}

internal class MaterialInfo
{
    public readonly Material Material;

    private readonly Dictionary<Renderer, List<int>> _renderers = new();
    public IReadOnlyDictionary<Renderer, IReadOnlyList<int>> Renderers =>
        _renderers.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<int>)kvp.Value
        );

    private readonly List<TextureInfo> _textureInfos = new();
    public IReadOnlyList<TextureInfo> TextureInfos => _textureInfos;

    public MaterialInfo(Material material)
    {
        Material = material;
    }

    public void AddReference(Renderer renderer, int index)
    {
        _renderers.GetOrAddNew(renderer).Add(index);
    }

    public void AddTextureInfo(TextureInfo textureInfo)
    {
        _textureInfos.Add(textureInfo);
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"MaterialInfo: {Material.name}");
        sb.Append("  Renderers: ");
        foreach (var renderer in _renderers)
        {
            sb.Append($"{renderer.Key.name}: ");
            foreach (var index in renderer.Value)
            {
                sb.Append($"{index}, ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

internal class TextureImportedInfo
{
    public readonly TextureImporter Importer;

    public TextureImporterType TextureImporterType => Importer.textureType;
    public TextureImporterCompression Compression => Importer.textureCompression;
    public int CompressionQuality => Importer.compressionQuality;
    public bool sRGBTexture => Importer.sRGBTexture;
    public TextureImporterAlphaSource AlphaSource => Importer.alphaSource;
    public bool AlphaIsTransparency => Importer.alphaIsTransparency;
    public bool MipmapEnabled => Importer.mipmapEnabled;
    public bool IsReadable => Importer.isReadable;

    public TextureImportedInfo(TextureImporter importer)
    {
        Importer = importer;
    }
}

internal enum TextureUsage
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
internal enum TextureChannel
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