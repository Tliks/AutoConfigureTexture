using System;
using System.Collections.Generic;
using System.Reflection;
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
        public bool MaintainCrunch = true;
        public bool OptimizeMipMap = true;
        public Reduction ResolutionReduction = Reduction.Normal;
        public bool IsPCOnly = true;
        public List<Texture2D> Exclude = new();

        // 削除された機能。バージョンを下げたときの互換性用にフィールドは残すが、次のマイナーあたりで削除する。
        [Obsolete] public bool OptimizeMaterial = false; 
    }
}