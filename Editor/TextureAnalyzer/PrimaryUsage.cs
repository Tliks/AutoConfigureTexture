namespace com.aoyon.AutoConfigureTexture;

internal class PrimaryUsageAnalyzer
{
    private static readonly TextureUsage[] s_usages = 
    {
        TextureUsage.MainTex,
        TextureUsage.NormalMap,
        TextureUsage.NormalMapSub,
        TextureUsage.AOMap,
        TextureUsage.MatCap,
        TextureUsage.Emission,
        TextureUsage.Others,
    };
    public TextureUsage Analyze(TextureInfo textureInfo)
    {
        var usages = textureInfo.Properties
            .Select(info => ShaderSupport.GetTextureUsage(info.Shader, info.PropertyName));
            
        // 不明プロパティが1つでも含まれる場合
        if (usages.Any(u => u == TextureUsage.Unknown))
        {
            return TextureUsage.Unknown;
        }
        return GetPrimaryUsage(usages);

        // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
        // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
        static TextureUsage GetPrimaryUsage(IEnumerable<TextureUsage> usages)
        {
            foreach (var usage in s_usages)
            {
                if (usages.Contains(usage))
                {
                    return usage;
                }
            }
            throw new InvalidOperationException();
        }
    }
}