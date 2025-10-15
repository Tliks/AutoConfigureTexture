using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// IslandIdマップを用い、Computeで全ピクセル同時に島別SSIMの加重和を集計する高速経路。
/// 各スケール（=ミップレベル）について一回のDispatchで N島分の (sumDelta, sumW) を取得できる。
/// </summary>
internal sealed class IslandSSIMEvaluator
{
    private readonly ComputeShader _cs;
    private readonly int _kernel;

    private const string Guid = "7a5346bd61a78b94caeb5f377765a49d";

    public IslandSSIMEvaluator()
    {
        _cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(Guid));
        _kernel = _cs.FindKernel("CSMain");
    }

    public Vector2[] Evaluate(Texture2D src, RenderTexture idRT, int mipLevel, int window, int stride, int numIslands)
    {
        if (src == null || idRT == null) throw new System.ArgumentNullException();

        var sums = new ComputeBuffer(numIslands * 2, sizeof(uint));
        var zer = new uint[numIslands * 2];
        sums.SetData(zer);

        try
        {
            _cs.SetTexture(_kernel, "_SrcTex", src);
            _cs.SetTexture(_kernel, "_IdTex", idRT);
            _cs.SetInts("_TexSize", new int[] { src.width, src.height });
            _cs.SetInt("_Window", window);
            _cs.SetInt("_Stride", stride);
            _cs.SetInt("_MipLevel", mipLevel);
            _cs.SetInt("_NumIslands", numIslands);
            _cs.SetBuffer(_kernel, "_IslandSums", sums);

            int step = Mathf.Max(1, stride);
            int outW = Mathf.Max(1, (src.width - window + 1 + (step - 1)) / step);
            int outH = Mathf.Max(1, (src.height - window + 1 + (step - 1)) / step);

            uint tgx, tgy, tgz;
            _cs.GetKernelThreadGroupSizes(_kernel, out tgx, out tgy, out tgz);
            int gx = Mathf.CeilToInt(outW / (float)tgx);
            int gy = Mathf.CeilToInt(outH / (float)tgy);
            _cs.Dispatch(_kernel, gx, gy, 1);

            var raw = new uint[numIslands * 2];
            sums.GetData(raw);
            var outArr = new Vector2[numIslands];
            const float FP_SCALE = 256.0f;
            for (int i = 0; i < numIslands; i++)
            {
                uint sumFixed = raw[i * 2 + 0];
                uint cnt = raw[i * 2 + 1];
                float mean = (cnt > 0) ? ((sumFixed / FP_SCALE) / cnt) : 0f;
                outArr[i] = new Vector2(mean, cnt);
            }
            return outArr;
        }
        finally
        {
            sums.Dispose();
        }
    }
}