using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransTool;
using nadena.dev.ndmf;
using nadena.dev.ndmf.runtime;
using Object = UnityEngine.Object;

namespace com.aoyon.AutoConfigureTexture
{    

    public class AutoAdjuterProcessor
    {
        private Func<(Object l, Object r), bool> _originEqual;

        public AutoAdjuterProcessor(BuildContext ctx)
        {
            
            var autoAdjustResolutions = ctx.AvatarRootObject.GetComponentsInChildren<AutoAdjustResolution>();
            var autoAdjustTextureFormats = ctx.AvatarRootObject.GetComponentsInChildren<AutoAdjustTextureFormat>();
            var AutoremoveMipMap = ctx.AvatarRootObject.GetComponentsInChildren<AutoRemoveMipMap>();

            // 一旦一つのみを想定
            // Todo: resolve 
            var autoAdjustResolution = autoAdjustResolutions.FirstOrDefault();
            var autoAdjustTextureFormat = autoAdjustTextureFormats.FirstOrDefault();
            var autoRemoveMipMap = AutoremoveMipMap.FirstOrDefault();

            


        }

        public static void Excute(GameObject root)
        {

        }
    }

    public class SetTextureConfigurator
    {
        public static GameObject Apply(AutoConfigureTexture component)
        {
            if (component == null || (!component.OptimizeTextureFormat && !component.OptimizeMipMap && component.ResolutionReduction == Reduction.None))
                return null;
            
            if (component.IsPCOnly) {
                BuildTarget currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                if (currentBuildTarget == BuildTarget.Android || currentBuildTarget == BuildTarget.iOS) {
                    return null;
                }
            }
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var r = ApplyImpl(component);
            stopwatch.Stop();
            Debug.Log($"ApplyImpl executed in {stopwatch.ElapsedMilliseconds} ms");
            return r;
        }

        private static GameObject ApplyImpl(AutoConfigureTexture component)
        {
            var avatarRoot = RuntimeUtil.FindAvatarInParents(component.transform);
            if (avatarRoot == null) return null;

            var materialinfos = MaterialInfo.Collect(component.gameObject);
            var textureInfos = TextureInfo.Collect(materialinfos).ToArray();

            var parent = new GameObject("Auto Texture Configurator");
            parent.transform.SetParent(avatarRoot);
            var configurators = CrateTextureConfigurators(textureInfos, parent, avatarRoot, component);

            var adjusters = Utils.GetInterfaceInstances<ITextureAdjuster>();
            foreach (var adjuster in adjusters)
            {
                ProcessAdjuster(adjuster, configurators, component.gameObject, component);
            }

            return parent;
        }
 
        private static IEnumerable<(TextureInfo, TextureConfigurator)> CrateTextureConfigurators(IEnumerable<TextureInfo> infos, GameObject parent, Transform avatarRoot, AutoConfigureTexture component)
        {
            // 除外するTexture2DのObjectReferenceを取得
            var excludes = component.Exclude
                .Where(t => t != null)
                .Select(t => ObjectRegistry.GetReference(t))
                .ToHashSet();
            
            /*
            // 既にTextureConfiguratorを設定しているテクスチャを取得
            var exists = avatarRoot.GetComponentsInChildren<TextureConfigurator>()
                .Select(c => c.TargetTexture.GetTexture()) // TTTInternal
                .Where(t => t != null)
                .ToHashSet();
            */

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
                //if (exists.Contains(tex2d)) continue;
                // 除外設定したテクスチャと参照が同一の場合除外
                var reference = ObjectRegistry.GetReference(tex2d);
                if (excludes.Any(r => r.Equals(reference))) continue;

                // TextureConfiguratorを生成
                var go = new GameObject(tex2d.name);
                go.transform.SetParent(parent.transform, false);
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

        private static void ProcessAdjuster(ITextureAdjuster adjuster, IEnumerable<(TextureInfo, TextureConfigurator)> targets, GameObject root, AutoConfigureTexture config)
        {
            var infos = targets.Select(x => x.Item1);

            adjuster.Init(root, infos, config);

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
