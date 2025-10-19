namespace com.aoyon.AutoConfigureTexture;

internal class ActiveRenderTextureScope : IDisposable
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

internal class ProfilerScope : IDisposable
{
    public ProfilerScope(string name)
    {
        Profiler.BeginSample(name);
    }
    public void Dispose()
    {
        Profiler.EndSample();
    }
}

internal class StopwatchScope : IDisposable
{
    private readonly string _name;
    private readonly System.Diagnostics.Stopwatch _stopwatch;
    public StopwatchScope(string name)
    {
        _name = name;
        _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    }
    public void Dispose()
    {
        _stopwatch.Stop();
        Debug.Log($"[ACT] {_name} {_stopwatch.ElapsedMilliseconds}ms");
    }
}