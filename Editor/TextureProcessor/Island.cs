using UnityEngine.Pool;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal class Island
{
    public readonly Vector3[] Vertices;
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

internal class IslandCalculator
{
	public static (Island[], IslandArgument[]) CalculateIslandsFor(TextureInfo info)
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
			var islandsPerArg = IslandCalculator.CalculateIslands(arg.Mesh, arg.SubMeshIndex, arg.UVchannel);
			allArgs.AddRange(Enumerable.Repeat(arg, islandsPerArg.Length)); // 同じArgにより生成されたIslands
			allIslands.AddRange(islandsPerArg);
		}
		return (allIslands.ToArray(), allArgs.ToArray());
	}

	public record IslandArgument(PropertyInfo PropertyInfo, int UVchannel, MaterialInfo MaterialInfo, Renderer Renderer, Mesh Mesh, int SubMeshIndex);
    
    public static Island[] CalculateIslands(Mesh mesh, int subMeshIndex, int uvChannel)
    {
        Profiler.BeginSample("GetIslands");

        Profiler.BeginSample("Init");
        var indies = mesh.GetIndices(subMeshIndex);
        var vertCount = indies.Length;
        
        var vertexList = new List<Vector3>();
        mesh.GetVertices(vertexList);
        var vertices = vertexList.ToArray();
        
        var uvList = new List<Vector2>(vertCount);
        mesh.GetUVs(uvChannel, uvList);
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
        var triangles = mesh.GetTriangles(subMeshIndex);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            unionFind.Unite(triangles[i], triangles[i + 1]);
            unionFind.Unite(triangles[i + 1], triangles[i + 2]);
        }
        Profiler.EndSample();

        Profiler.BeginSample("Result");
        var islandIndices = new Dictionary<int, List<int>>(vertCount);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int root = unionFind.Find(triangles[i]);
            islandIndices.GetOrAdd(root, ListPool<int>.Get()).Add(i);
        }
        var result = new Island[islandIndices.Count];
        int j = 0;
        foreach (var (_, indices) in islandIndices)
        {
            result[j] = new Island(vertices, uvs, triangles, indices.ToArray());
            ListPool<int>.Release(indices);
            j++;
        }
        Profiler.EndSample();

        Profiler.EndSample();
        return result;
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