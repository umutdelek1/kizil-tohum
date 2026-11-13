using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>
    /// Çalışma zamanında kurulan arayüz (sahne dosyası gerektirmez).
    /// Boş bir sahneye tek bir GameObject + bu bileşeni eklemek yeter; OyunDenetleyici otomatik eklenir.
    /// Görseller Resources/Gorseller/<ad> yolundan okunur; yoksa düz renkli yer tutucu kullanılır.
    /// </summary>
    public class KizilTohumArayuz : MonoBehaviour
    {
        static readonly Color Panel = Hex("171316", 0.94f);
        static readonly Color Turuncu = Hex("E0643B");
        static readonly Color Yesil = Hex("5DFFA6");
        static readonly Color Mavi = Hex("8FD8FF");
        static readonly Color Mor = Hex("B47CFF");
        static readonly Color Sari = Hex("FFD35A");
        static readonly Color Kirmizi = Hex("FF5A52");
        static readonly Color Koyu = Hex("0C0A0C");

        readonly List<RectTransform> paraKatman = new List<RectTransform>();
        static readonly float[] paraGuc = { 10f, 22f, 40f };

        bool duvarSpriteli;
        OyunDenetleyici oyun;
        GorevYoneticisi y;
        Font font, fontKalin, fontBaslik;
        Canvas canvas;

        GameObject menu, oyunEkrani, kartPaneli, sonEkrani, simPaneli;
        Image kartGorsel, sahne, duvarDolgu, portre, ipucuKutu, linKutu, menuZemin, sonZemin;
        Text duvarYazi, gorevBaslik, konusanYazi, metinYazi, ipucuYazi, linYazi, tepkiYazi, simYazi, kartBaslik, kartMetin, sonYazi, menuNot;
        RectTransform secenekKap, simKap;
        Button devamBtn, ipucuBtn;
        Slider kaydirici;
        Text devamYazi;
        readonly List<Button> secBtn = new List<Button>();
        float zorlukCarpan = 1f;
        Adim aktif;

        static Color Hex(string h, float a = 1f)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + h, out c);
            c.a = a;
            return c;
        }

        void Awake()
        {
            if (FindAnyObjectByType<OyunDenetleyici>() == null) gameObject.AddComponent<OyunDenetleyici>();
        }

        void Start()
        {
            oyun = OyunDenetleyici.Instance;
            y = oyun.Yonetici;
            font = Resources.Load<Font>("Fontlar/Inter-Regular");
            fontKalin = Resources.Load<Font>("Fontlar/Inter-SemiBold");
            fontBaslik = Resources.Load<Font>("Fontlar/SpaceGrotesk-Bold");
            Font yedek = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = yedek;
            if (fontKalin == null) fontKalin = font;
            if (fontBaslik == null) fontBaslik = fontKalin;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            Kur();
            Bagla();
            oyun.KlavyeKilitli = () => menu.activeSelf || kartPaneli.activeSelf || sonEkrani.activeSelf;
            string derin = OyunDenetleyici.DerinBaglantiGorevi();
            if (!string.IsNullOrEmpty(derin) && oyun.GoreveGit(derin)) Goster(false);
            else Goster(true);
        }

        void Update()
        {
            if (ipucuKutu != null) ipucuKutu.enabled = !string.IsNullOrEmpty(ipucuYazi.text);
            if (linKutu != null) linKutu.enabled = !string.IsNullOrEmpty(linYazi.text);
            var fare = UnityEngine.InputSystem.Mouse.current;
            if (fare != null && paraKatman.Count > 0)
            {
                Vector2 p = fare.position.ReadValue();
                Vector2 n = new Vector2(Mathf.Clamp(p.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f), Mathf.Clamp(p.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f));
                for (int i = 0; i < paraKatman.Count; i++)
                    if (paraKatman[i] != null)
                        paraKatman[i].anchoredPosition = Vector2.Lerp(paraKatman[i].anchoredPosition, new Vector2(-n.x, -n.y) * paraGuc[i], Time.unscaledDeltaTime * 4f);
            }
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null || kartPaneli == null || !kartPaneli.activeSelf) return;
            if (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame) kartPaneli.SetActive(false);
        }

        // ---------- kurulum ----------
        void Kur()
        {
            var cgo = new GameObject("KT_Canvas");
            cgo.transform.SetParent(transform, false);
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            var kok = (RectTransform)cgo.transform;

            var bg = Yuz("Zemin", kok, Koyu);
            Doldur(bg.rectTransform);

            // --- oyun ekranı ---
            oyunEkrani = Bos("OyunEkrani", kok);
            Doldur((RectTransform)oyunEkrani.transform);
            sahne = Yuz("Sahne", oyunEkrani.transform, Hex("2A2226"));
            Doldur(sahne.rectTransform);
            sahne.preserveAspect = false;

            // üst çubuk
            var ust = Yuz("Ust", oyunEkrani.transform, Panel);
            Konum(ust.rectTransform, 0, 1, 1, 1, 0, -90, 0, 0);
            gorevBaslik = Yazi("GorevBaslik", ust.transform, 30, Turuncu, TextAnchor.MiddleLeft);
            gorevBaslik.font = fontBaslik;
            Konum(gorevBaslik.rectTransform, 0, 0, 0.6f, 1, 30, 0, 0, 0);
            var duvarKap = Yuz("DuvarKap", ust.transform, Hex("2B2428"));
            Konum(duvarKap.rectTransform, 0.62f, 0.3f, 0.97f, 0.7f, 0, 0, 0, 0);
            var cerceve = Spr("Arayuz/tohum_duvari_cubuk_cercevesi");
            var dolgu = Spr("Arayuz/tohum_duvari_cubuk_dolgu");
            duvarSpriteli = cerceve != null && dolgu != null;
            if (duvarSpriteli) { duvarKap.sprite = cerceve; duvarKap.color = Color.white; Konum(duvarKap.rectTransform, 0.62f, 0.12f, 0.97f, 0.88f, 0, 0, 0, 0); }
            duvarDolgu = Yuz("DuvarDolgu", duvarKap.transform, Yesil);
            if (duvarSpriteli) duvarDolgu.sprite = dolgu;
            Doldur(duvarDolgu.rectTransform);
            duvarYazi = Yazi("DuvarYazi", duvarKap.transform, 24, duvarSpriteli ? Color.white : Koyu, TextAnchor.MiddleCenter);
            duvarYazi.font = fontKalin;
            Doldur(duvarYazi.rectTransform);

            // alt panel
            var alt = Yuz("Alt", oyunEkrani.transform, Panel);
            Kaplama(alt, "diyalog_paneli_9slice", Panel);
            Konum(alt.rectTransform, 0, 0, 1, 0.46f, 0, 0, 0, 0);
            portre = Yuz("Portre", oyunEkrani.transform, Color.white);
            portre.preserveAspect = true; portre.raycastTarget = false;
            Konum(portre.rectTransform, 0.01f, 0.46f, 0.20f, 0.46f, 0, 0, 0, 400);
            portre.gameObject.SetActive(false);
            konusanYazi = Yazi("Konusan", alt.transform, 28, Sari, TextAnchor.UpperLeft);
            konusanYazi.font = fontKalin;
            Konum(konusanYazi.rectTransform, 0.04f, 0.76f, 0.6f, 0.87f, 0, 0, 0, 0);
            metinYazi = Yazi("Metin", alt.transform, 30, Color.white, TextAnchor.UpperLeft);
            Konum(metinYazi.rectTransform, 0.04f, 0.50f, 0.96f, 0.77f, 0, 0, 0, 0);

            var sk = Bos("Secenekler", alt.transform);
            secenekKap = (RectTransform)sk.transform;
            Konum(secenekKap, 0.035f, 0.22f, 0.70f, 0.52f, 0, 0, 0, 0);
            var vl = sk.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.childControlHeight = true; vl.childControlWidth = true;
            vl.childForceExpandHeight = true; vl.childForceExpandWidth = true;

            ipucuKutu = Yuz("IpucuKutu", alt.transform, Hex("000000", 0));
            Konum(ipucuKutu.rectTransform, 0.72f, 0.5f, 0.975f, 0.90f, 0, 0, 0, 0);
            Kaplama(ipucuKutu, "ipucu_kutusu_9slice", Hex("1B2A33"));
            linKutu = Yuz("LinKutu", alt.transform, Hex("000000", 0));
            Konum(linKutu.rectTransform, 0.72f, 0.22f, 0.975f, 0.5f, 0, 0, 0, 0);
            Kaplama(linKutu, "ipucu_kutusu_9slice", Hex("2A1B33"));
            if (linKutu.sprite != null) linKutu.color = new Color(0.85f, 0.7f, 1f, 1f);
            tepkiYazi = Yazi("Tepki", alt.transform, 26, Kirmizi, TextAnchor.UpperLeft);
            Konum(tepkiYazi.rectTransform, 0.035f, 0.10f, 0.70f, 0.22f, 0, 0, 0, 0);
            ipucuYazi = Yazi("Ipucu", alt.transform, 24, Mavi, TextAnchor.UpperLeft);
            Konum(ipucuYazi.rectTransform, 0.73f, 0.52f, 0.965f, 0.88f, 0, 0, 0, 0);
            linYazi = Yazi("Lin", alt.transform, 24, Mor, TextAnchor.UpperLeft);
            Konum(linYazi.rectTransform, 0.73f, 0.24f, 0.965f, 0.48f, 0, 0, 0, 0);

            devamBtn = Dugme("Devam", alt.transform, "Devam ▶ (Boşluk)", Turuncu, out devamYazi);
            Konum((RectTransform)devamBtn.transform, 0.72f, 0.10f, 0.975f, 0.21f, 0, 0, 0, 0);
            devamBtn.onClick.AddListener(() => { if (aktif != null && aktif.tip == AdimTipi.Etkilesim && !y.Ilerlenebilir) y.EtkilesimTamamlandi(aktif.nesne); else y.Ilerle(); });
            Text t2;
            ipucuBtn = Dugme("IpucuBtn", alt.transform, "İpucu (H)", Hex("2F6F8F"), out t2);
            Konum((RectTransform)ipucuBtn.transform, 0.58f, 0.10f, 0.71f, 0.21f, 0, 0, 0, 0);
            ipucuBtn.onClick.AddListener(() => y.IpucuIste());
            Text t3;
            var menuBtn = Dugme("MenuBtn", ust.transform, "Menü", Hex("3A3036"), out t3);
            Konum((RectTransform)menuBtn.transform, 0.9f, 0.3f, 1f, 0.7f, -10, 0, 0, 0);
            menuBtn.onClick.AddListener(() => Goster(true));
            Konum((RectTransform)duvarKap.transform, 0.5f, 0.3f, 0.88f, 0.7f, 0, 0, 0, 0);

            // simülasyon paneli (kaydırıcı, grafik, tablo)
            simPaneli = Yuz("SimPaneli", oyunEkrani.transform, Hex("120F12", 0.92f)).gameObject;
            Konum((RectTransform)simPaneli.transform, 0.05f, 0.48f, 0.95f, 0.9f, 0, 0, 0, 0);
            simKap = (RectTransform)Bos("SimKap", simPaneli.transform).transform;
            Doldur(simKap);
            simYazi = Yazi("SimYazi", simPaneli.transform, 26, Yesil, TextAnchor.LowerCenter);
            Konum(simYazi.rectTransform, 0, 0.14f, 1, 0.3f, 10, 0, -10, 0);
            kaydirici = SliderYap(simPaneli.transform);
            Konum((RectTransform)kaydirici.transform, 0.1f, 0.02f, 0.9f, 0.13f, 0, 0, 0, 0);
            kaydirici.onValueChanged.AddListener(v => { if (aktif != null && aktif.tip == AdimTipi.Kaydirici) y.KaydiriciDegisti(v); });

            // kart paneli
            kartPaneli = Yuz("KartPaneli", kok, Hex("000000", 0.75f)).gameObject;
            Doldur((RectTransform)kartPaneli.transform);
            var kart = Yuz("Kart", kartPaneli.transform, Hex("221B20"));
            Kaplama(kart, "kavram_karti_cercevesi_9slice", Hex("221B20"));
            Konum(kart.rectTransform, 0.15f, 0.08f, 0.85f, 0.92f, 0, 0, 0, 0);
            kartBaslik = Yazi("KartBaslik", kart.transform, 40, Sari, TextAnchor.UpperCenter);
            kartBaslik.font = fontBaslik;
            Konum(kartBaslik.rectTransform, 0.03f, 0.86f, 0.97f, 0.97f, 0, 0, 0, 0);
            kartGorsel = Yuz("KartGorsel", kart.transform, Color.white);
            kartGorsel.preserveAspect = true;
            Konum(kartGorsel.rectTransform, 0.04f, 0.34f, 0.5f, 0.84f, 0, 0, 0, 0);
            kartMetin = Yazi("KartMetin", kart.transform, 30, Color.white, TextAnchor.UpperLeft);
            Konum(kartMetin.rectTransform, 0.53f, 0.2f, 0.96f, 0.85f, 0, 0, 0, 0);
            Text t4;
            var kb = Dugme("KartKapat", kart.transform, "Defterime ekle ✓", Turuncu, out t4);
            Konum((RectTransform)kb.transform, 0.3f, 0.04f, 0.7f, 0.16f, 0, 0, 0, 0);
            kb.onClick.AddListener(() => kartPaneli.SetActive(false));
            kartPaneli.SetActive(false);

            // son ekranı
            sonZemin = Yuz("SonEkrani", kok, Hex("0C0A0C", 0.96f));
            sonEkrani = sonZemin.gameObject;
            Doldur((RectTransform)sonEkrani.transform);
            var sonPanel = Yuz("SonPanel", sonEkrani.transform, Hex("171316", 0.88f));
            Kaplama(sonPanel, "diyalog_paneli_9slice", Hex("171316", 0.88f));
            Konum(sonPanel.rectTransform, 0.52f, 0.2f, 0.97f, 0.93f, 0, 0, 0, 0);
            sonYazi = Yazi("SonYazi", sonPanel.transform, 32, Color.white, TextAnchor.UpperCenter);
            Konum(sonYazi.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f, 0, 0, 0, 0);
            Text t5;
            var sb = Dugme("SonMenu", sonEkrani.transform, "Ana menü", Turuncu, out t5);
            Konum((RectTransform)sb.transform, 0.64f, 0.06f, 0.86f, 0.15f, 0, 0, 0, 0);
            sb.onClick.AddListener(() => Goster(true));
            sonEkrani.SetActive(false);

            // menü
            menuZemin = Yuz("Menu", kok, Hex("0C0A0C", 0.98f));
            menu = menuZemin.gameObject;
            var kapak = Spr("Menu/kapak_arka_plan");
            if (kapak != null) { menuZemin.sprite = kapak; menuZemin.color = new Color(0.55f, 0.55f, 0.55f, 1f); }
            Doldur((RectTransform)menu.transform);
            var baslik = Yazi("MenuBaslik", menu.transform, 90, Turuncu, TextAnchor.MiddleCenter);
            baslik.font = fontBaslik;
            baslik.text = "KIZIL TOHUM";
            var logo = Spr("Menu/logo_kizil_tohum");
            if (logo != null)
            {
                var li = Yuz("Logo", menu.transform, Color.white);
                li.sprite = logo; li.preserveAspect = true; li.raycastTarget = false;
                Konum(li.rectTransform, 0.2f, 0.74f, 0.8f, 0.96f, 0, 0, 0, 0);
                baslik.text = "";
            }
            Konum(baslik.rectTransform, 0.1f, 0.78f, 0.9f, 0.96f, 0, 0, 0, 0);
            var alti = Yazi("MenuAlti", menu.transform, 30, Mavi, TextAnchor.MiddleCenter);
            alti.text = Resources.Load<Sprite>("Gorseller/Menu/logo_kizil_tohum") != null ? "Umut-1 Mars Kolonisi" : "TYT Biyoloji · Umut-1 Mars Kolonisi";
            Konum(alti.rectTransform, 0.1f, 0.7f, 0.9f, 0.78f, 0, 0, 0, 0);

            MenuDugme("Yeni Oyun", 0.58f, () => { oyun.YeniOyun(); Uygula(); Goster(false); });
            MenuDugme("Devam Et", 0.48f, () => { if (oyun.KayitVarMi && oyun.DevamEt()) { Uygula(); Goster(false); } });
            MenuDugme("Görev 1 · Yüz Çekmece", 0.40f, () => GorevAc("KT01"));
            MenuDugme("Görev 2 · Islak Toprakta Susuzluk", 0.31f, () => GorevAc("KT04"));
            MenuDugme("Görev 3 · Kırk Yedi", 0.22f, () => GorevAc("KT08"));
            Text zt;
            var zb = Dugme("Zorluk", menu.transform, "Zorluk: Normal", Hex("3A3036"), out zt);
            Konum((RectTransform)zb.transform, 0.35f, 0.10f, 0.65f, 0.18f, 0, 0, 0, 0);
            zb.onClick.AddListener(() =>
            {
                if (zorlukCarpan < 0.75f) { zorlukCarpan = 1f; zt.text = "Zorluk: Normal"; }
                else if (zorlukCarpan < 1.25f) { zorlukCarpan = 1.5f; zt.text = "Zorluk: Zor"; }
                else { zorlukCarpan = 0.5f; zt.text = "Zorluk: Kolay"; }
                Uygula();
            });
            menuNot = Yazi("MenuNot", menu.transform, 22, Hex("999999"), TextAnchor.MiddleCenter);
            Konum(menuNot.rectTransform, 0.05f, 0.01f, 0.95f, 0.09f, 0, 0, 0, 0);
            menuNot.text = "Boşluk/Enter: devam · 1-4: seçenek · H: ipucu · E: etkileşim";
            if (oyun.IcerikHatalari.Count > 0) menuNot.text = "İÇERİK HATASI: " + oyun.IcerikHatalari[0];
        }

        void GorevAc(string id)
        {
            Uygula();
            if (oyun.GoreveGit(id)) Goster(false);
        }

        void Uygula() { y.CezaCarpani = zorlukCarpan; }

        void MenuDugme(string yazi, float yPos, UnityEngine.Events.UnityAction act)
        {
            Text t;
            var b = Dugme("M_" + yazi, menu.transform, yazi, Hex("2B2428"), out t);
            Konum((RectTransform)b.transform, 0.3f, yPos, 0.7f, yPos + 0.075f, 0, 0, 0, 0);
            b.onClick.AddListener(act);
        }

        void Goster(bool menuGoster)
        {
            menu.SetActive(menuGoster);
            oyunEkrani.SetActive(!menuGoster);
            if (menuGoster) sonEkrani.SetActive(false);
        }

        // ---------- olay bağlama ----------
        void Bagla()
        {
            y.GorevBasladi += g => { gorevBaslik.text = g.id + " · " + g.baslik; sonEkrani.SetActive(false); };
            y.AdimBasladi += AdimCiz;
            y.AdimGuncellendi += s => { DevamGuncelle(s); SecenekGuncelle(s); };
            y.Cevaplandi += CevapGoster;
            y.IpucuGosterildi += t => { ipucuYazi.text = "İpucu: " + t; };
            y.LinNotuGosterildi += t => { linYazi.text = "Dr. Lin: " + t; };
            y.SaglikDegisti += (v, fark) => DuvarCiz(v);
            y.KavramKartiAcildi += k => { kartBaslik.text = k.baslik; kartMetin.text = k.konu + "\n\n" + k.metin; var ks = Spr("Kartlar/" + k.id); kartGorsel.sprite = ks; kartGorsel.gameObject.SetActive(ks != null); kartPaneli.SetActive(true); };
            y.SimulasyonGuncellendi += SimCiz;
            y.OyunBitti += o => SonGoster(o);
            y.GorevBitti += g => { };
        }

        // Parallax: arka/orta/ön katmanlar fareye göre hafifçe kayar (katmanlar yoksa düz sahne görünür)
        void ParalaksKur(string ad)
        {
            for (int i = 0; i < paraKatman.Count; i++) if (paraKatman[i] != null) Destroy(paraKatman[i].gameObject);
            paraKatman.Clear();
            var arka = Spr("Parallax/" + ad + "_arka_plan_dolu");
            var orta = Spr("Parallax/" + ad + "_orta_plan");
            var on = Spr("Parallax/" + ad + "_on_plan");
            if (arka == null || orta == null) return;
            var ks = new[] { arka, orta, on };
            for (int i = 0; i < ks.Length; i++)
            {
                if (ks[i] == null) continue;
                var im = Yuz("Katman" + i, sahne.transform, Color.white);
                im.sprite = ks[i]; im.raycastTarget = false; im.preserveAspect = false;
                var r = im.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(-60, -60); r.offsetMax = new Vector2(60, 60);
                paraKatman.Add(r);
            }
            sahne.color = new Color(1, 1, 1, 0);
        }

        void PortreSec(string konusan, string durum)
        {
            string ad = null;
            if (!string.IsNullOrEmpty(konusan))
            {
                string k = konusan.ToLowerInvariant();
                if (k.Contains("ada")) ad = "Portreler/" + (durum == "yanlis" ? "ada_endiseli" : durum == "dogru" ? "ada_gulumseyen" : "ada_dusunceli");
                else if (k.Contains("okan")) ad = "Portreler/" + (durum == "yanlis" ? "okan_kizgin" : durum == "dogru" ? "okan_rahatlamis" : "okan_notr");
                else if (k.Contains("lin")) ad = "Portreler/lin_portre";
                else if (k.Contains("tohum")) ad = "Mercek/" + (durum == "yanlis" ? "mercek_uyari" : "mercek_normal");
            }
            Sprite sp = ad != null ? Resources.Load<Sprite>("Gorseller/" + ad) : null;
            if (sp == null && ad == "Portreler/okan_kizgin") sp = Resources.Load<Sprite>("Gorseller/Portreler/okan_sert");
            if (sp == null && ad == "Portreler/okan_rahatlamis") sp = Resources.Load<Sprite>("Gorseller/Portreler/okan_notr");
            portre.gameObject.SetActive(sp != null);
            if (sp != null) portre.sprite = sp;
        }

        void DuvarCiz(int v)
        {
            duvarYazi.text = "Tohum Duvarı  " + v + "/100";
            var r = duvarDolgu.rectTransform;
            float f = Mathf.Clamp01(v / 100f);
            if (duvarSpriteli)
            {
                r.anchorMin = new Vector2(0.013f, 0.13f);
                r.anchorMax = new Vector2(0.013f + 0.974f * f, 0.87f);
                r.offsetMin = r.offsetMax = Vector2.zero;
                duvarDolgu.color = v >= 85 ? Color.white : v >= 60 ? Hex("FFE08A") : Hex("FF8A80");
                return;
            }
            r.anchorMin = Vector2.zero;
            r.anchorMax = new Vector2(f, 1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            duvarDolgu.color = v >= 85 ? Yesil : v >= 60 ? Sari : Kirmizi;
        }

        void AdimCiz(AdimSunumu s)
        {
            aktif = s.adim;
            DuvarCiz(s.saglik);
            gorevBaslik.text = s.gorev.id + " · " + s.gorev.baslik + "   (" + s.adimNo + "/" + s.toplamAdim + ")";
            konusanYazi.text = string.IsNullOrEmpty(s.adim.konusan) ? "" : s.adim.konusan;
            metinYazi.text = s.adim.metin ?? "";
            PortreSec(s.adim.konusan, null);
            tepkiYazi.text = ""; ipucuYazi.text = ""; linYazi.text = "";
            if (!string.IsNullOrEmpty(s.adim.gorsel))
            {
                var spr = Resources.Load<Sprite>("Gorseller/Sahneler/" + s.adim.gorsel);
                if (spr == null) spr = Resources.Load<Sprite>("Gorseller/" + s.adim.gorsel);
                if (spr != null) { sahne.sprite = spr; sahne.color = Color.white; }
                else { sahne.sprite = null; sahne.color = Hex("2A2226"); }
                ParalaksKur(s.adim.gorsel);
            }
            bool soruTipi = s.adim.SecenekliMi;
            ipucuBtn.gameObject.SetActive(soruTipi);
            SecenekKur(s);
            simPaneli.SetActive(false);
            kaydirici.gameObject.SetActive(false);
            if (s.adim.tip == AdimTipi.Kaydirici)
            {
                simPaneli.SetActive(true);
                kaydirici.gameObject.SetActive(true);
                kaydirici.minValue = s.adim.kMin; kaydirici.maxValue = s.adim.kMax;
                kaydirici.SetValueWithoutNotify(s.adim.kBaslangic);
            }
            DevamGuncelle(s);
        }

        void DevamGuncelle(AdimSunumu s)
        {
            bool goster = s.adim.tip != AdimTipi.Son && !(s.adim.SecenekliMi && !s.ilerlenebilir);
            devamBtn.gameObject.SetActive(goster);
            devamBtn.interactable = s.ilerlenebilir || s.adim.tip == AdimTipi.Etkilesim;
            string metin = s.adim.tip == AdimTipi.Etkilesim ? "Etkileşim (E)" : "Devam ▶ (Boşluk)";
            devamYazi.text = metin;
        }

        // ---------- seçenekler ----------
        void SecenekKur(AdimSunumu s)
        {
            foreach (var b in secBtn) Destroy(b.gameObject);
            secBtn.Clear();
            if (!s.adim.SecenekliMi) return;
            foreach (var so in s.secenekler)
            {
                int idx = so.indeks;
                Text t;
                var b = Dugme("Sec" + idx, secenekKap, (idx + 1) + ") " + so.metin, Hex("2B2428"), out t);
                t.fontSize = 26; t.alignment = TextAnchor.MiddleLeft;
                Kaplama(b.image, "secenek_butonu_normal_9slice", Hex("2B2428"));
                t.rectTransform.offsetMin = new Vector2(56, 0); t.rectTransform.offsetMax = new Vector2(-48, 0);
                b.onClick.AddListener(() => y.CevapVer(idx));
                b.interactable = !so.elendi;
                secBtn.Add(b);
            }
        }

        void SecenekGuncelle(AdimSunumu s)
        {
            for (int i = 0; i < secBtn.Count && i < s.secenekler.Count; i++)
            {
                var so = s.secenekler[i];
                secBtn[i].interactable = !so.elendi && !s.ilerlenebilir;
                if (so.elendi) SecDurum(i, "secenek_butonu_elenmis_9slice", Hex("3A3A3A"));
            }
        }

        void SecDurum(int idx, string sprAd, Color yedek)
        {
            if (idx < 0 || idx >= secBtn.Count) return;
            var im = secBtn[idx].image;
            var s = Spr("Arayuz/" + sprAd);
            if (s != null) { im.sprite = s; im.color = Color.white; } else im.color = yedek;
        }

        void CevapGoster(CevapSonucu c)
        {
            if (c == null || !c.gecerli) return;
            if (c.dogru)
            {
                tepkiYazi.color = Yesil;
                tepkiYazi.text = string.IsNullOrEmpty(c.tepki) ? "Doğru!" : c.tepki;
                if (aktif != null) PortreSec(aktif.konusan, "dogru");
                SecDurum(c.secilenIndeks, "secenek_butonu_uzerinde_9slice", Hex("1E6B45"));
            }
            else
            {
                tepkiYazi.color = Kirmizi;
                string e = string.IsNullOrEmpty(c.tepki) ? "Bu doğru değil." : c.tepki;
                if (c.ceza > 0) e += "  (−" + c.ceza + ")";
                tepkiYazi.text = e;
                if (aktif != null) PortreSec(aktif.konusan, "yanlis");
                SecDurum(c.secilenIndeks, "secenek_butonu_yanlis_9slice", Hex("6B1E1E"));
            }
        }

        // ---------- simülasyon çizimi ----------
        void SimTemizle()
        {
            for (int i = simKap.childCount - 1; i >= 0; i--) Destroy(simKap.GetChild(i).gameObject);
        }

        void SimCiz(SimBilgisi s)
        {
            simPaneli.SetActive(true);
            SimTemizle();
            simYazi.text = "";
            if (s.tur == "osmoz" && s.osmoz.cumle != null)
            {
                var o = s.osmoz;
                float w = 240f;
                var hs = Resources.Load<Sprite>("Gorseller/Hucre/hucre_" + o.durum);
                if (hs != null)
                {
                    var hi = Yuz("Hucre", simKap, Color.white);
                    hi.sprite = hs; hi.preserveAspect = true; hi.raycastTarget = false;
                    Konum(hi.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, -290, -250, 290, 250);
                    simYazi.text = "Dış/iç derişim: " + o.oran.ToString("0.00") + " · " + o.cumle + (s.hedefeUlasti ? "\n✓ Hedefe ulaştın!" : "");
                    return;
                }
                // çeper (sabit)
                var cep = Yuz("Cep", simKap, Hex("5D8F3A"));
                Konum(cep.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, -w / 2, -w / 2, w / 2, w / 2);
                // protoplast (küçülür)
                var pro = Yuz("Proto", simKap, Hex("7FD1A0"));
                float p = w * 0.9f * Mathf.Clamp(o.protoplastOlcegi, 0.3f, 1f);
                Konum(pro.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, -p / 2, -p / 2, p / 2, p / 2);
                simYazi.text = "Dış/iç derişim: " + o.oran.ToString("0.00") + " · " + o.cumle + (s.hedefeUlasti ? "\n✓ Hedefe ulaştın!" : "");
            }
            else if (s.tur == "co2" && s.canliEgri != null)
            {
                Cubuklar(s.canliEgri, Yesil, 0.0f, 0.5f);
                if (s.kontrolEgri != null) Cubuklar(s.kontrolEgri, Sari, 0.5f, 1f);
                simYazi.text = "CO₂ (temsili) — yeşil: canlı bitki" + (s.kontrolEgri != null ? " · sarı: kontrol" : "");
            }
            else if (s.tur == "punnett" && s.punnett.genotipler != null)
            {
                var pn = s.punnett;
                for (int i = 0; i < 4 && i < pn.genotipler.Length; i++)
                {
                    float x = 0.3f + (i % 2) * 0.2f, yy = 0.55f - (i / 2) * 0.25f;
                    var k = Yuz("P" + i, simKap, pn.direncli != null && pn.direncli[i] ? Hex("5DFFA6", 0.5f) : Hex("2B2428"));
                    Konum(k.rectTransform, x, yy, x + 0.19f, yy + 0.23f, 0, 0, 0, 0);
                    var tx = Yazi("T", k.transform, 40, Color.white, TextAnchor.MiddleCenter);
                    Doldur(tx.rectTransform); tx.text = pn.genotipler[i];
                }
                simYazi.text = pn.ebeveynA + " × " + pn.ebeveynB + " · aa: " + pn.direncliSayisi + "/" + pn.toplam;
            }
            else if (s.tur == "f2" && s.direncliIndeksler != null)
            {
                for (int i = 0; i < s.toplamFide; i++)
                {
                    bool d = System.Array.IndexOf(s.direncliIndeksler, i) >= 0;
                    float x = 0.1f + (i % 8) * 0.1f, yy = 0.55f - (i / 8) * 0.27f;
                    var f = Yuz("F" + i, simKap, d ? Yesil : Hex("6B8F5A"));
                    var fs = Resources.Load<Sprite>("Gorseller/Sprite/" + (d ? "fide_direncli" : "fide_lekeli"));
                    if (fs != null) { f.sprite = fs; f.color = Color.white; f.preserveAspect = true; }
                    Konum(f.rectTransform, x, yy, x + 0.08f, yy + 0.22f, 0, 0, 0, 0);
                }
                simYazi.text = "Dirençli fide: " + s.direncliIndeksler.Length + "/" + s.toplamFide + (string.IsNullOrEmpty(s.notMetni) ? "" : " · " + s.notMetni);
            }
        }

        void Cubuklar(float[] v, Color c, float x0, float x1)
        {
            int n = v.Length;
            float gen = (x1 - x0) / n;
            for (int i = 0; i < n; i++)
            {
                var b = Yuz("B", simKap, c);
                float h = Mathf.Clamp01(v[i]);
                Konum(b.rectTransform, x0 + i * gen, 0.28f, x0 + (i + 0.8f) * gen, 0.28f + 0.65f * h, 0, 0, 0, 0);
            }
        }

        // ---------- son ----------
        void SonGoster(OyunSonu o)
        {
            sonEkrani.SetActive(true);
            var sz = (o.son != null && !string.IsNullOrEmpty(o.son.gorsel)) ? Spr("Sahneler/" + o.son.gorsel) : null;
            if (sz != null) { sonZemin.sprite = sz; sonZemin.color = new Color(0.9f, 0.9f, 0.9f, 1f); }
            string m = o.son != null ? o.son.ad + "\n\n" + o.son.metin : "";
            m += "\n\nTohum Duvarı: " + o.saglik + "/100";
            m += "\nKavram kartları: " + o.kartlar.Count + "/14";
            if (o.takilanlar.Count > 0) m += "\nTekrar edilecek soru: " + o.takilanlar.Count;
            sonYazi.text = m;
        }

        // ---------- yapı taşları ----------
        static Sprite Spr(string yol) { return Resources.Load<Sprite>("Gorseller/" + yol); }

        void Kaplama(Image im, string ad, Color yedek)
        {
            var s = Spr("Arayuz/" + ad);
            if (s != null) { im.sprite = s; im.type = Image.Type.Sliced; im.color = Color.white; }
            else im.color = yedek;
        }

        GameObject Bos(string ad, Transform ata)
        {
            var g = new GameObject(ad, typeof(RectTransform));
            g.transform.SetParent(ata, false);
            return g;
        }

        Image Yuz(string ad, Transform ata, Color c)
        {
            var g = Bos(ad, ata);
            var im = g.AddComponent<Image>();
            im.color = c;
            return im;
        }

        Text Yazi(string ad, Transform ata, int boy, Color c, TextAnchor hiza)
        {
            var g = Bos(ad, ata);
            var t = g.AddComponent<Text>();
            t.font = font; t.fontSize = boy; t.color = c; t.alignment = hiza;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        Button Dugme(string ad, Transform ata, string yazi, Color c, out Text t)
        {
            var im = Yuz(ad, ata, c);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            t = Yazi("Yazi", im.transform, 28, Color.white, TextAnchor.MiddleCenter);
            t.font = fontKalin;
            Doldur(t.rectTransform);
            t.text = yazi;
            var cb = b.colors;
            cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            b.colors = cb;
            return b;
        }

        Slider SliderYap(Transform ata)
        {
            var kok = Bos("Slider", ata);
            var arka = Yuz("Arka", kok.transform, Hex("3A3036"));
            Konum(arka.rectTransform, 0, 0.35f, 1, 0.65f, 0, 0, 0, 0);
            var alan = Bos("DolguAlani", kok.transform);
            Konum((RectTransform)alan.transform, 0, 0.35f, 1, 0.65f, 0, 0, 0, 0);
            var dolgu = Yuz("Dolgu", alan.transform, Mavi);
            dolgu.rectTransform.anchorMin = new Vector2(0, 0);
            dolgu.rectTransform.anchorMax = new Vector2(0, 1);
            dolgu.rectTransform.offsetMin = dolgu.rectTransform.offsetMax = Vector2.zero;
            var tutAlan = Bos("TutAlani", kok.transform);
            Doldur((RectTransform)tutAlan.transform);
            var tut = Yuz("Tutamac", tutAlan.transform, Turuncu);
            tut.rectTransform.anchorMin = new Vector2(0, 0.1f);
            tut.rectTransform.anchorMax = new Vector2(0, 0.9f);
            tut.rectTransform.sizeDelta = new Vector2(30, 0);
            var s = kok.AddComponent<Slider>();
            s.fillRect = dolgu.rectTransform;
            s.handleRect = tut.rectTransform;
            var ts = Spr("Arayuz/kaydirici_tutamaci");
            if (ts != null) { tut.sprite = ts; tut.color = Color.white; tut.preserveAspect = true; tut.rectTransform.anchorMin = new Vector2(0, 0); tut.rectTransform.anchorMax = new Vector2(0, 1); tut.rectTransform.sizeDelta = new Vector2(70, 0); }
            s.targetGraphic = tut;
            s.direction = Slider.Direction.LeftToRight;
            return s;
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
