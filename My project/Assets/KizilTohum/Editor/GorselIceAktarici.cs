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
            string ad = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (ad.EndsWith("_9slice"))
            {
                float b = ad.StartsWith("diyalog") ? 44f : ad.StartsWith("secenek") ? 32f : ad.StartsWith("ipucu") ? 36f : 48f;
                ti.spriteBorder = new Vector4(b, b, b, b);
            }
        }
    }
}
#endif
