using System;
using UnityEngine;
using VRC.Core;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Auto Remove MipMap")]
    public class AutoRemoveMipMap : AutoTextureAdjuterComponent
    {
        public override ManualTextureAdjuterComponent AddManualComponent(GameObject target)
        {
            return target.AddComponent<RemoveMipMap>();
        }
    }

}
