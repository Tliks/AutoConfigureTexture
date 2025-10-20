using UnityEditor;
using UnityEngine;
using com.aoyon.AutoConfigureTexture.Processor;

namespace com.aoyon.AutoConfigureTexture.GUI
{
	internal sealed class IslandDebugWindow : EditorWindow
	{
		private GameObject? _root = null;
		private TextureInfo[]? _textureInfos = null;
		private int _selectedTextureIndex = 0;

		private TextureInfo? _textureInfo = null;
		private Island[]? _islands = null;

		private RenderTexture? _idRT = null;
		private RenderTexture? _maskRT = null;
		private RenderTexture? _meanOverlayRT = null;

		private Vector2 _scroll = Vector2.zero;


		[MenuItem("Tools/AutoConfigureTexture/Island Debugger")]
		private static void Open()
		{
			GetWindow<IslandDebugWindow>("Island Debugger");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Island Debug / RenderTexture Preview", EditorStyles.boldLabel);

			DrawRootSelection();
			if (_root == null) return;

			DrawTextureSelection();
			if (_textureInfo == null) return;

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

			DrawPreview();
		}


		private void DrawRootSelection()
		{
			var newRoot = (GameObject)EditorGUILayout.ObjectField("Root", _root, typeof(GameObject), true);
			if (newRoot != _root)
			{
				_root = newRoot;
				OnRootChanged(newRoot);
			}
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
			using var islandCalculator = new IslandCalculator();
			var (islands, _) = islandCalculator.CalculateIslandsFor(_textureInfo);
			Debug.Log($"[ACT][IslandDebug] islands={islands.Length}");
			_islands = islands;
			// Clear cached data
			if (_idRT != null){
				_idRT.Release();
				_idRT = null;
			}
			if (_maskRT != null){
				_maskRT.Release();
				_maskRT = null;
			}
			if (_meanOverlayRT != null){
				_meanOverlayRT.Release();
				_meanOverlayRT = null;
			}
		}

		private void BuildAndPreviewIdRT()
		{
			if (_textureInfo == null || _islands == null) throw new InvalidOperationException("textureInfo or islands is null");
			using var stopwatch = new Utils.StopwatchScope("BuildIDMap");
			var svc = new IslandTextureService();
			_idRT = svc.BuildIDMap(_textureInfo.Texture2D, _islands);
			// IslandTextureService.DebugIDRT(_idRT, _textureInfo.Texture2D.name);
		}

		private void DrawAllIsland()
		{
			if (_textureInfo == null || _islands == null) throw new InvalidOperationException("textureInfo or islands is null");
			if (_islands.Length == 0) { Debug.LogWarning("[ACT][IslandDebug] No islands"); return; }
			if (_maskRT == null)
			{
				_maskRT = new RenderTexture(_textureInfo.Texture2D.width, _textureInfo.Texture2D.height, 0, RenderTextureFormat.ARGB32);
				_maskRT.filterMode = FilterMode.Point;
				_maskRT.wrapMode = TextureWrapMode.Clamp;
				_maskRT.Create();
			}
			var svc = new IslandTextureService();
			svc.DrawAllIsland(_maskRT, _islands);
			Repaint();
		}

		private void RunIslandSSIM()
		{
			if (_textureInfo == null || _islands == null || _idRT == null) throw new InvalidOperationException("textureInfo or islands or idRT is null");
			var eval = new IslandSSIMEvaluator();
			var (means, counts) = eval.Evaluate(_textureInfo.Texture2D, _idRT, 0, 7, 1, _islands.Length);
			_meanOverlayRT = new IslandMeanVisualizer().BuildMeanOverlay(_idRT, means, counts, useHeatColor: false);
			
			int nonzeroCount = counts.Count(c => c > 0);
			int totalCount = counts.Sum();
			var first16 = means.Take(16).Select((m, i) => $"{i + 1}:{m:F4}(cnt={counts[i]})");
			Debug.Log($"[ACT][IslandSSIM] islands={_islands.Length}, nonzero={nonzeroCount}, totalWindows={totalCount}\n  first16: {string.Join(" ", first16)}");
		}

		private void DrawPreview()
		{
			if (_textureInfo == null && _idRT == null && _maskRT == null && _meanOverlayRT == null)
				return;

			Texture?[] textures = { _textureInfo?.Texture2D, _idRT, _maskRT, _meanOverlayRT };

			int cols = 2;
			int rows = 2;

			var totalRect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			float cellWidth = totalRect.width / cols;
			float cellHeight = totalRect.height / rows;

			for (int i = 0; i < textures.Length; i++)
			{
				var tex = textures[i];
				if (tex != null)
				{
					int row = i / cols;
					int col = i % cols;
					var x = totalRect.x + col * cellWidth;
					var y = totalRect.y + row * cellHeight;
					float areaWidth = cellWidth;
					float areaHeight = cellHeight;

					float drawWidth = areaWidth;
					float drawHeight = areaHeight;

					float aspect = tex.width / (float)tex.height;
					float cellAspect = areaWidth / areaHeight;

					if (aspect > cellAspect)
					{
						drawWidth = areaWidth;
						drawHeight = areaWidth / aspect;
					}
					else
					{
						drawHeight = areaHeight;
						drawWidth = areaHeight * aspect;
					}

					float offsetX = (areaWidth - drawWidth) * 0.5f;
					float offsetY = (areaHeight - drawHeight) * 0.5f;

					var cellRect = new Rect(
						x + offsetX,
						y + offsetY,
						drawWidth,
						drawHeight
					);

					EditorGUI.DrawPreviewTexture(cellRect, tex);
				}
			}
		}
	}
}

