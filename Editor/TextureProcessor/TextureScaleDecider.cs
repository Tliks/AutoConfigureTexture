namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// 単一パラメータ q∈[0,1] から T(q), B(q), F(q) を導出し、
/// 各テクスチャの許容スケール（品質最優先）を決定する。
/// </summary>
internal sealed class TextureScaleDecider
{
	private readonly ResolutionDegradationSensitivityAnalyzer _analyzer = new();
	private readonly IslandMaskService _maskService = new();

	private static readonly float[] kScales = new[] { 0.5f, 0.25f, 0.125f, 0.0625f }; // 1/2, 1/4, 1/8, 1/16

	public struct Result
	{
		public Texture2D Texture;
		public float SelectedScale; // 1.0 if unchanged
		public long SavedBytes;
		public string Reason;

		public override readonly string ToString()
		{
			return $"Texture: {Texture.name}, SelectedScale: {SelectedScale}, SavedBytes: {SavedBytes}, Reason: {Reason}";
		}
	}

    public IReadOnlyList<Result> Decide(
        IReadOnlyList<TextureInfo> items,
        float q)
    {
        var results = new List<Result>(items.Count);
        foreach (var info in items)
        {
            var tex = info.Texture2D;
            if (tex == null) { continue; }
            long bytes = MathHelper.ComputeVRAMSize(tex, tex.format);

            var (islands, _) = CalculateIslandsFor(info);
            if (islands.Length == 0)
            {
                results.Add(new Result { Texture = tex, SelectedScale = 1.0f, SavedBytes = 0, Reason = "no-islands" });
                continue;
            }

            var idRT = _maskService.BuildIslandIdMapRT(tex, islands);
			_maskService.DebugLogIslandIdStats(tex, islands);
			_maskService.DebugLogIslandUvBounds(islands);
			var ssimEval = new IslandSSIMEvaluator();
            try
            {
				// IslandIdベースの一括SSIM集計（id==0をCompute側で除外）
				for (int si = 0; si < kScales.Length; si++)
				{
					float s = kScales[si];
					int mip = ComputeMipLevelForScale(tex, s);
					var sums = ssimEval.Evaluate(tex, idRT, mip, window: 11, stride: 2, numIslands: islands.Length);
					// デバッグ出力（島ごとの平均Δ=1-SSIM とサンプル数）
					{
						var sb = new System.Text.StringBuilder();
						sb.Append("[ACT][IslandSSIM] ").Append(tex.name)
							.Append(" s=").Append(s.ToString("0.#####"))
							.Append(" mip=").Append(mip)
							.Append(" islands=").Append(islands.Length).Append('\n');
						for (int ii = 0; ii < islands.Length; ii++)
						{
							float mean = sums[ii].x;
							int cnt = (int)sums[ii].y;
							sb.Append("  #").Append(ii + 1).Append(": mean=")
								.Append(mean.ToString("0.###"))
								.Append(" n=").Append(cnt).Append('\n');
						}
						Debug.Log(sb.ToString());
					}
				}

                // ここではスケール決定を行わない
                results.Add(new Result
                {
                    Texture = tex,
                    SelectedScale = 1.0f,
                    SavedBytes = 0,
                    Reason = "computed-only"
                });
            }
            finally
            {
				if (idRT != null) idRT.Release();
            }
        }

        return results;
	}

	private static (Island[], IslandArgument[]) CalculateIslandsFor(TextureInfo info)
	{
		var uniqueArgs = new HashSet<IslandArgument>();
		foreach (var property in info.Properties)
		{
			var uv = property.UVchannel;
			var materialInfo = property.MaterialInfo;
			foreach (var (renderer, indices) in materialInfo.Renderers)
			{
				var mesh = Utils.GetMesh(renderer);
				if (mesh == null) continue;
				foreach (var index in indices)
				{
					uniqueArgs.Add(new IslandArgument(property, uv, materialInfo, renderer, mesh, index));
				}
			}
		}
		var allIslands = new List<Island>();
		var allArgs = new List<IslandArgument>();
		foreach (var arg in uniqueArgs)
		{
			var islandsPerArg = IslandCalculator.CalculateIslands(arg.Mesh, arg.SubMeshIndex, arg.UVchannel);
			allArgs.AddRange(Enumerable.Repeat(arg, islandsPerArg.Length)); // 同じArgにより生成されたIslands
			allIslands.AddRange(islandsPerArg);
		}
		return (allIslands.ToArray(), allArgs.ToArray());
	}

	private record IslandArgument(PropertyInfo PropertyInfo, int UVchannel, MaterialInfo MaterialInfo, Renderer Renderer, Mesh Mesh, int SubMeshIndex);

	private static int ComputeMipLevelForScale(Texture2D src, float scale)
	{
		int upW = src.width;
		int downW = Mathf.Max(1, Mathf.RoundToInt(upW * Mathf.Clamp(scale, 1e-6f, 1f)));
		int mipLevel = 0;
		int sx = Mathf.Max(1, upW / downW);
		while ((1 << mipLevel) < sx) mipLevel++;
		return Mathf.Max(1, mipLevel);
	}
}