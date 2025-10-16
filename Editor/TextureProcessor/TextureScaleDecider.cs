namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class TextureScaleDecider
{
	private readonly IslandMaskService _maskService = new();

	public struct Result
	{
		public Texture2D Texture;
		public int SelectedDownScaleLevel; // 0: 1.0, 1: 1/2, 2: 1/4, 3: 1/8, ... n: 1/(2^n)
		public long SavedBytes;
		public string Reason;

		public override readonly string ToString()
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
            if (tex == null) { continue; }
            long bytes = MathHelper.ComputeVRAMSize(tex, tex.format);

            var (islands, _) = IslandCalculator.CalculateIslandsFor(info);
            if (islands.Length == 0)
            {
                results.Add(new Result { Texture = tex, SelectedDownScaleLevel = 0, SavedBytes = 0, Reason = "no-islands" });
                continue;
            }

            var idRT = _maskService.BuildIslandIdMapRT(tex, islands);
			var ssimEval = new IslandSSIMEvaluator();
            try
            {
				for (int si = 0; si < maxDownScaleLevel; si++)
				{
					var (means, counts) = ssimEval.Evaluate(tex, idRT, si, window: 11, stride: 2, numIslands: islands.Length);
				}
            }
            finally
            {
				if (idRT != null) idRT.Release();
            }
        }

        return results;
	}
}