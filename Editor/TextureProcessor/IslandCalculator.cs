namespace com.aoyon.AutoConfigureTexture.Processor;

internal record Island(IReadOnlyList<Vector3> Vertices, IReadOnlyList<Vector2> UVs, IReadOnlyList<int> Triangles, IReadOnlyList<int> TriangleIndices, float TriangleArea, float UVArea);
internal record IslandDescription(PropertyInfo PropertyInfo, int UVchannel, MaterialInfo MaterialInfo, Renderer Renderer, Mesh Mesh, int SubMeshIndex)
{
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"IslandDesciption: {PropertyInfo}");
        sb.Append($"  UVchannel: {UVchannel}");
        sb.Append($"  MaterialInfo: {MaterialInfo}");
        sb.Append($"  Renderer: {Renderer.name}");
        sb.Append($"  Mesh: {Mesh.name}");
        sb.Append($"  SubMeshIndex: {SubMeshIndex}");
        return sb.ToString();
    }
}

internal class IslandCalculator : IDisposable
{
    private readonly bool _bakeMesh;
    public IslandCalculator(bool bakeMesh = true)
    {
        _bakeMesh = bakeMesh;
    }

	public (Island[] Islands, IslandDescription[] Descriptions) CalculateIslandsFor(TextureInfo info)
	{
        using var profiler = new Utils.ProfilerScope("CalculateIslandsFor");

        var texture = info.Texture2D;
        float texWidth = texture != null ? texture.width : 1f;
        float texHeight = texture != null ? texture.height : 1f;

        var IslandswithDescription = new List<(Island[] islands, IslandDescription description)>();
        int totalIslandCount = 0;
        var uniqueKeys = new HashSet<IslandCalculationKey>();
		foreach (var property in info.ReferencedProperties)
		{
			var uv = property.UVchannel;
			var materialInfo = property.MaterialInfo;
			foreach (var (renderer, indices) in materialInfo.Renderers)
			{
				var mesh = RendererUtility.GetMesh(renderer);
				if (mesh == null) continue;
				foreach (var index in indices)
				{
                    var key = new IslandCalculationKey(renderer, mesh, index, uv);
                    if (uniqueKeys.Contains(key)) continue;
                    uniqueKeys.Add(key);
                    MayCalculateIslands(key, texWidth, texHeight, out var islands);
                    Debug.Log($"[ACT][IslandCalculator] islands={islands.Length} args={renderer.name}, {index}, {uv}");
					IslandswithDescription.Add((islands, new IslandDescription(property, uv, materialInfo, renderer, mesh, index)));
                    totalIslandCount += islands.Length;
				}
			}
		}

        var allIslands = new Island[totalIslandCount];
        var allDescriptions = new IslandDescription[totalIslandCount];
        int currentIndex = 0;
        foreach (var (islands, description) in IslandswithDescription)
        {
            foreach (var island in islands)
            {
                allIslands[currentIndex] = island;
                allDescriptions[currentIndex] = description; // same descrition
                currentIndex++;
            }
        }
        
        var result = (allIslands, allDescriptions);
        return result;
	}

    
    public record IslandCalculationKey(Renderer Renderer, Mesh Mesh, int SubMeshIndex, int UVchannel);
    
    private Dictionary<IslandCalculationKey, Island[]> _cachedIslands = new();
    private Dictionary<SkinnedMeshRenderer, Mesh> _cachedBakedMeshes = new();

    public bool MayCalculateIslands(IslandCalculationKey key, float texWidth, float texHeight, out Island[] islands)
    {
        using var profiler = new Utils.ProfilerScope("MayCalculateIslands");
        if (_cachedIslands.TryGetValue(key, out var cachedIslands))
        {
            islands = cachedIslands;
            return false;
        }
        Mesh bakedMesh = key.Mesh;
        if (_bakeMesh && key.Renderer is SkinnedMeshRenderer smr && !_cachedBakedMeshes.TryGetValue(smr, out bakedMesh))
        {
            bakedMesh = new Mesh();
            smr.BakeMesh(bakedMesh, false);
            _cachedBakedMeshes[smr] = bakedMesh;
        }
        islands = CalculateIslandsImpl(bakedMesh, key.SubMeshIndex, key.UVchannel, key.Renderer.transform.position, key.Renderer.transform.rotation, texWidth, texHeight);
        _cachedIslands[key] = islands;
        return true;
    }

