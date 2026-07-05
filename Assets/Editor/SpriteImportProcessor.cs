using UnityEditor;
using UnityEngine;

public class SpriteImportProcessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        var importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        // Compression
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Mipmap
        importer.mipmapEnabled = false;

        // Alpha
        importer.alphaIsTransparency = true;

        // Full Rect
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);

        settings.spriteMeshType = SpriteMeshType.FullRect;

        importer.SetTextureSettings(settings);
    }
}