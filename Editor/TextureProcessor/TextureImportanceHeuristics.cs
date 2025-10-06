using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// テクスチャ重要度の簡易ヒューリスティック（0..1）。
/// 初期実装は Usage と解像度で粗くスコアリング。
/// </summary>
internal static class TextureImportanceHeuristics
{
	public static float Compute(TextureInfo info, TextureUsage usage)
	{
		float baseScore = usage switch
		{
			TextureUsage.NormalMap => 1.0f,
			TextureUsage.NormalMapSub => 0.9f,
			TextureUsage.MainTex => 0.9f,
			TextureUsage.Emission => 0.7f,
			TextureUsage.AOMap => 0.6f,
			_ => 0.5f
		};
		// 大解像度は目立ちやすい（ごく軽い補正）
		var tex = info.Texture2D;
		float megaPixels = (tex.width * tex.height) / 1_000_000f;
		float areaBoost = Mathf.Clamp01(megaPixels * 0.05f); // 20MPで+1.0上限想定 → 現実は数%程度
		float s = Mathf.Clamp01(baseScore * (1.0f + 0.15f * areaBoost));
		return s;
	}
}


