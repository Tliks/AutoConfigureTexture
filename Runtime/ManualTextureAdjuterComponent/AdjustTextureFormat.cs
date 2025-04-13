using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Adjust Texture Format")]
    public class AdjustTextureFormat : ManualTextureAdjuterComponent
    {
        public TextureFormat TextureFormat = TextureFormat.BC7;
    }
}