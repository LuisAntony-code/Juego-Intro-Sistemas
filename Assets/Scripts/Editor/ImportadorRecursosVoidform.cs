#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidformBioForge.Herramientas
{
    /// <summary>
    /// Ajustes de importación automáticos para los recursos generados con Herramientas/ (Blender y numpy):
    /// - Texturas de Resources/Texturas: repetibles (Repeat), con mipmaps y filtrado anisótropo; las "*_normal"
    ///   se importan como mapa de normales (si no, Unity las trataría como color y el relieve saldría mal).
    /// - Modelos de Resources/Modelos: escala del archivo (1 unidad = 1 m), sin animaciones, cámaras, luces ni
    ///   colisionadores (el juego crea los suyos) y con las normales exportadas desde Blender.
    /// </summary>
    public class ImportadorRecursosVoidform : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string ruta = assetPath.Replace('\\', '/');
            if (!ruta.Contains("/Resources/Texturas/")) return;
            TextureImporter ti = (TextureImporter)assetImporter;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            if (Path.GetFileNameWithoutExtension(ruta).EndsWith("_normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
            }
        }

        private void OnPreprocessModel()
        {
            string ruta = assetPath.Replace('\\', '/');
            if (!ruta.Contains("/Resources/Modelos/")) return;
            ModelImporter mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.addCollider = false;
            mi.isReadable = false;
            mi.meshCompression = ModelImporterMeshCompression.Low;
            mi.importNormals = ModelImporterNormals.Import;
        }
    }
}
#endif
