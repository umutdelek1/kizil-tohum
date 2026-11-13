using System;
using System.Collections.Generic;

namespace KizilTohum
{
    // ---- Osmoz kaydırıcısı -------------------------------------------------
    public struct OsmozSonucu
    {
        public float oran;            // dış / iç derişim
        public string durum;          // turgorlu | gevsek | plazmoliz_basliyor | plazmolize
        public float plazmoliz;       // 0..1
        public float turgor;          // 0..1
        public float protoplastOlcegi;// 1 -> 0.55
        public int suYonu;            // +1 içeri, -1 dışarı, 0 net hareket yok
        public float ceperOlcegi;     // her zaman 1 (çeper hareket etmez)
        public string cumle;
    }

    public static class Osmoz
    {
        public const float Min = 0.2f;
        public const float Max = 3.0f;
        public const float TurgorEsigi = 0.95f;       // altında: turgorlu
        public const float PlazmolizBaslangic = 1.3f; // üstünde: plazmoliz başlıyor
        public const float PlazmolizEsigi = 1.8f;     // üstünde: plazmolize
        public const float TamPlazmoliz = 2.0f;

        public static OsmozSonucu Hesapla(float oran)
        {
            if (oran < Min) oran = Min;
            if (oran > Max) oran = Max;
            var s = new OsmozSonucu();
            s.oran = oran;
            s.ceperOlcegi = 1f;

            s.turgor = oran < 1f ? Clamp01((1f - oran) / 0.8f) : 0f;
            s.plazmoliz = Clamp01((oran - PlazmolizBaslangic) / (TamPlazmoliz - PlazmolizBaslangic));
            s.protoplastOlcegi = 1f - 0.45f * s.plazmoliz;

            if (Math.Abs(oran - 1f) < 0.01f) s.suYonu = 0;
            else s.suYonu = oran < 1f ? 1 : -1;

            if (oran < TurgorEsigi)
            {
                s.durum = "turgorlu";
                s.cumle = "Hücre su alıyor ve gerginleşiyor; çeper patlamayı engelliyor.";
            }
            else if (oran < PlazmolizBaslangic)
            {
                s.durum = "gevsek";
                s.cumle = oran < 1.01f && oran > 0.99f
                    ? "Derişimler eşit: net su hareketi yok (su iki yönde de geçer). Hücre gevşek."
                    : "Hücre gevşek; zar hâlâ çepere yaslı.";
            }
            else if (oran < PlazmolizEsigi)
            {
                s.durum = "plazmoliz_basliyor";
                s.cumle = "Su dışarı çıkıyor; zar köşelerden çeperden ayrılmaya başladı.";
            }
            else
            {
                s.durum = "plazmolize";
                s.cumle = "Zar ve sitoplazma çeperden ayrıldı. Çeper yerinde duruyor.";
            }
            return s;
        }

        public static bool HedefteMi(float oran, float hedefMin, float hedefMax)
        {
            return oran >= hedefMin && oran <= hedefMax;
        }

        static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
    }

    // ---- CO2 grafiği ------------------------------------------------------
    public static class Co2
    {
        public const int NoktaSayisi = 24;
        public const float Baslangic = 420f;   // ppm (temsili)
        public const float GrafikTavani = 3000f;

        /// <summary>Canlı tohum kavanozu: hızlı yükselir.</summary>
        public static float[] Canli()
        {
            var d = new float[NoktaSayisi];
            for (int i = 0; i < NoktaSayisi; i++)
                d[i] = Baslangic + 2400f * (1f - (float)Math.Exp(-i / 8.0));
            return d;
        }

        /// <summary>Kaynatılmış tohum kavanozu: geç başlar ama o da yükselir (başka bir CO2 kaynağı).</summary>
        public static float[] Kontrol()
        {
            var d = new float[NoktaSayisi];
            for (int i = 0; i < NoktaSayisi; i++)
            {
                float t = i - 6f; if (t < 0f) t = 0f;
                d[i] = Baslangic + 1500f * (1f - (float)Math.Exp(-t / 8.0));
            }
            return d;
        }

        /// <summary>ppm değerini 0..1 aralığına çevirir (grafik çizimi için).</summary>
        public static float Normalize(float ppm)
        {
            float v = (ppm - Baslangic) / (GrafikTavani - Baslangic);
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }

    // ---- Punnett ----------------------------------------------------------
    public struct PunnettSonucu
    {
        public string ebeveynA, ebeveynB;
        public string[] gametlerA;     // satırlar
        public string[] gametlerB;     // sütunlar
        public string[] genotipler;    // 4 kutu, satır sırasıyla (A satırı x B sütunu)
        public bool[] direncli;        // aa olan kutular
        public int direncliSayisi;
        public int toplam;
    }

    public static class Punnett
    {
        public static PunnettSonucu Hesapla(string a, string b)
        {
            var s = new PunnettSonucu();
            s.ebeveynA = a; s.ebeveynB = b;
            s.gametlerA = new[] { a.Substring(0, 1), a.Substring(1, 1) };
            s.gametlerB = new[] { b.Substring(0, 1), b.Substring(1, 1) };
            s.genotipler = new string[4];
            s.direncli = new bool[4];
            s.toplam = 4;
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 2; c++)
                {
                    string g = Sirala(s.gametlerA[r], s.gametlerB[c]);
                    int i = r * 2 + c;
                    s.genotipler[i] = g;
                    s.direncli[i] = (g == "aa");
                    if (g == "aa") s.direncliSayisi++;
                }
            return s;
        }

        // Büyük harf (baskın) önce: "aA" -> "Aa".
        static string Sirala(string x, string y)
        {
            bool xBuyuk = char.IsUpper(x[0]);
            bool yBuyuk = char.IsUpper(y[0]);
            if (!xBuyuk && yBuyuk) return y + x;
            return x + y;
        }
    }

    // ---- F2 fideleri ------------------------------------------------------
    public static class F2
    {
        public const int Toplam = 16;
        // Hikâyede sabit: 3., 10. ve 14. fide dirençli (0 tabanlı indeks 2, 9, 13).
        static readonly int[] DirencliIndeksler = { 2, 9, 13 };

        public static bool DirencliMi(int indeks)
        {
            for (int i = 0; i < DirencliIndeksler.Length; i++) if (DirencliIndeksler[i] == indeks) return true;
            return false;
        }

        public static int[] Direncliler() { return (int[])DirencliIndeksler.Clone(); }
        public static int Satir(int indeks) { return indeks / 4; }
        public static int Sutun(int indeks) { return indeks % 4; }
    }

    // ---- Arayüze giden simülasyon paketi ---------------------------------
    public class SimBilgisi
    {
        public string tur;      // co2 | osmoz | punnett | f2
        public string deger;    // canli | kontrol | 16 ...
        public float[] canliEgri;
        public float[] kontrolEgri;
        public OsmozSonucu osmoz;
        public bool hedefeUlasti;
        public PunnettSonucu punnett;
        public int[] direncliIndeksler;
        public int toplamFide;
        public string notMetni;
    }
}
