using System.Collections.Generic;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;

namespace com.aoyon.AutoConfigureTexture
{
    public abstract class AutoTextureAdjuterComponent : MonoBehaviour, INDMFEditorOnly
    {
        // AvatarObjectReferenceのnullは許容しない
        // その上でnullが返ってきた場合は意図的なもの判別できない
        // なのでnull非許容でいく
        // 衣装に付属されたprefabを作りたい場合などは衣装のリネームで動作するのが難点だけど、まあ大体Rootだし仕方ない
        [SerializeField] private AvatarObjectReference m_rootReference = new(){ referencePath = AvatarObjectReference.AVATAR_ROOT };
        public GameObject Root { get => m_rootReference?.Get(this); set => m_rootReference?.Set(value); }
        
        [SerializeField] private List<Texture2D> m_exclusion = new();
        public List<Texture2D> Exclusion { get => m_exclusion; set => m_exclusion = value; }

        public void ResolveReference() => m_rootReference?.Get(this);

        public abstract ManualTextureAdjuterComponent AddManualComponent(GameObject target);
    }
}
