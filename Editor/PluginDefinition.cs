using UnityEngine;
using nadena.dev.ndmf;
using com.aoyon.AutoConfigureTexture;

[assembly: ExportsPlugin(typeof(PluginDefinition))]

namespace com.aoyon.AutoConfigureTexture
{
    public class PluginDefinition : Plugin<PluginDefinition>
    {
        public override string QualifiedName => "com.aoyon.AutoConfigureTexture";

        public override string DisplayName => "Auto Configure Texture";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
            .Run("Resolve References", ctx =>
            {
                var components = ctx.AvatarRootObject.GetComponentsInChildren<AutoTextureAdjuterComponent>();
                foreach (var component in components)
                {
                    component.ResolveReference();
                }
            });

            InPhase(BuildPhase.Transforming)
            .Run("Auto Adjust Texture", ctx =>
            {
                var components = ctx.AvatarRootObject.GetComponentsInChildren<AutoTextureAdjuterComponent>();
                // process here
                foreach (var component in components)
                {
                    Object.DestroyImmediate(component);
                }
            }).Then
            .Run("Adjust Texture", ctx =>
            {
                var components = ctx.AvatarRootObject.GetComponentsInChildren<ManualTextureAdjuterComponent>();
                // process here
                foreach (var component in components)
                {
                    Object.DestroyImmediate(component);
                }
            });
        }
    }
}

