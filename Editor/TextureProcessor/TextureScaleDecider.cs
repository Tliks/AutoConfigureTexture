namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class TextureScaleDecider
{
    private readonly IslandCalculator _islandCalculator;
    private readonly IslandTextureService _maskService;
    private readonly IslandSSIMEvaluator _ssimEval;
    private readonly TextureImportanceHeuristics _importanceHeuristics;

    public TextureScaleDecider(GameObject root)
    {
        _islandCalculator = new IslandCalculator();
        _maskService = new IslandTextureService();
        _ssimEval = new IslandSSIMEvaluator();
        _importanceHeuristics = new TextureImportanceHeuristics(root);
    }

	public record Result(Texture2D Texture, int SelectedDownScaleLevel, long SavedBytes, string Reason)
	{
		public override string ToString()
		{
			return $"Texture: {Texture.name}, SelectedDownScaleLevel: {SelectedDownScaleLevel}, SavedBytes: {SavedBytes}, Reason: {Reason}";
		}
	}

    public IReadOnlyList<Result> Decide(
        IReadOnlyList<TextureInfo> items,
        float q,
		int maxDownScaleLevel = 3
		)
    {
        var results = new List<Result>(items.Count);


		foreach (var info in items)
        {
            var tex = info.Texture2D;
            if (tex == null) continue;
            
            var bytes = MathHelper.ComputeVRAMSize(tex, tex.format);

            // UV usageがないTextureもあるのだし、Islandに依存しすぎだね…
            var analysisResults = AnalyzeIslands(info, maxDownScaleLevel);
			if (analysisResults.Length == 0)
			{
				results.Add(new Result(tex, 0, 0, "No islands"));
				continue;
			}

			// パラメータ（最小実装の既定値）
			const float ssimRef = 0.96f;
			const float beta = 0.5f;
			const float percentile = 0.95f; // 95p
			float tThreshold = 0.9f + 0.6f * q * q; // T(q)

			// テクスチャ内 TD の基準（中央値）
			var tdArray = analysisResults.Select(a => a.TexelDensity).ToArray();
			float tdRef = Median(tdArray);

			// スケール別 TextureScore を算出
			var textureScores = new float[maxDownScaleLevel];
			for (int k = 0; k < maxDownScaleLevel; k++)
			{
				var pairs = new List<(float score, float weight)>(analysisResults.Length);
				for (int i = 0; i < analysisResults.Length; i++)
				{
					var ar = analysisResults[i];
					float ssim = ar.SSIMMeansPerLevel[k];
					float islandScore = ComputeIslandScore(ssim, ar.TexelDensity, tdRef, ssimRef, beta);
					float area = ComputeIslandArea(ar.Island);
					float weight = Mathf.Max(0f, ar.HeuristicImportance) * Mathf.Max(0f, area);
					if (weight > 0f)
					{
						pairs.Add((islandScore, weight));
					}
				}
				textureScores[k] = WeightedPercentile(pairs, percentile);
			}

			// 可否判定：最も粗い（k が大きい）許容レベルを選ぶ
			int selectedLevel = 0;
			for (int k = maxDownScaleLevel - 1; k >= 0; k--)
			{
				if (textureScores[k] <= tThreshold)
				{
					selectedLevel = k;
					break;
				}
			}

			long newBytes = EstimateBytesAtLevel(bytes, selectedLevel);
			long saved = Math.Max(0, bytes - newBytes);
			string reason = selectedLevel > 0 ? $"95p<={tThreshold:F3}" : "Below threshold at coarser levels";
			results.Add(new Result(tex, selectedLevel, saved, reason));
		}

        return results;
	}

    private IslandAnalysisResult[] AnalyzeIslands(TextureInfo info, int maxDownScaleLevel)
    {
        var tex = info.Texture2D;
		var texArea = tex.width * tex.height;

        Island[] islands;
        IslandDescription[] descriptions;
        using (new Utils.ProfilerScope("TextureScaleDecider.CalculateIslands"))
        {
            (islands, descriptions) = _islandCalculator.CalculateIslandsFor(info);
        }

        RenderTexture idRT;
        using (new Utils.ProfilerScope("TextureScaleDecider.BuildIDMap"))
        {
            idRT = _maskService.BuildIDMap(tex, islands).Value;
        }

        float[][] ssimMeansPerLevel = new float[maxDownScaleLevel][];
        using (new Utils.ProfilerScope("TextureScaleDecider.EvaluateSSIM"))
        {
            for (int si = 0; si < maxDownScaleLevel; si++) // Todo: 効率的な複数スケールのSSIM計算用関数を作る
            {
                var (means, _) = _ssimEval.Evaluate(tex, idRT, si, islands.Length);
                ssimMeansPerLevel[si] = means;
            }
        }

        float [] importanceScoresPerIsland = new float[islands.Length];
        using (new Utils.ProfilerScope("TextureScaleDecider.HeuristicsImportanceScores"))
        {
            for (int i = 0; i < islands.Length; i++)
            {
                importanceScoresPerIsland[i] = _importanceHeuristics.ComputeIslandImportance(descriptions[i]);
            }
        }

        IslandAnalysisResult[] analysisResults = new IslandAnalysisResult[islands.Length];
        using (new Utils.ProfilerScope("TextureScaleDecider.AnalysisResults"))
        {
            for (int i = 0; i < islands.Length; i++)
            {
                var ssimMeansPerLevelPerIsland = Enumerable.Range(0, maxDownScaleLevel).Select(si => ssimMeansPerLevel[si][i]).ToArray();
				var texelDensity = (islands[i].TriangleArea + islands[i].UVArea) / texArea;
                analysisResults[i] = new IslandAnalysisResult(islands[i], descriptions[i], ssimMeansPerLevelPerIsland, texelDensity, importanceScoresPerIsland[i]);
            }
        }

        return analysisResults;
    }

    private record IslandAnalysisResult(Island Island, IslandDescription Description, float[] SSIMMeansPerLevel, float TexelDensity, float HeuristicImportance);

	private static float ComputeIslandArea(Island island)
	{
		var vertices = island.Vertices;
		var indices = island.Triangles;
		var offs = island.TriangleIndices;
		double area = 0.0;
		for (int t = 0; t < offs.Count; t++)
		{
			int o = offs[t];
			int i0 = indices[o + 0];
			int i1 = indices[o + 1];
			int i2 = indices[o + 2];
			var v0 = vertices[i0];
			var v1 = vertices[i1];
			var v2 = vertices[i2];
			area += 0.5 * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
		}
		return (float)area;
	}

	private static float ComputeIslandScore(float ssim, float td, float tdRef, float ssimRef, float beta)
	{
		float denom = Mathf.Max(1e-6f, 1f - ssimRef);
		float norm = (1f - ssim) / denom;
		float visDenom = 1f + beta * (td - tdRef);
		if (visDenom < 0.1f) visDenom = 0.1f;
		float visFactor = 1f / visDenom;
		return Mathf.Max(0f, norm) * visFactor;
	}

	private static float WeightedPercentile(List<(float score, float weight)> pairs, float percentile)
	{
		if (pairs.Count == 0) return 0f;
		float totalW = 0f;
		for (int i = 0; i < pairs.Count; i++) totalW += pairs[i].weight;
		if (totalW <= 0f)
		{
			// フォールバック：最大値
			float maxv = 0f;
			for (int i = 0; i < pairs.Count; i++) maxv = Mathf.Max(maxv, pairs[i].score);
			return maxv;
		}
		pairs.Sort((a, b) => b.score.CompareTo(a.score));
		float target = totalW * Mathf.Clamp01(percentile);
		float cum = 0f;
		for (int i = 0; i < pairs.Count; i++)
		{
			cum += pairs[i].weight;
			if (cum >= target) return pairs[i].score;
		}
		return pairs[^1].score;
	}

	private static float Median(float[] values)
	{
		if (values.Length == 0) return 0f;
		Array.Sort(values);
		int n = values.Length;
		if ((n & 1) == 1) return values[n / 2];
		return 0.5f * (values[n / 2 - 1] + values[n / 2]);
	}

	private static long EstimateBytesAtLevel(long baseBytes, int level)
	{
		if (level <= 0) return baseBytes;
		int shift = level * 2; // 面積は 1/4^level
		if (shift >= 62) return 0; // 安全ガード
		return baseBytes >> shift;
	}
}