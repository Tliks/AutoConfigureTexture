namespace com.aoyon.AutoConfigureTexture;

internal static class TextureUtility
{
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
        Texture2D readableTextur2D = new Texture2D(texture2d.width, texture2d.height);
        using (new ActiveRenderTextureScope(renderTexture))
        {
            readableTextur2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        }
        readableTextur2D.Apply();
        RenderTexture.ReleaseTemporary(renderTexture);
        Profiler.EndSample();
        return readableTextur2D;
    }

    public static Texture2D CopyTexture2D(Texture2D texture2d)
    {
        if (texture2d.isReadable)
        {
            return Object.Instantiate(texture2d);
        }
        else
        {
            return GetReadableTexture2D(texture2d);
        }
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

}