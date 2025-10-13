using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// IDマップを1度だけ作り、各ミップ（スケール）で1回走査して島ごとに安価な劣化指標（勾配エネルギー低下比）を集計する。
/// 戻り値は scale→worst island ratio (0..1) の辞書。
/// </summary>
internal sealed class IslandErrorAggregator
{
    private readonly IslandMaskService _maskService = new();

    public Dictionary<float, float> ComputeWorstIslandGradientLossByScale(
        TextureInfo textureInfo,
        IReadOnlyList<Island> islands,
        IReadOnlyList<float> scales)
    {
        if (textureInfo == null || textureInfo.Texture2D == null) throw new ArgumentNullException(nameof(textureInfo));
        var srcTex = textureInfo.Texture2D;

        // IDマップ（base 解像度、ID=1..N、0=非対象）
        var idTex = _maskService.BuildIslandIdMapTexture(srcTex, islands);
        try
        {
            int baseW = idTex.width;
            int baseH = idTex.height;
            var idPixels = idTex.GetPixels32();

            // Baseレベルの勾配エネルギー（島ごと）
            var basePixels = textureInfo.ReadableTexture.GetPixels32(0);
            var baseMean = ComputeIslandMeanGradientEnergy(basePixels, idPixels, baseW, baseH, islands.Count);

            var result = new Dictionary<float, float>(scales.Count);
            foreach (var s in scales)
            {
                if (!(s > 0f && s < 1f)) { result[s] = 0f; continue; }

                int L = MipLevelForScale(srcTex, s); // log2(1/scale)
                // mipのピクセル群を取得（ReadableTextureから取り出す）
                Color32[] mipPixels;
                int wL, hL;
                GetPixelsAtMip(textureInfo.ReadableTexture, L, out mipPixels, out wL, out hL);

                var perIsland = ComputeIslandMeanGradientEnergyAtMip(mipPixels, idPixels, wL, hL, baseW, baseH, L, islands.Count);

                float worst = 0f;
                for (int i = 0; i < perIsland.Length; i++)
                {
                    float b = baseMean[i];
                    float m = perIsland[i];
                    if (b <= 1e-8f) continue;
                    float loss = Mathf.Clamp01((b - m) / Mathf.Max(1e-8f, b));
                    if (loss > worst) worst = loss;
                }
                result[s] = worst;
            }

            return result;
        }
        finally
        {
            if (idTex != null) UnityEngine.Object.DestroyImmediate(idTex);
        }
    }

    private static int MipLevelForScale(Texture2D src, float scale)
    {
        int upW = src.width;
        int downW = Mathf.Max(1, Mathf.RoundToInt(upW * scale));
        int mipLevel = 0;
        int sx = Mathf.Max(1, upW / downW);
        while ((1 << mipLevel) < sx) mipLevel++;
        return Mathf.Max(1, mipLevel);
    }

    private static void GetPixelsAtMip(Texture2D readable, int mip, out Color32[] pixels, out int w, out int h)
    {
        if (mip <= 0)
        {
            w = readable.width; h = readable.height;
            pixels = readable.GetPixels32(0);
            return;
        }

        try
        {
            w = Mathf.Max(1, readable.width >> mip);
            h = Mathf.Max(1, readable.height >> mip);
            pixels = readable.GetPixels32(mip);
        }
        catch
        {
            // フォールバック: 指定ミップをRTにBlitしてから読み戻し
            w = Mathf.Max(1, readable.width >> mip);
            h = Mathf.Max(1, readable.height >> mip);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            try
            {
                var cmd = new UnityEngine.Rendering.CommandBuffer();
                cmd.Blit(new UnityEngine.Rendering.RenderTargetIdentifier(readable, mip), rt);
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Release();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tmp = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                tmp.Apply(false, false);
                RenderTexture.active = prev;
                pixels = tmp.GetPixels32();
                UnityEngine.Object.DestroyImmediate(tmp);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }

    private static float[] ComputeIslandMeanGradientEnergy(Color32[] pixels, Color32[] idPixels, int w, int h, int islandCount)
    {
        var sum = new double[islandCount];
        var cnt = new int[islandCount];

        for (int y = 0; y < h - 1; y++)
        {
            int row = y * w;
            for (int x = 0; x < w - 1; x++)
            {
                int i = row + x;
                int id = DecodeId24(idPixels[i]);
                if (id <= 0) continue;
                int islandIndex = id - 1;

                float y00 = Luma(pixels[i]);
                float y10 = Luma(pixels[i + 1]);
                float y01 = Luma(pixels[i + w]);
                float gx = Mathf.Abs(y10 - y00);
                float gy = Mathf.Abs(y01 - y00);
                float g = gx + gy;
                sum[islandIndex] += g;
                cnt[islandIndex] += 1;
            }
        }

        var mean = new float[islandCount];
        for (int i = 0; i < islandCount; i++)
        {
            mean[i] = cnt[i] > 0 ? (float)(sum[i] / cnt[i]) : 0f;
        }
        return mean;
    }

    private static float[] ComputeIslandMeanGradientEnergyAtMip(Color32[] mipPixels, Color32[] idPixels, int wL, int hL, int baseW, int baseH, int L, int islandCount)
    {
        var sum = new double[islandCount];
        var cnt = new int[islandCount];
        int scale = 1 << L;

        for (int y = 0; y < hL - 1; y++)
        {
            int row = y * wL;
            for (int x = 0; x < wL - 1; x++)
            {
                int i = row + x;
                int bx = Mathf.Min(baseW - 1, x * scale);
                int by = Mathf.Min(baseH - 1, y * scale);
                int bid = by * baseW + bx;
                int id = DecodeId24(idPixels[bid]);
                if (id <= 0) continue;
                int islandIndex = id - 1;

                float y00 = Luma(mipPixels[i]);
                float y10 = Luma(mipPixels[i + 1]);
                float y01 = Luma(mipPixels[i + wL]);
                float gx = Mathf.Abs(y10 - y00);
                float gy = Mathf.Abs(y01 - y00);
                float g = gx + gy;
                sum[islandIndex] += g;
                cnt[islandIndex] += 1;
            }
        }

        var mean = new float[islandCount];
        for (int i = 0; i < islandCount; i++)
        {
            mean[i] = cnt[i] > 0 ? (float)(sum[i] / cnt[i]) : 0f;
        }
        return mean;
    }

    private static int DecodeId24(in Color32 c)
    {
        return c.r | (c.g << 8) | (c.b << 16);
    }

    private static float Luma(in Color32 c)
    {
        return (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
    }
}


