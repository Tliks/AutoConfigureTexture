using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(com.aoyon.AutoConfigureTexture.Build.PluginDefinition))]

namespace com.aoyon.AutoConfigureTexture.Build;

internal class PluginDefinition : Plugin<PluginDefinition>
{
    public override string QualifiedName => "com.aoyon.AutoConfigureTexture";
    public override string DisplayName => "Auto Configure Texture";

    protected override void Configure()
    {
        InPhase(BuildPhase.Generating).
        Run("Build", ctx =>
        {
            var root = ctx.AvatarRootObject;
            var components = root.GetComponentsInChildren<AutoConfigureTexture>();
            if (components.Length == 0) return;
            if (components.Length > 1)
            {
                throw new Exception("AutoConfigureTexture is not allowed to be more than one");
            }

            var component = components[0];
            // Build

            Object.DestroyImmediate(component);
        });
    }
}
