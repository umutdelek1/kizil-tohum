#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace KizilTohum.Editor
{
    /// <summary>WebGL yayını için proje ayarlarını tek adımda uygular (Kızıl Tohum/WebGL Ayarlarını Uygula).</summary>
    public static class WebGLAyarlari
    {
        [MenuItem("Kızıl Tohum/WebGL Ayarlarını Uygula")]
        public static void Uygula()
        {
            var hedef = NamedBuildTarget.WebGL;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;   // sıkıştırma kapalı (sunucu başlığı gerekmez)
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;         // boyut için
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.SetIl2CppCodeGeneration(hedef, Il2CppCodeGeneration.OptimizeSize);   // Code Optimization = Size
            PlayerSettings.SetManagedStrippingLevel(hedef, ManagedStrippingLevel.Medium);
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;                                // 16:9
            PlayerSettings.defaultWebScreenHeight = 720;
            AssetDatabase.SaveAssets();
            Debug.Log("[KızılTohum] WebGL ayarları uygulandı.");
        }
    }
}
#endif
