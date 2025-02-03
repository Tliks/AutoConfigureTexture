using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;
using nadena.dev.ndmf;

namespace com.aoyon.AutoConfigureTexture
{    
    public class SetTextureConfigurator
    {
        public async static Task<GameObject> Apply(AutoConfigureTexture component, Transform parent)
        {
            if (component == null || (!component.OptimizeTextureFormat && !component.OptimizeMipMap && component.ResolutionReduction == Reduction.None))
                return null;
            
            if (component.IsPCOnly) {
                BuildTarget currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                if (currentBuildTarget == BuildTarget.Android || currentBuildTarget == BuildTarget.iOS) {
                    return null;
                }
            }
            
            var root = new GameObject("Auto Texture Configurator");
            root.transform.SetParent(parent);

            // 除外するTexture2DのObjectReferenceを取得
            var excludes = component.Exclude
                .Where(t => t != null)
                .Select(t => ObjectRegistry.GetReference(t))
                .ToHashSet();

            // 既にTextureConfiguratorを設定しているテクスチャを取得
            var exists = component.GetComponentsInChildren<TextureConfigurator>()
                .Select(c => c.TargetTexture.GetTexture()) // TTTInternal
                .Where(t => t != null)
                .ToHashSet();

            var materialinfos = MaterialInfo.Collect(component.gameObject);
            var textureInfos = TextureInfo.Collect(materialinfos).ToArray();
            var configurators = CrateTextureConfigurators(textureInfos, root, excludes, exists);

            var tasks = new List<Task>();
            var adjusters = Utils.GetImplementClasses<ITextureAdjuster>();
            foreach (var adjuster in adjusters)
            {
                tasks.Add(ProcessAdjuster(adjuster, configurators, component.gameObject, component));
            }
            await Task.WhenAll(tasks);

            return root;
        }

        private static IEnumerable<(TextureInfo, TextureConfigurator)> CrateTextureConfigurators(IEnumerable<TextureInfo> infos, GameObject root, HashSet<ObjectReference> excludes, HashSet<Texture> exists)
        {
            var configurators = new List<(TextureInfo, TextureConfigurator)>();
            foreach (var info in infos)
            {
                var texture = info.Texture;
                var properties = info.Properties;

                // Texture2D以外は現状何もしない
                if (texture is not Texture2D tex2d) continue;
                // TextureConfiguratorは正方形のみ(多分)
                if (texture.width != texture.height) continue;
                // 既存の設定がある場合除外
                if (exists.Contains(tex2d)) continue;
                // 除外設定したテクスチャと参照が同一の場合除外
                var reference = ObjectRegistry.GetReference(tex2d);
                if (excludes.Any(r => r.Equals(reference))) continue;

                // TextureConfiguratorを生成
                var go = new GameObject(tex2d.name);
                go.transform.SetParent(root.transform, false);
                var textureConfigurator = go.AddComponent<TextureConfigurator>();

                var textureSelector = new TextureSelector();
                textureSelector.Mode = TextureSelector.SelectMode.Relative;
                // 代表のプロパティ, Renderer, Material
                var property = properties.First();
                var materialInfo = property.MaterialInfo;
                textureSelector.RendererAsPath = materialInfo.Renderers.First();
                textureSelector.SlotAsPath = materialInfo.MaterialIndices.First();
                var propertyName = new net.rs64.TexTransTool.PropertyName(property.PropertyName);
                textureSelector.PropertyNameAsPath = propertyName;
                textureConfigurator.TargetTexture = textureSelector;

                configurators.Add((info, textureConfigurator));
            }
            return configurators;
        }

        private async static Task ProcessAdjuster(ITextureAdjuster adjuster, IEnumerable<(TextureInfo, TextureConfigurator)> targets, GameObject root, AutoConfigureTexture config)
        {
            var infos = targets.Select(x => x.Item1);

            await adjuster.Init(root, infos, config);

            foreach (var (info, configurator) in targets)
            {
                if (adjuster.ShouldProcess && adjuster.Validate(info) && adjuster.Process(info, out var data))
                {
                    adjuster.SetValue(configurator, data);
                }
                else
                {
                    adjuster.SetDefaultValue(configurator, info);
                }
            }
        }
    }
}
