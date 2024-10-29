using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.AutoConfigureTexture
{
    public class SerachShader
    {
        static Dictionary<string, List<string>> shaderDictionary = new Dictionary<string, List<string>>
        {
            { "liltoon", new List<string>
                {
                    "lilToon",
                    "Hidden/lilToonCutout",
                    "Hidden/lilToonTransparent",
                    "Hidden/lilToonOnePassTransparent",
                    "Hidden/lilToonTwoPassTransparent",
                    "Hidden/lilToonOutline",
                    "Hidden/lilToonCutoutOutline",
                    "Hidden/lilToonTransparentOutline",
                    "Hidden/lilToonOnePassTransparentOutline",
                    "Hidden/lilToonTwoPassTransparentOutline",
                    "_lil/[Optional] lilToonOutlineOnly",
                    "_lil/[Optional] lilToonOutlineOnlyCutout",
                    "_lil/[Optional] lilToonOutlineOnlyTransparent",
                    "Hidden/lilToonTessellation",
                    "Hidden/lilToonTessellationCutout",
                    "Hidden/lilToonTessellationTransparent",
                    "Hidden/lilToonTessellationOnePassTransparent",
                    "Hidden/lilToonTessellationTwoPassTransparent",
                    "Hidden/lilToonTessellationOutline",
                    "Hidden/lilToonTessellationCutoutOutline",
                    "Hidden/lilToonTessellationTransparentOutline",
                    "Hidden/lilToonTessellationOnePassTransparentOutline",
                    "Hidden/lilToonTessellationTwoPassTransparentOutline",
                    "Hidden/lilToonLite",
                    "Hidden/lilToonLiteCutout",
                    "Hidden/lilToonLiteTransparent",
                    "Hidden/lilToonLiteOnePassTransparent",
                    "Hidden/lilToonLiteTwoPassTransparent",
                    "Hidden/lilToonLiteOutline",
                    "Hidden/lilToonLiteCutoutOutline",
                    "Hidden/lilToonLiteTransparentOutline",
                    "Hidden/lilToonLiteOnePassTransparentOutline",
                    "Hidden/lilToonLiteTwoPassTransparentOutline",
                    "Hidden/lilToonRefraction",
                    "Hidden/lilToonRefractionBlur",
                    "Hidden/lilToonFur",
                    "Hidden/lilToonFurCutout",
                    "Hidden/lilToonFurTwoPass",
                    "_lil/[Optional] lilToonFurOnlyTransparent",
                    "_lil/[Optional] lilToonFurOnlyCutout",
                    "_lil/[Optional] lilToonFurOnlyTwoPass",
                    "Hidden/lilToonGem",
                    "_lil/[Optional] lilToonFakeShadow",
                    "_lil/[Optional] lilToonOverlay",
                    "_lil/[Optional] lilToonOverlayOnePass",
                    "_lil/[Optional] lilToonLiteOverlay",
                    "_lil/[Optional] lilToonLiteOverlayOnePass",
                    "Hidden/ltsother_baker",
                    "Hidden/ltspass_opaque",
                    "Hidden/ltspass_cutout",
                    "Hidden/ltspass_transparent",
                    "Hidden/ltspass_tess_opaque",
                    "Hidden/ltspass_tess_cutout",
                    "Hidden/ltspass_tess_transparent",
                    "_lil/lilToonMulti",
                    "Hidden/lilToonMultiOutline",
                    "Hidden/lilToonMultiRefraction",
                    "Hidden/lilToonMultiFur",
                    "Hidden/lilToonMultiGem",
                    "VRM/MToon"
                }
            }
        };

        
        public static bool IsLilToonShader(Shader shader)
        {
            if (shaderDictionary.TryGetValue("liltoon", out List<string> shaders))
            {
                return shaders.Contains(shader.name);
            }
            return false;
        }
    }
}
