using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace com.aoyon.AutoConfigureTexture.Analyzer;

internal class AlphaAnalyzer
{
    private readonly Dictionary<Texture2D, bool> _hasAlphaCache = new();

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

    public bool HasAlpha(TextureInfo textureInfo)
    {
        if (_hasAlphaCache.TryGetValue(textureInfo.Texture2D, out var hasAlpha))
        {
            return hasAlpha;
        }

        hasAlpha = HasAlphaImpl(textureInfo);
        _hasAlphaCache.Add(textureInfo.Texture2D, hasAlpha);
        return hasAlpha;
    }

    private bool HasAlphaImpl(TextureInfo textureInfo)
    {
        if (GraphicsFormatUtility.HasAlphaChannel(textureInfo.Format))
        {
            try
            {
                return HasAlphaWithBinarization(textureInfo.Texture2D);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to check alpha channel: {e.Message}");
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    private static bool HasAlphaWithBinarization(Texture texture)
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

    private static bool HasAlphaInMipMap(Texture2D readableTexture)
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
}