#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace KizilTohum.Editor
{
    /// <summary>Resources/Gorseller altındaki PNG'leri otomatik Sprite olarak içe aktarır.</summary>
    public class GorselIceAktarici : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Gorseller/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.compressionQuality = 50; // Normal Quality (WebGL yük boyutu için; Harita/ 1920x1080 sahneleri dahil)
            string ad = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (ad.EndsWith("_9slice"))
            {
                float b = 36f; // bugünkü arayüz seti: tüm 9-slice parçalarda 36 px
                ti.spriteBorder = new Vector4(b, b, b, b);
            }
        }
    }
}
#endif
