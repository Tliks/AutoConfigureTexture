using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureGradientProcessor : IDisposable
    {
        private static ComputeShader _gradientShader;
        private static ComputeShader _histogramShader;

        private const string GradientShaderGUID = "6e8cd9e410ac31740b1706b2844868b5";
        private const string HistogramShaderGUID = "d0d300d7d332a544c87b96f9c64a78b4";

        public RenderTexture MaskTexture => _maskTexture;
        public RenderTexture GradientTexture => _gradientTexture;
        private RenderTexture _maskTexture;
        private RenderTexture _gradientTexture;
        

        [InitializeOnLoadMethod]
        static void Init()
        {
            _gradientShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(GradientShaderGUID));
            _histogramShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath(HistogramShaderGUID));
        }

        public AsyncGPUReadbackRequest CalculateIntensityAsync(Texture2D texture, Mesh mesh)
        {
            var width = texture.width;
            var height = texture.height;
            _maskTexture = GetUVMask(mesh, width, height);
            _gradientTexture = CalculateGradient(texture, _maskTexture);
            return CalculateHistogram(_gradientTexture);
        }

        public void Dispose()
        {
            if (_maskTexture != null)
            {
                _maskTexture.Release();
                _maskTexture = null;
            }
            if (_gradientTexture != null)
            {
                _gradientTexture.Release();
                _gradientTexture = null;
            }
        }


        private RenderTexture GetUVMask(Mesh mesh, int width, int height)
        {
            var uvMesh = new Mesh
            {
                vertices = Array.ConvertAll(mesh.uv, uv => new Vector3(uv.x, uv.y, 0)),
                triangles = mesh.triangles
            };
            return GetMask(uvMesh, width, height);
        }

        // xy平面
        private RenderTexture GetMask(Mesh mesh, int width, int height)
        {
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.R8, 0);
            var maskedTexture = new RenderTexture(descriptor);
            maskedTexture.enableRandomWrite = true;
            maskedTexture.Create();

            var material = new Material(Shader.Find("Unlit/Texture"));
            using (var commandBuffer = new CommandBuffer())
            {
                commandBuffer.SetRenderTarget(maskedTexture);

                var lookMatrix = Matrix4x4.LookAt(Vector3.forward * -10f, Vector3.zero, Vector3.up);
                var orthoMatrix = Matrix4x4.Ortho(0, 1, 0, 1, 0.01f, 20f);
                commandBuffer.SetViewProjectionMatrices(lookMatrix, orthoMatrix);

                commandBuffer.DrawMesh(mesh, Matrix4x4.identity, material, 0);

                Graphics.ExecuteCommandBuffer(commandBuffer);

                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            //WaitForCompletion(maskedTexture);
            return maskedTexture;
        }

        private RenderTexture CalculateGradient(Texture2D texture, RenderTexture maskedTexture)
        {
            var width = texture.width;
            var height = texture.height;

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.RGFloat, 0);
            RenderTexture gradientTexture = new RenderTexture(descriptor);
            gradientTexture.enableRandomWrite = true;
            gradientTexture.Create();

            int kernelHandle = _gradientShader.FindKernel("CSMain");
            _gradientShader.SetTexture(kernelHandle, "InputTexture", texture);
            _gradientShader.SetTexture(kernelHandle, "MaskTexture", maskedTexture);
            _gradientShader.SetTexture(kernelHandle, "Result", gradientTexture);
            _gradientShader.SetInt("Width", width);
            _gradientShader.SetInt("Height", height);

            int threadGroupsX = Mathf.CeilToInt((float)width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt((float)height / 8.0f);
            _gradientShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);

            //WaitForCompletion(gradientTexture);
            return gradientTexture;
        }

        //
        private AsyncGPUReadbackRequest CalculateHistogram(RenderTexture inputTexture)
        {
            var histogramBins = 100;
            var histogramBuffer = new ComputeBuffer(histogramBins, sizeof(int));
            var initialData = new int[histogramBins];
            histogramBuffer.SetData(initialData);

            _histogramShader.SetTexture(0, "_InputTexture", inputTexture);
            _histogramShader.SetBuffer(0, "_HistogramBuffer", histogramBuffer);
            _histogramShader.SetInt("_HistogramBins", histogramBins);

            int threadGroupsX = Mathf.CeilToInt((float)inputTexture.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt((float)inputTexture.height / 8.0f);
            _histogramShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            var request = AsyncGPUReadback.Request(histogramBuffer);
            //histogramBuffer.Release();

            return request;
        }
 
        internal float GetResult(AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                throw new Exception("GPU readback error");
            }

            request.WaitForCompletion();
            var histogramData = request.GetData<int>().ToArray();
            var histogramBins = histogramData.Length;
            var histogram = new Dictionary<float, int>();
            for (int i = 0; i < histogramBins; i++)
            {
                float key = (float)(i + 1) / histogramBins;
                histogram[key] = histogramData[i];
            }
            return CalculateIntensity(histogram);
        }

        // threshold: 上位0.5%は勘
        private float CalculateIntensity(Dictionary<float, int> histogramData, float threshold = 0.005f)
        {
            var targetkey = 0f;

            if (histogramData == null || histogramData.Count == 0) return 0f;

            int totalCount = 0;
            foreach (var value in histogramData.Values)
            {
                totalCount += value;
            }
            if (totalCount == 0) return 0f;

            int targetCount = (int)(totalCount * threshold);
            float sum = 0;
            int count = 0;

            var sortedHistogram = histogramData.OrderByDescending(pair => pair.Key);
            foreach (var pair in sortedHistogram)
            {
                float key = pair.Key;
                int value = pair.Value;
                sum += key * value;
                count += value;
                if (count >= targetCount)
                {
                    targetkey = key;
                    break;
                }
            }

            return sum / count;
        }

        public RenderTexture DebugHistogram(Dictionary<float, int> histogram, float targetkey)
        {
            if (histogram == null || histogram.Count == 0)
            {
                Debug.LogError("Histogram data is null or empty.");
                return null;
            }

            int dataLength = histogram.Count;
            float width = 1f / dataLength;

            float max1 = 0;
            foreach (var value in histogram.Values)
            {
                max1 = Mathf.Max(max1, value);
            }
            if (max1 == 0) max1 = 1;

            float max2 = 0;
            foreach (var pair in histogram)
            {
                if (pair.Key >= targetkey)
                {
                    max2 = Mathf.Max(max2, pair.Value);
                }
            }
            if (max2 == 0) max2 = 1;

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[dataLength * 8];
            int[] triangles = new int[dataLength * 12];

            int i = 0;
            foreach (var pair in histogram.OrderBy(p => p.Key))
            {
                float x = i * width;

                // 上半分
                float y1 = (float)pair.Value / max1 * 0.5f;
                int vBaseTop = i * 4;
                int tBaseTop = i * 6;
                float yOffsetTop = 0.5f;

                vertices[vBaseTop + 0] = new Vector3(x, yOffsetTop, 0);
                vertices[vBaseTop + 1] = new Vector3(x + width, yOffsetTop, 0);
                vertices[vBaseTop + 2] = new Vector3(x + width, yOffsetTop + y1, 0);
                vertices[vBaseTop + 3] = new Vector3(x, yOffsetTop + y1, 0);

                triangles[tBaseTop + 0] = vBaseTop + 0;
                triangles[tBaseTop + 1] = vBaseTop + 2;
                triangles[tBaseTop + 2] = vBaseTop + 1;
                triangles[tBaseTop + 3] = vBaseTop + 0;
                triangles[tBaseTop + 4] = vBaseTop + 3;
                triangles[tBaseTop + 5] = vBaseTop + 2;

                // 下半分
                float y2 = (pair.Key < targetkey) ? 0 : (float)pair.Value / max2 * 0.5f;
                int vBaseBottom = dataLength * 4 + i * 4;
                int tBaseBottom = dataLength * 6 + i * 6;
                float yOffsetBottom = 0f;

                vertices[vBaseBottom + 0] = new Vector3(x, yOffsetBottom, 0);
                vertices[vBaseBottom + 1] = new Vector3(x + width, yOffsetBottom, 0);
                vertices[vBaseBottom + 2] = new Vector3(x + width, yOffsetBottom + y2, 0);
                vertices[vBaseBottom + 3] = new Vector3(x, yOffsetBottom + y2, 0);

                triangles[tBaseBottom + 0] = vBaseBottom + 0;
                triangles[tBaseBottom + 1] = vBaseBottom + 2;
                triangles[tBaseBottom + 2] = vBaseBottom + 1;
                triangles[tBaseBottom + 3] = vBaseBottom + 0;
                triangles[tBaseBottom + 4] = vBaseBottom + 3;
                triangles[tBaseBottom + 5] = vBaseBottom + 2;

                i++;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return GetMask(mesh, 512, 512);
        }

        private void WaitForCompletion(Texture src)
        {
            var request = AsyncGPUReadback.Request(src, 0, (request) =>
            {
            });
            request.WaitForCompletion();
        }
    }

    public class TextureGradientCalculator
    {
        public static float[] CalculateGradientIntensityAsync(TextureInfo[] infos)
        {
            var targets = infos
                .Select(info =>
                {
                    var meshes = info.Properties
                        .Select(p => p.MaterialInfo)
                        .SelectMany(mi => Enumerable.Range(0, mi.Renderers.Count)
                            .Select(i => (Utils.GetMesh(mi.Renderers[i]), mi.MaterialIndices[i])))
                        .ToHashSet();
                    var mesh = Utils.MergeMesh(meshes);
                    return (info.Texture as Texture2D, mesh);
                }).ToArray();

            return CalculateGradientIntensityAsync(targets);
        }
        public static float[] CalculateGradientIntensityAsync((Texture2D, Mesh)[] targets)
        {
            var processors = new TextureGradientProcessor[targets.Length];
            var requests = new AsyncGPUReadbackRequest[targets.Length];
            var results = new float[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                processors[i] = new TextureGradientProcessor();
                requests[i] = processors[i].CalculateIntensityAsync(targets[i].Item1, targets[i].Item2);
            }

            for (int i = 0; i < targets.Length; i++)
            {
                var processor = processors[i];
                results[i] = processor.GetResult(requests[i]);
                processor.Dispose();
            }

            return results;
        }
    }
}
