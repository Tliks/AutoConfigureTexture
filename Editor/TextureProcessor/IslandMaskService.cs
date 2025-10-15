using UnityEngine.Rendering;
using System.IO;

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
    private readonly Shader _selectShader;

    private const string ShaderName = "Hidden/ACT/IslandMaskRenderer";
    private const string IdShaderName = "Hidden/ACT/IslandIdRenderer";
    private const string SelectShaderName = "Hidden/ACT/IslandSelectMask";

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
        _selectShader = Shader.Find(SelectShaderName);
        if (_selectShader == null)
        {
            throw new Exception($"Shader not found: {SelectShaderName}");
        }
	}

    public RenderTexture BuildIslandIdMapRT(Texture2D src, IReadOnlyList<Island> islands)
    {
        // IslandIdRenderer（作成済みシェーダ）を使用。VPはIdentity、シェーダ内で uv*2-1 へ変換。
        var rt = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32)
        {
            name = "__ACT_IslandIdRT__",
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
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
        var created = new System.Collections.Generic.List<Mesh>(Mathf.Max(1, islands.Count));
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
			var mesh = BuildUvMeshCombined(islands);
			mat.color = Color.white;
			cmd.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
			Graphics.ExecuteCommandBuffer(cmd);
			cmd.Release();
			UnityEngine.Object.DestroyImmediate(mesh);
		}
		finally
		{
			UnityEngine.Object.DestroyImmediate(mat);
		}
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

    /// <summary>
    /// IslandIdRT/Texture の内容を読み出し、IDが正しく描かれているかをログに出力します（デバッグ用）。
    /// 0=非対象, 1..N=島ID。必要に応じてPNG保存も可能です。
    /// </summary>
    public void DebugLogIslandIdStats(Texture2D src, IReadOnlyList<Island> islands, bool savePng = false, string? savePath = null, int maxPrintIds = 16)
    {
        var idTex = BuildIslandIdMapTexture(src, islands);
        try
        {
            var px = idTex.GetPixels32();
            int w = idTex.width, h = idTex.height;
            int total = px.Length;
            int zero = 0, nonzero = 0;

            var counts = new Dictionary<int, int>();
            int maxId = 0;
            for (int i = 0; i < total; i++)
            {
                int id = DecodeId24(px[i]);
                if (id == 0) { zero++; continue; }
                nonzero++;
                if (id > maxId) maxId = id;
                if (id <= maxPrintIds)
                {
                    counts.TryGetValue(id, out var c);
                    counts[id] = c + 1;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("[ACT][IslandId Debug] ").Append(src.name)
              .Append(" size=").Append(w).Append("x").Append(h)
              .Append(" islands(N)=").Append(islands.Count)
              .Append(" nonzero=").Append(nonzero)
              .Append(" (" ).Append((nonzero * 100f / Mathf.Max(1,total)).ToString("0.00")).Append("%)\n");
            sb.Append("  maxId=").Append(maxId).Append(" unique(first ").Append(maxPrintIds).Append("):");
            for (int id = 1; id <= Mathf.Min(maxId, maxPrintIds); id++)
            {
                counts.TryGetValue(id, out var c);
                sb.Append(" ").Append(id).Append(":").Append(c);
            }
            Debug.Log(sb.ToString());

            if (savePng)
            {
                try
                {
                    var bytes = idTex.EncodeToPNG();
                    var path = savePath;
                    if (string.IsNullOrEmpty(path))
                    {
                        path = Path.Combine(Application.dataPath, "__ACT_IslandIdDebug_" + src.name + ".png");
                    }
                    File.WriteAllBytes(path, bytes);
                    Debug.Log("[ACT][IslandId Debug] Saved PNG: " + path);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[ACT][IslandId Debug] Save PNG failed: " + e.Message);
                }
            }
        }
        finally
        {
            if (idTex != null) Object.DestroyImmediate(idTex);
        }
    }

	private static Mesh BuildUvMeshCombined(IReadOnlyList<Island> islands)
	{
		var vertices = new System.Collections.Generic.List<Vector3>();
		var indices = new System.Collections.Generic.List<int>();
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
        var vertices = new System.Collections.Generic.List<Vector3>(triCount * 3);
        var indices = new System.Collections.Generic.List<int>(triCount * 3);
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

    private static Color EncodeIdColor(int id)
    {
        byte r = (byte)(id & 255);
        byte g = (byte)((id >> 8) & 255);
        byte b = (byte)((id >> 16) & 255);
        return new Color32(r, g, b, 255);
    }

    /// <summary>
    /// 各アイランドのUV AABB（min/max）を集計し、[0,1]^2 と交差する数/しない数をログ出力します（デバッグ用）。
    /// UVが [0,1] 範囲に全く入っていなければ、IDマップに描かれない可能性が高いです（タイル/オフセットが原因）。
    /// </summary>
    public void DebugLogIslandUvBounds(IReadOnlyList<Island> islands)
    {
        int inCount = 0, outCount = 0;
        var sb = new System.Text.StringBuilder();
        sb.Append("[ACT][IslandUV Debug] islands=").Append(islands.Count).Append('\n');
        for (int i = 0; i < islands.Count; i++)
        {
            var isl = islands[i];
            var uvs = isl.UVs;
            var tris = isl.Triangles;
            var offs = isl.TriangleIndices;
            float minx =  1e9f, miny =  1e9f, maxx = -1e9f, maxy = -1e9f;
            for (int t = 0; t < offs.Length; t++)
            {
                int o = offs[t]; int a = tris[o+0], b = tris[o+1], c = tris[o+2];
                var uv0 = uvs[a]; var uv1 = uvs[b]; var uv2 = uvs[c];
                minx = Mathf.Min(minx, Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x)));
                miny = Mathf.Min(miny, Mathf.Min(uv0.y, Mathf.Min(uv1.y, uv2.y)));
                maxx = Mathf.Max(maxx, Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x)));
                maxy = Mathf.Max(maxy, Mathf.Max(uv0.y, Mathf.Max(uv1.y, uv2.y)));
            }
            bool intersects = !(maxx < 0f || maxy < 0f || minx > 1f || miny > 1f);
            if (intersects) { inCount++; }
            else { outCount++; }
            if (i < 10)
            {
                sb.Append("  #").Append(i+1).Append(" UV AABB=[(")
                  .Append(minx.ToString("0.###")).Append(",")
                  .Append(miny.ToString("0.###")).Append(")-(")
                  .Append(maxx.ToString("0.###")).Append(",")
                  .Append(maxy.ToString("0.###")).Append(")] ")
                  .Append(intersects ? "intersect" : "outside")
                  .Append('\n');
            }
        }
        sb.Append("  intersect=").Append(inCount).Append(" outside=").Append(outCount);
        Debug.Log(sb.ToString());
    }

    private static int DecodeId24(in Color32 c)
    {
        return c.r | (c.g << 8) | (c.b << 16);
    }
}