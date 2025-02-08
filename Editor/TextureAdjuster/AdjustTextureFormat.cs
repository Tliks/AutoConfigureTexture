using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{
    internal class AdjustTextureFormat : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        private AutoConfigureTexture _config;

        public void Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config)
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
        public bool Validate(TextureInfo info)
        {
            var currentFormat = info.Format;
            if (_config.MaintainCrunch &&
                (currentFormat == TextureFormat.DXT5Crunched || currentFormat == TextureFormat.DXT1Crunched)){
                return false;
            }
            return true;
        }

        public bool Process(TextureInfo info, out AdjustData<object> data)
        {
            var tex = info.Texture as Texture2D;

            var current = info.Format;
            var format = current;

            var mode = _config.FormatMode;
            var currentBPP = MathHelper.FormatToBPP(current);

            var channels = info.Properties
                .Select(propertyInfo => ShaderSupport.GetChannels(propertyInfo.Shader, propertyInfo.PropertyName));

            int maxChannel = channels.All(c => c != -1)
                ? channels.Max()
                : 4; // 不明な使用用途が一つでもあった場合は4チャンネルとして処理
        
            switch(maxChannel)
            {
                case 4:
                    // ノーマルマップ(4チャンネル)にBC5が入っている場合への対症療法
                    if (current == TextureFormat.BC7 || current == TextureFormat.BC5)
                    {
                        format = current;
                    }
                    else if (Utils.HasAlpha(info))
                    {
                        if (mode == FormatMode.HighQuality){
                            format = TextureFormat.BC7;
                        }
                        else if (mode == FormatMode.Balanced){
                            format = info.PrimaryUsage == TextureUsage.MainTex 
                                ? TextureFormat.BC7
                                : TextureFormat.DXT5;
                        }
                        else if (mode == FormatMode.LowDownloadSize){
                            format = TextureFormat.DXT5;
                        }
                    }
                    else
                    {
                        goto case 3;
                    }
                    break;
                case 3:
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
                case 2:
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
                case 1:
                    format = TextureFormat.BC4;
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

            data = new AdjustData<object>(format);
            return current != format;
        }

        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.CompressionSetting.UseOverride = true;
            configurator.CompressionSetting.OverrideTextureFormat = info.Format;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData<object> data)
        {
            configurator.OverrideCompression = true;
            configurator.CompressionSetting.UseOverride = true;
            configurator.CompressionSetting.OverrideTextureFormat = (TextureFormat)data.Data;
        }
    }
}
