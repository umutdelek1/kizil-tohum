using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace KizilTohum
{
    /// <summary>
    /// Sahne arka planında bir kez oynayan sessiz iç mekân klibi. Harita dalış videolarıyla aynı yöntemi kullanır:
    /// StreamingAssets/Video/&lt;dosya&gt;.mp4 URL'si, VideoPlayer → RenderTexture → RawImage (WebGL dahil her platformda).
    /// Hazır olana kadar altındaki duran görsel görünür; bitince son karede durur (döngü yok, sprite'a geçmez).
    /// Dosya yoksa (Kur null döner) ya da oynatma hata verirse hata geri çağrısı çalışır ve bileşen kendini kaldırır.
    /// Video bir SahneSlot'un çocuğudur (Kapla katmanı), pencere oranından bağımsız ekranı kaplar.
    /// </summary>
    public class SahneVideosu : MonoBehaviour
    {
        RawImage goruntu;
        VideoPlayer vp;
        RenderTexture rt;
        Action hata;
        string dosya;
        bool basladi, bitti;
        float baslangic;

        /// <summary>Video kurulduysa bileşeni, dosya yoksa null döner. Ata etkin değilse hazırlık etkinleşince başlar.</summary>
        public static SahneVideosu Kur(RectTransform ata, string dosyaAdi, Action hataOlursa)
        {
            if (string.IsNullOrEmpty(dosyaAdi)) return null;
            string ad = dosyaAdi.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? dosyaAdi : dosyaAdi + ".mp4";
            string yol = Application.streamingAssetsPath + "/Video/" + ad;
            if (!yol.Contains("://") && !System.IO.File.Exists(yol)) // WebGL/Android'de yol URL'dir, denetlenemez
            {
                Debug.LogWarning("KizilTohum: sahne videosu bulunamadı, duran görsel kullanılıyor: " + yol);
                return null;
            }
            var go = new GameObject("Video", typeof(RectTransform));
            go.transform.SetParent(ata, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var v = go.AddComponent<SahneVideosu>();
            v.hata = hataOlursa; v.dosya = ad;
            v.goruntu = go.AddComponent<RawImage>();
            v.goruntu.raycastTarget = false;
            v.goruntu.enabled = false;   // ilk kare gelene kadar görünmez: duran görsel kalır

            var p = go.AddComponent<VideoPlayer>();
            v.vp = p;
            p.playOnAwake = false;
            p.isLooping = false;
            p.skipOnDrop = true;
            p.waitForFirstFrame = true;
            p.source = VideoSource.Url;
            p.url = yol;
            p.audioOutputMode = VideoAudioOutputMode.None;   // sessiz
            p.renderMode = VideoRenderMode.RenderTexture;     // WebGL dahil her platformda desteklenen çıkış
            p.sendFrameReadyEvents = true;
            p.errorReceived += (pl, m) => { if (pl == v.vp) { Debug.LogWarning("KizilTohum: sahne videosu oynatılamadı (" + v.dosya + "): " + m); v.Basarisiz(); } };
            p.prepareCompleted += pl =>
            {
                if (pl != v.vp || v.bitti) return;
                if (pl.width == 0 || pl.height == 0) { Debug.LogWarning("KizilTohum: sahne videosu boyutu okunamadı: " + v.dosya); v.Basarisiz(); return; }
                v.rt = new RenderTexture((int)pl.width, (int)pl.height, 0);
                pl.targetTexture = v.rt;
                v.goruntu.texture = v.rt;
                pl.Play();
            };
            p.frameReady += (pl, kare) => { if (pl == v.vp && v.goruntu != null && !v.goruntu.enabled) v.goruntu.enabled = true; };
            p.loopPointReached += pl => { if (pl == v.vp) pl.Pause(); };   // son karede dur; sprite'a geçme
            return v;
        }

        // Etkin olmayan VideoPlayer hazırlanamaz: görev menüden/haritadan açılırken ekran kapalıdır, etkinleşince başlar.
        void OnEnable() { Baslat(); }

        // Kur() içinde bileşen VideoPlayer'dan önce eklendiği için OnEnable ilk çağrıda vp henüz null olabilir; Update yeniden dener.
        void Baslat()
        {
            if (basladi || bitti || vp == null || !isActiveAndEnabled) return;
            basladi = true;
            baslangic = Time.unscaledTime;
            vp.Prepare();
        }

        void Update()
        {
            if (!basladi) Baslat();
            if (basladi && !bitti && vp != null && !vp.isPrepared && Time.unscaledTime - baslangic > Tema.VideoHazirlikSiniri)
            {
                Debug.LogWarning("KizilTohum: sahne videosu zamanında hazırlanamadı: " + dosya);
                Basarisiz();
            }
        }

        void Basarisiz()
        {
            if (bitti) return;
            bitti = true;
            if (vp != null) vp.Stop();
            var h = hata; hata = null;
            Destroy(gameObject);   // duran görsel kalır
            if (h != null) h();
        }

        void OnDestroy()
        {
            bitti = true;
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
        }
    }
}
