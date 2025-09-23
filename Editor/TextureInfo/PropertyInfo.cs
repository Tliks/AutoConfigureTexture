namespace com.aoyon.AutoConfigureTexture;

internal readonly struct PropertyInfo
{
    public readonly MaterialInfo MaterialInfo;
    public readonly Shader Shader; 
    public readonly string PropertyName;
    public readonly int UVchannel;

    public PropertyInfo(MaterialInfo materialInfo, Shader shader, string propertyName, int uvchannel)
    {
        MaterialInfo = materialInfo;
        Shader = shader;
        PropertyName = propertyName;
        UVchannel = uvchannel;
    }
}
