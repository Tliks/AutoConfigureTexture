namespace com.aoyon.AutoConfigureTexture.Processor;

internal class TextureAnalyzer
{
    private readonly PrimaryUsageAnalyzer _primaryUsageAnalyzer;
    private readonly AlphaAnalyzer _alphaAnalyzer;
    private readonly ResolutionDegradationSensitivityAnalyzer _resolutionAnalyzer;

    public TextureAnalyzer(GameObject root)
    {
        _primaryUsageAnalyzer = new PrimaryUsageAnalyzer();
        _alphaAnalyzer = new AlphaAnalyzer();
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

    public List<Island> GetIslands(Mesh mesh, int subMeshIndex, int uvChannel)
    {
        return IslandCalculator.CalculateIslands(mesh, subMeshIndex, uvChannel).ToList();
    }

    public float ComputeResolutionReductionScore(TextureInfo textureInfo, Texture2D? usageMask, float scale)
    {
        var usage = PrimaryUsage(textureInfo);
        return _resolutionAnalyzer.ComputeResolutionReductionScore(textureInfo, usage, usageMask, scale);
    }

}