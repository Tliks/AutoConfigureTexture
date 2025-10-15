using UnityEditor;
using UnityEngine;
using com.aoyon.AutoConfigureTexture.Processor;

namespace com.aoyon.AutoConfigureTexture.GUI
{
	internal sealed class IslandDebugWindow : EditorWindow
	{
		private Texture2D? _texture;
		private Renderer? _renderer;
		private int _subMeshIndex = 0;
		private int _uvChannel = 0;

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
			_texture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", _texture, typeof(Texture2D), false);
			_renderer = (Renderer)EditorGUILayout.ObjectField("Renderer", _renderer, typeof(Renderer), true);
			_subMeshIndex = EditorGUILayout.IntField("SubMesh Index", _subMeshIndex);
			_uvChannel = EditorGUILayout.IntField("UV Channel", _uvChannel);

			using (new EditorGUI.DisabledScope(_texture == null || _renderer == null))
			{
				EditorGUILayout.Space();
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

		private void BuildAndPreviewIdRT()
		{
			if (_texture == null || _renderer == null) return;
			var mesh = Utils.GetMesh(_renderer);
			if (mesh == null) { Debug.LogWarning("[ACT][IslandDebug] Mesh not found on renderer"); return; }
			var islands = IslandCalculator.CalculateIslands(mesh, _subMeshIndex, _uvChannel);
			var svc = new IslandMaskService();
			_idRT = svc.BuildIslandIdMapRT(_texture, islands);
		}

		private void DrawAllIsland()
		{
			if (_texture == null || _renderer == null) return;
			var mesh = Utils.GetMesh(_renderer);
			if (mesh == null) { Debug.LogWarning("[ACT][IslandDebug] Mesh not found on renderer"); return; }
			var islands = IslandCalculator.CalculateIslands(mesh, _subMeshIndex, _uvChannel);
			if (islands.Length == 0) { Debug.LogWarning("[ACT][IslandDebug] No islands"); return; }
			if (_maskRT == null)
			{
				_maskRT = new RenderTexture(_texture.width, _texture.height, 0, RenderTextureFormat.ARGB32);
				_maskRT.filterMode = FilterMode.Point;
				_maskRT.wrapMode = TextureWrapMode.Clamp;
				_maskRT.Create();
			}
			var svc = new IslandMaskService();
			svc.DrawAllIsland(_maskRT, islands);
			Repaint();
		}

		private void RunIslandSSIM()
		{
			if (_texture == null || _renderer == null) return;
			var mesh = Utils.GetMesh(_renderer);
			if (mesh == null) { Debug.LogWarning("[ACT][IslandDebug] Mesh not found on renderer"); return; }
			var islands = IslandCalculator.CalculateIslands(mesh, _subMeshIndex, _uvChannel);
			var svc = new IslandMaskService();
			_idRT = svc.BuildIslandIdMapRT(_texture, islands);

			var eval = new IslandSSIMEvaluator();
			float[] scales = new float[] { 0.5f, 0.25f, 0.125f, 0.0625f };
			for (int si = 0; si < scales.Length; si++)
			{
				int mip = ComputeMipLevelForScale(_texture, scales[si]);
				var res = eval.Evaluate(_texture, _idRT, mip, 11, 2, islands.Length);
				_lastMeans = new float[res.Length];
				_lastCounts = new int[res.Length];
				for (int i = 0; i < res.Length; i++) { _lastMeans[i] = res[i].x; _lastCounts[i] = (int)res[i].y; }
				Debug.Log($"[ACT][IslandSSIM] {_texture.name} s={scales[si]:0.###} mip={mip} islands={islands.Length}");
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
			float spacing = 8f;
			int rows = 2, cols = 2;
			float previewSizeW = Mathf.Min((position.width - (cols + 1) * spacing) / cols, 400f);
			float previewSizeH = Mathf.Min((position.height - (rows + 1) * spacing) / rows, 400f);
			float previewSize = Mathf.Min(previewSizeW, previewSizeH);

			string[] labels = { "Original", "ID Map", "Mask", "Mean Overlay" };
			Texture?[] textures = { _texture, _idRT, _maskRT, _meanOverlayRT };

			for (int i = 0; i < 4; i++)
			{
				if (textures[i] == null) continue;
				int row = i / cols;
				int col = i % cols;
				float x = spacing + col * (previewSize + spacing);
				float y = spacing + row * (previewSize + spacing);
				Rect r = new Rect(x, y, previewSize, previewSize);
				EditorGUI.LabelField(new Rect(r.x, r.y - 18f, r.width, 16f), labels[i], EditorStyles.boldLabel);
				EditorGUI.DrawPreviewTexture(r, textures[i]);
			}
		}
	}
}

