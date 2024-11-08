using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;
using net.rs64.TexTransTool.TextureAtlas;

namespace com.aoyon.AutoConfigureTexture
{    
    public class AttachAtlasTexture
    {
        public static GameObject Apply(AutoConfigureTexture component, Transform parent)
        {
            var root = new GameObject("Auto Atlas Texture");
            root.transform.SetParent(parent);
        
            var materials = Utils.CollectMaterials(component.gameObject);

            foreach (var material in materials)
            {
                var go = new GameObject(material.name);
                go.transform.SetParent(root.transform, false);
                var atlasTexture = go.AddComponent<AtlasTexture>();
            }

            Undo.RegisterCreatedObjectUndo(root, "Auto Configure Texture Setup");

            return root;
        }


    }
}
