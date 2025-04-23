using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{    
    internal class RemoveMipMaps : ITextureAdjuster
    {
        public bool ShouldProcess => _shouldProcess;
        private bool _shouldProcess = false;

        public void Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config)
        {
            _shouldProcess = config.OptimizeMipMap;
            return;
        }
        public bool Validate(TextureInfo info)
        {
            return true;
        }
        public bool Process(TextureInfo info, out AdjustData<object> data)
        {
            var shouldRemove = info.Properties.All(p => ShaderSupport.IsVertexShader(p.Shader, p.PropertyName));
            data = new AdjustData<object>(shouldRemove);
            return shouldRemove;
        }
        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info)
        {
            configurator.MipMap = true;
        }

        public void SetValue(TextureConfigurator configurator, AdjustData<object> data)
        {
            configurator.OverrideCompression = true;
            configurator.MipMap = true;
        }
    }
}
