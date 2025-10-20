using UnityEngine.Pool;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal class Island
{
    public readonly Vector3[] Vertices; // world space
    public readonly Vector2[] UVs;
    public readonly int[] Triangles;

    public readonly int[] TriangleIndices;
    
    public Island(Vector3[] vertices, Vector2[] uvs, int[] triangles, int[] triangleIndices)
    {
        Vertices = vertices;
        UVs = uvs;
        Triangles = triangles;
        TriangleIndices = triangleIndices;
    }
}

internal class IslandCalculator : IDisposable
{
    private readonly bool _bakeMesh;
    private readonly Dictionary<SkinnedMeshRenderer, Mesh> _bakedMeshes;
    public IslandCalculator(bool bakeMesh = true)
    {
        _bakeMesh = bakeMesh;
        _bakedMeshes = new();
    }

	public (Island[], IslandArgument[]) CalculateIslandsFor(TextureInfo info)
	{
		var uniqueArgs = new HashSet<IslandArgument>();
		foreach (var property in info.Properties)
		{
			var uv = property.UVchannel;
			var materialInfo = property.MaterialInfo;
			foreach (var (renderer, indices) in materialInfo.Renderers)
			{
				var mesh = Utils.GetMesh(renderer);
				if (mesh == null) continue;
				foreach (var index in indices)
				{
					uniqueArgs.Add(new IslandArgument(property, uv, materialInfo, renderer, mesh, index));
				}
			}
		}
		var allIslands = new List<Island>();
		var allArgs = new List<IslandArgument>();
		foreach (var arg in uniqueArgs)
		{
            Debug.Log($"[ACT] CalculateIslands: {arg.ToString()}");
			var islandsPerArg = CalculateIslands(arg);
			allArgs.AddRange(Enumerable.Repeat(arg, islandsPerArg.Length)); // 同じArgにより生成されたIslands
			allIslands.AddRange(islandsPerArg);
		}
		return (allIslands.ToArray(), allArgs.ToArray());
	}

	public record IslandArgument(PropertyInfo PropertyInfo, int UVchannel, MaterialInfo MaterialInfo, Renderer Renderer, Mesh Mesh, int SubMeshIndex)
    {
        public override string ToString()
        {
            return $"IslandArgument(PropertyInfo={PropertyInfo}, UVchannel={UVchannel}, MaterialInfo={MaterialInfo}, Renderer={Renderer}, Mesh={Mesh}, SubMeshIndex={SubMeshIndex})";
        }
    }

    private Dictionary<(Mesh, int, int, Vector3, Quaternion), Island[]> _cachedIslands = new();

    public Island[] CalculateIslands(IslandArgument arg)
    {
        var renderer = arg.Renderer;

        var mesh = arg.Mesh;
        var subMeshIndex = arg.SubMeshIndex;
        var uvChannel = arg.UVchannel;

        Mesh bakedMesh = mesh;
        if (_bakeMesh && renderer is SkinnedMeshRenderer smr)
        {
            bakedMesh = _bakedMeshes.GetOrAdd(smr, static (r) => {
                var bakedMesh = new Mesh();
                r.BakeMesh(bakedMesh, false);
                return bakedMesh;
            });
        }

        return CalculateIslands(bakedMesh, subMeshIndex, uvChannel, renderer.transform.position, renderer.transform.rotation);
    }
    
    public Island[] CalculateIslands(Mesh bakedMesh, int subMeshIndex, int uvChannel, Vector3 basePosition, Quaternion baseRotation)
    {
        var key = (bakedMesh, subMeshIndex, uvChannel, basePosition, baseRotation);
        if (_cachedIslands.TryGetValue(key, out var cachedIslands))
        {
            return cachedIslands;
        }

        using var profiler = new ProfilerScope("CalculateIslands");

        Profiler.BeginSample("Init");
        var indies = bakedMesh.GetIndices(subMeshIndex);
        var vertCount = indies.Length;
        
        var vertexList = new List<Vector3>();
        bakedMesh.GetVertices(vertexList);
        var vertices = vertexList.ToArray();
        
        var uvList = new List<Vector2>(vertCount);
        bakedMesh.GetUVs(uvChannel, uvList);
        var uvs = uvList.ToArray();
        Profiler.EndSample();

        Profiler.BeginSample("InitUnionFind");
        var unionFind = new UnionFind(vertCount);
        Profiler.EndSample();

        Profiler.BeginSample("MergeSamePos");
        var uvMap = new Dictionary<Vector2, int>(vertCount);
        foreach (var i in indies)
        {
            if (uvMap.TryGetValue(uvs[i], out int existingIndex))
            {
                unionFind.Unite(existingIndex, i);
            }
            else
            {
                uvMap[uvs[i]] = i;
            }
        }
        Profiler.EndSample();

        Profiler.BeginSample("Merge");
        var triangles = bakedMesh.GetTriangles(subMeshIndex);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            unionFind.Unite(triangles[i], triangles[i + 1]);
            unionFind.Unite(triangles[i + 1], triangles[i + 2]);
        }
        Profiler.EndSample();

        Profiler.BeginSample("islandIndices");
        var islandIndices = new Dictionary<int, List<int>>(vertCount);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int root = unionFind.Find(triangles[i]);
            islandIndices.GetOrAdd(root, ListPool<int>.Get()).Add(i);
        }
        Profiler.EndSample();
        Profiler.BeginSample("worldVerts");
        var worldVerts = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; ++i)
        {
            worldVerts[i] = basePosition + baseRotation * vertices[i];
        }
        Profiler.EndSample();
        Profiler.BeginSample("result");
        var result = new Island[islandIndices.Count];
        int j = 0;
        foreach (var (_, indices) in islandIndices)
        {
            result[j] = new Island(worldVerts, uvs, triangles, indices.ToArray());
            ListPool<int>.Release(indices);
            j++;
        }
        Profiler.EndSample();

        Profiler.EndSample();
        return result;
    }

    public void Dispose()
    {
        foreach (var mesh in _bakedMeshes.Values)
        {
            if (mesh != null)
            {
                Object.DestroyImmediate(mesh);
            }
        }
        _bakedMeshes.Clear();
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