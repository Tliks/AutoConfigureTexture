using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Processor;

/// <summary>
/// UVアイランドを R8 マスクにGPU描画するユーティリティ。
/// - マスクはソースと同解像度（赤=1.0が島、0.0が非対象）
/// - RenderTexture と Texture2D の両方生成に対応（用途に合わせて選択）
/// </summary>
internal sealed class IslandMaskService
{
	private readonly Shader _shader;

	private readonly Dictionary<(Texture, int), RenderTexture> _rtCache = new();
	private readonly Dictionary<(Texture, int), Texture2D> _texCache = new();

	private const string ShaderName = "Hidden/ACT/IslandMaskRenderer";

	public IslandMaskService()
	{
		_shader = Shader.Find(ShaderName);
		if (_shader == null)
		{
			Debug.LogError($"Shader not found: {ShaderName}");
		}
	}

	public RenderTexture BuildIslandMaskRT(Texture2D src, IslandAnalyzer.Island island)
	{
		int triCount = island.TriangleIndices.Count;
		if (triCount == 0)
		{
			return GetClearedRT(src.width, src.height);
		}

		// Fallback if shader is not found (avoid runtime error)
		if (_shader == null)
		{
			return GetClearedRT(src.width, src.height);
		}

		var mat = new Material(_shader);
		mat.hideFlags = HideFlags.HideAndDontSave;
		var rt = GetClearedRT(src.width, src.height);
		var mesh = BuildIslandMesh(island);
		var cmd = new CommandBuffer { name = "ACT/DrawIslandMask" };
		cmd.SetRenderTarget(rt);
		cmd.ClearRenderTarget(true, true, Color.black);
		cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
		Graphics.ExecuteCommandBuffer(cmd);
		cmd.Release();
		Object.DestroyImmediate(mesh);
		Object.DestroyImmediate(mat);
		return rt;
	}

	public Texture2D BuildIslandMaskTexture(Texture2D src, IslandAnalyzer.Island island)
	{
		var rt = BuildIslandMaskRT(src, island);
		var prev = RenderTexture.active;
		try
		{
			RenderTexture.active = rt;
			var tex = new Texture2D(rt.width, rt.height, TextureFormat.R8, false, true)
			{
				name = "__ACT_IslandMask2D__",
				filterMode = FilterMode.Bilinear,
				wrapMode = TextureWrapMode.Clamp
			};
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
			tex.Apply(false, false);
			return tex;
		}
		finally
		{
			RenderTexture.active = prev;
		}
	}

	private static Mesh BuildIslandMesh(IslandAnalyzer.Island island)
	{
		var tris = island.Triangles;
		var uvs = island.UVs;
		int triCount = island.TriangleIndices.Count;
		var vertices = new List<Vector3>(triCount * 3);
		var indices = new List<int>(triCount * 3);
		int vi = 0;
		for (int t = 0; t < triCount; t++)
		{
			int i0 = island.TriangleIndices[t];
			int a = tris[i0 + 0];
			int b = tris[i0 + 1];
			int c = tris[i0 + 2];
			vertices.Add(new Vector3(uvs[a].x, uvs[a].y, 0));
			vertices.Add(new Vector3(uvs[b].x, uvs[b].y, 0));
			vertices.Add(new Vector3(uvs[c].x, uvs[c].y, 0));
			indices.Add(vi++);
			indices.Add(vi++);
			indices.Add(vi++);
		}
		var mesh = new Mesh { name = "__ACT_IslandMesh__" };
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
		mesh.UploadMeshData(false);
		return mesh;
	}

	private static RenderTexture GetClearedRT(int w, int h)
	{
		var fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
			? RenderTextureFormat.R8
			: RenderTextureFormat.ARGB32;
		var rt = new RenderTexture(w, h, 0, fmt)
		{
			name = "__ACT_IslandMaskRT__",
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
			enableRandomWrite = false
		};
		rt.Create();
		var active = RenderTexture.active;
		RenderTexture.active = rt;
		GL.Clear(true, true, Color.black);
		RenderTexture.active = active;
		return rt;
	}
}


