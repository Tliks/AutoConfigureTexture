using UnityEditor;
using UnityEngine;
using com.aoyon.AutoConfigureTexture.Processor;

namespace com.aoyon.AutoConfigureTexture.GUI
{
	internal sealed class IslandDebugWindow : EditorWindow
	{
		private GameObject? _root;
		private Texture2D? _texture;

		private TextureInfo? _textureInfo;
		private Island[]? _islands;

		private RenderTexture? _idRT;
		private RenderTexture? _maskRT;
		private RenderTexture? _meanOverlayRT;

		private Vector2 _scroll;
		private float[] _lastMeans = System.Array.Empty<float>();
		private int[] _lastCounts = System.Array.Empty<int>();

		[MenuItem("Tools/AutoConfigureTexture/Island Debugger")]
		private static void Open()
		{
			GetWindow<IslandDebugWindow>("Island Debugger");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Island Debug / RenderTexture Preview", EditorStyles.boldLabel);
			_root = (GameObject)EditorGUILayout.ObjectField("Root", _root, typeof(GameObject), true);
			_texture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", _texture, typeof(Texture2D), false);

			using (new EditorGUI.DisabledScope(_texture == null || _root == null))
			{
				EditorGUILayout.Space();
				if (GUILayout.Button("0) Collect Data"))
				{
					CollectData();
				}
				if (GUILayout.Button("1) Build & Preview IslandId RT"))
				{
					BuildAndPreviewIdRT();
				}
				if (GUILayout.Button("1b) Draw All Islands (sanity)"))
				{
					DrawAllIsland();
				}
				if (GUILayout.Button("2) Run Island SSIM (all candidate scales)"))
				{
					RunIslandSSIM();
				}
			}

			EditorGUILayout.Space();
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			DrawPreview();
			EditorGUILayout.EndScrollView();
		}

		private void CollectData()
		{
			if (_root == null || _texture == null) return;
			var collector = new TextureInfoCollector();
			var textureInfos = collector.Execute(_root);
			_textureInfo = textureInfos.First(info => info.Texture2D == _texture);
			if (_textureInfo == null) return;
			var (islands, _) = IslandCalculator.CalculateIslandsFor(_textureInfo);
			_islands = islands;
		}

		private void BuildAndPreviewIdRT()
		{
			if (_textureInfo == null || _islands == null) return;
			var svc = new IslandMaskService();
			_idRT = svc.BuildIslandIdMapRT(_textureInfo.Texture2D, _islands);
		}

		private void DrawAllIsland()
		{
			if (_texture == null || _islands == null) return;
			if (_islands.Length == 0) { Debug.LogWarning("[ACT][IslandDebug] No islands"); return; }
			if (_maskRT == null)
			{
				_maskRT = new RenderTexture(_texture.width, _texture.height, 0, RenderTextureFormat.ARGB32);
				_maskRT.filterMode = FilterMode.Point;
				_maskRT.wrapMode = TextureWrapMode.Clamp;
				_maskRT.Create();
			}
			var svc = new IslandMaskService();
			svc.DrawAllIsland(_maskRT, _islands);
			Repaint();
		}

		private void RunIslandSSIM()
		{
			if (_texture == null || _islands == null || _idRT == null) return;
			var eval = new IslandSSIMEvaluator();
			float[] scales = new float[] { 0.5f, 0.25f, 0.125f, 0.0625f };
			for (int si = 0; si < scales.Length; si++)
			{
				int mip = ComputeMipLevelForScale(_texture, scales[si]);
				var res = eval.Evaluate(_texture, _idRT, mip, 11, 2, _islands.Length);
				_lastMeans = new float[res.Length];
				_lastCounts = new int[res.Length];
				for (int i = 0; i < res.Length; i++) { _lastMeans[i] = res[i].x; _lastCounts[i] = (int)res[i].y; }
				Debug.Log($"[ACT][IslandSSIM] {_texture.name} s={scales[si]:0.###} mip={mip} islands={_islands.Length}");
				for (int i = 0; i < Mathf.Min(16, res.Length); i++)
				{
					Debug.Log($"  #{i+1}: mean={res[i].x:0.###} n={(int)res[i].y}");
				}
				_meanOverlayRT = IslandMeanVisualizer.BuildMeanOverlay(_idRT, res, useHeatColor: false);
			}
		}

		private static int ComputeMipLevelForScale(Texture2D src, float scale)
		{
			int upW = src.width;
			int downW = Mathf.Max(1, Mathf.RoundToInt(upW * Mathf.Clamp(scale, 1e-6f, 1f)));
			int mipLevel = 0;
			int sx = Mathf.Max(1, upW / downW);
			while ((1 << mipLevel) < sx) mipLevel++;
			return Mathf.Max(1, mipLevel);
		}

		private void DrawPreview()
		{
			if (_texture == null && _idRT == null && _maskRT == null && _meanOverlayRT == null) return;

			string[] labels = { "Original", "ID Map", "Mask", "Mean Overlay" };
			Texture?[] textures = { _texture, _idRT, _maskRT, _meanOverlayRT };

			float previewMaxSize = 400f;
			int cols = 2;
			int rows = 2;

			EditorGUILayout.BeginHorizontal();
			for (int row = 0; row < rows; row++)
			{
				EditorGUILayout.BeginVertical();
				for (int col = 0; col < cols; col++)
				{
					int idx = row * cols + col;
					if (idx >= textures.Length) break;

					var tex = textures[idx];
					if (tex != null)
					{
						EditorGUILayout.LabelField(labels[idx], EditorStyles.boldLabel, GUILayout.MaxWidth(previewMaxSize));
						GUILayout.Box(tex, GUILayout.Width(previewMaxSize), GUILayout.Height(previewMaxSize));
					}
					else
					{
						GUILayout.Space(previewMaxSize + 18f);
					}
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}

