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

            InPhase(BuildPhase.Resolving).          
            Run("Invoke Shader Optimization", ctx =>
            {
                var root = ctx.AvatarRootObject;

                var components = root.GetComponentsInChildren<AutoConfigureTexture>();

                foreach (var component in components)
                {
                    ShaderOptimization.Apply(ctx, component);
                }
            });

            InPhase(BuildPhase.Generating).
            Run("Attach TextureConfigurator", ctx =>
            {
                var root = ctx.AvatarRootObject;

                var components = root.GetComponentsInChildren<AutoConfigureTexture>();

                foreach (var component in components)
                {
                    AttachConfigurators.Apply(component, root.transform);

                    Object.DestroyImmediate(component);
                }
            });
        }
    }
}

