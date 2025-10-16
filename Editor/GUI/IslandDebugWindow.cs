using UnityEditor;
using UnityEngine;
using com.aoyon.AutoConfigureTexture.Processor;

namespace com.aoyon.AutoConfigureTexture.GUI
{
	internal sealed class IslandDebugWindow : EditorWindow
	{
		private GameObject? _root;
		private TextureInfo[]? _textureInfos;
		private int _selectedTextureIndex = 0;

		private TextureInfo? _textureInfo;
		private Island[]? _islands;

		private RenderTexture? _idRT;
		private RenderTexture? _maskRT;
		private RenderTexture? _meanOverlayRT;

		private Vector2 _scroll;


		[MenuItem("Tools/AutoConfigureTexture/Island Debugger")]
		private static void Open()
		{
			GetWindow<IslandDebugWindow>("Island Debugger");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Island Debug / RenderTexture Preview", EditorStyles.boldLabel);
			var newRoot = (GameObject)EditorGUILayout.ObjectField("Root", _root, typeof(GameObject), true);
			if (newRoot != _root)
			{
				_root = newRoot;
				OnRootChanged(newRoot);
			}

			DrawTextureSelection();

			using (new EditorGUI.DisabledScope(_textureInfo == null || _root == null))
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
		
		private void OnRootChanged(GameObject newRoot)
		{
			_root = newRoot;
			var collector = new TextureInfoCollector();
			_textureInfos = collector.Execute(_root).ToArray();
			_selectedTextureIndex = 0;
		}

		private void DrawTextureSelection()
		{
			if (_textureInfos == null || _textureInfos.Length == 0) return;
			string[] textureNames = _textureInfos.Select(info => $"{info.Texture2D.name} ({info.Texture2D.width}x{info.Texture2D.height})").ToArray();
			int newSelectedIndex = EditorGUILayout.Popup("Source Texture", _selectedTextureIndex, textureNames);
			if (newSelectedIndex != _selectedTextureIndex)
			{
				OnTextureSelected(newSelectedIndex);
			}
		}

		private void OnTextureSelected(int newSelectedIndex)
		{
			if (_textureInfos == null || _textureInfos.Length == 0) return;
			_selectedTextureIndex = newSelectedIndex;
			_textureInfo = _textureInfos[_selectedTextureIndex];
			// Recalculate islands for the new texture
			var (islands, _) = IslandCalculator.CalculateIslandsFor(_textureInfo);
			_islands = islands;
			// Clear cached data
			_idRT?.Release();
			_idRT = null;
			_maskRT?.Release();
			_maskRT = null;
			_meanOverlayRT?.Release();
			_meanOverlayRT = null;
		}

		private void BuildAndPreviewIdRT()
		{
			if (_textureInfo == null || _islands == null) return;
			var svc = new IslandMaskService();
			_idRT = svc.BuildIslandIdMapRT(_textureInfo.Texture2D, _islands);
			IslandMaskService.DebugIDRT(_idRT, srcName: _textureInfo.Texture2D.name, islandCount: _islands.Length);
		}

		private void DrawAllIsland()
		{
			if (_textureInfo == null || _islands == null) return;
			if (_islands.Length == 0) { Debug.LogWarning("[ACT][IslandDebug] No islands"); return; }
			if (_maskRT == null)
			{
				_maskRT = new RenderTexture(_textureInfo.Texture2D.width, _textureInfo.Texture2D.height, 0, RenderTextureFormat.ARGB32);
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
			if (_textureInfo == null || _islands == null || _idRT == null) return;
			var eval = new IslandSSIMEvaluator();
			var (means, counts) = eval.Evaluate(_textureInfo.Texture2D, _idRT, 0, 7, 1, _islands.Length);
			_meanOverlayRT = IslandMeanVisualizer.BuildMeanOverlay(_idRT, means, counts, useHeatColor: false);
			
			int nonzeroCount = counts.Count(c => c > 0);
			int totalCount = counts.Sum();
			var first16 = means.Take(16).Select((m, i) => $"{i + 1}:{m:F4}(cnt={counts[i]})");
			Debug.Log($"[ACT][IslandSSIM] islands={_islands.Length}, nonzero={nonzeroCount}, totalWindows={totalCount}\n  first16: {string.Join(" ", first16)}");
		}

		private void DrawPreview()
		{
			if (_textureInfo == null && _idRT == null && _maskRT == null && _meanOverlayRT == null) return;

			string[] labels = { "Original", "ID Map", "Mask", "Mean Overlay" };
			Texture?[] textures = { _textureInfo?.Texture2D, _idRT, _maskRT, _meanOverlayRT };

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

