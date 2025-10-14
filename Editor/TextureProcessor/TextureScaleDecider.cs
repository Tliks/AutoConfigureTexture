namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// 単一パラメータ q∈[0,1] から T(q), B(q), F(q) を導出し、
/// 各テクスチャの許容スケール（品質最優先）を決定する。
/// </summary>
internal sealed class TextureScaleDecider
{
	private readonly ResolutionDegradationSensitivityAnalyzer _analyzer = new();
	private readonly IslandMaskService _maskService = new();
	private readonly IslandErrorAggregator _aggregator = new();

	private static readonly float[] kScales = new[] { 0.5f, 0.25f, 0.0625f }; // 1/2, 1/4, 1/16

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
		Profiler.BeginSample("TextureScaleDecider.Decide");

		Profiler.BeginSample("Params");
		// q→内部パラメータ
		float T = MapQualityThreshold(q);
		float B = MapBudgetRatio(q); // 0..1 of total
		float F = MapApplyFraction(q);
		Profiler.EndSample();
		
		long totalBytes = 0;
		var perTex = new List<(TextureInfo info, float scale, long savedBytes)>();
		foreach (var info in items)
		{
			var originalTexture = info.Texture2D;
			var bytes = MathHelper.ComputeVRAMSize(originalTexture, originalTexture.format);
			totalBytes += bytes;

			(var islands, var islandsArgs) = CalculateIslandsFor(info);

			var w = TextureImportanceHeuristics.Compute(info, usage);

			// 高速経路：IDマップ + 各ミップ一回走査で worst-island を算出
			Profiler.BeginSample("ComputeWorstIslandGradientLossByScale");
			var DjByScale = new Dictionary<float, float>();
			float sMax = 1.0f;
			long savedMax = 0;
			var agg = _aggregator.ComputeWorstIslandGradientLossByScale(info, islands, kScales);
			foreach (var s in kScales)
			{
				float worst = agg.TryGetValue(s, out var v) ? v : 0f;
				DjByScale[s] = NormalizeDelta(w, worst, T);
				if (DjByScale[s] <= 1.0f)
				{
					sMax = s;
					savedMax = bytes - MathHelper.ComputeVRAMSize(originalTexture, s);
				}
			}
			Profiler.EndSample();
			perTex.Add((info, sMax, savedMax));
		}

		var results = new List<Result>(perTex.Count);
		foreach (var t in perTex)
		{
			results.Add(new Result
			{
				Texture = t.info.Texture2D,
				SelectedScale = t.scale,
				SavedBytes = t.savedBytes,
				Reason = "quality-ok:max"
			});
		}
		return results;

		/*
		// 予算超過: 低重要度から割当
		perTex.Sort((a, b) => a.w.CompareTo(b.w) != 0 ? a.w.CompareTo(b.w) : b.savedMax.CompareTo(a.savedMax));
		long acc = 0;
		int limit = Mathf.CeilToInt(F * perTex.Count);
		int applied = 0;
		foreach (var t in perTex)
		{
			bool take = (acc + t.savedMax) <= budgetMax && applied < limit && t.sMax < 1.0f;
			if (take)
			{
				results.Add(new Result { Texture = t.info.Texture2D, SelectedScale = t.sMax, SavedBytes = t.savedMax, Reason = "budget-assign" });
				acc += t.savedMax;
				applied++;
			}
			else
			{
				results.Add(new Result { Texture = t.info.Texture2D, SelectedScale = 1.0f, SavedBytes = 0, Reason = "kept" });
			}
		}
		return results;
		*/
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

	private static float MapQualityThreshold(float q)
	{
		const float Tmin = 0.9f, Tmax = 1.5f, gamma = 2f;
		return Mathf.Lerp(Tmin, Tmax, Mathf.Pow(Mathf.Clamp01(q), gamma));
	}
	private static float MapBudgetRatio(float q)
	{
		const float Rmax = 0.9f;
		return Mathf.Clamp01(q) * Rmax;
	}
	private static float MapApplyFraction(float q)
	{
		return Mathf.Max(0.05f, Mathf.Clamp01(q));
	}
	private static float NormalizeDelta(float importance, float delta, float Tq)
	{
		// 重要度が高いほど厳しく（=実効閾値を下げる）
		float eff = Tq * (1.0f + 0.5f * (1.0f - Mathf.Clamp01(importance)));
		return delta / Mathf.Max(1e-6f, eff);
	}
}