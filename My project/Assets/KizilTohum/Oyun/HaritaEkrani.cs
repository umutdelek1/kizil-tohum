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

        public static void Sifirla()
        {
            PlayerPrefs.DeleteKey(Anahtar);
            PlayerPrefs.Save();
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
            public RectTransform kok, govde, vurus, nabiz;
            public bool acikSpr, tamamSpr, kilitSpr;   // damla simgesi (sprite) var mı
            public Image nabizGorsel;
            public GameObject acikGorsel, tamamGorsel, kilitliGorsel;
            public float gx, gy;                            // pencereye sığdırılmış konum (normalize, sol üst 0,0)
            public Text etiket; public Image etiketKilit;   // görev adı etiketi (kilitlide yanında kilit ikonu)
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
        Image bilgiIkon, bilgiKart;
        GameObject yakinda;                       // kilitli işaret "Yakında" kartı (tam ekran karartma + güvenli alanda kart)
        Text yakindaYer, yakindaKonu, yakindaIpucu;
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
            // dünya = Kapla çerçevesi: harita ekranı tamamen kaplar (kırpılır); simgeler dünyanın çocuğudur, kaymaz
            dunya = EkranKatmani.Olustur("Dunya", kok, EkranKatmani.Mod.Kapla);
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
            bilgiIkon = Yuz("Ikon", bilgi, Color.white);
            bilgiIkon.preserveAspect = true; bilgiIkon.raycastTarget = false;
            bilgiIkon.rectTransform.anchorMin = bilgiIkon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            bilgiIkon.rectTransform.pivot = new Vector2(0f, 0.5f);
            bilgiIkon.rectTransform.sizeDelta = new Vector2(Tema.IkonMenu, Tema.IkonMenu);
            bilgiIkon.rectTransform.anchoredPosition = new Vector2(Tema.MetinSag - 8f, 0f);
            bilgiIkon.enabled = false;
            bilgiKart = Yuz("DosyaKapak", bilgi, Color.white);   // görev dosya kapağı küçük resmi (sağ uç)
            bilgiKart.preserveAspect = true; bilgiKart.raycastTarget = false;
            bilgiKart.rectTransform.anchorMin = new Vector2(1f, 0.1f); bilgiKart.rectTransform.anchorMax = new Vector2(1f, 0.9f);
            bilgiKart.rectTransform.pivot = new Vector2(1f, 0.5f);
            bilgiKart.rectTransform.sizeDelta = new Vector2(Tema.HaritaBilgiYukseklik * 0.8f * 1.39f, 0f);
            bilgiKart.rectTransform.anchoredPosition = new Vector2(-14f, 0f);
            bilgiKart.enabled = false;
            bilgiYer = Yazi("Yer", bilgi, Tema.YaziOrta, Tema.Konusan, TextAnchor.MiddleLeft, fontKalin);
            Konum(bilgiYer.rectTransform, 0, 0.5f, 1, 0.9f, Tema.MetinSag, 0, -Tema.MetinSag, 0);
            bilgiGorev = Yazi("Gorev", bilgi, Tema.YaziKucuk, Tema.Beyaz, TextAnchor.MiddleLeft, font);
            Konum(bilgiGorev.rectTransform, 0, 0.1f, 1, 0.5f, Tema.MetinSag, 0, -Tema.MetinSag, 0);
            bilgi.gameObject.SetActive(false);

            // üst çubuk
            var cubukSerit = Yuz("Ust", kok, Tema.Panel);   // şerit tam genişlik; içerik güvenli genişlikte
            Konum(cubukSerit.rectTransform, 0, 1, 1, 1, 0, -90, 0, 0);
            cubukSerit.raycastTarget = false;
            var cubuk = EkranKatmani.Olustur("Ic", cubukSerit.transform, EkranKatmani.Mod.Yatay);
            sayac = Yazi("Sayac", cubuk.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleLeft, fontKalin);
            var di = Spr("Ikonlar/ikon_defter");
            float sayacSol = 30f;
            if (di != null)
            {
                var dim = Yuz("DefterIkon", cubuk.transform, Color.white);
                dim.sprite = di; dim.preserveAspect = true; dim.raycastTarget = false;
                dim.rectTransform.anchorMin = dim.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                dim.rectTransform.pivot = new Vector2(0f, 0.5f);
                dim.rectTransform.sizeDelta = new Vector2(Tema.IkonKucuk + 8f, Tema.IkonKucuk + 8f);
                dim.rectTransform.anchoredPosition = new Vector2(24f, 0f);
                sayacSol = 24f + Tema.IkonKucuk + 8f + 12f;
            }
            Konum(sayac.rectTransform, 0, 0, 0.3f, 1, sayacSol, 0, 0, 0);
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
            YakindaKur();

            go.SetActive(false);
        }

        void NoktaKur(Tema.HaritaNoktaTanimi t)
        {
            var n = new Nokta { t = t };
            var go = Bos("Nokta_" + t.id, dunya);
            n.kok = (RectTransform)go.transform;
            n.kok.anchorMin = n.kok.anchorMax = new Vector2(t.x, 1f - t.y);
            n.kok.sizeDelta = Vector2.zero;           // kök yalnızca nokta konumudur
            n.kok.anchoredPosition = Vector2.zero;
            // şeffaf tıklama alanı: sprite'lı simgede pivot alt orta (sivri uç noktada), dairede merkez
            n.vurus = NoktaRect(Bos("Vurus", n.kok));
            var vurus = n.vurus.gameObject.AddComponent<Image>();
            vurus.color = Tema.Saydam;
            var olay = n.vurus.gameObject.AddComponent<HaritaNoktasi>();
            olay.Girdi = () => { n.hover = true; BilgiGoster(n); };
            olay.Cikti = () => { n.hover = false; if (bilgiNokta == n) BilgiGizle(); };
            olay.Tiklandi = () => Tikla(n);

            n.govde = NoktaRect(Bos("Govde", n.kok));

            // nabız halkası (yalnızca açık noktalarda görünür)
            var nb = Yuz("Nabiz", n.govde, Tema.Kehribar);
            nb.sprite = Halka(); nb.raycastTarget = false;
            Ortala(nb.rectTransform, Tema.HaritaNoktaBoyu * 0.55f);
            n.nabiz = nb.rectTransform; n.nabizGorsel = nb;

            // açık: kehribar daire + halka (sprite varsa pin_acik)
            n.acikGorsel = Bos("Acik", n.govde);
            Doldur((RectTransform)n.acikGorsel.transform);
            var pa = Spr("Harita/pin_acik");
            n.acikSpr = pa != null;
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
            n.tamamSpr = pt != null;
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
            n.kilitSpr = pk != null;
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
            EtiketKur(n);
            noktalar.Add(n);
        }

        // Görev adı etiketi: pin ucunun hemen altında, koyu yarı saydam zemin üzerinde. Hover ölçeğinden etkilenmez (Govde'nin kardeşi).
        void EtiketKur(Nokta n)
        {
            var zemin = Yuz("Etiket", n.kok, new Color(0.05f, 0.03f, 0.03f, 0.72f));
            zemin.raycastTarget = false;
            var r = zemin.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, -6f);
            r.sizeDelta = new Vector2(220f, 34f);
            n.etiket = Yazi("Yazi", r, Tema.YaziKucuk, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            n.etiket.text = n.t.yer;
            n.etiket.horizontalOverflow = HorizontalWrapMode.Overflow;
            Doldur(n.etiket.rectTransform);
            n.etiketKilit = Yuz("Kilit", r, Color.white);
            n.etiketKilit.sprite = Spr("Ikonlar/ikon_kilit");
            n.etiketKilit.preserveAspect = true; n.etiketKilit.raycastTarget = false;
            var kr = n.etiketKilit.rectTransform;
            kr.anchorMin = kr.anchorMax = new Vector2(0f, 0.5f);
            kr.pivot = new Vector2(0f, 0.5f);
            kr.sizeDelta = new Vector2(24f, 24f);
            kr.anchoredPosition = new Vector2(8f, 0f);
            n.etiketKilit.enabled = false;
        }

        // Etiketi içeriğe göre boyutla; kilitlide solda kilit ikonu için yer aç.
        void EtiketGuncelle(Nokta n)
        {
            bool kilit = n.durum == Durum.Kilitli && n.etiketKilit.sprite != null;
            n.etiketKilit.enabled = kilit;
            float w = Mathf.Max(80f, n.etiket.preferredWidth) + 28f + (kilit ? 26f : 0f);
            ((RectTransform)n.etiket.transform.parent).sizeDelta = new Vector2(w, 34f);
            n.etiket.rectTransform.offsetMin = new Vector2(kilit ? 30f : 0f, 0f);
            n.etiket.color = n.durum == Durum.Kilitli ? Tema.Soluk : Tema.Beyaz;
        }

        // ---------- açma / kapama ----------
        public void Ac()
        {
            kok.gameObject.SetActive(true);
            if (yakinda != null) yakinda.SetActive(false);
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
                EtiketGuncelle(n);
                // damla simgesi: pivot (0.5, 0), sivri uç nokta konumunda, simge noktanın hemen üstünde; daire: merkez
                bool sp = SimgeVar(n);
                var pv = new Vector2(0.5f, sp ? 0f : 0.5f);
                n.govde.pivot = pv; n.vurus.pivot = pv;
                n.govde.anchoredPosition = Vector2.zero; n.vurus.anchoredPosition = Vector2.zero;
                n.nabizGorsel.enabled = n.durum == Durum.Acik && !sp;   // sprite'lı açık simgede halka yerine ölçek nabzı
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
                KartKumesi.Birlestir(y.AcilanKartlar);   // eski kayıttaki kartlar da kümeye girsin
                int toplam = y.Icerik != null ? y.Icerik.kartlar.Count : 14;
                sayac.text = "Kavram kartları: " + KartKumesi.Sayi + "/" + toplam;
            }
        }

        public void Kapat()
        {
            BilgiGizle();
            YakindaKapat();
            if (kok != null) kok.gameObject.SetActive(false);
        }

        public void GorevTamamlandi(string gorevId) { HaritaKayit.Isaretle(gorevId); }

        // Harita ekranı kaplamak için kırpılır (çok geniş/dar pencerede kenarlar kesilir). İşaretler görünür alanda
        // (üst çubuğun altında, etiketiyle birlikte) kalacak şekilde en yakın noktaya çekilir: 16:9'da hiçbiri oynamaz.
        void NoktalariSigdir()
        {
            var kokRt = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (kokRt == null) return;
            Vector2 ekran = kokRt.rect.size;
            if (ekran.x < 1f || ekran.y < 1f) return;
            float oran = ekran.x / ekran.y, s = Tema.HaritaTemelOlcek;
            Vector2 c = oran > EkranKatmani.TasarimOrani ? new Vector2(1f, oran / EkranKatmani.TasarimOrani) : new Vector2(EkranKatmani.TasarimOrani / oran, 1f);
            float W = ekran.x * c.x, H = ekran.y * c.y;                    // çerçeve boyu (canvas birimi)
            float vx = 1f / (c.x * s), vy = 1f / (c.y * s);                // görünen oran (çerçeve = 1)
            float x0 = 0.5f - vx * 0.5f + 140f / W, x1 = 0.5f + vx * 0.5f - 140f / W;
            float y0 = 0.5f - vy * 0.5f + (90f + 104f * s) / (H * s), y1 = 0.5f + vy * 0.5f - (56f * s + 10f) / (H * s);
            for (int i = 0; i < noktalar.Count; i++)
            {
                var n = noktalar[i];
                n.gx = x0 < x1 ? Mathf.Clamp(n.t.x, x0, x1) : n.t.x;
                n.gy = y0 < y1 ? Mathf.Clamp(n.t.y, y0, y1) : n.t.y;
                n.kok.anchorMin = n.kok.anchorMax = new Vector2(n.gx, 1f - n.gy);
            }
        }

        // ---------- döngü ----------
        void Update()
        {
            if (!Acik) return;
            float dt = Time.unscaledDeltaTime;
            float simdi = Time.unscaledTime;
            NoktalariSigdir();

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
                Vector2 pn = new Vector2((dalisNokta.gx - 0.5f) * r.width, (0.5f - dalisNokta.gy) * r.height);
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
                float simgeNabzi = 1f;   // pin_acik: kehribar parıltı için ölçek 1.0 → 1.08
                if (n.durum == Durum.Acik && n.acikSpr)
                    simgeNabzi = Mathf.Lerp(1f, Tema.HaritaSimgeNabizOlcek, 0.5f - 0.5f * Mathf.Cos(nabizF * 2f * Mathf.PI));
                n.govde.localScale = Vector3.one * (n.olcek * simgeNabzi);
                if (n.durum == Durum.Acik && n.nabiz != null && !n.acikSpr)
                {
                    float boy = Tema.HaritaNoktaBoyu * 0.55f * Mathf.Lerp(1f, Tema.HaritaNabizOlcek, nabizF);
                    n.nabiz.sizeDelta = new Vector2(boy, boy);
                    var c = Tema.Kehribar; c.a = (1f - nabizF) * 0.7f;
                    n.nabizGorsel.color = c;
                }
            }
            if (bilgiNokta != null && bilgi.gameObject.activeSelf) BilgiYerlestir(bilgiNokta);
        }

        // ---------- "Yakında" kartı (kilitli işaretler) ----------
        // Karartma tüm ekranı kaplar ve dışarı tıklayınca kapatır; kart 16:9 güvenli alanda ortalanır.
        // Not: oyunda henüz hiçbir ses dosyası/çalıcı yok; "kilitli" efekti için KilitSesi kancası bırakıldı.
        public Action KilitSesi;

        void YakindaKur()
        {
            var dim = Yuz("Yakinda", kok, Tema.Orten);
            yakinda = dim.gameObject;
            Doldur(dim.rectTransform);
            var kapatDis = yakinda.AddComponent<Button>();
            kapatDis.transition = Selectable.Transition.None; kapatDis.targetGraphic = dim;
            kapatDis.onClick.AddListener(YakindaKapat);   // dışarı tıklayınca kapanır
            var guv = EkranKatmani.Olustur("Guvenli", yakinda.transform, EkranKatmani.Mod.Guvenli);

            var kart = Yuz("Kart", guv, Tema.Kart);
            var ks = Spr("Arayuz/kavram_karti_cercevesi_9slice");
            if (ks != null) { kart.sprite = ks; kart.type = Image.Type.Sliced; kart.color = Color.white; }
            Konum(kart.rectTransform, 0.28f, 0.24f, 0.72f, 0.76f, 0, 0, 0, 0);
            var kartTik = kart.gameObject.AddComponent<Button>();   // karta tıklamak kapatmaz
            kartTik.transition = Selectable.Transition.None; kartTik.targetGraphic = kart;

            var kilit = Spr("Ikonlar/ikon_kilit");
            if (kilit != null)
            {
                var ki = Yuz("Kilit", kart.transform, Color.white);
                ki.sprite = kilit; ki.preserveAspect = true; ki.raycastTarget = false;
                Konum(ki.rectTransform, 0.5f, 0.76f, 0.5f, 0.76f, -40, -40, 40, 40);
            }
            yakindaYer = Yazi("Yer", kart.transform, Tema.YaziBaslik, Tema.Konusan, TextAnchor.MiddleCenter, fontBaslik);
            Konum(yakindaYer.rectTransform, 0.04f, 0.56f, 0.96f, 0.70f, 0, 0, 0, 0);
            var durum = Yazi("Durum", kart.transform, Tema.YaziOrta, Tema.Soluk, TextAnchor.MiddleCenter, fontKalin);
            durum.text = "Bu dosya henüz açılmadı";
            Konum(durum.rectTransform, 0.04f, 0.47f, 0.96f, 0.56f, 0, 0, 0, 0);
            yakindaKonu = Yazi("Konu", kart.transform, Tema.YaziOrta, Tema.Marka, TextAnchor.MiddleCenter, fontKalin);
            Konum(yakindaKonu.rectTransform, 0.04f, 0.37f, 0.96f, 0.46f, 0, 0, 0, 0);
            yakindaIpucu = Yazi("Ipucu", kart.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.UpperCenter, font);
            Konum(yakindaIpucu.rectTransform, 0.08f, 0.2f, 0.92f, 0.37f, 0, 0, 0, 0);

            var kb = Yuz("Kapat", kart.transform, Tema.Kontrol);
            var bs = Spr("Arayuz/secenek_butonu_normal_9slice");
            if (bs != null) { kb.sprite = bs; kb.type = Image.Type.Sliced; kb.color = Color.white; }
            Konum(kb.rectTransform, 0.34f, 0.04f, 0.66f, 0.15f, 0, 0, 0, 0);
            var btn = kb.gameObject.AddComponent<Button>();
            btn.targetGraphic = kb;
            var bt = Yazi("Yazi", kb.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleCenter, fontKalin);
            bt.text = "Kapat";
            Doldur(bt.rectTransform);
            btn.onClick.AddListener(YakindaKapat);
            yakinda.SetActive(false);
        }

        void YakindaGoster(string noktaId)
        {
            YakindaKartlari.Kart k;
            if (!YakindaKartlari.Bul(noktaId, out k)) return;
            BilgiGizle();
            yakindaYer.text = k.yer;
            yakindaKonu.text = "Konu: " + k.konu;
            yakindaIpucu.text = k.ipucu;
            yakinda.transform.SetAsLastSibling();
            yakinda.SetActive(true);
            if (KilitSesi != null) KilitSesi();
        }

        void YakindaKapat() { if (yakinda != null) yakinda.SetActive(false); }

        // ---------- etkileşim ----------
        void Tikla(Nokta n)
        {
            if (mesgul) return;
            if (n.durum == Durum.Kilitli) { YakindaGoster(n.t.id); return; }   // kilitli: "Yakında" kartı
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
            // ikon: kilitli → kilit; açık/tamam → görev konusu (menüdeki ikonlarla aynı)
            string ik = n.durum == Durum.Kilitli ? "ikon_kilit" : n.t.gorevId == "KT01" ? "ikon_mikroskop" : n.t.gorevId == "KT04" ? "ikon_su_bitki" : "ikon_dna";
            var isp = Spr("Ikonlar/" + ik);
            bilgiIkon.sprite = isp; bilgiIkon.enabled = isp != null;
            float solBosluk = isp != null ? Tema.MetinSag + Tema.IkonMenu : Tema.MetinSag;
            bilgiYer.rectTransform.offsetMin = new Vector2(solBosluk, 0f);
            bilgiGorev.rectTransform.offsetMin = new Vector2(solBosluk, 0f);
            string kd = n.durum == Durum.Kilitli ? null : n.t.gorevId == "KT01" ? "dosya_yuz_cekmece_kart" : n.t.gorevId == "KT04" ? "dosya_susuzluk_kart" : n.t.gorevId == "KT08" ? "dosya_kirk_yedi_kart" : null;
            var ksp = kd != null ? Spr("Dosyalar/" + kd) : null;
            bilgiKart.sprite = ksp; bilgiKart.enabled = ksp != null;
            float sagBosluk = ksp != null ? bilgiKart.rectTransform.sizeDelta.x + 24f : Tema.MetinSag;
            bilgiYer.rectTransform.offsetMax = new Vector2(-sagBosluk, 0f);
            bilgiGorev.rectTransform.offsetMax = new Vector2(-sagBosluk, 0f);
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
            Vector3 p = n.govde.TransformPoint(n.govde.rect.center);   // simgenin ortası: panel üstüne değil yanına
            float yarim = n.govde.rect.width * 0.5f * n.govde.lossyScale.x;
            p.x += (sagda ? 1f : -1f) * (yarim + Tema.HaritaBilgiUzaklik * ol);
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
            Doldur(im.rectTransform);   // ata (Govde) 96x96; pivotu alt orta olduğundan simge noktanın üstünde durur
        }

        static bool SimgeVar(Nokta n)
        {
            return n.durum == Durum.Acik ? n.acikSpr : n.durum == Durum.Tamam ? n.tamamSpr : n.kilitSpr;
        }

        // Nokta konumunda 96x96 dikdörtgen; pivot Ac()'ta duruma göre ayarlanır.
        static RectTransform NoktaRect(GameObject go)
        {
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(Tema.HaritaNoktaBoyu, Tema.HaritaNoktaBoyu);
            r.anchoredPosition = Vector2.zero;
            return r;
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
