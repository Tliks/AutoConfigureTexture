using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Auto Adjust Texture Format")]
    public class AutoAdjustTextureFormat : AutoTextureAdjuterComponent
    {
        public FormatMode FormatMode = FormatMode.Balanced;
    }

    public enum FormatMode
    {
        LowDownloadSize,
        Balanced,
        HighQuality
    }
}