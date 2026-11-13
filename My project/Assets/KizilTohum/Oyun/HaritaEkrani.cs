using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>
    /// Tamamlanan görevler. Mevcut oyun kaydı yalnızca etkin görevi tuttuğu için görev ilerlemesi
    /// ayrı bir PlayerPrefs anahtarında durur (virgülle ayrılmış görev id'leri). Oyun mantığına dokunmaz.
    /// </summary>
    public static class HaritaKayit
    {
        const string Anahtar = "kizil_tohum_harita_tamam_v1";

        public static HashSet<string> Oku()
        {
            var k = new HashSet<string>();
            string s = PlayerPrefs.GetString(Anahtar, "");
            if (!string.IsNullOrEmpty(s))
            {
                var parca = s.Split(',');
                for (int i = 0; i < parca.Length; i++) if (parca[i].Length > 0) k.Add(parca[i]);
            }
            return k;
        }

        public static void Isaretle(string gorevId)
        {
            if (string.IsNullOrEmpty(gorevId)) return;
            var k = Oku();
            if (!k.Add(gorevId)) return;
            PlayerPrefs.SetString(Anahtar, string.Join(",", new List<string>(k).ToArray()));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Koloni Haritası: tam ekran harita, tamamlanan göreve göre gündüz/gün batımı/gece, görev noktaları,
    /// üzerine gelince bilgi paneli, tıklayınca noktaya dalış ve görev başlangıcı.
    /// KizilTohumArayuz yalnızca Kur/Ac/Kapat ve olayları bağlar.
    /// </summary>
    public class HaritaEkrani : MonoBehaviour
    {
        enum Durum { Acik, Tamam, Kilitli }

        class Nokta
        {
            public Tema.HaritaNoktaTanimi t;
            public RectTransform kok, govde, nabiz;
            public Image nabizGorsel;
            public GameObject acikGorsel, tamamGorsel, kilitliGorsel;
            public Durum durum;
            public bool hover;
            public float olcek = 1f;
        }

        public Action<string> GorevSecildi;   // görev id; arayüz haritayı kapatıp görevi başlatır
        public Action MenuIstendi;
        /// <summary>(dosya, bitince(hata)) → video başladıysa true. Yoksa/başlamadıysa false: kod dalışı oynar.</summary>
        public Func<string, Action<bool>, bool> VideoOynat;

        public bool Acik { get { return kok != null && kok.gameObject.activeSelf; } }
        public RectTransform Kok { get { return kok; } }   // video gibi üst katmanlar bunun altına konur

        GorevYoneticisi y;
        Font font, fontKalin, fontBaslik;
        RectTransform kok, dunya, bilgi;
        Image arkaA, arkaB, karart;
        Text sayac, bilgiYer, bilgiGorev;
        Canvas canvas;
        readonly List<Nokta> noktalar = new List<Nokta>();
        Nokta bilgiNokta;

        int gosterilenAsama = -1, hedefAsama;
        float caprazT = 999f;
        Vector2 paralaks;
        bool mesgul;
        float dalisT = -1f;
        Nokta dalisNokta;

        static Sprite daireSprite, halkaSprite;

        // ---------- kurulum ----------
        public void Kur(RectTransform ust, Font f, Font fk, Font fb, GorevYoneticisi yonetici)
        {
            y = yonetici; font = f; fontKalin = fk; fontBaslik = fb;
            canvas = ust.GetComponentInParent<Canvas>();

            var go = Bos("Harita", ust);
            kok = (RectTransform)go.transform;
            Doldur(kok);
            var engel = go.AddComponent<Image>();   // arkadaki arayüzü tıklamaya kapatır
            engel.color = Tema.Base;

            // dünya: sürüklenen / dalışta yakınlaşan katman
            dunya = (RectTransform)Bos("Dunya", kok).transform;
            Doldur(dunya);
            arkaA = Yuz("ArkaA", dunya, Tema.Base); Doldur(arkaA.rectTransform); arkaA.raycastTarget = false;
            arkaB = Yuz("ArkaB", dunya, Tema.Saydam); Doldur(arkaB.rectTransform); arkaB.raycastTarget = false;

            var tanimlar = Tema.HaritaNoktalari;
            for (int i = 0; i < tanimlar.Length; i++) NoktaKur(tanimlar[i]);

            // bilgi paneli (dünyanın dışında: yakınlaşınca büyümez)
            var bg = Yuz("Bilgi", kok, Tema.PanelHafif);
            bilgi = bg.rectTransform;
            var pn = Spr("Arayuz/diyalog_paneli_9slice");
            if (pn != null) { bg.sprite = pn; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            bg.raycastTarget = false;
            bilgi.anchorMin = bilgi.anchorMax = new Vector2(0.5f, 0.5f);
            bilgi.sizeDelta = new Vector2(Tema.HaritaBilgiGenislik, Tema.HaritaBilgiYukseklik);
            bilgiYer = Yazi("Yer", bilgi, Tema.YaziOrta, Tema.Konusan, TextAnchor.MiddleLeft, fontKalin);
            Konum(bilgiYer.rectTransform, 0, 0.5f, 1, 0.9f, Tema.MetinSag, 0, -Tema.MetinSag, 0);
            bilgiGorev = Yazi("Gorev", bilgi, Tema.YaziKucuk, Tema.Beyaz, TextAnchor.MiddleLeft, font);
            Konum(bilgiGorev.rectTransform, 0, 0.1f, 1, 0.5f, Tema.MetinSag, 0, -Tema.MetinSag, 0);
            bilgi.gameObject.SetActive(false);

            // üst çubuk
            var cubuk = Yuz("Ust", kok, Tema.Panel);
            Konum(cubuk.rectTransform, 0, 1, 1, 1, 0, -90, 0, 0);
            cubuk.raycastTarget = false;
            sayac = Yazi("Sayac", cubuk.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleLeft, fontKalin);
            Konum(sayac.rectTransform, 0, 0, 0.3f, 1, 30, 0, 0, 0);
            var baslik = Yazi("Baslik", cubuk.transform, Tema.YaziBaslik, Tema.Marka, TextAnchor.MiddleCenter, fontBaslik);
            baslik.text = Tema.HaritaBaslik;
            Konum(baslik.rectTransform, 0.3f, 0, 0.7f, 1, 0, 0, 0, 0);
            var mb = Yuz("MenuBtn", cubuk.transform, Tema.KontrolAlt);
            var ms = Spr("Arayuz/secenek_butonu_normal_9slice");
            if (ms != null) { mb.sprite = ms; mb.type = Image.Type.Sliced; mb.color = Color.white; }
            Konum(mb.rectTransform, 0.89f, 0.2f, 0.99f, 0.8f, 0, 0, 0, 0);
            var btn = mb.gameObject.AddComponent<Button>();
            btn.targetGraphic = mb;
            var mt = Yazi("Yazi", mb.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            mt.text = "Menü";
            Doldur(mt.rectTransform);
            btn.onClick.AddListener(() => { if (MenuIstendi != null) MenuIstendi(); });

            // dalış karartması en üstte
            karart = Yuz("Karart", kok, Tema.Saydam);
            Doldur(karart.rectTransform);
            karart.raycastTarget = false;

            go.SetActive(false);
        }

        void NoktaKur(Tema.HaritaNoktaTanimi t)
        {
            var n = new Nokta { t = t };
            var go = Bos("Nokta_" + t.id, dunya);
            n.kok = (RectTransform)go.transform;
            n.kok.anchorMin = n.kok.anchorMax = new Vector2(t.x, 1f - t.y);
            n.kok.sizeDelta = new Vector2(Tema.HaritaNoktaBoyu, Tema.HaritaNoktaBoyu);
            n.kok.anchoredPosition = Vector2.zero;
            var vurus = go.AddComponent<Image>();   // şeffaf tıklama alanı
            vurus.color = Tema.Saydam;
            var olay = go.AddComponent<HaritaNoktasi>();
            olay.Girdi = () => { n.hover = true; BilgiGoster(n); };
            olay.Cikti = () => { n.hover = false; if (bilgiNokta == n) BilgiGizle(); };
            olay.Tiklandi = () => Tikla(n);

            n.govde = (RectTransform)Bos("Govde", n.kok).transform;
            Doldur(n.govde);

            // nabız halkası (yalnızca açık noktalarda görünür)
            var nb = Yuz("Nabiz", n.govde, Tema.Kehribar);
            nb.sprite = Halka(); nb.raycastTarget = false;
            Ortala(nb.rectTransform, Tema.HaritaNoktaBoyu * 0.55f);
            n.nabiz = nb.rectTransform; n.nabizGorsel = nb;

            // açık: kehribar daire + halka (sprite varsa pin_acik)
            n.acikGorsel = Bos("Acik", n.govde);
            Doldur((RectTransform)n.acikGorsel.transform);
            var pa = Spr("Harita/pin_acik");
            if (pa != null) Pin(n.acikGorsel.transform, pa);
            else
            {
                Disk(n.acikGorsel.transform, Tema.Kehribar, Tema.HaritaNoktaBoyu * 0.32f);
                Halkaci(n.acikGorsel.transform, Tema.Kehribar, Tema.HaritaNoktaBoyu * 0.55f);
            }

            // tamamlandı: yeşil tik
            n.tamamGorsel = Bos("Tamam", n.govde);
            Doldur((RectTransform)n.tamamGorsel.transform);
            var pt = Spr("Harita/pin_tamam");
            if (pt != null) Pin(n.tamamGorsel.transform, pt);
            else
            {
                Disk(n.tamamGorsel.transform, Tema.Panel, Tema.HaritaNoktaBoyu * 0.6f);
                Halkaci(n.tamamGorsel.transform, Tema.Basari, Tema.HaritaNoktaBoyu * 0.6f);
                var tik = Yazi("Tik", n.tamamGorsel.transform, Tema.YaziBuyuk, Tema.Basari, TextAnchor.MiddleCenter, fontKalin);
                tik.text = "✓";
                Doldur(tik.rectTransform);
            }

            // kilitli: gri kilit
            n.kilitliGorsel = Bos("Kilitli", n.govde);
            Doldur((RectTransform)n.kilitliGorsel.transform);
            var pk = Spr("Harita/pin_kilitli");
            if (pk != null) Pin(n.kilitliGorsel.transform, pk);
            else
            {
                float b = Tema.HaritaNoktaBoyu;
                Disk(n.kilitliGorsel.transform, Tema.Panel, b * 0.6f);
                Halkaci(n.kilitliGorsel.transform, Tema.Soluk, b * 0.6f);
                var kanca = Halkaci(n.kilitliGorsel.transform, Tema.Soluk, b * 0.22f);
                kanca.rectTransform.anchoredPosition = new Vector2(0f, b * 0.09f);
                var gov = Yuz("Gov", n.kilitliGorsel.transform, Tema.Soluk);
                gov.raycastTarget = false;
                Ortala(gov.rectTransform, b * 0.26f);
                gov.rectTransform.sizeDelta = new Vector2(b * 0.26f, b * 0.19f);
                gov.rectTransform.anchoredPosition = new Vector2(0f, -b * 0.05f);
            }
            noktalar.Add(n);
        }

        // ---------- açma / kapama ----------
        public void Ac()
        {
            kok.gameObject.SetActive(true);
            mesgul = false; dalisT = -1f; dalisNokta = null;
            karart.color = Tema.Saydam;
            dunya.localScale = Vector3.one * Tema.HaritaTemelOlcek;
            BilgiGizle();

            var tam = HaritaKayit.Oku();
            for (int i = 0; i < noktalar.Count; i++)
            {
                var n = noktalar[i];
                n.durum = string.IsNullOrEmpty(n.t.gorevId) ? Durum.Kilitli : tam.Contains(n.t.gorevId) ? Durum.Tamam : Durum.Acik;
                n.acikGorsel.SetActive(n.durum == Durum.Acik);
                n.tamamGorsel.SetActive(n.durum == Durum.Tamam);
                n.kilitliGorsel.SetActive(n.durum == Durum.Kilitli);
                n.nabizGorsel.enabled = n.durum == Durum.Acik;
                n.hover = false; n.olcek = 1f;
                n.govde.localScale = Vector3.one;
            }

            // arka plan: 0 → gündüz, 1 → gün batımı, 2+ → gece; değişimde 1.5 sn çapraz erime
            int asama = Mathf.Min(tam.Count, Tema.HaritaResimleri.Length - 1);
            if (gosterilenAsama < 0)
            {
                ZeminAyarla(arkaA, asama, 1f);
                ZeminAyarla(arkaB, asama, 0f);
                gosterilenAsama = asama; hedefAsama = asama; caprazT = 999f;
            }
            else if (asama != gosterilenAsama)
            {
                ZeminAyarla(arkaA, gosterilenAsama, 1f);
                ZeminAyarla(arkaB, asama, 0f);
                hedefAsama = asama; caprazT = 0f;
            }

            if (y != null)
            {
                int toplam = y.Icerik != null ? y.Icerik.kartlar.Count : 14;
                sayac.text = "Kavram kartları: " + y.AcilanKartlar.Count + "/" + toplam;
            }
        }

        public void Kapat()
        {
            BilgiGizle();
            if (kok != null) kok.gameObject.SetActive(false);
        }

        public void GorevTamamlandi(string gorevId) { HaritaKayit.Isaretle(gorevId); }

        // ---------- döngü ----------
        void Update()
        {
            if (!Acik) return;
            float dt = Time.unscaledDeltaTime;
            float simdi = Time.unscaledTime;

            // çapraz erime
            if (caprazT < Tema.HaritaCaprazSure)
            {
                caprazT += dt;
                float f = Mathf.SmoothStep(0f, 1f, caprazT / Tema.HaritaCaprazSure);
                var c = arkaB.color; c.a = f; arkaB.color = c;
                if (caprazT >= Tema.HaritaCaprazSure)
                {
                    ZeminAyarla(arkaA, hedefAsama, 1f);
                    ZeminAyarla(arkaB, hedefAsama, 0f);
                    gosterilenAsama = hedefAsama;
                }
            }

            // kendi kendine sürüklenme (±12 px) + fare paralaksı (±20 px)
            var fare = UnityEngine.InputSystem.Mouse.current;
            Vector2 hedef = Vector2.zero;
            if (fare != null)
            {
                Vector2 p = fare.position.ReadValue();
                hedef = -new Vector2(Mathf.Clamp(p.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f),
                                     Mathf.Clamp(p.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f)) * Tema.HaritaParalaks;
            }
            paralaks = Vector2.Lerp(paralaks, hedef, dt * Tema.Takip);
            Vector2 surukle = new Vector2(Mathf.Sin(simdi * Tema.HaritaSurukleHiz), Mathf.Cos(simdi * Tema.HaritaSurukleHiz * 0.77f)) * Tema.HaritaSurukle;
            Vector2 konum = paralaks + surukle;
            float olcek = Tema.HaritaTemelOlcek;

            // dalış: tıklanan noktaya yakınlaş, sonra karar
            if (dalisT >= 0f && dalisNokta != null)
            {
                dalisT += dt;
                float t = Mathf.Clamp01(dalisT / Tema.HaritaDalisSure);
                float e = Mathf.SmoothStep(0f, 1f, t);
                olcek = Tema.HaritaTemelOlcek * Mathf.Lerp(1f, Tema.HaritaDalisOlcek, e);
                Rect r = dunya.rect;
                Vector2 pn = new Vector2((dalisNokta.t.x - 0.5f) * r.width, (0.5f - dalisNokta.t.y) * r.height);
                Vector2 yeni = Vector2.Lerp(pn * Tema.HaritaTemelOlcek, Vector2.zero, e);
                Vector2 T = yeni - pn * olcek;
                float sx = (olcek - 1f) * r.width * 0.5f, sy = (olcek - 1f) * r.height * 0.5f; // kenarlar boş görünmesin
                T.x = Mathf.Clamp(T.x, -sx, sx); T.y = Mathf.Clamp(T.y, -sy, sy);
                konum += T;
                var kc = Tema.Base; kc.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Tema.HaritaKararmaBasi, 1f, t));
                karart.color = kc;
                if (t >= 1f)
                {
                    var n = dalisNokta; dalisT = -1f; dalisNokta = null;
                    Basla(n);
                }
            }
            dunya.anchoredPosition = konum;
            dunya.localScale = Vector3.one * olcek;

            // noktalar: hover büyümesi + nabız
            float nabizF = (simdi % Tema.HaritaNabizSure) / Tema.HaritaNabizSure;
            for (int i = 0; i < noktalar.Count; i++)
            {
                var n = noktalar[i];
                float h = n.hover && !mesgul ? Tema.HaritaHoverOlcek : 1f;
                n.olcek = Mathf.Lerp(n.olcek, h, 1f - Mathf.Exp(-Tema.HaritaOlcekHiz * dt));
                n.govde.localScale = Vector3.one * n.olcek;
                if (n.durum == Durum.Acik && n.nabiz != null)
                {
                    float boy = Tema.HaritaNoktaBoyu * 0.55f * Mathf.Lerp(1f, Tema.HaritaNabizOlcek, nabizF);
                    n.nabiz.sizeDelta = new Vector2(boy, boy);
                    var c = Tema.Kehribar; c.a = (1f - nabizF) * 0.7f;
                    n.nabizGorsel.color = c;
                }
            }
            if (bilgiNokta != null && bilgi.gameObject.activeSelf) BilgiYerlestir(bilgiNokta);
        }

        // ---------- etkileşim ----------
        void Tikla(Nokta n)
        {
            if (mesgul || n.durum == Durum.Kilitli) return;
            mesgul = true;
            BilgiGizle();
            string dosya = "Harita_dalis_" + n.t.id + ".mp4";
            if (VideoOynat != null && VideoOynat(dosya, hata => { if (!Acik) return; if (hata) DalisBaslat(n); else Basla(n); })) return;
            DalisBaslat(n);
        }

        void DalisBaslat(Nokta n) { dalisNokta = n; dalisT = 0f; }

        void Basla(Nokta n)
        {
            if (GorevSecildi != null) GorevSecildi(n.t.gorevId);
        }

        void BilgiGoster(Nokta n)
        {
            if (mesgul) return;
            bilgiNokta = n;
            bilgiYer.text = n.t.yer;
            if (n.durum == Durum.Kilitli) { bilgiGorev.text = Tema.HaritaKapaliMetni; bilgiGorev.color = Tema.Soluk; }
            else
            {
                string ad = n.t.gorevId;
                if (y != null && y.Icerik != null)
                {
                    int gi = y.Icerik.GorevIndeksi(n.t.gorevId);
                    if (gi >= 0) ad = n.t.gorevId + " · " + y.Icerik.gorevler[gi].baslik;
                }
                bilgiGorev.text = ad + (n.durum == Durum.Tamam ? "  ✓" : "");
                bilgiGorev.color = n.durum == Durum.Tamam ? Tema.Basari : Tema.Beyaz;
            }
            bilgi.gameObject.SetActive(true);
            BilgiYerlestir(n);
        }

        void BilgiGizle()
        {
            bilgiNokta = null;
            if (bilgi != null) bilgi.gameObject.SetActive(false);
        }

        // Panel noktanın yanında: soldaki noktalarda sağda, sağdakilerde solda.
        void BilgiYerlestir(Nokta n)
        {
            bool sagda = n.t.x < 0.6f;
            bilgi.pivot = new Vector2(sagda ? 0f : 1f, 0.5f);
            float ol = canvas != null ? canvas.scaleFactor : 1f;
            Vector3 p = n.kok.position;
            p.x += (sagda ? 1f : -1f) * Tema.HaritaBilgiUzaklik * ol;
            bilgi.position = p;
        }

        // ---------- arka plan ----------
        void ZeminAyarla(Image im, int asama, float alfa)
        {
            Sprite sp = null;
            for (int i = Mathf.Min(asama, Tema.HaritaResimleri.Length - 1); i >= 0 && sp == null; i--)
                sp = Spr(Tema.HaritaResimleri[i]);
            im.sprite = sp;
            var c = sp != null ? Color.white : Tema.Base; // resim yoksa düz zemin
            c.a = alfa;
            im.color = c;
        }

        // ---------- çizim yardımcıları ----------
        static Sprite Spr(string yol) { return Resources.Load<Sprite>("Gorseller/" + yol); }

        static Sprite Disk() { if (daireSprite == null) daireSprite = Uret(false); return daireSprite; }
        static Sprite Halka() { if (halkaSprite == null) halkaSprite = Uret(true); return halkaSprite; }

        static Sprite Uret(bool halka)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float r = n * 0.5f - 1f, ic = r - 8f;
            var c = new Vector2(n * 0.5f, n * 0.5f);
            for (int yy = 0; yy < n; yy++)
                for (int xx = 0; xx < n; xx++)
                {
                    float d = Vector2.Distance(new Vector2(xx + 0.5f, yy + 0.5f), c);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (halka) a *= Mathf.Clamp01(d - ic + 0.5f);
                    tex.SetPixel(xx, yy, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        Image Disk(Transform ata, Color c, float boy)
        {
            var im = Yuz("Disk", ata, c);
            im.sprite = Disk(); im.raycastTarget = false;
            Ortala(im.rectTransform, boy);
            return im;
        }

        Image Halkaci(Transform ata, Color c, float boy)
        {
            var im = Yuz("Halka", ata, c);
            im.sprite = Halka(); im.raycastTarget = false;
            Ortala(im.rectTransform, boy);
            return im;
        }

        void Pin(Transform ata, Sprite sp)
        {
            var im = Yuz("Pin", ata, Color.white);
            im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false;
            Ortala(im.rectTransform, Tema.HaritaNoktaBoyu);
        }

        static void Ortala(RectTransform r, float boy)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(boy, boy);
            r.anchoredPosition = Vector2.zero;
        }

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

        static void Konum(RectTransform r, float ax0, float ay0, float ax1, float ay1, float ox0, float oy0, float ox1, float oy1)
        {
            r.anchorMin = new Vector2(ax0, ay0);
            r.anchorMax = new Vector2(ax1, ay1);
            r.offsetMin = new Vector2(ox0, oy0);
            r.offsetMax = new Vector2(ox1, oy1);
        }
    }
}
