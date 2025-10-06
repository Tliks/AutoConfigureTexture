using com.aoyon.AutoConfigureTexture.Processor;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture.Build
{    
    internal interface ITextureAdjuster
    {
        public void Init(GameObject root, AutoConfigureTexture config);
        public bool ShouldProcess { get; }
        public bool Process(TextureInfo info, TextureAnalyzer analyzer, [NotNullWhen(true)] out AdjustData? data);
        public void SetValue(TextureConfigurator configurator, AdjustData data);
        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info);
    }

    internal class AdjustData
    {
        public static AdjustData Create<T>(T data)
        {
            return new AdjustData<T>(data);
        }

        public T GetData<T>()
        {
            return ((AdjustData<T>)this).Data;
        }
    }

    internal class AdjustData<T> : AdjustData
    {
        public T Data { get; }

        public AdjustData(T data)
        {
            Data = data;
        }
    }
}
