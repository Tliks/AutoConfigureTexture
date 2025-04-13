using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Auto Adjust Resolution")]
    public class AutoAdjustResolution : AutoTextureAdjuterComponent
    {
        public Reduction ResolutionReduction = Reduction.Normal;
        public bool UsePosition = true;
        public bool UseGradient = true;
    }

    public enum Reduction
    {
        None,
        Low,
        Normal,
        High,
        Ultra
    }
}