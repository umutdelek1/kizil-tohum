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
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;   // Brotli: .br dosyaları için Dagitim/vercel.json başlıkları gerekir
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;         // boyut için
            PlayerSettings.WebGL.template = "PROJECT:KizilTohum";   // Assets/WebGLTemplates/KizilTohum (letterbox 16:9, tam ekran düğmesi)
            PlayerSettings.SplashScreen.show = false;               // "Made with Unity" açılış ekranı kapalı
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
