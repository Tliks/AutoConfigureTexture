using com.aoyon.AutoConfigureTexture.ShaderInformations;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace com.aoyon.AutoConfigureTexture.Processor;

internal class TextureImportanceHeuristics
{
    private readonly GameObject _root;
    private SkinnedMeshRenderer? _faceRenderer;
    public TextureImportanceHeuristics(GameObject root)
    {
        _root = root;
        TryGetFaceRenderer(out _faceRenderer);
    }

    public float ComputeIslandImportance(IslandDescription description)
    {
        var shader = description.PropertyInfo.Shader;
        var propertyName = description.PropertyInfo.PropertyName;
        var usage = ShaderInformation.GetTextureUsage(shader, propertyName);

        var renderer = description.Renderer;
        if (renderer == _faceRenderer && usage == TextureUsage.MainTex)
        {
            return 1.0f;
        }

        return ComputeUsageImportance(usage);
    }

    public float ComputeUsageImportance(TextureUsage usage)
    {
        return usage switch
        {
            TextureUsage.MainTex => 1.0f,
            TextureUsage.NormalMap => 0.9f,
            TextureUsage.AOMap => 0.6f,
            _ => 0.5f
        };
    }

    private bool TryGetFaceRenderer([NotNullWhen(true)] out SkinnedMeshRenderer? faceRenderer)
    {
        faceRenderer = null;
        if (!_root.TryGetComponent(out VRCAvatarDescriptor descriptor)) return false;

        if (descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape && descriptor.VisemeSkinnedMesh != null)
        {
            faceRenderer = descriptor.VisemeSkinnedMesh;
            return true;
        }
        if (descriptor.customEyeLookSettings.eyelidType == VRCAvatarDescriptor.EyelidType.Blendshapes && descriptor.customEyeLookSettings.eyelidsSkinnedMesh != null)
        {
            faceRenderer = descriptor.customEyeLookSettings.eyelidsSkinnedMesh;
            return true;
        }

        return false;
    }
}