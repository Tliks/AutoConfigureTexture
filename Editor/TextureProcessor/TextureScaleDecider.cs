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
        GameObject root,
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

            // int selected = SelectDownScaleLevel(analysisResults, q);
            // results.Add(new Result { Texture = tex, SelectedDownScaleLevel = selected, Reason = $"ssim>={q:0.###}" });
        }

        return results;
	}

    private IslandAnalysisResult[] AnalyzeIslands(TextureInfo info, int maxDownScaleLevel)
    {
        var tex = info.Texture2D;

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

        float[] texelDensitiesPerIsland = new float[islands.Length];
        using (new Utils.ProfilerScope("TextureScaleDecider.TexelDensities"))
        {
            for (int i = 0; i < islands.Length; i++)
            {
                texelDensitiesPerIsland[i] = CalculateTexelDensity(tex.width, tex.height, islands[i]);
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
                analysisResults[i] = new IslandAnalysisResult(islands[i], descriptions[i], ssimMeansPerLevelPerIsland, texelDensitiesPerIsland[i], importanceScoresPerIsland[i]);
            }
        }

        return analysisResults;
    }

    private record IslandAnalysisResult(Island Island, IslandDescription Description, float[] SSIMMeansPerLevel, float TexelDensity, float HeuristicImportance);

    private static float CalculateTexelDensity(float width, float height, Island island)
    {
        double sumTotalDensity = 0.0;
        double sumTotalArea = 0.0;

        var vertices = island.Vertices;
        var uvs = island.UVs;
        var indices = island.Triangles;

        for (int i = 0; i + 2 < indices.Count; i += 3)
        {
            int i0 = indices[i];
            int i1 = indices[i + 1];
            int i2 = indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var uv0 = uvs[i0];
            var uv1 = uvs[i1];
            var uv2 = uvs[i2];

            float triArea3D = 0.5f * Vector3.Cross(v1 - v0, v2 - v0).magnitude;
            float triAreaUV = 0.5f * Mathf.Abs((uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv2.x - uv0.x) * (uv1.y - uv0.y));

            if (triArea3D > 0.0f && triAreaUV > 0.0f)
            {
                float texelAreaUV = triAreaUV * width * height;
                float texelDensity = Mathf.Sqrt(texelAreaUV / triArea3D); // 1単位長さあたりのピクセル数
                sumTotalDensity += texelDensity * triArea3D;
                sumTotalArea += triArea3D;
            }
        }

        if (sumTotalArea == 0.0) return 0f;
        return (float)(sumTotalDensity / sumTotalArea);
    }

	private static int SelectDownScaleLevel(float[][] meansPerLevel, int[][] countsPerLevel, float qualityThreshold)
	{
		int selected = 0;
		for (int level = 0; level < meansPerLevel.Length; level++)
		{
			float avg = ComputeWeightedMean(meansPerLevel[level], countsPerLevel[level]);
			if (avg >= qualityThreshold)
			{
				selected = level;
			}
			else
			{
				break;
			}
		}
		return selected;
	}

	private static float ComputeWeightedMean(float[] means, int[] counts)
	{
		if (means == null || counts == null || means.Length == 0 || counts.Length == 0) return 0f;
		long total = 0;
		double sum = 0.0;
		int len = System.Math.Min(means.Length, counts.Length);
		for (int i = 0; i < len; i++)
		{
			int c = counts[i];
			if (c <= 0) continue;
			total += c;
			sum += means[i] * c;
		}
		if (total == 0) return 0f;
		return (float)(sum / total);
	}
}