/*
MIT License

Copyright (c) 2024 lilxyzw

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if ACT_lILTOON_1_8_0

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;

namespace com.aoyon.AutoConfigureTexture
{
    internal static class Optimizer
    {
        internal static void OptimizeMaterials(BuildContext ctx, Material[] materials)
        {
            var propMap = materials.Select(m => m.shader).Distinct().Where(s => s).ToDictionary(s => s, s => new ShaderPropertyContainer(s));

            var controllers = new HashSet<RuntimeAnimatorController>();
            controllers.UnionWith(ctx.AvatarRootObject.GetComponentsInChildren<Animator>(true).Where(a => a.runtimeAnimatorController).Select(a => a.runtimeAnimatorController));
            VRChatHelper.GetAnimatorControllers(ctx.AvatarDescriptor, controllers);
            var props = controllers.SelectMany(c => c.animationClips).SelectMany(c => AnimationUtility.GetCurveBindings(c)).Select(b => b.propertyName).Where(n => n.Contains("material."))
            .Select(n => n=n.Substring("material.".Length))
            .Select(n => {if(n.Contains(".")) n=n.Substring(0, n.IndexOf(".")); return n;}).Distinct().ToArray();

            foreach(var m in materials)
            {
                RemoveUnusedProperties(m, propMap);
                if(lilToon.lilMaterialUtils.CheckShaderIslilToon(m)) lilToon.lilMaterialUtils.RemoveUnusedTextureOnly(m, m.shader.name.Contains("Lite"), props);
            }
        }

        // シェーダーで使われていないプロパティを除去
        private static void RemoveUnusedProperties(Material material, Dictionary<Shader, ShaderPropertyContainer> propMap)
        {
            using var so = new SerializedObject(material);
            so.Update();
            using var savedProps = so.FindProperty("m_SavedProperties");
            if(material.shader)
            {
                var dic = propMap[material.shader];
                DeleteUnused(savedProps, "m_TexEnvs", dic.textures);
                DeleteUnused(savedProps, "m_Floats", dic.floats);
                DeleteUnused(savedProps, "m_Colors", dic.vectors);
            }
            else
            {
                DeleteAll(savedProps, "m_TexEnvs");
                DeleteAll(savedProps, "m_Floats");
                DeleteAll(savedProps, "m_Colors");
            }
            so.ApplyModifiedProperties();
        }

        private static void DeleteUnused(SerializedProperty prop, string name, HashSet<string> names)
        {
            using var props = prop.FPR(name);
            if(props.arraySize == 0) return;
            int i = 0;
            var size = props.arraySize;
            var p = props.GetArrayElementAtIndex(i);
            void DeleteUnused()
            {
                if(!names.Contains(p.GetStringInProperty("first")))
                {
                    p.DeleteCommand();
                    if(i < --size)
                    {
                        p = props.GetArrayElementAtIndex(i);
                        DeleteUnused();
                    }
                }
                else if(p.NextVisible(false))
                {
                    if(++i < size) DeleteUnused();
                }
            }
            DeleteUnused();
            p.Dispose();
        }

        internal static SerializedProperty FPR(this SerializedProperty property, string name)
        {
            return property.FindPropertyRelative(name);
        }

        private static void DeleteAll(SerializedProperty prop, string name)
        {
            using var props = prop.FPR(name);
            props.arraySize = 0;
        }
        
        // stringValueを取得
        internal static string GetStringInProperty(this SerializedProperty serializedProperty, string name)
        {
            using var prop = serializedProperty.FPR(name);
            return prop.stringValue;
        }
    }

    // シェーダーのプロパティを検索して保持するクラス
    internal class ShaderPropertyContainer
    {
        internal HashSet<string> textures;
        internal HashSet<string> floats;
        internal HashSet<string> vectors;

        internal ShaderPropertyContainer(Shader shader)
        {
            textures = new HashSet<string>();
            floats = new HashSet<string>();
            vectors = new HashSet<string>();

            var count = shader.GetPropertyCount();

            for(int i = 0; i < count; i++)
            {
                var t = shader.GetPropertyType(i);
                var name = shader.GetPropertyName(i);
                if(t == ShaderPropertyType.Texture) textures.Add(name);
                else if(t == ShaderPropertyType.Color || t == ShaderPropertyType.Vector) vectors.Add(name);
                else floats.Add(name);
            }
        }
    }

    internal static class VRChatHelper
    {
        internal static void GetAnimatorControllers(VRCAvatarDescriptor descriptor, HashSet<RuntimeAnimatorController> controllers)
        {
            controllers.UnionWith(descriptor.specialAnimationLayers.Where(l => l.animatorController).Select(l => l.animatorController));
            if(descriptor.customizeAnimationLayers) controllers.UnionWith(descriptor.baseAnimationLayers.Where(l => l.animatorController).Select(l => l.animatorController));
        }
    }    
}
#endif
