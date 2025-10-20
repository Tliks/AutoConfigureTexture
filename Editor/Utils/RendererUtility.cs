namespace com.aoyon.AutoConfigureTexture;

internal static class RendererUtility
{
    public static IEnumerable<Material> CollectMaterials(GameObject obj)
    {
        return obj.GetComponentsInChildren<Renderer>(true)
        .SelectMany(renderer => renderer.sharedMaterials)
        .Where(m => m != null)
        .ToHashSet();
    }

    public static void ReplaceMaterials(Dictionary<Material, Material> mapping, IEnumerable<Renderer> renderers)
    {
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                if (mapping.TryGetValue(material, out var proxy))
                {
                    materials[i] = proxy;
                }
            }
            renderer.sharedMaterials = materials;
        }
    }

    public static Mesh? GetMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.sharedMesh;
        }
        else if (renderer is MeshRenderer meshRenderer)
        {
            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter == null) return null;
            return meshFilter.sharedMesh;
        }
        else
        {
            return null;
        }
    }
}