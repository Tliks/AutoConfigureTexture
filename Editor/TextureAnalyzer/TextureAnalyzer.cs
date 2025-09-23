namespace com.aoyon.AutoConfigureTexture.Analyzer;

internal class TextureAnalyzer
{
    private readonly PrimaryUsageAnalyzer _primaryUsageAnalyzer;
    private readonly AlphaAnalyzer _alphaAnalyzer;
    private readonly DrawingCoordinatesAnalyzer _drawingCoordinatesAnalyzer;

    public TextureAnalyzer(GameObject root)
    {
        _primaryUsageAnalyzer = new PrimaryUsageAnalyzer();
        _alphaAnalyzer = new AlphaAnalyzer();
        _drawingCoordinatesAnalyzer = new DrawingCoordinatesAnalyzer(root.transform);
    }

    public TextureUsage PrimaryUsage(TextureInfo textureInfo)
    {
        return _primaryUsageAnalyzer.Analyze(textureInfo);
    }

    public bool HasAlpha(TextureInfo textureInfo)
    {
        return _alphaAnalyzer.HasAlpha(textureInfo);
    }

    public bool IsAllDrawingCoordinatesUnderHeight(TextureInfo textureInfo, float thresholdRatio)
    {
        return _drawingCoordinatesAnalyzer.IsAllDrawingCoordinatesUnderHeight(textureInfo, thresholdRatio);
    }
}