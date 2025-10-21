namespace com.aoyon.AutoConfigureTexture;

internal static partial class TextureUtility
{
    public static Texture2D EnsureReadableTexture2D(Texture2D texture2d, bool isSRGB)
    {
        if (texture2d.isReadable)
        {
            return texture2d;
        }

        return GetReadableTexture2D(texture2d, isSRGB);
    }

    public static Texture2D GetReadableTexture2D(Texture2D texture2d, bool isSRGB)
    {
        using var scope = new Utils.ProfilerScope("GetReadableTexture2D");

        var colorSpace = isSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear;
        var hasMipMaps = texture2d.mipmapCount > 1;
        
        RenderTexture renderTexture = RenderTexture.GetTemporary(
                    texture2d.width,
                    texture2d.height,
                    0,
                    RenderTextureFormat.Default,
                    colorSpace);

        Graphics.Blit(texture2d, renderTexture);
        Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height, TextureFormat.RGBA32, hasMipMaps);
        using (new ActiveRenderTextureScope(renderTexture))
        {
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        }
        readableTextur2D.Apply(hasMipMaps);
        RenderTexture.ReleaseTemporary(renderTexture);
        return readableTextur2D;
    }

    public static Texture2D CopyTexture2D(Texture2D texture2d, bool isSRGB)
    {
        if (texture2d.isReadable)
        {
            return Object.Instantiate(texture2d);
        }
        else
        {
            return GetReadableTexture2D(texture2d, isSRGB);
        }
    }

    public static Texture2D GetMipMapTexture2D(Texture2D texture2d, int mipLevel, bool isSRGB)
    {
        if (mipLevel < 0 || mipLevel >= texture2d.mipmapCount)
        {
            throw new ArgumentException("mipLevel is out of range");
        }
            
        var mipWidth = Mathf.Max(1, texture2d.width >> mipLevel);
        var mipHeight = Mathf.Max(1, texture2d.height >> mipLevel);
        
        var readableTexture = EnsureReadableTexture2D(texture2d, isSRGB);
        var pixels = readableTexture.GetPixels(mipLevel);
        
        var mipTexture = new Texture2D(mipWidth, mipHeight, TextureFormat.RGBA32, false);
        mipTexture.SetPixels(pixels);
        mipTexture.Apply();
        return mipTexture;
    }

    public class ActiveRenderTextureScope : IDisposable
    {
        private readonly RenderTexture _previous;
        public ActiveRenderTextureScope(RenderTexture renderTexture)
        {
            _previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
        }
        public void Dispose()
        {
            RenderTexture.active = _previous;
        }
    }

    public class DisposableRendererTexture : Utils.IDisposableWrapper<RenderTexture>
    {
        public DisposableRendererTexture(RenderTexture texture) : base(texture, (t) => t.Release())
        {
        }
    }
}