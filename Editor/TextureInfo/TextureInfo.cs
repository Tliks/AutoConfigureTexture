using UnityEngine.Experimental.Rendering;

namespace com.aoyon.AutoConfigureTexture;

/// <summary>
/// Texture2D以外を対象としない
/// </summary>
internal class TextureInfo
{
    public readonly Texture2D Texture2D;
    public readonly TextureFormat Format;

    private readonly List<PropertyInfo> _properties = new();
    public IReadOnlyList<PropertyInfo> Properties => _properties;

    public readonly TextureImportedInfo? ImportedInfo;

    private bool? _hasAlpha = null;
    public bool HasAlpha
    {
        get
        {
            _hasAlpha ??= CheckHasAlpha();
            return _hasAlpha.Value;
        }
    }

    private Texture2D? _readableTexture = null;
    public Texture2D ReadableTexture => EnsureReadableTexture2D();

    public TextureInfo(Texture2D texture)
    {
        Texture2D = texture;
        Format = texture.format;
        _properties = new List<PropertyInfo>();

        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        if (importer is TextureImporter ti)
        {
            ImportedInfo = new TextureImportedInfo(ti);
        }
    }

    public void AddPropertyInfo(PropertyInfo propertyInfo)
    {
        _properties.Add(propertyInfo);
    }

    private Texture2D EnsureReadableTexture2D()
    {
        if (_readableTexture == null)
        {
            _readableTexture = Utils.EnsureReadableTexture2D(Texture2D);
        }
        return _readableTexture;
    }

    private bool CheckHasAlpha()
    {
        Profiler.BeginSample("HasAlpha");
        if (_hasAlpha == null)
        {
            if (GraphicsFormatUtility.HasAlphaChannel(Format))
            {
                Profiler.BeginSample("HasAlphaImpl");
                try
                {
                    _hasAlpha = Utils.HasAlphaWithBinarization(Texture2D);
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