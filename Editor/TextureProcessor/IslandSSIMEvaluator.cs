namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class IslandSSIMEvaluator
{
    private readonly ComputeShader _cs;
    private readonly int _kernel;

    private const string Guid = "0cf44ee9582557a499579111d3b3b7f4";

    public IslandSSIMEvaluator()
    {
        _cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(Guid));
        if (_cs == null) throw new InvalidOperationException("ComputeShader is null (failed to load by GUID)");
        _kernel = _cs.FindKernel("CSMain");
    }

    public (float[] means, int[] counts) Evaluate(Texture2D src, RenderTexture idRT, int mipLevel, int numIslands, float alpha = 1.0f, float beta = 1.0f, float gamma = 1.0f, int window = 11)
    {
        using var profiler = new Utils.StopwatchScope("IslandSSIMEvaluator.Evaluate");
        
        var sums = new ComputeBuffer(numIslands * 2, sizeof(uint));
        var debugZeroCounter = new ComputeBuffer(1, sizeof(uint));
        var debugNonZeroCounter = new ComputeBuffer(1, sizeof(uint));
        var zer = new uint[numIslands * 2];
        sums.SetData(zer);
        debugZeroCounter.SetData(new uint[] { 0u });
        debugNonZeroCounter.SetData(new uint[] { 0u });

        try
        {
            _cs.SetTexture(_kernel, "_SrcTex", src);
            _cs.SetInts("_TexSize", new int[] { src.width, src.height });
            _cs.SetInt("_MipLevel", mipLevel);
            _cs.SetFloat("_Alpha", alpha);
            _cs.SetFloat("_Beta", beta);
            _cs.SetFloat("_Gamma", gamma);
            _cs.SetInt("_Window", window);
            _cs.SetTexture(_kernel, "_IdTex", idRT);
            _cs.SetBuffer(_kernel, "_IslandSums", sums);
            _cs.SetBuffer(_kernel, "_DebugZeroCounter", debugZeroCounter);
            _cs.SetBuffer(_kernel, "_DebugNonZeroCounter", debugNonZeroCounter);

            uint tgx, tgy, tgz;
            _cs.GetKernelThreadGroupSizes(_kernel, out tgx, out tgy, out tgz);
            int gx = Mathf.CeilToInt((float)src.width / tgx);
            int gy = Mathf.CeilToInt((float)src.height / tgy);
            _cs.Dispatch(_kernel, gx, gy, 1);

            var raw = new uint[numIslands * 2];
            sums.GetData(raw);
            var dbgz = new uint[1];
            var dbgnz = new uint[1];
            debugZeroCounter.GetData(dbgz);
            debugNonZeroCounter.GetData(dbgnz);
            Debug.Log($"[ACT][IslandSSIM][dbg] ZeroIdCenters={dbgz[0]}");
            Debug.Log($"[ACT][IslandSSIM][dbg] positiveIdCenters={dbgnz[0]}");
            var means = new float[numIslands];
            var counts = new int[numIslands];
            const float FP_SCALE = 50.0f;  // 理論上8K(64MP * 50)で全ピクセルが単一アイランドに集中すると3.2E9でuintの限界(4.3E9)ギリギリ
            for (int i = 0; i < numIslands; i++)
            {
                uint sumFixed = raw[i * 2 + 0];
                uint cnt = raw[i * 2 + 1];
                float mean = cnt > 0 ? sumFixed / FP_SCALE / cnt : 0f; // Todo pixelが0のアイランドを適切に除外する？
                means[i] = mean;
                counts[i] = (int)cnt;
            }
            return (means, counts);
        }
        finally
        {
            sums.Dispose();
            debugZeroCounter.Dispose();
            debugNonZeroCounter.Dispose();
        }
    }
}