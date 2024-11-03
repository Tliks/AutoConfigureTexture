using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using nadena.dev.ndmf;

namespace com.aoyon.AutoConfigureTexture
{    
    public class ShaderOptimization
    {

        [InitializeOnLoadMethod]
        static void Initialize()
        {
        }

        public static void Apply(BuildContext ctx, AutoConfigureTexture component)
        {
            if (component == null || (component.RunShaderOptimization == false))
                return;

            var mapping = Utils.CopyAndRegisterMaterials(Utils.CollectMaterials(component.gameObject));

            var lilmats = mapping.Values.Where(m => SerachShader.IsLilToonShader(m.shader));
            InvokelilToon(ctx, lilmats);

            var renderers = component.GetComponentsInChildren<Renderer>(true);
            Utils.ReplaceMaterials(mapping, renderers);
        }

        private static void InvokelilToon(BuildContext ctx, IEnumerable<Material> materials)
        {
#if ACT_lILTOON_1_8_0
            Optimizer.OptimizeMaterials(ctx, materials.ToArray());
#endif
        }
    }
}
