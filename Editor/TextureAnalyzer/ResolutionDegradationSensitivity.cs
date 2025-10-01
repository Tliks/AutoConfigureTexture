using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Analyzer;

internal class ResolutionDegradationSensitivityAnalyzer
{
	private const string SSIMShaderGUID = "4d5e622efbbc6944c881f96a374613da";
	private const float Epsilon = 1e-6f;

	private static Texture2D? s_whiteMask;
	private ComputeShader _ssimShader;

	public ResolutionDegradationSensitivityAnalyzer()
	{
		_ssimShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(SSIMShaderGUID));
	}

    public float ComputeResolutionReductionScore(
        TextureInfo textureInfo,
        TextureUsage usage,
        Texture2D? usageMask,
        float scale)
    {
        ValidatePow2Scale(textureInfo.Texture2D, scale);
        return ComputeResolutionImpactSSIM(textureInfo, usageMask, scale, windowSize: 11, stride: 2);
        switch (usage)
        {
            case TextureUsage.NormalMap:
            case TextureUsage.NormalMapSub:
                return ComputeResolutionImpactNormal(textureInfo, usageMask, scale);
            default:
                return ComputeResolutionImpactSSIM(textureInfo, usageMask, scale, windowSize: 11, stride: 2);
        }
    }

    private static void ValidatePow2Scale(Texture2D tex, float scale)
    {
        if (!(scale > 0f && scale < 1f))
            throw new System.InvalidOperationException($"Scale must be in (0,1). Given: {scale}");

        float inv = 1f / scale;
        int pow = 0;
        float p = 1f;
        while (p < inv && pow < 12) { p *= 2f; pow++; }
        bool isPow2 = Mathf.Abs(p - inv) <= 1e-3f;
        if (!isPow2)
            throw new System.InvalidOperationException($"Scale must be 1/(2^k) (e.g., 0.5, 0.25). Given: {scale}");

        int div = 1 << pow;
        if ((tex.width % div) != 0 || (tex.height % div) != 0)
            throw new System.InvalidOperationException($"Texture size must be divisible by 2^{pow}. Tex: {tex.width}x{tex.height}");
    }
	
	/// <summary>
	/// SSIMベースのスコア: 1 - SSIM(I, I_sim) のマスク加重平均（0..1）。
	/// ウィンドウは正方形(既定11)。コストを抑えるためstride指定で間引き評価。
	/// </summary>
	public float ComputeResolutionImpactSSIM(TextureInfo textureInfo, Texture2D? mask, float scale, int windowSize = 11, int stride = 2)
	{
		if (textureInfo == null) throw new System.ArgumentNullException(nameof(textureInfo));
		if (textureInfo.Texture2D == null) throw new System.ArgumentException("Texture2D is null");
		if (!(scale > 0f && scale < 1f)) return 0f;

		if (SystemInfo.supportsComputeShaders)
		{
			// return ComputeSSIM_GPU(textureInfo, mask, scale, windowSize, stride);
			return 0f;
		}
		else
		{
			Debug.LogWarning("ComputeShader not supported");
			return 0f;
		}
	}

	private float ComputeSSIM_GPU(TextureInfo textureInfo, Texture2D? mask, float scale, int windowSize, int stride)
	{
		var shader = _ssimShader;
		if (shader == null) throw new System.InvalidOperationException("ComputeShader not found: " + SSIMShaderGUID);

		var src = textureInfo.Texture2D;
		if (src == null) throw new System.InvalidOperationException("Texture2D is null");

		// mipLevel 計算（2のべき乗前提の上位で検証済み）
		int upW = src.width;
		int upH = src.height;
		int downW = Mathf.Max(1, Mathf.RoundToInt(upW * scale));
		int downH = Mathf.Max(1, Mathf.RoundToInt(upH * scale));
		int mipLevel = 0;
		int sx = upW / downW;
		while ((1 << mipLevel) < sx) mipLevel++;
		if (mipLevel <= 0 || src.mipmapCount <= mipLevel)
        {
            Debug.LogWarning($"Invalid mipLevel {mipLevel} for texture mipCount {src.mipmapCount}");
			return 0f;
        }

		int kernel = shader.FindKernel("CSMain");
		int outW = Mathf.Max(1, (upW - windowSize + 1) / Mathf.Max(1, stride));
		int outH = Mathf.Max(1, (upH - windowSize + 1) / Mathf.Max(1, stride));
		int outCount = outW * outH;

		var partial = new ComputeBuffer(outCount, sizeof(float) * 2);
		int outCountReduce = Mathf.CeilToInt(outCount / 1024f);
		var reduceA = new ComputeBuffer(outCountReduce, sizeof(float) * 2);
		try
		{
			shader.SetTexture(kernel, "_SrcTex", src);
			shader.SetTexture(kernel, "_MaskTex", mask ?? GetWhiteMask());
			shader.SetInt("_MipLevel", mipLevel);
			shader.SetInt("_Window", windowSize);
			shader.SetInt("_Stride", stride);
			shader.SetInts("_TexSize", new int[] { upW, upH });
			shader.SetInts("_OutSize", new int[] { outW, outH });
			shader.SetBuffer(kernel, "_Partial", partial);

			uint tgx, tgy, tgz;
			shader.GetKernelThreadGroupSizes(kernel, out tgx, out tgy, out tgz);
			int gx = Mathf.CeilToInt(outW / (float)tgx);
			int gy = Mathf.CeilToInt(outH / (float)tgy);
			shader.Dispatch(kernel, gx, gy, 1);

			// GPUリダクション（1段）
			int reduceKernel = shader.FindKernel("ReducePass");
			shader.SetBuffer(reduceKernel, "_In", partial);
			shader.SetBuffer(reduceKernel, "_OutBuf", reduceA);
			shader.SetInt("_CountIn", outCount);
			shader.SetInt("_ReduceStride", 1024);
			uint rtx, rty, rtz;
			shader.GetKernelThreadGroupSizes(reduceKernel, out rtx, out rty, out rtz);
			int rgx = Mathf.CeilToInt(outCountReduce / (float)rtx);
			shader.Dispatch(reduceKernel, Mathf.Max(1, rgx), 1, 1);

			// 最終読み戻し（小バッファ）
			var arr = new Vector2[outCountReduce];
			reduceA.GetData(arr);
			float sumD = 0f, sumW = 0f;
			for (int i = 0; i < arr.Length; i++) { sumD += arr[i].x; sumW += arr[i].y; }
			if (sumW <= 0f) return 0f;
			return Mathf.Clamp01(sumD / sumW);
		}
		finally
		{
			partial.Dispose();
			reduceA.Dispose();
		}
	}

	/// <summary>
	/// ノーマルマップ向け: Down→Up後の法線ベクトル角度誤差のマスク重み平均を0..1に正規化して返す。
	/// 角度は度数法で評価し、90度でクリップして 角度/90 をスコアとする。
	/// </summary>
	public float ComputeResolutionImpactNormal(TextureInfo textureInfo, Texture2D? mask, float scale)
	{
		if (textureInfo == null) throw new System.ArgumentNullException(nameof(textureInfo));
		if (textureInfo.Texture2D == null) throw new System.ArgumentException("Texture2D is null");
		if (!(scale > 0f && scale < 1f)) return 0f;

		var srcTex = textureInfo.Texture2D;
		int width = srcTex.width;
		int height = srcTex.height;

		var simTex = SimulateDownUp(srcTex, Mathf.Max(1, Mathf.RoundToInt(width * scale)), Mathf.Max(1, Mathf.RoundToInt(height * scale)));
		try
		{
			var orig = textureInfo.ReadableTexture;

			var origPixels = orig.GetPixels32();
			var simPixels = simTex.GetPixels32();

			if (mask != null && (mask.width != orig.width || mask.height != orig.height))
			{
				mask = null;
			}
			Color32[]? maskPixels = mask != null ? mask.GetPixels32() : null;

			float sumWeightedDeg = 0f;
			float sumW = 0f;
			int length = origPixels.Length;
			for (int i = 0; i < length; i++)
			{
				float w = 1f;
				if (maskPixels != null)
				{
					w = maskPixels[i].r / 255f;
					if (w <= 0f) continue;
				}

				Vector3 no = DecodeNormalRGB(origPixels[i]);
				Vector3 ns = DecodeNormalRGB(simPixels[i]);
				float dot = Mathf.Clamp(Vector3.Dot(no, ns), -1f, 1f);
				float deg = Mathf.Acos(dot) * Mathf.Rad2Deg; // 0..180
				if (deg > 90f) deg = 90f; // 上限クリップ

				sumWeightedDeg += w * deg;
				sumW += w;
			}

			if (sumW <= 0f) return 0f;
			float meanDeg = sumWeightedDeg / sumW; // 0..90
			float score = meanDeg / 90f;
			return Mathf.Clamp01(score);
		}
		finally
		{
			if (simTex != null)
			{
				UnityEngine.Object.DestroyImmediate(simTex);
			}
		}
	}

	private static Vector3 DecodeNormalRGB(in Color32 c)
	{
		// [0,1]→[-1,1] かつ正規化
		float nx = c.r / 255f * 2f - 1f;
		float ny = c.g / 255f * 2f - 1f;
		float nz = c.b / 255f * 2f - 1f;
		var v = new Vector3(nx, ny, nz);
		float len = v.magnitude;
		if (len > Epsilon) v /= len; else v = new Vector3(0f, 0f, 1f);
		return v;
	}

	/// <summary>
	/// GPUで src を縮小→拡大し、元サイズのシミュレーション結果を Texture2D として返す。
	/// </summary>
	private static Texture2D SimulateDownUp(Texture src, int downW, int downH)
	{
		int upW = src.width;
		int upH = src.height;

		var active = RenderTexture.active;
		var up = RenderTexture.GetTemporary(upW, upH, 0, RenderTextureFormat.ARGB32);
		up.filterMode = FilterMode.Bilinear;

		try
		{
			// 2のべき乗縮小ならミップレベルを直接参照し、そのまま原寸へアップサンプル1回のみ
			bool isPow2 = (upW % downW == 0) && (upH % downH == 0);
			int mipLevel = 0;
			if (isPow2)
			{
				int sx2 = upW / downW;
				int sy2 = upH / downH;
				if ((sx2 & (sx2 - 1)) == 0 && (sy2 & (sy2 - 1)) == 0 && sx2 == sy2)
				{
					// mipLevel = log2(scaleDiv)
					while ((1 << mipLevel) < sx2) mipLevel++;
				}
			}

			if (mipLevel > 0 && src is Texture2D src2D && src2D.mipmapCount > mipLevel)
			{
				// 指定ミップレベルから、原寸RTへ直接アップサンプル（CommandBuffer経由でmip指定）
				var cmd = new CommandBuffer();
				cmd.Blit(new RenderTargetIdentifier(src, mipLevel), up);
				Graphics.ExecuteCommandBuffer(cmd);
				cmd.Release();
			}
			else
			{
				// フォールバック（非べき乗やミップ無し時）
				Graphics.Blit(src, up);
			}

			// Readback to Texture2D
			var tex = new Texture2D(upW, upH, TextureFormat.RGBA32, mipChain: false, linear: false);
			RenderTexture.active = up;
			tex.ReadPixels(new Rect(0, 0, upW, upH), 0, 0, false);
			tex.Apply(false, false);
			return tex;
		}
		finally
		{
			RenderTexture.active = active;
			RenderTexture.ReleaseTemporary(up);
		}
	}

	private static Texture2D GetWhiteMask()
	{
		if (s_whiteMask == null)
		{
			var tex = new Texture2D(1, 1, TextureFormat.R8, false, true)
			{
				name = "__ACT_WhiteMask__",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 1f));
			tex.Apply(false, false);
			s_whiteMask = tex;
		}
		return s_whiteMask;
	}
}