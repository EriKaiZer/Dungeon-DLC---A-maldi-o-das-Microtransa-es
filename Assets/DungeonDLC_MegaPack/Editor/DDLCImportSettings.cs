using UnityEditor;
using UnityEngine;
namespace DungeonDLC.MegaPack.Editor
{
    public sealed class DDLCImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(DDLCPackBuilder.Root + "/Art/")) return;
            var t = (TextureImporter)assetImporter;
            bool surface = assetPath.Contains("/Textures/");
            t.textureType = TextureImporterType.Default;
            t.filterMode = FilterMode.Point;
            t.anisoLevel = 0;
            t.mipmapEnabled = surface;
            t.maxTextureSize = 512;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.npotScale = surface ? TextureImporterNPOTScale.ToNearest : TextureImporterNPOTScale.None;
            t.alphaSource = TextureImporterAlphaSource.FromInput;
            t.alphaIsTransparency = !surface;
            // Mirror joins a texture to itself at each edge. It does not claim that
            // the source PNG is mathematically seamless under ordinary Repeat.
            t.wrapMode = surface ? TextureWrapMode.Mirror : TextureWrapMode.Clamp;
        }
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(DDLCPackBuilder.Root + "/Models/")) return;
            var m = (ModelImporter)assetImporter;
            m.globalScale = 1;
            m.useFileScale = false;
            m.importAnimation = false;
            m.importCameras = false;
            m.importLights = false;
            m.importNormals = ModelImporterNormals.Calculate;
            m.normalSmoothingAngle = 0;
            m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            m.isReadable = true;
        }
    }
}
