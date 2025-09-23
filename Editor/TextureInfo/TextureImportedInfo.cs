namespace com.aoyon.AutoConfigureTexture;

internal class TextureImportedInfo
{
    public readonly TextureImporter Importer;

    public TextureImporterType TextureImporterType => Importer.textureType;
    public TextureImporterCompression Compression => Importer.textureCompression;
    public int CompressionQuality => Importer.compressionQuality;
    public bool sRGBTexture => Importer.sRGBTexture;
    public TextureImporterAlphaSource AlphaSource => Importer.alphaSource;
    public bool AlphaIsTransparency => Importer.alphaIsTransparency;
    public bool MipmapEnabled => Importer.mipmapEnabled;
    public bool IsReadable => Importer.isReadable;

    public TextureImportedInfo(TextureImporter importer)
    {
        Importer = importer;
    }
}