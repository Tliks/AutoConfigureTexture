/*
MIT License

Copyright (c) 2024 lilxyzw

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if ACT_lILTOON_1_8_0

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

namespace com.aoyon.AutoConfigureTexture
{
    internal partial class ShaderOptimization
    {
        internal static void OptimizelilToon(BuildContext ctx, IEnumerable<Material> materials)
        {
            var controllers = new HashSet<RuntimeAnimatorController>();
            controllers.UnionWith(ctx.AvatarRootObject.GetComponentsInChildren<Animator>(true).Where(a => a.runtimeAnimatorController).Select(a => a.runtimeAnimatorController));
            VRChatHelper.GetAnimatorControllers(ctx.AvatarDescriptor, controllers);
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
}
#endif
