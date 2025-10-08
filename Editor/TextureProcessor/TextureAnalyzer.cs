namespace com.aoyon.AutoConfigureTexture.Processor;

internal class TextureAnalyzer
{
    private readonly PrimaryUsageAnalyzer _primaryUsageAnalyzer;
    private readonly AlphaAnalyzer _alphaAnalyzer;
    private readonly DrawingCoordinatesAnalyzer _drawingCoordinatesAnalyzer;
    private readonly IslandAnalyzer _islandAnalyzer;
    private readonly ResolutionDegradationSensitivityAnalyzer _resolutionAnalyzer;

    public TextureAnalyzer(GameObject root)
    {
        _primaryUsageAnalyzer = new PrimaryUsageAnalyzer();
        _alphaAnalyzer = new AlphaAnalyzer();
        _drawingCoordinatesAnalyzer = new DrawingCoordinatesAnalyzer(root.transform);
        _islandAnalyzer = new IslandAnalyzer();
        _resolutionAnalyzer = new ResolutionDegradationSensitivityAnalyzer();
    }

    public TextureUsage PrimaryUsage(TextureInfo textureInfo)
    {
        return PrimaryUsageAnalyzer.Analyze(textureInfo);
    }

    public bool HasAlpha(TextureInfo textureInfo)
    {
        return _alphaAnalyzer.HasAlpha(textureInfo);
    }

    public bool IsAllDrawingCoordinatesUnderHeight(TextureInfo textureInfo, float thresholdRatio)
    {
        return _drawingCoordinatesAnalyzer.IsAllDrawingCoordinatesUnderHeight(textureInfo, thresholdRatio);
    }

    public List<Island> GetIslands(Mesh mesh, int subMeshIndex, int uvChannel)
    {
        return _islandAnalyzer.GetIslands(mesh, subMeshIndex, uvChannel);
    }

    public float ComputeResolutionReductionScore(TextureInfo textureInfo, Texture2D? usageMask, float scale)
    {
        var usage = PrimaryUsage(textureInfo);
        return _resolutionAnalyzer.ComputeResolutionReductionScore(textureInfo, usage, usageMask, scale);
    }

}