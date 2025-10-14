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
	private readonly Shader _idShader;

	private const string ShaderName = "Hidden/ACT/IslandMaskRenderer";
	private const string IdShaderName = "Hidden/ACT/IslandIdRenderer";

	public IslandMaskService()
	{
		_shader = Shader.Find(ShaderName);
		if (_shader == null)
		{
			throw new Exception($"Shader not found: {ShaderName}");
		}
		_idShader = Shader.Find(IdShaderName);
		if (_idShader == null)
		{
			throw new Exception($"Shader not found: {IdShaderName}");
		}
	}

	public RenderTexture BuildIslandIdMapRT(Texture2D src, IReadOnlyList<Island> islands)
	{
		var fmt = RenderTextureFormat.ARGB32;
		var rt = new RenderTexture(src.width, src.height, 0, fmt)
		{
			name = "__ACT_IslandIdRT__",
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp
		};
		rt.Create();

		var cmd = new CommandBuffer { name = "ACT/DrawIslandIdMap" };
		cmd.SetRenderTarget(rt);
		cmd.ClearRenderTarget(true, true, Color.black);

		var mat = new Material(_idShader) { hideFlags = HideFlags.HideAndDontSave };
		for (int i = 0; i < islands.Count; i++)
		{
			var mesh = BuildIslandMesh(islands[i]);
			mat.SetFloat("_IslandId", i + 1); // 0 を非対象に、1..N を有効ID
			cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
			Object.DestroyImmediate(mesh);
		}
		Graphics.ExecuteCommandBuffer(cmd);
		cmd.Release();
		Object.DestroyImmediate(mat);
		return rt;
	}

	public Texture2D BuildIslandIdMapTexture(Texture2D src, IReadOnlyList<Island> islands)
	{
		var rt = BuildIslandIdMapRT(src, islands);
		var prev = RenderTexture.active;
		try
		{
			RenderTexture.active = rt;
			var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true)
			{
				name = "__ACT_IslandIdTex__",
				filterMode = FilterMode.Point,
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

	private static Mesh BuildIslandMesh(Island island)
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
}