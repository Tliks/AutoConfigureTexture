using System;
using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;

namespace com.aoyon.AutoConfigureTexture
{
    [AddComponentMenu("Auto Configure Texture/Auto Configure Texture")]
    [DisallowMultipleComponent]
    public class AutoConfigureTexture: MonoBehaviour, INDMFEditorOnly
    {
        public float Quality = 0.5f;
    }
}