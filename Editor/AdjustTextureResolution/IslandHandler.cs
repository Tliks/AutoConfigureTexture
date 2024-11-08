using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace com.aoyon.AutoConfigureTexture
{
    public class IslandHandler
    {
        private readonly Dictionary<(Mesh, int, int), List<Island>> cache = new Dictionary<(Mesh, int, int), List<Island>>();

        public List<Island> GetIslands(Mesh mesh, int subMeshIndex, int uvChannel)
        {
            var key = (mesh, subMeshIndex, uvChannel);
            if (cache.TryGetValue(key, out var cachedIslands))
            {
                return cachedIslands;
            }

            var islands = CalculateIslands(mesh, subMeshIndex, uvChannel);
            cache[key] = islands;
            return islands;
        }

        private static List<Island> CalculateIslands(Mesh mesh, int subMeshIndex, int uvChannel)
        {
            Profiler.BeginSample("GetIslands.Init");
            var indies = mesh.GetIndices(subMeshIndex);
            var vertCount = indies.Length;
            
            var vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            var uvs = new List<Vector2>(vertCount);
            mesh.GetUVs(uvChannel, uvs);
            Profiler.EndSample();

            Profiler.BeginSample("GetIslands.InitUnionFind");
            UnionFind unionFind = new UnionFind(vertCount);
            Profiler.EndSample();

            Profiler.BeginSample("GetIslands.MergeSamePos");
            Dictionary<Vector2, int> uvMap = new Dictionary<Vector2, int>(vertCount);
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

            Profiler.BeginSample("GetIslands.Merge");
            var Triangles = mesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < Triangles.Length; i += 3)
            {
                unionFind.Unite(Triangles[i], Triangles[i + 1]);
                unionFind.Unite(Triangles[i + 1], Triangles[i + 2]);
            }
            Profiler.EndSample();

            Profiler.BeginSample("GetIslands.Result");
            Dictionary<int, Island> islandDict = new Dictionary<int, Island>(vertCount);
            foreach (var i in indies)
            {
                int root = unionFind.Find(i);
                if (!islandDict.TryGetValue(root, out var island))
                {
                    island = new Island();
                    islandDict[root] = island;
                }
                island.AddVertex(i, uvs[i], vertices[i]);
            }
            Profiler.EndSample();

            return new List<Island>(islandDict.Values);
        }
    }

    public class Island
    {
        public readonly List<int> VertexIndices = new();
        
        public Vector2 MinUV = new Vector2(float.MaxValue, float.MaxValue);
        public Vector2 MaxUV = new Vector2(float.MinValue, float.MinValue);
        
        public Vector3 MinVertex = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        public Vector3 MaxVertex = new Vector3(float.MinValue, float.MinValue, float.MinValue);
 
        public void AddVertex(int index, Vector2 uv, Vector3 vertex)
        {
            VertexIndices.Add(index);
            
            if (uv.x < MinUV.x) MinUV.x = uv.x;
            if (uv.y < MinUV.y) MinUV.y = uv.y;
            if (uv.x > MaxUV.x) MaxUV.x = uv.x;
            if (uv.y > MaxUV.y) MaxUV.y = uv.y;
            
            if (vertex.x < MinVertex.x) MinVertex.x = vertex.x;
            if (vertex.y < MinVertex.y) MinVertex.y = vertex.y;
            if (vertex.z < MinVertex.z) MinVertex.z = vertex.z;
            if (vertex.x > MaxVertex.x) MaxVertex.x = vertex.x;
            if (vertex.y > MaxVertex.y) MaxVertex.y = vertex.y;
            if (vertex.z > MaxVertex.z) MaxVertex.z = vertex.z;
        }
    }
    
    public class UnionFind
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
