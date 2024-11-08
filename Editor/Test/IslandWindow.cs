using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
// ... 既存のコード ...

namespace com.aoyon.AutoConfigureTexture
{
public class IslandWindow : EditorWindow
{
    private Renderer renderer;
    private int subMeshIndex;
    private List<Island> islands;

    [MenuItem("Tools/AutoConfigureTexture/Island Viewer")]
    public static void ShowWindow()
    {
        GetWindow<IslandWindow>("Island Viewer");
    }

    private void OnGUI()
    {
        renderer = (Renderer)EditorGUILayout.ObjectField("Renderer", renderer, typeof(Renderer), true);
        subMeshIndex = EditorGUILayout.IntField("SubMesh Index", subMeshIndex);

           if (GUILayout.Button("Process Islands"))
        {
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Mesh mesh = null;
                if (renderer is MeshRenderer)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        mesh = meshFilter.sharedMesh;
                    }
                }
                else if (renderer is SkinnedMeshRenderer)
                {
                    var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                    mesh = skinnedMeshRenderer.sharedMesh;
                }

                if (mesh != null)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var islandHandler = new IslandHandler();
                    islands = islandHandler.GetIslands(mesh, subMeshIndex, 0);
                    stopwatch.Stop();
                    UnityEngine.Debug.Log($"Processing time: {stopwatch.ElapsedMilliseconds} ms");

                    var mainTexture = renderer.sharedMaterial.mainTexture as Texture2D;
                    if (mainTexture != null)
                    {
                        var maskedTexture = GenerateMaskedTexture(mainTexture, islands);

                        // テクスチャを保存する処理
                        var path = EditorUtility.SaveFilePanel("Save Masked Texture", "", "MaskedTexture.png", "png");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var bytes = maskedTexture.EncodeToPNG();
                            System.IO.File.WriteAllBytes(path, bytes);
                            UnityEngine.Debug.Log($"テクスチャが保存されました: {path}");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Main texture is not a Texture2D.");
                    }

                }
                else
                {
                    UnityEngine.Debug.LogError("No valid mesh found on the renderer.");
                }
            }
            else
            {
                UnityEngine.Debug.LogError("Renderer or its material is null.");
            }
        }

    }

    private Texture2D GenerateMaskedTexture(Texture2D mainTexture, List<Island> islands)
    {
        var texture = new Texture2D(mainTexture.width, mainTexture.height);
        mainTexture = Utils.EnsureReadableTexture2D(mainTexture);
        var pixels = mainTexture.GetPixels();

        // IslandのUV範囲を事前に計算しておく
        var islandBounds = islands.Select(island => new Rect(island.MinUV.x, island.MinUV.y, island.MaxUV.x - island.MinUV.x, island.MaxUV.y - island.MinUV.y)).ToList();

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                var uv = new Vector2((float)x / texture.width, (float)y / texture.height);
                bool isInsideAnyIsland = false;

                // 事前計算したUV範囲を使用して判定
                foreach (var bounds in islandBounds)
                {
                    if (bounds.Contains(uv))
                    {
                        isInsideAnyIsland = true;
                        break;
                    }
                }
            
                if (isInsideAnyIsland)
                {
                    texture.SetPixel(x, y, Color.black);
                }
                else
                {
                    texture.SetPixel(x, y, pixels[y * texture.width + x]);
                }
            }
        }
        texture.Apply();
        return texture;
    }
}
}
