using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>
    /// Bekçi Defteri: yalnızca okuma ekranı. Oyuncunun keşfettiği kavram kartlarını (KartKumesi) defter görseli
    /// üzerinde, kareli sayfa alanının içinde sayfa sayfa gösterir. Her kaydın arkasında sayfa renginde yarı saydam
    /// zemin vardır; yazı kareli çizgileri kesmez. Arka plan Bekci_Defteri/defter_sayfasi (yoksa defter_sayfa_bos).
    /// Başlık, sayaç ve sayfa düğmeleri defterin yanındaki boş masa alanındadır.
    /// </summary>
    public class DefterEkrani : MonoBehaviour
    {
        const int Satir = 4, Sayfada = Satir;

        // kareli sayfa alanı (defter_sayfasi.png üzerinde, 1920x1080'e göre normalize; y aşağı doğru)
        const float SayfaX0 = 0.255f, SayfaX1 = 0.615f, SayfaY0 = 0.19f, SayfaY1 = 0.80f;

        public Action GeriIstendi;
        public bool Acik { get { return kok != null && kok.gameObject.activeSelf; } }

        GorevYoneticisi y;
        Font font, fontKalin, fontBaslik;
        RectTransform kok, alan;
        Text sayac, sayfaYazi, bos;
        Button onceki, sonraki;
        readonly List<Kart> kartlar = new List<Kart>();
        int sayfa;
        bool kareliSayfa;

        public void Kur(RectTransform ust, Font f, Font fk, Font fb, GorevYoneticisi yonetici)
        {
            y = yonetici; font = f; fontKalin = fk; fontBaslik = fb;
            var go = Bos("Defter", ust);
            kok = (RectTransform)go.transform;
            Doldur(kok);
            var zemin = go.AddComponent<Image>();   // arkadaki arayüzü tıklamaya kapatır
            zemin.color = Tema.Base;

            var sp = Resources.Load<Sprite>("Gorseller/Defter/defter_sayfasi");
            kareliSayfa = sp != null;
            if (sp == null) sp = Resources.Load<Sprite>("Gorseller/Bekci_Defteri/defter_sayfa_bos");
            if (sp != null)
            {
                var im = Yuz("Sayfa", kok, Color.white);
                im.sprite = sp; im.raycastTarget = false;
                Doldur(im.rectTransform);
                var oran = im.gameObject.AddComponent<AspectRatioFitter>();   // oranı koruyarak ekranı kaplar
                oran.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                oran.aspectRatio = sp.rect.width / Mathf.Max(1f, sp.rect.height);
            }

            // sağdaki boş masa alanı: ikon, başlık, sayaç, sayfa düğmeleri
            var ikon = Resources.Load<Sprite>("Gorseller/Ikonlar/ikon_defter");
            if (ikon != null)
            {
                var ii = Yuz("Ikon", kok, Color.white);
                ii.sprite = ikon; ii.preserveAspect = true; ii.raycastTarget = false;
                Konum(ii.rectTransform, 0.72f, 0.64f, 0.90f, 0.86f);
            }
            var baslik = Yazi("Baslik", kok, Tema.YaziBaslik, Tema.Marka, TextAnchor.MiddleCenter, fontBaslik);
            baslik.text = "BEKÇİ DEFTERİ";
            Konum(baslik.rectTransform, 0.64f, 0.52f, 0.98f, 0.62f);
            sayac = Yazi("Sayac", kok, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            Konum(sayac.rectTransform, 0.64f, 0.44f, 0.98f, 0.51f);

            alan = (RectTransform)Bos("Alan", kok).transform;
            if (kareliSayfa) Konum(alan, SayfaX0, 1f - SayfaY1, SayfaX1, 1f - SayfaY0);
            else Konum(alan, 0.07f, 0.12f, 0.93f, 0.87f);
            bos = Yazi("Bos", kok, Tema.YaziOrta, Tema.DefterMurekkep, TextAnchor.MiddleCenter, font);
            bos.text = "Henüz kayıt yok.\nSoruları yanıtladıkça öğrendiğin kurallar buraya yazılır.";
            if (kareliSayfa) Konum(bos.rectTransform, SayfaX0 + 0.02f, 0.40f, SayfaX1 - 0.02f, 0.60f);
            else Konum(bos.rectTransform, 0.2f, 0.35f, 0.8f, 0.65f);

            var geri = DugmeYap("Geri", "‹ Geri", 0.02f, 0.89f, 0.14f, 0.97f);
            geri.onClick.AddListener(() => { if (GeriIstendi != null) GeriIstendi(); });
            onceki = DugmeYap("Onceki", "‹", 0.72f, 0.30f, 0.78f, 0.37f);
            sonraki = DugmeYap("Sonraki", "›", 0.84f, 0.30f, 0.90f, 0.37f);
            onceki.onClick.AddListener(() => { sayfa = Mathf.Max(0, sayfa - 1); Ciz(); });
            sonraki.onClick.AddListener(() => { sayfa++; Ciz(); });
            sayfaYazi = Yazi("SayfaNo", kok, Tema.YaziKucuk, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            Konum(sayfaYazi.rectTransform, 0.78f, 0.30f, 0.84f, 0.37f);

            go.SetActive(false);
        }

        public void Ac()
        {
            KartKumesi.Birlestir(y.AcilanKartlar);
            var set = KartKumesi.Oku();
            kartlar.Clear();
            if (y.Icerik != null)
                for (int i = 0; i < y.Icerik.kartlar.Count; i++)
                    if (set.Contains(y.Icerik.kartlar[i].id)) kartlar.Add(y.Icerik.kartlar[i]);
            sayfa = 0;
            kok.gameObject.SetActive(true);
            kok.SetAsLastSibling();
            Ciz();
        }

        public void Kapat() { if (kok != null) kok.gameObject.SetActive(false); }

        void Ciz()
        {
            for (int i = alan.childCount - 1; i >= 0; i--) Destroy(alan.GetChild(i).gameObject);
            int toplam = y.Icerik != null ? y.Icerik.kartlar.Count : 14;
            sayac.text = "Keşfedilen: " + kartlar.Count + "/" + toplam;
            int sayfaSayisi = Mathf.Max(1, Mathf.CeilToInt(kartlar.Count / (float)Sayfada));
            sayfa = Mathf.Clamp(sayfa, 0, sayfaSayisi - 1);
            bos.gameObject.SetActive(kartlar.Count == 0);
            sayfaYazi.text = (sayfa + 1) + "/" + sayfaSayisi;
            onceki.gameObject.SetActive(sayfa > 0);
            sonraki.gameObject.SetActive(sayfa < sayfaSayisi - 1);
            sayfaYazi.gameObject.SetActive(sayfaSayisi > 1);

            float yuk = 1f / Satir;
            for (int i = 0; i < Sayfada; i++)
            {
                int ix = sayfa * Sayfada + i;
                if (ix >= kartlar.Count) break;
                var k = kartlar[ix];
                var hucre = (RectTransform)Bos("Kayit" + ix, alan).transform;
                Konum(hucre, 0f, 1f - (i + 1) * yuk, 1f, 1f - i * yuk);
                // sayfa renginde yarı saydam zemin: kareli çizgiler yazının arkasında kaybolur
                var zm = Yuz("Zemin", hucre, Tema.DefterZemin);
                zm.raycastTarget = false;
                Konum(zm.rectTransform, 0f, 0f, 1f, 1f);
                zm.rectTransform.offsetMin = new Vector2(4f, 4f); zm.rectTransform.offsetMax = new Vector2(-4f, -4f);
                var t = Yazi("Baslik", hucre, Tema.YaziOrta, Tema.DefterBaslik, TextAnchor.UpperLeft, fontKalin);
                t.text = k.baslik;
                Konum(t.rectTransform, 0f, 0.74f, 1f, 1f);
                t.rectTransform.offsetMin = new Vector2(14f, 0f); t.rectTransform.offsetMax = new Vector2(-14f, -6f);
                var m = Yazi("Metin", hucre, Tema.YaziKucuk, Tema.DefterMurekkep, TextAnchor.UpperLeft, font);
                m.text = k.metin;
                Konum(m.rectTransform, 0f, 0f, 1f, 0.74f);
                m.rectTransform.offsetMin = new Vector2(14f, 6f); m.rectTransform.offsetMax = new Vector2(-14f, 0f);
            }
        }

        Button DugmeYap(string ad, string yazi, float x0, float y0, float x1, float y1)
        {
            var im = Yuz(ad, kok, Tema.Kontrol);
            var sp = Resources.Load<Sprite>("Gorseller/Arayuz/secenek_butonu_normal_9slice");
            if (sp != null) { im.sprite = sp; im.type = Image.Type.Sliced; im.color = Color.white; }
            Konum(im.rectTransform, x0, y0, x1, y1);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            var t = Yazi("Yazi", im.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            t.text = yazi;
            Doldur(t.rectTransform);
            return b;
        }

        // ---------- küçük yardımcılar ----------
        static GameObject Bos(string ad, Transform ata)
        {
            var g = new GameObject(ad, typeof(RectTransform));
            g.transform.SetParent(ata, false);
            return g;
        }

        static Image Yuz(string ad, Transform ata, Color c)
        {
            var im = Bos(ad, ata).AddComponent<Image>();
            im.color = c;
            return im;
        }

        Text Yazi(string ad, Transform ata, int boy, Color c, TextAnchor hiza, Font fnt)
        {
            var t = Bos(ad, ata).AddComponent<Text>();
            t.font = fnt != null ? fnt : font; t.fontSize = boy; t.color = c; t.alignment = hiza;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static void Doldur(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void Konum(RectTransform r, float ax0, float ay0, float ax1, float ay1)
        {
            r.anchorMin = new Vector2(ax0, ay0); r.anchorMax = new Vector2(ax1, ay1);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
