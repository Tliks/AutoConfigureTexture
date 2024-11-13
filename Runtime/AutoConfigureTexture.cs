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

    [AddComponentMenu("Auto Configure Texture/Auto Configure Texture")]
    [DisallowMultipleComponent]
    public class AutoConfigureTexture: MonoBehaviour, IEditorOnly
    {
        public bool OptimizeTextureFormat = true;
        public bool OptimizeMipMap = true;
        public Reduction ResolutionReduction = Reduction.Normal;
        public bool ResizeIslands = true;
    }
}