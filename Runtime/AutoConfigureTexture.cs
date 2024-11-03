using UnityEngine;
using VRC.SDKBase;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Auto Configure Texture")]
    [DisallowMultipleComponent]
    public class AutoConfigureTexture: MonoBehaviour, IEditorOnly
    {
        public bool OptimizeTextureFormat = true;
        public bool OptimizeMipMap = true;
        public bool RunShaderOptimization = true;
        public bool ResizeTexture = true;
    }
}