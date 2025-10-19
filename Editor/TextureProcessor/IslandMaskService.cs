using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class IslandTextureService
{
    private readonly Shader _idShader;
    private const string IdShaderName = "Hidden/ACT/IslandIdRenderer";

	public IslandTextureService()
	{
        _idShader = Shader.Find(IdShaderName);
		if (_idShader == null) throw new Exception($"Shader not found: {IdShaderName}");
	}

    public RenderTexture BuildIDMap(Texture2D src, IReadOnlyList<Island> islands)
    {
        var idRT = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };
        idRT.Create();

        var mat = new Material(_idShader);
        var mpb = new MaterialPropertyBlock();
        var cmd = new CommandBuffer();
        cmd.SetRenderTarget(idRT);
        cmd.SetViewport(new Rect(0, 0, idRT.width, idRT.height));
        // Editor可視化用：renderIntoTexture=false で左上原点（Editor表示座標系）に合わせる
        var view = Matrix4x4.LookAt(Vector3.back * 10f, Vector3.zero, Vector3.up);
        var proj = Matrix4x4.Ortho(0, 1, 0, 1, 0.01f, 20f);
        var gpuProj = GL.GetGPUProjectionMatrix(proj, /*renderIntoTexture*/ false);
        cmd.SetViewProjectionMatrices(view, gpuProj);
        cmd.ClearRenderTarget(true, true, Color.black);

        // Mesh はコマンド実行後に破棄する（実行前に破棄すると描画されない）
        var created = new List<Mesh>(Mathf.Max(1, islands.Count));
        for (int i = 0; i < islands.Count; i++)
        {
            var mesh = BuildUvMesh(islands[i]);
            created.Add(mesh);
            mpb.SetFloat("_IslandId", i + 1);
            cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0, mpb);
        }
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
        for (int i = 0; i < created.Count; i++)
        {
            if (created[i] != null) Object.DestroyImmediate(created[i]);
        }
        Object.DestroyImmediate(mat);
        return idRT;
    }

	public void DrawAllIsland(RenderTexture rt, IReadOnlyList<Island> islands)
	{
		if (rt == null) throw new Exception("RT is null");
		var cmd = new CommandBuffer { name = "ACT/DrawAllIslands" };
		cmd.SetRenderTarget(rt);
		cmd.SetViewport(new Rect(0, 0, rt.width, rt.height));
		// Editor可視化用：renderIntoTexture=false で左上原点（Editor表示座標系）に統一
		var view = Matrix4x4.LookAt(Vector3.back * 10f, Vector3.zero, Vector3.up);
		var proj = Matrix4x4.Ortho(0, 1, 0, 1, 0.01f, 20f);
		var gpuProj = GL.GetGPUProjectionMatrix(proj, /*renderIntoTexture*/ false);
		cmd.SetViewProjectionMatrices(view, gpuProj);
		cmd.ClearRenderTarget(true, true, Color.black);
		var mat = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideAndDontSave };
		try
		{
			var mesh = BuildUvMesh(islands);
			mat.color = Color.white;
			cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
			Graphics.ExecuteCommandBuffer(cmd);
			cmd.Release();
			Object.DestroyImmediate(mesh);
		}
		finally
		{
			Object.DestroyImmediate(mat);
		}
	}

    public static void DebugIDRT(RenderTexture rt, string srcName = "RT")
    {
        RenderTexture prev = RenderTexture.active;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
        try
        {
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            var raw = tex.GetRawTextureData<float>();
            var countPerId = new Dictionary<int, int>();
            foreach (var v in raw)
            {
                int id = Mathf.RoundToInt(v);
                countPerId[id] = countPerId.GetOrAdd(id, 0) + 1;
            }

            // カウントの多い順に10個だけデバッグ表示
            var top10 = countPerId.OrderByDescending(kv => kv.Value)
                                  .Take(10)
                                  .Select(kv => $"id={kv.Key},count={kv.Value}");
            Debug.Log(
                $"[ACT][IslandId Debug] {srcName} nonzero={countPerId[0]} uniqueIdCount={countPerId.Count} " +
                $"top10=[{string.Join(", ", top10)}]"
            );
        }
        finally
        {
            RenderTexture.active = prev;
            Object.DestroyImmediate(tex);
        }
    }

	private static Mesh BuildUvMesh(IReadOnlyList<Island> islands)
	{
		var vertices = new List<Vector3>();
		var indices = new List<int>();
		int vertexIndex = 0;
		foreach (var island in islands)
		{
			var tris = island.Triangles;
			var offs = island.TriangleIndices;
			var uvs = island.UVs;
			int triCount = offs.Length;
			for (int t = 0; t < triCount; t++)
			{
				int o = offs[t];
				int a = tris[o + 0], b = tris[o + 1], c = tris[o + 2];
				vertices.Add(new Vector3(uvs[a].x, uvs[a].y, 0));
				vertices.Add(new Vector3(uvs[b].x, uvs[b].y, 0));
				vertices.Add(new Vector3(uvs[c].x, uvs[c].y, 0));
				indices.Add(vertexIndex++);
				indices.Add(vertexIndex++);
				indices.Add(vertexIndex++);
			}
		}
		var mesh = new Mesh { name = "__ACT_IslandUvMesh__" };
		mesh.SetVertices(vertices);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
		mesh.UploadMeshData(false);
		return mesh;
	}
    private static Mesh BuildUvMesh(Island island)
    {
        var tris = island.Triangles;
        var offs = island.TriangleIndices;
        var uvs = island.UVs;
        int triCount = offs.Length;
        var vertices = new List<Vector3>(triCount * 3);
        var indices = new List<int>(triCount * 3);
        int vi = 0;
        for (int t = 0; t < triCount; t++)
        {
            int o = offs[t]; int a = tris[o + 0], b = tris[o + 1], c = tris[o + 2];
            vertices.Add(new Vector3(uvs[a].x, uvs[a].y, 0));
            vertices.Add(new Vector3(uvs[b].x, uvs[b].y, 0));
            vertices.Add(new Vector3(uvs[c].x, uvs[c].y, 0));
            indices.Add(vi++); indices.Add(vi++); indices.Add(vi++);
        }
        var mesh = new Mesh { name = "__ACT_IslandUvMesh__" };
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
        mesh.UploadMeshData(false);
        return mesh;
    }
}