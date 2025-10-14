namespace com.aoyon.AutoConfigureTexture.Processor;

public class Island
{
    public readonly Vector3[] Vertices;
    public readonly Vector2[] UVs;
    public readonly int[] Triangles;

    public readonly List<int> TriangleIndices = new();
    
    public Island(Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        Vertices = vertices;
        UVs = uvs;
        Triangles = triangles;
    }

    public void AddTriangleIndex(int triangleIndex)
    {
        TriangleIndices.Add(triangleIndex);
    }
}

internal class IslandCalculator
{
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
            islandIndices.GetOrAddNew(root).Add(i);
        }
        var result = new Island[islandIndices.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Island(vertices, uvs, islandIndices[i].ToArray());
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