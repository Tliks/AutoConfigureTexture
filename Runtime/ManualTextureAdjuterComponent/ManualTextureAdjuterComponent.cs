using UnityEngine;
using nadena.dev.ndmf;

namespace com.aoyon.AutoConfigureTexture
{
    public abstract class ManualTextureAdjuterComponent : MonoBehaviour, INDMFEditorOnly
    {
        public Texture2D TargetTexture = null;
    }
}
