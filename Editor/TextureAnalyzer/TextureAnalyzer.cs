namespace com.aoyon.AutoConfigureTexture.Analyzer;

internal class TextureAnalyzer
{
    private readonly PrimaryUsageAnalyzer _primaryUsageAnalyzer;
    private readonly TextureAreaAnalyzer _textureAreaAnalyzer;

    public TextureAnalyzer(GameObject root)
    {
        _primaryUsageAnalyzer = new PrimaryUsageAnalyzer();
        _textureAreaAnalyzer = new TextureAreaAnalyzer(root.transform);
    }

    public TextureUsage PrimaryUsage(TextureInfo textureInfo)
    {
        return _primaryUsageAnalyzer.Analyze(textureInfo);
    }

    public bool IsAllAreaUnderHeight(TextureInfo textureInfo, float thresholdRatio)
    {
        return _textureAreaAnalyzer.IsAllAreaUnderHeight(textureInfo, thresholdRatio);
    }
}