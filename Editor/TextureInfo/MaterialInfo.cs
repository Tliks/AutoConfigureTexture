namespace com.aoyon.AutoConfigureTexture;

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
}
