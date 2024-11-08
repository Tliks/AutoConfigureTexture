using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureWrite
    {
        public static void AnalyzeEdgeStrength(Texture2D texture, List<Island> islands) 
        {
            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
            
            int[,] sobelX = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] sobelY = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            List<float> edgeStrengths = new List<float>();

            foreach (var island in islands)
            {
                float totalEdgeStrength = 0.0f;
                for (float u = island.MinUV.x; u <= island.MaxUV.x; u += 0.01f)
                {
                    for (float v = island.MinUV.y; v <= island.MaxUV.y; v += 0.01f)
                    {
                        int x = (int)(u * width);
                        int y = (int)(v * height);

                        if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
                            continue;

                        float gx = 0, gy = 0;
                        for (int ky = -1; ky <= 1; ky++) 
                        {
                            for (int kx = -1; kx <= 1; kx++) 
                            {
                                Color pixel = pixels[(y + ky) * width + (x + kx)];
                                float intensity = pixel.grayscale;
                                gx += intensity * sobelX[ky + 1, kx + 1];
                                gy += intensity * sobelY[ky + 1, kx + 1];
                            }
                        }
                        totalEdgeStrength += Mathf.Sqrt(gx * gx + gy * gy);
                    }
                }

                float normalizedEdgeStrength = totalEdgeStrength / ((island.MaxUV.x - island.MinUV.x) * width * (island.MaxUV.y - island.MinUV.y) * height);
                edgeStrengths.Add(normalizedEdgeStrength * 1000);
            }

            if (edgeStrengths.Count > 0)
            {
                float min = edgeStrengths.Min();
                float max = edgeStrengths.Max();
                float average = edgeStrengths.Average();
                float median = edgeStrengths.OrderBy(x => x).ElementAt(edgeStrengths.Count / 2);

                Debug.Log($"最小値: {min}, 平均: {average}, 中央値: {median}, 最大値: {max}");
            }
        }
    }
}