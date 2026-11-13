#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace KizilTohum.Editor
{
    /// <summary>Resources/Gorseller altındaki PNG'leri otomatik Sprite olarak içe aktarır.</summary>
    public class GorselIceAktarici : AssetPostprocessor
    {
        static string ad0(string yol) { return System.IO.Path.GetFileNameWithoutExtension(yol); }

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
            string yol = assetPath.Replace('\\', '/');
            if (yol.Contains("/Gorseller/Logo/") || yol.Contains("/Gorseller/Ada/")) ti.maxTextureSize = 1024;   // emblem, Ada portreleri
            if (yol.Contains("/Gorseller/Ikonlar/")) ti.maxTextureSize = 256;
            if (yol.Contains("/Gorseller/Portreler/") && ad0(yol).EndsWith("_3d")) ti.maxTextureSize = 1024;   // 3D portreler
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
