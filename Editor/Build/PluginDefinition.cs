using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(com.aoyon.AutoConfigureTexture.Build.PluginDefinition))]

namespace com.aoyon.AutoConfigureTexture.Build;

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
                SetTextureConfigurator.Apply(component);

                Object.DestroyImmediate(component);
            }
        });
    }
}
