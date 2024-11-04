using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace com.aoyon.AutoConfigureTexture
{
    public class MaterialArea
    {
        private readonly Vector3 _worldViewPos;
        private readonly IEnumerable<Renderer> _renderers;
        private Dictionary<Renderer, Mesh> _meshes = new Dictionary<Renderer, Mesh>();

        public MaterialArea(Transform root)
        {
            var descriptor = root.GetComponentInParent<VRCAvatarDescriptor>();
            if (descriptor == null) throw new InvalidOperationException();

            var rootPos = descriptor.transform.position;
            var viewPos = descriptor.ViewPosition;

            _worldViewPos = rootPos + viewPos;

            _renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => r is SkinnedMeshRenderer or MeshRenderer);
        }

        public bool IsUnderHeight(IEnumerable<Material> materials, float thresholdRatio)
        {
            return materials.All(m => IsUnderHeight(m, thresholdRatio));
        }

        public bool IsUnderHeight(Material material, float thresholdRatio)
        {
            var height = _worldViewPos.y * thresholdRatio;

            foreach (var renderer in _renderers)
            {
                var subMeshIndex = GetSubmeshIndex(renderer, material);
                if (subMeshIndex == -1) continue;

                var mesh = GetMesh(renderer, _meshes);
                if (mesh == null) continue;

                if (!IsMeshUnderHeight(renderer.transform, mesh, subMeshIndex, height)) 
                    return false;
            }
            return true;
        }

        private static Mesh GetMesh(Renderer renderer, Dictionary<Renderer, Mesh> meshes)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                if (!meshes.TryGetValue(skinnedMeshRenderer, out var bakedMesh))
                {
                    if (skinnedMeshRenderer.sharedMesh == null) return null;
                    bakedMesh = new Mesh();
                    skinnedMeshRenderer.BakeMesh(bakedMesh, true);
                    meshes.Add(skinnedMeshRenderer, bakedMesh);
                }
                return bakedMesh;
            }
            else if (renderer is MeshRenderer meshRenderer)
            {
                if (!meshes.TryGetValue(meshRenderer, out var mesh))
                {
                    mesh = meshRenderer.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) return null;
                    meshes.Add(meshRenderer, mesh);
                }
                return mesh;
            }
            else{
                throw new InvalidOperationException();
            }
        }

        private static bool IsMeshUnderHeight(Transform transform, Mesh mesh, int subMeshIndex, float height)
        {
            var vertices = mesh.vertices;

            int[] indices = mesh.GetIndices(subMeshIndex);

            foreach (var index in indices)
            {
                var vertex = vertices[index];
                var worldPos = transform.TransformPoint(vertex);

                if (worldPos.y >= height)
                {
                    return false; // 一つでも条件を満たさない頂点があればfalseを返す
                }
            }

            return true;
        }

        private static int GetSubmeshIndex(Renderer renderer, Material material)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == material)
                {
                    return i;
                }
            }
            return -1; 
        }
    }
}