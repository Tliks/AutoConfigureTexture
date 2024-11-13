using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace com.aoyon.AutoConfigureTexture
{    
    public class Utils
    {    
        public static bool HasAlpha(Texture2D texture)
        {
            if (!GraphicsFormatUtility.HasAlphaChannel(texture.format))
                return false;

            var readableTexture = EnsureReadableTexture2D(texture);
            return HasAlphaInData(readableTexture);
        }

        // TextureInfoにキャッシュする方
        // 基本的にこっちを使いたい
        public static bool HasAlpha(TextureInfo info)
        {
            var texture = info.Texture as Texture2D;
            if (!GraphicsFormatUtility.HasAlphaChannel(texture.format))
                return false;

            var readableTexture = info.AssignReadbleTexture2D();
            return HasAlphaInData(readableTexture);
        }

        private static bool HasAlphaInData(Texture2D readableTexture)
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
    }
}
