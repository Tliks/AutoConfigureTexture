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
            var sequence =
                InPhase(BuildPhase.Generating);
            
            sequence.Run(DisplayName, ctx =>
            {
                var root = ctx.AvatarRootObject;

                var components = root.GetComponentsInChildren<AutoConfigureTexture>();

                foreach (var component in components)
                {
                    CompressTextureProcessor.SetConfigurators(component, root.transform);
                    Object.DestroyImmediate(component);
                }
            });
        }
    }
}