    private Island[] CalculateIslandsImpl(Mesh bakedMesh, int subMeshIndex, int uvChannel, Vector3 basePosition, Quaternion baseRotation, float texWidth, float texHeight)
    {
        using var profiler = new Utils.ProfilerScope("CalculateIslandsImpl");

        Profiler.BeginSample("BakeMesh");
        Profiler.EndSample();

        Profiler.BeginSample("Init");
        var indies = bakedMesh.GetIndices(subMeshIndex);
        var vertCount = indies.Length;
        
        var vertices = new List<Vector3>();
        bakedMesh.GetVertices(vertices);
        
        var uvs = new List<Vector2>(vertCount);
        bakedMesh.GetUVs(uvChannel, uvs);
        Profiler.EndSample();

        Profiler.BeginSample("InitUnionFind");
        var unionFind = new UnionFind(vertCount);
        Profiler.EndSample();

        Profiler.BeginSample("Merge Triangles");
        var triangles = bakedMesh.GetTriangles(subMeshIndex);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            unionFind.Unite(triangles[i], triangles[i + 1]);
            unionFind.Unite(triangles[i + 1], triangles[i + 2]);
        }
        Profiler.EndSample();

        Profiler.BeginSample("Merge Same UV Position");
        var uvMap = new Dictionary<Vector2, int>();
        for (int i = 0; i < uvs.Count; i++)
        {
            var uv = uvs[i];
            if (uvMap.TryGetValue(uv, out var index))
            {
                unionFind.Unite(i, index);
            }
            uvMap[uv] = i;
        }
        Profiler.EndSample();

        Profiler.BeginSample("islandIndices");
        var islandIndices = new List<int>?[vertCount];
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int root = unionFind.Find(triangles[i]);
            var indices = islandIndices[root];
            if (indices is null)
            {
                indices = new();
                islandIndices[root] = indices;
            }
            indices.Add(i);
        }
        Profiler.EndSample();
        Profiler.BeginSample("worldVerts");
        var worldVerts = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; ++i)
        {
            worldVerts[i] = basePosition + baseRotation * vertices[i];
        }
        Profiler.EndSample();
        Profiler.BeginSample("result");
        var islands = new List<Island>();
        foreach (var indices in islandIndices)
        {
            if (indices is null) continue;
            var (triangleArea, uvArea) = CalculateArea(worldVerts, uvs, triangles, indices);
            islands.Add(new Island(worldVerts, uvs, triangles, indices, triangleArea, uvArea));
        }
        var result = islands.ToArray();
        Profiler.EndSample();
        return result;
    }

    private static (float triangleArea, float uvArea) CalculateArea(IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector2> uvs, IReadOnlyList<int> triangles, IReadOnlyList<int> triangleIndices)
    {
        float sumTotalTriangleArea = 0.0f;
        float sumTotalUvArea = 0.0f;

        int triangleCount = triangleIndices.Count;
        for (int t = 0; t < triangleCount; t++)
        {
            int offset = triangleIndices[t];
            int i0 = triangles[offset];
            int i1 = triangles[offset + 1];
            int i2 = triangles[offset + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var uv0 = uvs[i0];
            var uv1 = uvs[i1];
            var uv2 = uvs[i2];

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            float triArea3D = 0.5f * Vector3.Cross(edge1, edge2).magnitude;

            float uvDx1 = uv1.x - uv0.x;
            float uvDy1 = uv1.y - uv0.y;
            float uvDx2 = uv2.x - uv0.x;
            float uvDy2 = uv2.y - uv0.y;
            float triAreaUV = 0.5f * Mathf.Abs(uvDx1 * uvDy2 - uvDx2 * uvDy1);

            if (triArea3D > 0.0f && triAreaUV > 0.0f)
            {
                sumTotalTriangleArea += triArea3D;
                sumTotalUvArea += triAreaUV;
            }
        }
        return (sumTotalTriangleArea, sumTotalUvArea);
    }

    public void Dispose()
    {
        foreach (var bakedMesh in _cachedBakedMeshes)
        {
            Object.DestroyImmediate(bakedMesh.Value);
        }
        _cachedBakedMeshes.Clear();
    }

    class UnionFind
    {
        private int[] parent;
        private int[] rank;

        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];

            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        public int Find(int x)
        {
            if (parent[x] == x)
                return x;
            else
                return parent[x] = Find(parent[x]);
        }

        public void Unite(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);
            if (rootX == rootY) return;

            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
            }
            else
            {
                parent[rootX] = rootY;
                rank[rootY]++;
            }
        }
    }
}