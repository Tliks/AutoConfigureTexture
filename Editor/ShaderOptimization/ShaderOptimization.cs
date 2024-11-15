using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using nadena.dev.ndmf;

namespace com.aoyon.AutoConfigureTexture
{    
    internal partial class ShaderOptimization
    {

        [InitializeOnLoadMethod]
        static void Initialize()
        {
        }

        public static void Apply(BuildContext ctx, AutoConfigureTexture component)
        {
            if (component == null || (!component.RunShaderOptimization))
                return;

            var mapping = Utils.CopyAndRegisterMaterials(Utils.CollectMaterials(component.gameObject));

            var materials = mapping.Values;
            OptimizeMaterials(materials);
#if ACT_lILTOON_1_8_0
            OptimizelilToon(ctx, materials);
#endif

            var renderers = component.GetComponentsInChildren<Renderer>(true);
            Utils.ReplaceMaterials(mapping, renderers);
        }

    }
}
