namespace com.aoyon.AutoConfigureTexture;


internal static class Utils
{
    public class ProfilerScope : IDisposable
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

    public class StopwatchScope : IDisposable
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