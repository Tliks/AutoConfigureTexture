using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using net.rs64.TexTransTool;

namespace com.aoyon.AutoConfigureTexture
{    
    public interface ITextureAdjuster
    {
        public Task Init(GameObject root, IEnumerable<TextureInfo> textureinfos, AutoConfigureTexture config);
        public bool ShouldProcess { get; }
        public bool Validate(TextureInfo info);
        public bool Process(TextureInfo info, out AdjustData<object> data);
        public void SetValue(TextureConfigurator configurator, AdjustData<object> data);
        public void SetDefaultValue(TextureConfigurator configurator, TextureInfo info);
    }

    public class AdjustData<T>
    {
        public T Data { get; }

        public AdjustData(T data)
        {
            Data = data;
        }
    }
}
