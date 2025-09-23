namespace com.aoyon.AutoConfigureTexture;

internal class TextureInfoCollector // Todo: 追加のマテリアルの参照を考慮する？
{
    public IEnumerable<TextureInfo> Execute(GameObject root)
    {
        var materialInfos = CollectMaterialInfos(root);
        var textureInfos = CollectTextureInfos(materialInfos);
        return textureInfos;
    }

    public IEnumerable<MaterialInfo> CollectMaterialInfos(GameObject root)
    {
        var materialInfos = new Dictionary<Material, MaterialInfo>();

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                var material = materials[index];
                if (material == null) continue;

                materialInfos.GetOrAdd(material, static (m) => new MaterialInfo(m)).AddReference(renderer, index);
            }
        }

        return materialInfos.Values;
    }

    public IEnumerable<TextureInfo> CollectTextureInfos(IEnumerable<MaterialInfo> materialInfos)
    {
        var textureInfos = new Dictionary<Texture2D, TextureInfo>();

        foreach (var materialInfo in materialInfos)
        {
            var material = materialInfo.Material;
            var shader = material.shader;

            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    continue;
                }

                var NameID = shader.GetPropertyNameId(i);
                var texture = material.GetTexture(NameID);
                if (texture == null) continue;

                if (texture is not Texture2D texture2d) continue;

                var propertyName = shader.GetPropertyName(i);
                var propertyInfo = new PropertyInfo(materialInfo, shader, propertyName, 0); // Todo: UVchannelを取得する

                var textureInfo = textureInfos.GetOrAdd(texture2d, static (t) => new TextureInfo(t));

                textureInfo.AddPropertyInfo(propertyInfo);
                materialInfo.AddTextureInfo(textureInfo);
            }
        }

        return textureInfos.Values;
    }
}