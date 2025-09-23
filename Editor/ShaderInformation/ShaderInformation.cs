namespace com.aoyon.AutoConfigureTexture.ShaderInformations;

internal static class ShaderInformation
{
    private static IShaderInformation[] _shaderSupports = Array.Empty<IShaderInformation>();
    
    [InitializeOnLoadMethod]
    static void Init()
    {
        _shaderSupports = new IShaderInformation[]
        {
            new lilToonInformation(),
        };
    }

    private static IShaderInformation? GetShaderSupport(Shader shader)
    {
        if (shader == null) return null;
        var supports = _shaderSupports.Where(s => s.IsTarget(shader));
        if (supports == null || supports.Count() == 0) return null;
        if (supports.Count() > 1)
        {
            Debug.LogWarning($"ShaderSupport: {shader.name} is supported by multiple shader supports.");
        }
        return supports.First();
    }

    private static IShaderInformation? GetShaderSupport(Material material)
    {
        var shader = material?.shader;
        if (shader == null) return null;
        return GetShaderSupport(shader);
    }

    public static TextureChannel GetTextureChannel(Shader shader, string property)
    {
        var shaderSupport = GetShaderSupport(shader);
        if (shaderSupport == null)
            return TextureChannel.Unknown;
        return shaderSupport.GetTextureChannel(shader, property);
    }

    public static TextureUsage GetTextureUsage(Shader shader, string property)
    {
        // _MainTexはUnityの予約語らしいので辞書を使わず返す
        if (property == "_MainTex") 
            return TextureUsage.MainTex; 
        var shaderSupport = GetShaderSupport(shader);
        if (shaderSupport == null)
            return TextureUsage.Unknown;
        return shaderSupport.GetTextureUsage(shader, property);
    }

    public static bool? IsVertexShader(Shader shader, string property) 
    {
        var shaderSupport = GetShaderSupport(shader);
        if (shaderSupport == null)
            return null;
        return shaderSupport.IsVertexShader(shader, property);
    }
}
