using com.aoyon.AutoConfigureTexture.Processor;

namespace com.aoyon.AutoConfigureTexture.GUI
{
	internal sealed class IslandDebugWindow : EditorWindow
	{
		private GameObject? _root;
		private TextureInfo[]? _textureInfos;
		private int _selectedTextureIndex;

		private int _selectedMipLevel;
		private float _alpha = 1.0f;
		private float _beta = 1.0f;
		private float _gamma = 1.0f;

		private TextureInfo? _textureInfo;
		private Island[]? _islands;
		private Texture2D? _mipMapTexture;

		private RenderTexture? _idRT;
		private RenderTexture? _meanOverlayRT;


		[MenuItem("Tools/AutoConfigureTexture/Island Debugger")]
		private static void Open()
		{
			GetWindow<IslandDebugWindow>("Island Debugger");
		}

		private void OnEnable()
		{
			OnRootChanged(_root);
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Island Debug / RenderTexture Preview", EditorStyles.boldLabel);

			DrawRootSelection();
			if (_root == null) return;

			DrawTextureSelection();
			if (_textureInfo == null) return;

			DrawParameterSelection();

			EditorGUILayout.Space();
			if (GUILayout.Button("Build IslandId RT"))
			{
				BuildAndPreviewIdRT();
			}
			if (GUILayout.Button("Run Island SSIM"))
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
		
		private void OnRootChanged(GameObject? newRoot)
		{
			_root = newRoot;
			if (_root == null) return;
			var collector = new TextureInfoCollector();
			_textureInfos = collector.Execute(_root).ToArray();
			OnTextureSelected(0);
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
			OnMipLevelChanged(_selectedMipLevel);
			if (_idRT != null){
				_idRT.Release();
				_idRT = null;
			}
			if (_meanOverlayRT != null){
				_meanOverlayRT.Release();
				_meanOverlayRT = null;
			}
		}

		private void DrawParameterSelection()
		{
			if (_textureInfo != null)
			{
				var newMipLevel = EditorGUILayout.IntSlider("Mip Level", _selectedMipLevel, 0, _textureInfo.MipCount - 1);
				if (newMipLevel != _selectedMipLevel)
				{
					OnMipLevelChanged(newMipLevel);
				}
			}

			EditorGUILayout.LabelField("SSIM Parameters");
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Alpha, Beta, Gamma");
				_alpha = EditorGUILayout.Slider(_alpha, 1.0f, 10.0f);
				_beta = EditorGUILayout.Slider(_beta, 1.0f, 10.0f);
				_gamma = EditorGUILayout.Slider(_gamma, 1.0f, 10.0f);
			}
		}

		private void OnMipLevelChanged(int newMipLevel)
		{
			_mipMapTexture = null;
			if (_textureInfo == null) return;
			_selectedMipLevel = newMipLevel;
			var impoeredInfo = _textureInfo.ImportedInfo;
			if (impoeredInfo == null) throw new InvalidOperationException("importedInfo is null");
			_mipMapTexture = TextureUtility.GetMipMapTexture2D(_textureInfo.Texture2D, _selectedMipLevel, impoeredInfo.sRGBTexture);
		}

		private void BuildAndPreviewIdRT()
		{
			if (_textureInfo == null || _islands == null) throw new InvalidOperationException("textureInfo or islands is null");
			using var stopwatch = new Utils.StopwatchScope("BuildIDMap");
			var svc = new IslandTextureService();
			_idRT = svc.BuildIDMap(_textureInfo.Texture2D, _islands).Value;
			// IslandTextureService.DebugIDRT(_idRT, _textureInfo.Texture2D.name);
		}

		private void RunIslandSSIM()
		{
			if (_textureInfo == null || _islands == null || _idRT == null) throw new InvalidOperationException("textureInfo or islands or idRT is null");
			var eval = new IslandSSIMEvaluator();
			var (means, counts) = eval.Evaluate(_textureInfo.Texture2D, _idRT, _selectedMipLevel, _islands.Length, _alpha, _beta, _gamma);
			_meanOverlayRT = new IslandMeanVisualizer().BuildMeanOverlay(_idRT, means, counts, useHeatColor: true);
			
			int nonzeroCount = counts.Count(c => c > 0);
			int totalCount = counts.Sum();
			var first16 = means.Take(16).Select((m, i) => $"{i + 1}:{m:F4}(cnt={counts[i]})");
			Debug.Log($"[ACT][IslandSSIM] islands={_islands.Length}, nonzero={nonzeroCount}, totalWindows={totalCount}\n  first16: {string.Join(" ", first16)}");
		}

		private void DrawPreview()
		{
			if (_textureInfo == null && _mipMapTexture == null && _idRT == null && _meanOverlayRT == null)
				return;

			Texture?[] textures = { _textureInfo?.Texture2D, _mipMapTexture, _idRT, _meanOverlayRT};

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

