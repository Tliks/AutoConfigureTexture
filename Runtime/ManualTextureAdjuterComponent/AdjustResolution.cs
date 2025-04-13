using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Adjust Resolution")]
    public class AdjustResolution : ManualTextureAdjuterComponent
    {
        public int TextureSize = 2048;
    }
}