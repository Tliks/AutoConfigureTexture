namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class IslandSSIMEvaluator
{
    private readonly ComputeShader _cs;
    private readonly int _kernel;

    private const string Guid = "0cf44ee9582557a499579111d3b3b7f4";

    public IslandSSIMEvaluator()
    {
        _cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(Guid));
        _kernel = _cs.FindKernel("CSMain");
    }

    public (float[] means, int[] counts) Evaluate(Texture2D src, RenderTexture idRT, int mipLevel, int window, int stride, int numIslands)
    {
        // ComputeShader で Mip を読むために、元テクスチャから RenderTexture を作成し Mip を生成
        RenderTexture? srcRT = null;
        bool needsCleanup = false;
        
        // src が Mip を持つかチェック（mipmapCount > 1）。なければ自前で RT 作成
        if (src.mipmapCount <= 1 || mipLevel > 0)
        {
            srcRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                useMipMap = true,
                autoGenerateMips = false,  // 手動で GenerateMips を呼ぶため false
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            srcRT.Create();
            
            // RenderTexture.active を一時的に保存
            var prevActive = RenderTexture.active;
            try
            {
                Graphics.Blit(src, srcRT);
                srcRT.GenerateMips();
            }
            finally
            {
                RenderTexture.active = prevActive;
            }
            needsCleanup = true;
        }

        var sums = new ComputeBuffer(numIslands * 2, sizeof(uint));
        var debugCounter = new ComputeBuffer(1, sizeof(uint));
        var zer = new uint[numIslands * 2];
        sums.SetData(zer);
        debugCounter.SetData(new uint[] { 0u });

        try
        {
            _cs.SetTexture(_kernel, "_SrcTex", needsCleanup ? srcRT : (Texture)src);
            _cs.SetTexture(_kernel, "_IdTex", idRT);
            _cs.SetInts("_TexSize", new int[] { src.width, src.height });
            _cs.SetInt("_Window", window);
            _cs.SetInt("_Stride", stride);
            _cs.SetInt("_MipLevel", mipLevel);
            _cs.SetInt("_NumIslands", numIslands);
            
            int step = Mathf.Max(1, stride);
            int outW = Mathf.Max(1, (src.width - window + 1 + (step - 1)) / step);
            int outH = Mathf.Max(1, (src.height - window + 1 + (step - 1)) / step);
            _cs.SetInts("_OutSize", new int[] { outW, outH });
            _cs.SetBuffer(_kernel, "_IslandSums", sums);
            _cs.SetBuffer(_kernel, "_DebugCounter", debugCounter);

            uint tgx, tgy, tgz;
            _cs.GetKernelThreadGroupSizes(_kernel, out tgx, out tgy, out tgz);
            int gx = Mathf.CeilToInt(outW / (float)tgx);
            int gy = Mathf.CeilToInt(outH / (float)tgy);
            _cs.Dispatch(_kernel, gx, gy, 1);

            var raw = new uint[numIslands * 2];
            sums.GetData(raw);
            var dbg = new uint[1];
            debugCounter.GetData(dbg);
            Debug.Log($"[ACT][IslandSSIM][dbg] positiveIdCenters={dbg[0]}");
            var means = new float[numIslands];
            var counts = new int[numIslands];
            const float FP_SCALE = 256.0f;
            for (int i = 0; i < numIslands; i++)
            {
                uint sumFixed = raw[i * 2 + 0];
                uint cnt = raw[i * 2 + 1];
                float mean = (cnt > 0) ? ((sumFixed / FP_SCALE) / cnt) : 0f;
                means[i] = mean;
                counts[i] = (int)cnt;
            }
            return (means, counts);
        }
        finally
        {
            sums.Dispose();
            debugCounter.Dispose();
            if (needsCleanup && srcRT != null)
            {
                // RT が active の場合は解除してから Release
                if (RenderTexture.active == srcRT)
                {
                    RenderTexture.active = null;
                }
                srcRT.Release();
                Object.DestroyImmediate(srcRT);
            }
        }
    }
}