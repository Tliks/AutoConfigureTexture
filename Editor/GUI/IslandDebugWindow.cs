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
			DrawLastStats();
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
			svc.DebugLogIslandUvBounds(islands);
			svc.DebugLogIslandIdStats(_texture, islands, savePng: false);
		}

		private void RunIslandSSIM()
		{
			if (_texture == null || _renderer == null) return;
			var mesh = Utils.GetMesh(_renderer);
			if (mesh == null) { Debug.LogWarning("[ACT][IslandDebug] Mesh not found on renderer"); return; }
			var islands = IslandCalculator.CalculateIslands(mesh, _subMeshIndex, _uvChannel);
			var svc = new IslandMaskService();
			if (_idRT == null) _idRT = svc.BuildIslandIdMapRT(_texture, islands);

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
			}
		}

		private void DrawAllIsland()
		{
			if (_texture == null || _renderer == null) return;
			var mesh = Utils.GetMesh(_renderer);
			if (mesh == null) { Debug.LogWarning("[ACT][IslandDebug] Mesh not found on renderer"); return; }
			var islands = IslandCalculator.CalculateIslands(mesh, _subMeshIndex, _uvChannel);
			if (islands.Length == 0) { Debug.LogWarning("[ACT][IslandDebug] No islands"); return; }
			if (_idRT == null)
			{
				_idRT = new RenderTexture(_texture.width, _texture.height, 0, RenderTextureFormat.ARGB32);
				_idRT.filterMode = FilterMode.Point;
				_idRT.wrapMode = TextureWrapMode.Clamp;
				_idRT.Create();
			}
			var svc = new IslandMaskService();
			svc.DrawAllIsland(_idRT, islands);
			Repaint();
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
			if (_idRT == null) return;
			Rect r = GUILayoutUtility.GetRect(position.width - 20, position.width - 20, 256, 2048);
			EditorGUI.DrawPreviewTexture(r, _idRT);
		}

		private void DrawLastStats()
		{
			if (_lastMeans == null || _lastCounts == null || _lastMeans.Length == 0) return;
			EditorGUILayout.LabelField("Last SSIM Means (first 32)", EditorStyles.boldLabel);
			int show = Mathf.Min(32, _lastMeans.Length);
			for (int i = 0; i < show; i++)
			{
				EditorGUILayout.LabelField($"#{i+1}: mean={_lastMeans[i]:0.###} n={_lastCounts[i]}");
			}
		}
	}
}

