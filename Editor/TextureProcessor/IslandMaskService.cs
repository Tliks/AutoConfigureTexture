using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal sealed class IslandMaskService
{
    private readonly Shader _idShader;
    private const string IdShaderName = "Hidden/ACT/IslandIdRenderer";

	public IslandMaskService()
	{
        _idShader = Shader.Find(IdShaderName);
		if (_idShader == null)
		{
			throw new Exception($"Shader not found: {IdShaderName}");
		}
	}

    public RenderTexture BuildIslandIdMapRT(Texture2D src, IReadOnlyList<Island> islands)
    {
        // IslandIdRenderer（作成済みシェーダ）を使用。VPはIdentity、シェーダ内で uv*2-1 へ変換。
        // ID をガンマに依存させないため、RFloat で保持（任意精度・変換なし）
        var rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
        {
            name = "__ACT_IslandIdRT__",
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        rt.Create();

        var mat = new Material(_idShader) { hideFlags = HideFlags.HideAndDontSave };
        var mpb = new MaterialPropertyBlock();
        var cmd = new CommandBuffer { name = "ACT/DrawIslandIdMap" };
        cmd.SetRenderTarget(rt);
        cmd.SetViewport(new Rect(0, 0, rt.width, rt.height));
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
        return rt;
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

    public static void DebugIDRT(RenderTexture rt, string srcName = "RT", int islandCount = 0, int maxPrintIds = 16)
    {
        if (rt == null)
        {
            Debug.LogWarning("[ACT][IslandID Debug] RenderTexture is null");
            return;
        }

        RenderTexture prev = RenderTexture.active;


        // RFloat で書かれた ID をそのまま読み出す
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, /*linear*/ true);
        try
        {
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            var raw = tex.GetRawTextureData<float>();
            int w = rt.width, h = rt.height;
            int total = raw.Length;
            int zero = 0, nonzero = 0;
            var counts = new Dictionary<int, int>();
            int maxId = 0;

            for (int i = 0; i < total; i++)
            {
                int id = Mathf.RoundToInt(raw[i]);
                if (id <= 0) { zero++; continue; }
                nonzero++;
                if (id > maxId) maxId = id;
                if (id <= maxPrintIds)
                {
                    counts.TryGetValue(id, out var c);
                    counts[id] = c + 1;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("[ACT][IslandId Debug] ").Append(srcName)
              .Append(" size=").Append(w).Append("x").Append(h);
            if (islandCount > 0) sb.Append(" islands(N)=").Append(islandCount);
            sb.Append(" nonzero=").Append(nonzero)
              .Append(" (" ).Append((nonzero * 100f / Mathf.Max(1, total)).ToString("0.00")).Append("%)\n");
            sb.Append("  maxId=").Append(maxId).Append(" unique(first ").Append(maxPrintIds).Append("):");
            for (int id = 1; id <= Mathf.Min(maxId, maxPrintIds); id++)
            {
                counts.TryGetValue(id, out var c);
                sb.Append(" ").Append(id).Append(":").Append(c);
            }
            Debug.Log(sb.ToString());
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