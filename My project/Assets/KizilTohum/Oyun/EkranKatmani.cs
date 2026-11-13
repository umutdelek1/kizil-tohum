using UnityEngine;

namespace KizilTohum
{
    /// <summary>
    /// Pencere 16:9'dan geniş ya da dar olduğunda siyah bant bırakmayan iki katman düzeni için tek bileşen.
    /// Üst RectTransform'un (canvas ya da ekran kökü) oranına bakıp kendi çapalarını ayarlar; içeride çalışan
    /// tüm kod 16:9 tasarım koordinatlarıyla çalışmaya devam eder.
    ///  · Kapla: 16:9 çerçeve, ekranı TAMAMEN kaplayacak kadar büyür (en-boy oranı korunur, taşan kısım kırpılır).
    ///           Arka planlar, sahne görselleri, harita, final arka planı buraya konur.
    ///  · Guvenli: 16:9 çerçeve, ekranın içine SIĞAR ve ortalanır. Diyalog, seçenekler, paneller, düğmeler buraya konur.
    ///  · Yatay: yalnızca güvenli genişliği alır, yüksekliği tam kullanır (üst çubuğun iç kısmı).
    ///  · SolPanel: ekranın sol kenarından, güvenli alanın "panelGenislik" oranına kadar (menü gradient paneli).
    /// CanvasScaler Expand olduğundan güvenli çerçeve her pencerede tam 1920x1080 canvas birimidir.
    /// </summary>
    public class EkranKatmani : MonoBehaviour
    {
        public enum Mod { Kapla, Guvenli, Yatay, SolPanel }

        public const float TasarimOrani = 16f / 9f;

        public Mod mod = Mod.Guvenli;
        public float panelGenislik = 0.32f;

        RectTransform rt, kokCanvas;
        Vector2 sonBoyut = new Vector2(-1f, -1f);

        public static RectTransform Olustur(string ad, Transform ata, Mod mod, float panelGenislik = 0.32f)
        {
            var g = new GameObject(ad, typeof(RectTransform));
            g.transform.SetParent(ata, false);
            var k = g.AddComponent<EkranKatmani>();
            k.mod = mod; k.panelGenislik = panelGenislik;
            var r = (RectTransform)g.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            k.Uygula(true);
            return r;
        }

        void OnEnable() { Uygula(true); }
        void Update() { Uygula(false); }

        /// <summary>Güvenli çerçevenin ekrana göre oranı (genişlik, yükseklik) — testler ve kod için.</summary>
        public static Vector2 GuvenliOran(float ekranOrani)
        {
            return ekranOrani > TasarimOrani ? new Vector2(TasarimOrani / ekranOrani, 1f) : new Vector2(1f, ekranOrani / TasarimOrani);
        }

        void Uygula(bool zorla)
        {
            if (rt == null) rt = transform as RectTransform;
            var p = rt != null ? rt.parent as RectTransform : null;
            if (p == null) return;
            // oran, üst RectTransform'a değil canvas köküne göre alınır (üst çubuğun iç katmanı ince bir şeridin çocuğudur)
            if (kokCanvas == null) { var c = GetComponentInParent<Canvas>(); if (c != null) kokCanvas = c.rootCanvas.transform as RectTransform; }
            Vector2 boyut = kokCanvas != null ? kokCanvas.rect.size : p.rect.size;
            if (boyut.x < 1f || boyut.y < 1f) return;
            if (!zorla && (boyut - sonBoyut).sqrMagnitude < 0.01f) return;
            sonBoyut = boyut;

            float oran = boyut.x / boyut.y;
            Vector2 g = GuvenliOran(oran);          // sığan çerçeve (0..1)
            Vector2 min, max;
            switch (mod)
            {
                case Mod.Kapla:
                    {
                        // kaplayan çerçeve: en az ekran kadar, oranı 16:9
                        Vector2 c = oran > TasarimOrani ? new Vector2(1f, oran / TasarimOrani) : new Vector2(TasarimOrani / oran, 1f);
                        min = new Vector2(0.5f - c.x * 0.5f, 0.5f - c.y * 0.5f);
                        max = new Vector2(0.5f + c.x * 0.5f, 0.5f + c.y * 0.5f);
                        break;
                    }
                case Mod.Yatay:
                    min = new Vector2(0.5f - g.x * 0.5f, 0f); max = new Vector2(0.5f + g.x * 0.5f, 1f);
                    break;
                case Mod.SolPanel:
                    min = new Vector2(0f, 0f); max = new Vector2((0.5f - g.x * 0.5f) + g.x * panelGenislik, 1f);
                    break;
                default:
                    min = new Vector2(0.5f - g.x * 0.5f, 0.5f - g.y * 0.5f);
                    max = new Vector2(0.5f + g.x * 0.5f, 0.5f + g.y * 0.5f);
                    break;
            }
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
