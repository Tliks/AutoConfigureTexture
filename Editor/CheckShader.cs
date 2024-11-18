using UnityEditor;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public class CheckShader
    {
        public static bool IslilToon(Material material)
        {
            if(material == null) return false;
            return IslilToon(material.shader);
        }

        public static bool IslilToon(Shader shader)
        {
            if(shader == null) return false;
            if(shader.name.Contains("lilToon") || shader.name.Contains("lts_pass")) return true;
            string shaderPath = AssetDatabase.GetAssetPath(shader);
            return !string.IsNullOrEmpty(shaderPath) && shaderPath.Contains(".lilcontainer");
        }
    }
}
