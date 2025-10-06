using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// 単一パラメータ q∈[0,1] から T(q), B(q), F(q) を導出し、
/// 各テクスチャの許容スケール（品質最優先）を決定する。
/// </summary>
internal sealed class TextureScaleDecider
{
	private readonly ResolutionDegradationSensitivityAnalyzer _analyzer = new();
	private readonly IslandAnalyzer _islandAnalyzer = new();
	private readonly IslandMaskService _maskService = new();

	private static readonly float[] kScales = new[] { 0.5f, 0.25f, 0.0625f }; // 1/2, 1/4, 1/16

	public struct Result
	{
		public Texture2D Texture;
		public float SelectedScale; // 1.0 if unchanged
		public long SavedBytes;
		public string Reason;
	}

	public IReadOnlyList<Result> Decide(
		IReadOnlyList<(TextureInfo info, TextureUsage usage, Mesh mesh, int subMesh, int uvChannel)> items,
		float q)
	{
		// q→内部パラメータ
		float T = MapQualityThreshold(q);
		float B = MapBudgetRatio(q); // 0..1 of total
		float F = MapApplyFraction(q);

		// 各テクスチャごとに island→D_j(s) を計算し、許容集合 S_j^ok を作る
		var perTex = new List<(TextureInfo info, TextureUsage usage, float w, Dictionary<float, float> DjByScale, float sMax, long savedMax)>();
		long totalBytes = 0;
		foreach (var (info, usage, mesh, sub, uv) in items)
		{
			var tex = info.Texture2D;
			long bytes = EstimateSizeBytes(tex);
			totalBytes += bytes;
			float w = TextureImportanceHeuristics.Compute(info, usage);

			var islands = _islandAnalyzer.GetIslands(mesh, sub, uv);
			var DjByScale = new Dictionary<float, float>();
			float sMax = 1.0f;
			long savedMax = 0;
			foreach (var s in kScales)
			{
				// アイランド毎のスコアを計算し、重要度重み付き95p近似として max を採用（最小実装）
				float worst = 0f;
				for (int i = 0; i < islands.Count; i++)
				{
					float d = _analyzer.ComputeResolutionImpactForIsland(info, usage, islands[i], _maskService, s);
					if (d > worst) worst = d;
				}
				DjByScale[s] = NormalizeDelta(w, worst, T);
				if (DjByScale[s] <= 1.0f)
				{
					sMax = s; // 最大の粗い s を最後に残す
					savedMax = bytes - EstimateSizeBytes(tex, s);
				}
			}
			perTex.Add((info, usage, w, DjByScale, sMax, savedMax));
		}

		// 品質内での最大候補合計
		long potentialSaved = 0;
		foreach (var t in perTex) potentialSaved += t.savedMax;
		long budgetMax = (long)(B * totalBytes);

		var results = new List<Result>(perTex.Count);
		if (potentialSaved <= budgetMax)
		{
			foreach (var t in perTex)
			{
				results.Add(new Result
				{
					Texture = t.info.Texture2D,
					SelectedScale = t.sMax,
					SavedBytes = t.savedMax,
					Reason = "quality-ok:max"
				});
			}
			return results;
		}

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
	}

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
	private static long EstimateSizeBytes(Texture2D tex, float scale = 1.0f)
	{
		int w = Mathf.Max(1, Mathf.RoundToInt(tex.width * scale));
		int h = Mathf.Max(1, Mathf.RoundToInt(tex.height * scale));
		int bpp = GetFormatBytesPerPixel(tex.format);
		return (long)w * h * Mathf.Max(1, bpp);
	}
	private static int GetFormatBytesPerPixel(TextureFormat fmt)
	{
		switch (fmt)
		{
			case TextureFormat.RGBA32: return 4;
			case TextureFormat.RGB24: return 3;
			case TextureFormat.R8: return 1;
			default: return 4; // 簡易見積もり
		}
	}
}


