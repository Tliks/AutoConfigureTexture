using com.aoyon.AutoConfigureTexture.Processor;
using com.aoyon.AutoConfigureTexture.ShaderInformations;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture.Build
{    
    internal class RemoveMipMaps : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        public void Init(GameObject root, AutoConfigureTexture config)
        {
            _shouldProcess = config.OptimizeMipMap;
            return;
        }   
        public bool Process(TextureInfo info, TextureAnalyzer analyzer, [NotNullWhen(true)] out AdjustData? data)
        {
            var shouldRemove = info.Properties.All(p => ShaderInformation.IsVertexShader(p.Shader, p.PropertyName) == true);
            data = AdjustData.Create(shouldRemove);
            return shouldRemove;
        }
        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.MipMap = true;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData data)
        {
            configurator.OverrideCompression = true;
            configurator.MipMap = true;
        }
    }
}
