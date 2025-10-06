using VRC.SDK3.Avatars.Components;

namespace com.aoyon.AutoConfigureTexture.Processor
{
    internal class DrawingCoordinatesAnalyzer
    {
        private readonly Vector3 _worldViewPos;
        private readonly Dictionary<Renderer, Mesh> _meshes = new Dictionary<Renderer, Mesh>();

        public DrawingCoordinatesAnalyzer(Transform avatarRoot)
        {
            var descriptor = avatarRoot.GetComponent<VRCAvatarDescriptor>(); // Todo: VRC非依存でアバターの高さを取得する
            if (descriptor == null) throw new InvalidOperationException();
            var rootPos = descriptor.transform.position;
            var viewPos = descriptor.ViewPosition;
            _worldViewPos = rootPos + viewPos;
        }

        public bool IsAllDrawingCoordinatesUnderHeight(TextureInfo textureInfo, float thresholdRatio)
        {
            var height = _worldViewPos.y * thresholdRatio;

            var renderers = textureInfo.Properties
                .SelectMany(p => p.MaterialInfo.Renderers);

            foreach ((var renderer, var indices) in renderers)
            {
                var mesh = GetBakedMesh(renderer, _meshes);
                if (mesh == null) continue;

                foreach (var index in indices)
                {
                    if (!IsMeshUnderHeight(renderer.transform, mesh, index, height)) 
                        return false;
                }
            }
            return true;
        }

        private Mesh? GetBakedMesh(Renderer renderer, Dictionary<Renderer, Mesh> meshes)
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

        private bool IsMeshUnderHeight(Transform rendererTransform, Mesh mesh, int subMeshIndex, float height)
        {
            var vertices = mesh.vertices;

            int[] indices = mesh.GetIndices(subMeshIndex);

            foreach (var index in indices)
            {
                var vertex = vertices[index];
                var worldPos = rendererTransform.TransformPoint(vertex);

                if (worldPos.y >= height)
                {
                    return false; // 一つでも条件を満たさない頂点があればfalseを返す
                }
            }

            return true;
        }

    }
}