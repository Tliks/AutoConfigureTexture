using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// IslandSSIMEvaluator の結果（mean, count）を1Dテクスチャに詰め、IdMapを使ってヒートマップ可視化するユーティリティ。
/// </summary>
internal static class IslandMeanVisualizer
{
    private static Material? s_mat;

    public static RenderTexture BuildMeanOverlay(RenderTexture idRT, float[] means, int[] counts, bool useHeatColor = true)
    {
        if (idRT == null) throw new System.ArgumentNullException(nameof(idRT));
        if (means == null || means.Length == 0) throw new System.ArgumentException("means");
        if (counts == null || counts.Length != means.Length) throw new System.ArgumentException("counts");

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

        if (s_mat == null)
        {
            var sh = Shader.Find("Hidden/ACT/IslandMeanVis");
            s_mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
        }
        s_mat.SetTexture("_IdTex", idRT);
        s_mat.SetTexture("_MeanTex", meanTex);
        s_mat.SetInt("_NumIslands", means.Length);
        s_mat.SetFloat("_UseColor", useHeatColor ? 1f : 0f);

        Graphics.Blit(null as Texture, outRT, s_mat, 0);

        Object.DestroyImmediate(meanTex);
        return outRT;
    }
}


