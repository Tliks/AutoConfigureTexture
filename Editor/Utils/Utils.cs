using nadena.dev.ndmf;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace com.aoyon.AutoConfigureTexture
{    
    public class Utils
    {
        private static Material? s_alphaBinarizationMaterial = null;
        private static Material AlphaBinarizationMaterial
        {
            get
            {
                if (s_alphaBinarizationMaterial == null)
                {
                    var alphaBinarization = Shader.Find("Hidden/AutoConfigureTexture/AlphaBinarization");
                    if (alphaBinarization == null)
                    {
                        throw new Exception("Shader not found: Hidden/AutoConfigureTexture/AlphaBinarization");
                    }
                    s_alphaBinarizationMaterial = new Material(alphaBinarization);
                }
                return s_alphaBinarizationMaterial;
            }
        }

        public static bool HasAlphaWithBinarization(Texture texture)
        {
            var temp = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.R8);
            var active = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, temp, AlphaBinarizationMaterial);
                var request = AsyncGPUReadback.Request(temp, 0, TextureFormat.R8);
                request.WaitForCompletion();

                using var data = request.GetData<byte>();
                var span = MemoryMarshal.Cast<byte, ulong>(data.AsReadOnlySpan());
                const ulong AllBitSets = unchecked((ulong)-1); // 0xFF_FF_FF_FF_FF_FF_FF_FF
                for (int i = 0; i < span.Length; i++)
                {
                    var x = span[i];
                    //Debug.LogError($"{i} => {Convert.ToString((long)x, 16).ToUpper().PadLeft(16, '0')}");
                    if (x - AllBitSets != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(temp);
            }
        }
 
        public static bool HasAlphaInData(Texture2D readableTexture)
        {
            var span = readableTexture.GetRawTextureData<Color32>().AsReadOnlySpan();

            bool hasAlpha = false;
            for (int i = 0; span.Length > i; i += 1)
            {
                if (span[i].a != 255 && span[i].a != 254)
                {
                    return true;
                }
            }
            return hasAlpha;
        }

        public static bool HasAlphaInMipMap(Texture2D readableTexture)
        {
            if (readableTexture.mipmapCount > 1)
            {
                var pixels = readableTexture.GetPixels32(readableTexture.mipmapCount - 1);
                for (int i = 0; pixels.Length > i; i += 1)
                {
                    if (pixels[i].a != 255)
                    {
                        return true;
                    }
                }
            }
            throw new Exception();
        }

        public static Texture2D EnsureReadableTexture2D(Texture2D texture2d)
        {
            if (texture2d.isReadable)
            {
                return texture2d;
            }

            return GetReadableTexture2D(texture2d);
        }

        public static Texture2D GetReadableTexture2D(Texture2D texture2d)
        {
            Profiler.BeginSample("GetReadableTexture2D");
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                        texture2d.width,
                        texture2d.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(texture2d, renderTexture);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height);
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTextur2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            Profiler.EndSample();
            return readableTextur2D;
        }
        
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

        public static Material CopyAndRegister(Material original)
        {
            var proxy = new Material(original);
            ObjectRegistry.RegisterReplacedObject(original, proxy);
            return proxy;
        }

        public static Texture2D CopyAndRegister(Texture2D original)
        {
            var proxy = CopyTexture2D(original);
            ObjectRegistry.RegisterReplacedObject(original, proxy);
            return proxy;
        }

        public static Texture2D CopyTexture2D(Texture2D texture2d)
        {
            if (texture2d.isReadable)
            {
                return UnityEngine.Object.Instantiate(texture2d);
            }
            else
            {
                return GetReadableTexture2D(texture2d);
            }
        }

        public static Dictionary<Material, Material> CopyAndRegisterMaterials(IEnumerable<Material> originals)
        {
            var mapping = new Dictionary<Material, Material>();
            foreach (var original in originals)
            {
                var proxy = CopyAndRegister(original);
                mapping[original] = proxy;
            }
            return mapping;
        }

        public static bool IsOpaqueMaterial(Material material)
        {
            string materialTag = "RenderType";
            string result = material.GetTag(materialTag, true, "Nothing");
            if (result == "Nothing")
            {
                Debug.LogError(materialTag + " not found in " + material.shader.name);
            }
            return result == "Opaque";
        }

        public static bool IsOpaqueShader(Shader shader)
        {
            var tagid = new UnityEngine.Rendering.ShaderTagId(name:"RenderType");
            var isOpaque = Enumerable.Range(0, shader.subshaderCount)
                .Select(i => shader.FindSubshaderTagValue(i, tagid))
                .All(tag => tag.name == "Opaque");
            return isOpaque;
        }

        public static void Assert(bool condition)
        {
            if (!condition) throw new InvalidOperationException("assertion failed");
        } 

        public static Texture GetMainTexture(Material material)
        {
            return material.GetTexture("_MainTex");
        }

        public static float CalculateArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Mathf.Abs((p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y)) / 2);
        }

        public static float CalculateArea(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var cross = Vector3.Cross(p2 - p1, p3 - p1);
            return cross.magnitude / 2;
        }

        public static Mesh MergeMesh(IEnumerable<(Mesh mesh, int submeshIndex)> meshes)
        {
            Mesh? combinedMesh = null;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            int vertexOffset = 0;
            foreach (var meshData in meshes)
            {
                var mesh = meshData.mesh;
                var submeshIndex = meshData.submeshIndex;

                if (combinedMesh == null)
                {
                    combinedMesh = UnityEngine.Object.Instantiate(mesh);
                }

                vertices.AddRange(mesh.vertices);
                uvs.AddRange(mesh.uv);

                if(submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                {
                    Debug.LogWarning($"submeshIndex is out of range. submeshIndex:{submeshIndex} mesh.subMeshCount:{mesh.subMeshCount}");
                    continue;
                }
                var meshTriangles = mesh.GetTriangles(submeshIndex);
                
                for(int i = 0; i < meshTriangles.Length; i++)
                {
                    triangles.Add(meshTriangles[i] + vertexOffset);
                }
                vertexOffset += mesh.vertices.Length;
            }

            if (combinedMesh != null)
            {
                combinedMesh.vertices = vertices.ToArray();
                combinedMesh.triangles = triangles.ToArray();
                combinedMesh.uv = uvs.ToArray();
            }
            else
            {
                combinedMesh = new Mesh(); // 空のMeshを返すか、nullを返すか、エラーを投げるか検討が必要
            }

            return combinedMesh;
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
}
