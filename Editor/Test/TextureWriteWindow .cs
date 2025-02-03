using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace com.aoyon.AutoConfigureTexture
{
    public class TextureGradientEditorWindow : EditorWindow
    {
    private float intensity = 0;
    private Texture2D sourceTexture;
    private RenderTexture gradientTexture;
    private RenderTexture maskTexture;
    private RenderTexture debug;

    private TextureGradientProcessor processor;
    private GameObject root;
    private long processtim = 0;


    [MenuItem("Tools/AutoConfigureTexture/Debug Gradient")]
    public static void ShowWindow()
    {
        GetWindow<TextureGradientEditorWindow>("Debug Gradient");
    }

    private void OnEnable()
    {

    }

    private async void OnGUI()
    {
        root = (GameObject)EditorGUILayout.ObjectField("Target Root", root, typeof(GameObject), true);
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Target Texture", sourceTexture, typeof(Texture2D), true);

        if (GUILayout.Button("Calculate Gradient"))
        {
            processor?.Dispose();
            gradientTexture = null;
            maskTexture = null;
            debug = null;

            if (root != null && sourceTexture != null)
            {
                var tinfo = TextureInfo.Collect(root)
                    .Where(i => i.Texture == sourceTexture)
                    .First();

                var meshes = tinfo.Properties
                    .Select(p => p.MaterialInfo)
                    .SelectMany(mi => Enumerable.Range(0, mi.Renderers.Count)
                        .Select(i => (Utils.GetMesh(mi.Renderers[i]), mi.MaterialIndices[i])))
                    .ToHashSet();
                var mesh = Utils.MergeMesh(meshes);

                //var _islandHandler = new IslandHandler();
                //var islands = _islandHandler.GetIslands(_targetRenderer.sharedMesh, 0, 0);
                var swa = System.Diagnostics.Stopwatch.StartNew();
                processor = new TextureGradientProcessor();
                var task = processor.CalculateIntensityAsync(sourceTexture, mesh);
                intensity = await task;
                swa.Stop();
                Debug.Log($"all: {swa.ElapsedMilliseconds}ms");
                processtim = swa.ElapsedMilliseconds;
                gradientTexture = processor.GradientTexture;
                maskTexture = processor.MaskTexture;

                //sw.Restart();
                //debug = processor.DebugHistogram(histogram, loopIndex);
                //sw.Stop();
                //Debug.Log($"DebugHistogram: {sw.ElapsedMilliseconds}ms");

            }
            else
            {
                Debug.LogError("Source Texture, Gradient Shader and Target Renderer must be assigned.");
            }
        }


        if (gradientTexture != null)
        {
            EditorGUILayout.LabelField($"Intensity: {intensity}, Elapsed: {processtim}ms");
            var width = 256;
            Rect rect1 = GUILayoutUtility.GetRect(width, width, GUILayout.ExpandWidth(false));
            Rect rect2 = new Rect(rect1.x + width, rect1.y, width, width);
            Rect rect3 = new Rect(rect1.x, rect1.y + width, width, width);
            Rect rect4 = new Rect(rect1.x + width, rect1.y + width, width, width);
            if(sourceTexture != null)
            {
                EditorGUI.DrawPreviewTexture(rect1, sourceTexture);
            }
            if(maskTexture != null)
            {
                EditorGUI.DrawPreviewTexture(rect2, maskTexture);
            }
            if(gradientTexture != null)
            {
                EditorGUI.DrawPreviewTexture(rect3, gradientTexture, null, ScaleMode.StretchToFill, 0, -1, UnityEngine.Rendering.ColorWriteMask.Red);
            }
            if(debug != null)
            {
                EditorGUI.DrawPreviewTexture(rect4, debug);
            }
        }
    }

    }
}