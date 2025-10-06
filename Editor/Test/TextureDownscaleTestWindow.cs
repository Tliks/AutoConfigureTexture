using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using com.aoyon.AutoConfigureTexture.Analyzer;


namespace com.aoyon.AutoConfigureTexture;

public class TextureDownscaleTestWindow : EditorWindow
{
	[SerializeField] private float q = 0.25f; // 0..1
	[SerializeField] private List<Texture2D> targets = new();
	[SerializeField] private Mesh mesh;
	[SerializeField] private int subMeshIndex = 0;
	[SerializeField] private int uvChannel = 0;
	[SerializeField] private TextureUsage usage = TextureUsage.MainTex;

	private Vector2 scroll;
	private List<TextureScaleDecider.Result> lastResults;

	[MenuItem("Tools/ACT/Texture Downscale Test")] public static void Open()
	{
		GetWindow<TextureDownscaleTestWindow>(false, "ACT Downscale Test");
	}

	private void OnGUI()
	{
		scroll = EditorGUILayout.BeginScrollView(scroll);
		EditorGUILayout.LabelField("q (0..1)");
		q = EditorGUILayout.Slider(q, 0f, 1f);
		usage = (TextureUsage)EditorGUILayout.EnumPopup("Usage", usage);
		mesh = (Mesh)EditorGUILayout.ObjectField("Mesh (for UV)", mesh, typeof(Mesh), false);
		subMeshIndex = EditorGUILayout.IntField("SubMesh", subMeshIndex);
		uvChannel = EditorGUILayout.IntField("UV Channel", uvChannel);

		SerializedObject so = new SerializedObject(this);
		var sp = so.FindProperty("targets");
		EditorGUILayout.PropertyField(sp, includeChildren: true);
		so.ApplyModifiedProperties();

		if (GUILayout.Button("Analyze"))
		{
			Analyze();
		}
		if (lastResults != null && lastResults.Count > 0)
		{
			long totalSaved = 0;
			foreach (var r in lastResults) totalSaved += r.SavedBytes;
			EditorGUILayout.LabelField($"Preview Saved: {EditorUtility.FormatBytes(totalSaved)}");
			if (GUILayout.Button("Apply"))
			{
				foreach (var r in lastResults)
				{
					DownscaleExecutor.Apply(r.Texture, r.SelectedScale);
				}
				AssetDatabase.Refresh();
			}
		}
		EditorGUILayout.EndScrollView();
	}

	private void Analyze()
	{
		if (targets == null || targets.Count == 0 || mesh == null)
		{
			EditorUtility.DisplayDialog("ACT", "Set targets and mesh.", "OK");
			return;
		}
		var items = new List<(TextureInfo, TextureUsage, Mesh, int, int)>();
		foreach (var t in targets)
		{
			if (t == null) continue;
			var ti = new TextureInfo(t);
			items.Add((ti, usage, mesh, subMeshIndex, uvChannel));
		}
		var decider = new TextureScaleDecider();
		lastResults = new List<TextureScaleDecider.Result>(decider.Decide(items, Mathf.Clamp01(q)));
	}
}


