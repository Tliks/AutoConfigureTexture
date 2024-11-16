#if ACT_lILTOON_1_8_0

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;
#if ACT_MA
using nadena.dev.modular_avatar.core;
#endif

namespace com.aoyon.AutoConfigureTexture
{
    internal partial class OptimizeMaterial
    {
        internal static void OptimizelilToon(BuildContext ctx, IEnumerable<Material> materials)
        {
            var controllers = new HashSet<RuntimeAnimatorController>();
            controllers.UnionWith(ctx.AvatarRootObject.GetComponentsInChildren<Animator>(true).Where(a => a.runtimeAnimatorController).Select(a => a.runtimeAnimatorController));
            VRChatHelper.GetAnimatorControllers(ctx.AvatarDescriptor, controllers);
            MAHelper.GetAnimatorControllers(ctx.AvatarRootObject, controllers);
            var props = controllers.SelectMany(c => c.animationClips).SelectMany(c => AnimationUtility.GetCurveBindings(c)).Select(b => b.propertyName).Where(n => n.Contains("material."))
            .Select(n => n=n.Substring("material.".Length))
            .Select(n => {if(n.Contains(".")) n=n.Substring(0, n.IndexOf(".")); return n;}).Distinct().ToArray();

            foreach(var m in materials)
            {
                if(lilToon.lilMaterialUtils.CheckShaderIslilToon(m)) lilToon.lilMaterialUtils.RemoveUnusedTextureOnly(m, m.shader.name.Contains("Lite"), props);
            }
        }
    }

    internal static class VRChatHelper
    {
        internal static void GetAnimatorControllers(VRCAvatarDescriptor descriptor, HashSet<RuntimeAnimatorController> controllers)
        {
            controllers.UnionWith(descriptor.specialAnimationLayers.Where(l => l.animatorController).Select(l => l.animatorController));
            if(descriptor.customizeAnimationLayers) controllers.UnionWith(descriptor.baseAnimationLayers.Where(l => l.animatorController).Select(l => l.animatorController));
        }
    }

    internal static class MAHelper
    {
        internal static void GetAnimatorControllers(GameObject root, HashSet<RuntimeAnimatorController> controllers)
        {
#if ACT_MA
            var mergeAnimators = root.GetComponentsInChildren<ModularAvatarMergeAnimator>(true);
            controllers.UnionWith(mergeAnimators.Select(ma => ma.animator));
#endif            
        }
    }    
}
#endif
