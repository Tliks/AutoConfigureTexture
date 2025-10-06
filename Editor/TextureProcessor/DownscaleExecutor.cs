using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal static class DownscaleExecutor
{
	public static void Apply(Texture2D texture, float scale)
	{
		if (texture == null || scale >= 1.0f) return;
		string path = AssetDatabase.GetAssetPath(texture);
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		if (importer == null) return;

		int targetW = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
		int targetH = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));
		int maxSide = Mathf.Max(targetW, targetH);
		// 2のべき乗の上限に寄せる（Unityの maxTextureSize はプリセット値を使う）
		int mts = ClosestAllowedMaxSize(maxSide);
		importer.maxTextureSize = mts;
		importer.mipmapEnabled = true;
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
	}

	private static int ClosestAllowedMaxSize(int side)
	{
		// Unity標準の maxTextureSize 候補（代表）
		int[] allowed = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
		int best = allowed[0];
		for (int i = 0; i < allowed.Length; i++)
		{
			if (allowed[i] >= side) { best = allowed[i]; break; }
			best = allowed[i];
		}
		return best;
	}
}


