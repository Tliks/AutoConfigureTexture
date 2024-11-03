using UnityEngine;
using VRC.SDKBase;

namespace com.aoyon.AutoConfigureTexture
{
    public enum Reduction
    {
        None,
        Low,
        Normal,
        High,
        Ultra
    }

    public enum FormatMode
    {
        LowDownloadSize,
        Balanced,
        HighQuality
    }

    [AddComponentMenu("Auto Configure Texture/Auto Configure Texture")]
    [DisallowMultipleComponent]
    public class AutoConfigureTexture: MonoBehaviour, IEditorOnly
    {
        public bool OptimizeTextureFormat = true;
        public FormatMode FormatMode = FormatMode.Balanced;
        public bool OptimizeMipMap = true;
        public bool RunShaderOptimization = true;
        public Reduction ResolutionReduction = Reduction.Normal;
    }
}