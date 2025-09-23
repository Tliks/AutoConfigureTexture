using com.aoyon.AutoConfigureTexture.Analyzer;
using com.aoyon.AutoConfigureTexture.ShaderInformations;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture.Adjuster
{
    internal class AdjustTextureFormat : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        private AutoConfigureTexture _config = null!;

        public void Init(GameObject root, AutoConfigureTexture config)
        {
            _config = config;
            BuildTarget currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (currentBuildTarget == BuildTarget.Android || currentBuildTarget == BuildTarget.iOS) {
                _shouldProcess = false;
            }
            if (config.OptimizeTextureFormat) {
                _shouldProcess = false;
            }
            _shouldProcess = true;
            return;
        }

        public bool Process(TextureInfo info, TextureAnalyzer analyzer, [NotNullWhen(true)] out AdjustData? data)
        {
            data = null;
            
            var current = info.Format;
            var format = current;

            if (_config.MaintainCrunch &&
                (current == TextureFormat.DXT5Crunched || current == TextureFormat.DXT1Crunched)){
                return false;
            }

            var mode = _config.FormatMode;
            var currentBPP = MathHelper.FormatToBPP(current);

            var channels = info.Properties
                .Select(propertyInfo => ShaderInformation.GetTextureChannel(propertyInfo.Shader, propertyInfo.PropertyName));

            var channel = channels.Any(c => c.HasFlag(TextureChannel.Unknown))
                ? TextureChannel.RGBA // 不明な使用用途が一つでもあった場合はRGBAとして処理
                : channels.Aggregate((a, b) => a | b);

            switch (channel)
            {
                case TextureChannel.RGBA:
                    // ノーマルマップ(4チャンネル)にBC5が入っている場合への対症療法
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else if (analyzer.HasAlpha(info))
                    {
                        if (mode == FormatMode.HighQuality){
                            format = TextureFormat.BC7;
                        }
                        else if (mode == FormatMode.Balanced){
                            format = analyzer.PrimaryUsage(info) == TextureUsage.MainTex 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT5;
                        }
                        else if (mode == FormatMode.LowDownloadSize){
                            format = TextureFormat.DXT5;
                        }
                    }
                    else
                    {
                        goto case TextureChannel.RGB;
                    }
                    break;
                case TextureChannel.RGB:
                case TextureChannel.RGA:
                case TextureChannel.RBA:
                case TextureChannel.GBA:
                    if (current == TextureFormat.BC7)
                    {
                        format = current;
                    }
                    else
                    {
                        if (mode == FormatMode.HighQuality){
                            format = currentBPP >= 8d 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT1;
                            //Debug.LogWarning($"Conversion: {tex.name} {current} format with {currentBPP}bpp to {format} format with {MathHelper.FormatToBPP(format)}bpp");
                        }
                        else if (mode == FormatMode.Balanced){
                            /*
                            format = currentBPP >= 8  && info.PrimaryUsage == TextureUsage.MainTex
                                ? TextureFormat.BC7 
                                : TextureFormat.DXT1;
                            */
                            format = TextureFormat.DXT1;
                        }
                        else if (mode == FormatMode.LowDownloadSize){
                            format = TextureFormat.DXT1;
                        }
                    }
                    break;
                case TextureChannel.RG:
                case TextureChannel.RB:
                case TextureChannel.RA:
                case TextureChannel.GB:
                case TextureChannel.GA:
                case TextureChannel.BA:
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else
                    {
                        if (mode == FormatMode.HighQuality){
                            format = currentBPP >= 8d 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT1;
                        }
                        else{
                            format = TextureFormat.DXT1;
                        }
                    }
                    break;
                case TextureChannel.R:
                    // Unityはガンマ空間のテクスチャをBC4で解釈しない
                    // よってリニア空間のテクスチャに限りBC4にする
                    // リニア変換するほどではない
                    if (info.ImportedInfo?.sRGBTexture ?? true)
                    {
                        format = TextureFormat.DXT1;
                    }
                    else
                    {
                        format = TextureFormat.BC4;
                    }
                    break;
                // TextureConfiguratorはBC4でRのみにするしそもそも訳分からない情報が渡ってきてるので安全寄りで現在のフォーマットを使用する
                case TextureChannel.G:
                case TextureChannel.B:
                case TextureChannel.A:
                    format = current;
                    Debug.LogWarning($"Invalid info: {info.Texture2D.name} {current} format with {channel} channel");
                    break;
                default:
                    break;
            }

            var BPP = MathHelper.FormatToBPP(format);
            if (BPP > currentBPP)
            {
                Debug.LogWarning($"Conversion cancelled: {nameof(current)} format with {currentBPP}bpp to {nameof(format)} format with {BPP}bpp");
                format = current;
            }

            data = AdjustData.Create(format);
            return current != format;
        }

        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.CompressionSetting.UseOverride = true;
            configurator.CompressionSetting.OverrideTextureFormat = info.Format;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData data)
        {
            configurator.OverrideCompression = true;
            configurator.CompressionSetting.UseOverride = true;
            configurator.CompressionSetting.OverrideTextureFormat = data.GetData<TextureFormat>();
        }
    }
}
