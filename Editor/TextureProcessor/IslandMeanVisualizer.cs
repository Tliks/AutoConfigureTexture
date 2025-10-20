namespace com.aoyon.AutoConfigureTexture.Processor;

internal class IslandMeanVisualizer
{
    private readonly Material _mat;

    public IslandMeanVisualizer()
    {
        var sh = Shader.Find("Hidden/ACT/IslandMeanVis");
        if (sh == null) throw new Exception("Shader not found");
        _mat = new Material(sh);
    }

    public RenderTexture BuildMeanOverlay(RenderTexture idRT, float[] means, int[] counts, bool useHeatColor = true)
    {
        if (idRT == null) throw new ArgumentNullException(nameof(idRT));
        if (means == null || means.Length == 0) throw new ArgumentException("means");
        if (counts == null || counts.Length != means.Length) throw new ArgumentException("counts");

        var meanTex = new Texture2D(means.Length, 1, TextureFormat.RFloat, false, true)
        {
            name = "__ACT_IslandMean__",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var cols = new Color[means.Length];
        for (int i = 0; i < means.Length; i++)
        {
            cols[i] = new Color(Mathf.Clamp01(means[i]), 0, 0, 1);
        }
        meanTex.SetPixels(cols);
        meanTex.Apply(false, false);

        var outRT = new RenderTexture(idRT.width, idRT.height, 0, RenderTextureFormat.ARGB32)
        {
            name = "__ACT_IslandMeanOverlay__",
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        outRT.Create();

        _mat.SetTexture("_IdTex", idRT);
        _mat.SetTexture("_MeanTex", meanTex);
        _mat.SetInt("_NumIslands", means.Length);
        _mat.SetFloat("_UseColor", useHeatColor ? 1f : 0f);

        Graphics.Blit(null, outRT, _mat, 0);

        Object.DestroyImmediate(meanTex);
        return outRT;
    }
}


