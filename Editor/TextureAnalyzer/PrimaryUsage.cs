using com.aoyon.AutoConfigureTexture.ShaderInformations;

namespace com.aoyon.AutoConfigureTexture.Analyzer;

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
            .Select(info => ShaderInformation.GetTextureUsage(info.Shader, info.PropertyName))
            .Where(u => u != TextureUsage.Unknown) // 一旦除外する
            .ToList();
        return GetPrimaryUsage(usages);

        // primaryでない使用用途を全て無視しているのでもう少し良い取り扱い方はしたい
        // MainTex > NormalMap > Emission > AOMap > NormalMapSub > Others > MatCap
        static TextureUsage GetPrimaryUsage(List<TextureUsage> usages)
        {
            if (usages.Count == 0) return TextureUsage.Unknown;
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