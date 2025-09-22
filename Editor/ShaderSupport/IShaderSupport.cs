namespace com.aoyon.AutoConfigureTexture
{    
    internal interface IShaderSupport
    {
        public bool IsTarget(Shader shader);
        public TextureChannel GetTextureChannel(Shader shader, string property);
        public TextureUsage GetTextureUsage(Shader shader, string property);
        public bool IsVertexShader(Shader shader, string property);
    }
    
    internal struct PropertyData
    {
        public TextureChannel OpaqueChannel;
        public TextureChannel TransparentChannel;
        public TextureUsage TextureUsage;
        public bool IsVertex;

        internal PropertyData(TextureChannel baseChannel, TextureUsage textureUsage = TextureUsage.Others, bool isVertex = false)
        {
            OpaqueChannel = baseChannel;
            TransparentChannel = baseChannel;
            TextureUsage = textureUsage;
            IsVertex = isVertex;
        }

        internal PropertyData(TextureChannel opaqueChannel, TextureChannel transparentChannel, TextureUsage usage = TextureUsage.Others, bool isVertex = false)
        {
            OpaqueChannel = opaqueChannel;
            TransparentChannel = transparentChannel;
            TextureUsage = usage;
            IsVertex = isVertex;
        }
    }
}
