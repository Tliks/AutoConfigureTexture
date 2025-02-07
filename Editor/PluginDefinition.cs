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
            InPhase(BuildPhase.Generating).
            Run("Attach TextureConfigurator", ctx =>
            {
                var root = ctx.AvatarRootObject;

                var components = root.GetComponentsInChildren<AutoConfigureTexture>();

                foreach (var component in components)
                {
                    await SetTextureConfigurator.Apply(component, root.transform);

                    Object.DestroyImmediate(component);
                }
            });
        }
    }
}

