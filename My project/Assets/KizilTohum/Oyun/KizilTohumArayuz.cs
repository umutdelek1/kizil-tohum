using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace KizilTohum
{
    /// <summary>
    /// Çalışma zamanında kurulan arayüz (sahne dosyası gerektirmez).
    /// Boş bir sahneye tek bir GameObject + bu bileşeni eklemek yeter; OyunDenetleyici otomatik eklenir.
    /// Görseller Resources/Gorseller/<ad> yolundan okunur; yoksa düz renkli yer tutucu kullanılır.
    /// </summary>
    public class KizilTohumArayuz : MonoBehaviour
    {
        // Bir sahne "slotu": tek resim ya da arka/orta/ön katmanlar + kendi kamera hareketi.
        // Geçiş sırasında eski ve yeni slot birlikte yaşar; eskisi kamerasını sürdürürken yenisi üstte erir.
        class SahneSlot
        {
            public RectTransform kok;
            public CanvasGroup grup;
            public readonly List<RectTransform> katman = new List<RectTransform>();
            public readonly List<int> derinlik = new List<int>(); // 0 arka, 1 orta, 2 ön; -1 tek resim
            public string ad, kamera;
            public float sure, basla;
            public bool giris;    // hafif yakın başlayıp geri çekilir
            public Vector2 fare;  // yumuşatılmış fare paralaksı
        }

        readonly List<SahneSlot> slotlar = new List<SahneSlot>();
        Coroutine gecisRutin;
        CanvasGroup karartGrup;
        Text gecisYazi;
        RectTransform ekran;

        GameObject videoKok;
        VideoPlayer video;
        RawImage videoGorsel;
        Coroutine videoZaman;
        int videoKapandiKare = -1;
        System.Action<bool> videoBitince; // harita dalış videosu için: true = oynatılamadı

        HaritaEkrani harita;
        HucreGorunumu hucre;   // KT04: üç görselin çapraz erimesi (görseller yoksa null, eski çizim)
        bool hucreDenendi;

        // arayüz hareketi (hepsi Update'te unscaledDeltaTime ile akar)
        string metinTam = "";
        float daktiloT;
        bool daktiloAkiyor;
        int daktiloGorunen = -1;
        CanvasGroup altGrup;
        RectTransform altRect;
        float panelT = 999f;
        RectTransform portreRect;
        Vector2 portreTaban;
        float portreT = 999f, portreParlamaT = 999f;
        string sonKonusan;
        RectTransform duvarKapRect;
        Image duvarKapGorsel;
        float duvarSarsintiT = 999f;
        float duvarParlamaT = 999f;
        float duvarGoster = -1f, duvarKaynak, duvarHedef = -1f, duvarKaymaT = 999f;
        RectTransform kartKok;
        CanvasGroup kartGrup;
        Image kartPaneliGorsel;
        float kartT = 999f;

        bool duvarSpriteli;
        readonly List<RectTransform> menuKatman = new List<RectTransform>();
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
            oyun.KlavyeKilitli = () => DaktiloKilidi() || (harita != null && harita.Acik) || menu.activeSelf || kartPaneli.activeSelf || sonEkrani.activeSelf
                || videoKok != null || Time.frameCount == videoKapandiKare;
            string derin = OyunDenetleyici.DerinBaglantiGorevi();
            if (!string.IsNullOrEmpty(derin) && oyun.GoreveGit(derin)) Goster(false);
            else Goster(true);
        }

        void Update()
        {
            Hareket(Time.unscaledDeltaTime);
            if (ipucuKutu != null) ipucuKutu.enabled = !string.IsNullOrEmpty(ipucuYazi.text);
            if (linKutu != null) linKutu.enabled = !string.IsNullOrEmpty(linYazi.text);
            if (menu != null && menu.activeSelf && menuKatman.Count > 0)
            {
                var fm = UnityEngine.InputSystem.Mouse.current;
                Vector2 mp = fm != null ? fm.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                float mx = Mathf.Clamp(mp.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f) * 0.5f + Mathf.Sin(Time.unscaledTime * Tema.MenuKayma) * 0.6f;
                float my = Mathf.Clamp(mp.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f) * 0.3f + Mathf.Cos(Time.unscaledTime * 0.27f) * 0.3f;
                float zum = 1f + Tema.MenuZoom * (1f + Mathf.Sin(Time.unscaledTime * Tema.MenuYaklasma));
                for (int i = 0; i < menuKatman.Count; i++)
                {
                    if (menuKatman[i] == null) continue;
                    menuKatman[i].anchoredPosition = new Vector2(-mx, -my) * Tema.ParalaksGuc[Mathf.Min(i, Tema.ParalaksGuc.Length - 1)];
                    menuKatman[i].localScale = Vector3.one * zum;
                }
            }
            // sinematik kamera: her slot kendi hareketini oynar; fare paralaksı katmanlıda küçük ek
            var fare = UnityEngine.InputSystem.Mouse.current;
            Vector2 fn = Vector2.zero;
            if (fare != null)
            {
                Vector2 p = fare.position.ReadValue();
                fn = -new Vector2(Mathf.Clamp(p.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f), Mathf.Clamp(p.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f));
            }
            float simdi = Time.unscaledTime;
            for (int i = 0; i < slotlar.Count; i++)
            {
                if (fare != null) slotlar[i].fare = Vector2.Lerp(slotlar[i].fare, fn, Time.unscaledDeltaTime * Tema.Takip);
                KameraUygula(slotlar[i], simdi);
            }
            if (video != null && videoGorsel != null && video.texture != null)
            {
                videoGorsel.texture = video.texture;
                videoGorsel.enabled = true;
            }
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (videoKok != null && k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) { VideoBitir(); return; }
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

            var bg = Yuz("Zemin", kok, Tema.Base);
            Doldur(bg.rectTransform);

            // --- oyun ekranı ---
            oyunEkrani = Bos("OyunEkrani", kok);
            Doldur((RectTransform)oyunEkrani.transform);
            sahne = Yuz("Sahne", oyunEkrani.transform, Tema.Sahne);
            Doldur(sahne.rectTransform);
            sahne.preserveAspect = false;

            // üst çubuk
            var ust = Yuz("Ust", oyunEkrani.transform, Tema.Panel);
            Konum(ust.rectTransform, 0, 1, 1, 1, 0, -90, 0, 0);
            gorevBaslik = Yazi("GorevBaslik", ust.transform, Tema.YaziOrta, Tema.Marka, TextAnchor.MiddleLeft);
            gorevBaslik.font = fontBaslik;
            Konum(gorevBaslik.rectTransform, 0, 0, 0.6f, 1, 30, 0, 0, 0);
            var duvarKap = Yuz("DuvarKap", ust.transform, Tema.Kontrol);
            duvarKapGorsel = duvarKap; duvarKapRect = duvarKap.rectTransform;
            Konum(duvarKap.rectTransform, 0.5f, 0.3f, 0.88f, 0.7f, 0, 0, 0, 0);
            var cerceve = Spr("Arayuz/tohum_duvari_cubuk_cercevesi");
            var dolgu = Spr("Arayuz/tohum_duvari_cubuk_dolgu");
            duvarSpriteli = cerceve != null && dolgu != null;
            if (duvarSpriteli) { duvarKap.sprite = cerceve; duvarKap.color = Color.white; }
            duvarDolgu = Yuz("DuvarDolgu", duvarKap.transform, Tema.Basari);
            if (duvarSpriteli) duvarDolgu.sprite = dolgu;
            Doldur(duvarDolgu.rectTransform);
            duvarYazi = Yazi("DuvarYazi", duvarKap.transform, Tema.YaziKucuk, duvarSpriteli ? Tema.Beyaz : Tema.Base, TextAnchor.MiddleCenter);
            duvarYazi.font = fontKalin;
            Doldur(duvarYazi.rectTransform);

            // alt panel
            var alt = Yuz("Alt", oyunEkrani.transform, Tema.Panel);
            Kaplama(alt, "diyalog_paneli_9slice", Tema.Panel);
            Konum(alt.rectTransform, 0, 0, 1, 0.46f, 0, 0, 0, 0);
            altRect = alt.rectTransform;
            altGrup = alt.gameObject.AddComponent<CanvasGroup>();
            var altTik = alt.gameObject.AddComponent<Button>(); // panele tıklayınca daktilo hemen tamamlanır
            altTik.transition = Selectable.Transition.None;
            altTik.targetGraphic = alt;
            altTik.onClick.AddListener(() => DaktiloTamamla());
            portre = Yuz("Portre", oyunEkrani.transform, Color.white);
            portre.preserveAspect = true; portre.raycastTarget = false;
            Konum(portre.rectTransform, 0.01f, 0.46f, 0.20f, 0.46f, 0, 0, 0, 400);
            portreRect = portre.rectTransform; portreTaban = portreRect.anchoredPosition;
            portre.gameObject.SetActive(false);
            konusanYazi = Yazi("Konusan", alt.transform, Tema.YaziOrta, Tema.Konusan, TextAnchor.UpperLeft);
            konusanYazi.font = fontKalin;
            Konum(konusanYazi.rectTransform, 0.04f, 0.76f, 0.6f, 0.87f, 0, 0, 0, 0);
            metinYazi = Yazi("Metin", alt.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.UpperLeft);
            Konum(metinYazi.rectTransform, 0.04f, 0.50f, 0.96f, 0.77f, 0, 0, 0, 0);

            var sk = Bos("Secenekler", alt.transform);
            secenekKap = (RectTransform)sk.transform;
            Konum(secenekKap, 0.035f, 0.22f, 0.70f, 0.52f, 0, 0, 0, 0);
            var vl = sk.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8; vl.childControlHeight = true; vl.childControlWidth = true;
            vl.childForceExpandHeight = true; vl.childForceExpandWidth = true;

            ipucuKutu = Yuz("IpucuKutu", alt.transform, Tema.Saydam);
            Konum(ipucuKutu.rectTransform, 0.72f, 0.5f, 0.975f, 0.90f, 0, 0, 0, 0);
            Kaplama(ipucuKutu, "ipucu_kutusu_9slice", Tema.Ipucu);
            linKutu = Yuz("LinKutu", alt.transform, Tema.Saydam);
            Konum(linKutu.rectTransform, 0.72f, 0.22f, 0.975f, 0.5f, 0, 0, 0, 0);
            Kaplama(linKutu, "ipucu_kutusu_9slice", Tema.Lin);
            if (linKutu.sprite != null) linKutu.color = Tema.LinKutuTon;
            tepkiYazi = Yazi("Tepki", alt.transform, Tema.YaziKucuk, Tema.Hata, TextAnchor.UpperLeft);
            Konum(tepkiYazi.rectTransform, 0.035f, 0.10f, 0.70f, 0.22f, 0, 0, 0, 0);
            ipucuYazi = Yazi("Ipucu", alt.transform, Tema.YaziKucuk, Tema.Bilgi, TextAnchor.UpperLeft);
            Konum(ipucuYazi.rectTransform, 0.73f, 0.52f, 0.965f, 0.88f, 0, 0, 0, 0);
            linYazi = Yazi("Lin", alt.transform, Tema.YaziKucuk, Tema.LinRengi, TextAnchor.UpperLeft);
            Konum(linYazi.rectTransform, 0.73f, 0.24f, 0.965f, 0.48f, 0, 0, 0, 0);

            devamBtn = Dugme("Devam", alt.transform, "Devam ▶ (Boşluk)", Tema.Marka, out devamYazi);
            Konum((RectTransform)devamBtn.transform, 0.72f, 0.10f, 0.975f, 0.21f, 0, 0, 0, 0);
            devamBtn.onClick.AddListener(() => { if (DaktiloTamamla()) return; if (aktif != null && aktif.tip == AdimTipi.Etkilesim && !y.Ilerlenebilir) y.EtkilesimTamamlandi(aktif.nesne); else y.Ilerle(); });
            Text t2;
            ipucuBtn = Dugme("IpucuBtn", alt.transform, "İpucu (H)", Tema.IpucuDugme, out t2);
            Konum((RectTransform)ipucuBtn.transform, 0.58f, 0.10f, 0.71f, 0.21f, 0, 0, 0, 0);
            ipucuBtn.onClick.AddListener(() => y.IpucuIste());
            Text t3;
            var menuBtn = Dugme("MenuBtn", ust.transform, "Menü", Tema.KontrolAlt, out t3);
            Konum((RectTransform)menuBtn.transform, 0.9f, 0.3f, 1f, 0.7f, -10, 0, 0, 0);
            menuBtn.onClick.AddListener(() => Goster(true));

            // simülasyon paneli (kaydırıcı, grafik, tablo)
            simPaneli = Yuz("SimPaneli", oyunEkrani.transform, Tema.Sim).gameObject;
            Konum((RectTransform)simPaneli.transform, 0.05f, 0.48f, 0.95f, 0.9f, 0, 0, 0, 0);
            simKap = (RectTransform)Bos("SimKap", simPaneli.transform).transform;
            Doldur(simKap);
            simYazi = Yazi("SimYazi", simPaneli.transform, Tema.YaziOrta, Tema.Basari, TextAnchor.LowerCenter);
            Konum(simYazi.rectTransform, 0, 0.14f, 1, 0.3f, 10, 0, -10, 0);
            kaydirici = SliderYap(simPaneli.transform);
            Konum((RectTransform)kaydirici.transform, 0.1f, 0.02f, 0.9f, 0.13f, 0, 0, 0, 0);
            kaydirici.onValueChanged.AddListener(v => { if (aktif != null && aktif.tip == AdimTipi.Kaydirici) y.KaydiriciDegisti(v); });

            // geçiş örtüsü (karartma + "haftalar sonra" kartı): oyun ekranının en üstünde, tıklamayı engellemez
            ekran = (RectTransform)oyunEkrani.transform;
            var ortu = Bos("GecisOrtusu", oyunEkrani.transform);
            Doldur((RectTransform)ortu.transform);
            karartGrup = ortu.AddComponent<CanvasGroup>();
            karartGrup.alpha = 0f; karartGrup.blocksRaycasts = false; karartGrup.interactable = false;
            var siyah = Yuz("Siyah", ortu.transform, Tema.Base);
            siyah.raycastTarget = false;
            Doldur(siyah.rectTransform);
            gecisYazi = Yazi("GecisYazi", ortu.transform, Tema.YaziBaslik, Tema.Beyaz, TextAnchor.MiddleCenter);
            gecisYazi.font = fontBaslik;
            Doldur(gecisYazi.rectTransform);

            // kart paneli
            kartPaneli = Yuz("KartPaneli", kok, Tema.Orten).gameObject;
            Doldur((RectTransform)kartPaneli.transform);
            kartPaneliGorsel = kartPaneli.GetComponent<Image>();
            var kart = Yuz("Kart", kartPaneli.transform, Tema.Kart);
            kartKok = kart.rectTransform;
            kartGrup = kart.gameObject.AddComponent<CanvasGroup>();
            Kaplama(kart, "kavram_karti_cercevesi_9slice", Tema.Kart);
            Konum(kart.rectTransform, 0.15f, 0.08f, 0.85f, 0.92f, 0, 0, 0, 0);
            kartBaslik = Yazi("KartBaslik", kart.transform, Tema.YaziBaslik, Tema.Konusan, TextAnchor.UpperCenter);
            kartBaslik.font = fontBaslik;
            Konum(kartBaslik.rectTransform, 0.03f, 0.86f, 0.97f, 0.97f, 0, 0, 0, 0);
            kartGorsel = Yuz("KartGorsel", kart.transform, Color.white);
            kartGorsel.preserveAspect = true;
            Konum(kartGorsel.rectTransform, 0.04f, 0.34f, 0.5f, 0.84f, 0, 0, 0, 0);
            kartMetin = Yazi("KartMetin", kart.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.UpperLeft);
            Konum(kartMetin.rectTransform, 0.53f, 0.2f, 0.96f, 0.85f, 0, 0, 0, 0);
            Text t4;
            var kb = Dugme("KartKapat", kart.transform, "Defterime ekle ✓", Tema.Marka, out t4);
            Konum((RectTransform)kb.transform, 0.3f, 0.04f, 0.7f, 0.16f, 0, 0, 0, 0);
            kb.onClick.AddListener(() => kartPaneli.SetActive(false));
            kartPaneli.SetActive(false);

            // son ekranı
            sonZemin = Yuz("SonEkrani", kok, Tema.SonZemin);
            sonEkrani = sonZemin.gameObject;
            Doldur((RectTransform)sonEkrani.transform);
            var sonPanel = Yuz("SonPanel", sonEkrani.transform, Tema.PanelHafif);
            Kaplama(sonPanel, "diyalog_paneli_9slice", Tema.PanelHafif);
            Konum(sonPanel.rectTransform, 0.52f, 0.2f, 0.97f, 0.93f, 0, 0, 0, 0);
            sonYazi = Yazi("SonYazi", sonPanel.transform, Tema.YaziBuyuk, Tema.Beyaz, TextAnchor.UpperCenter);
            Konum(sonYazi.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f, 0, 0, 0, 0);
            Text t5;
            var sb = Dugme("SonMenu", sonEkrani.transform, "Ana menü", Tema.Marka, out t5);
            Konum((RectTransform)sb.transform, 0.64f, 0.06f, 0.86f, 0.15f, 0, 0, 0, 0);
            sb.onClick.AddListener(() => HaritaAc());
            sonEkrani.SetActive(false);

            // menü
            menuZemin = Yuz("Menu", kok, Tema.MenuZemin);
            menu = menuZemin.gameObject;
            var kapak = Spr("Menu/kapak_arka_plan");
            if (kapak != null) { menuZemin.sprite = kapak; menuZemin.color = Tema.KapakTon; }
            Doldur((RectTransform)menu.transform);
            // canlı arka plan: Ada çekmecede — katmanlar fareyle ve kendiliğinden hafifçe kayar
            var mArka = Spr("Parallax/tohum_duvari_arka_plan_dolu");
            var mOrta = Spr("Parallax/tohum_duvari_orta_plan");
            var mOn = Spr("Parallax/tohum_duvari_on_plan");
            if (mArka != null && mOrta != null)
            {
                menuZemin.sprite = null; menuZemin.color = Tema.Base;
                var ml = new[] { mArka, mOrta, mOn };
                for (int i = 0; i < ml.Length; i++)
                {
                    if (ml[i] == null) continue;
                    var im = Yuz("MenuKatman" + i, menu.transform, Color.white);
                    im.sprite = ml[i]; im.raycastTarget = false;
                    var r = im.rectTransform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(-Tema.ParalaksMenu, -Tema.ParalaksMenu); r.offsetMax = new Vector2(Tema.ParalaksMenu, Tema.ParalaksMenu);
                    menuKatman.Add(r);
                }
            }
            var logoPlaka = Yuz("LogoPlaka", menu.transform, Tema.PanelHafif);
            Kaplama(logoPlaka, "diyalog_paneli_9slice", Tema.PanelHafif);
            logoPlaka.color = Tema.LogoPlakaTon; logoPlaka.raycastTarget = false;
            Konum(logoPlaka.rectTransform, 0.025f, 0.585f, 0.385f, 0.975f, 0, 0, 0, 0);
            var baslik = Yazi("MenuBaslik", menu.transform, Tema.YaziLogo, Tema.Marka, TextAnchor.MiddleLeft);
            baslik.font = fontBaslik;
            baslik.text = "KIZIL TOHUM";
            var logo = Spr("Menu/logo_kizil_tohum");
            if (logo != null)
            {
                var li = Yuz("Logo", menu.transform, Color.white);
                li.sprite = logo; li.preserveAspect = true; li.raycastTarget = false;
                Konum(li.rectTransform, 0.03f, 0.66f, 0.40f, 0.97f, 0, 0, 0, 0);
                baslik.text = "";
            }
            Konum(baslik.rectTransform, 0.05f, 0.76f, 0.45f, 0.95f, 0, 0, 0, 0);
            var alti = Yazi("MenuAlti", menu.transform, Tema.YaziOrta, Tema.Bilgi, TextAnchor.MiddleLeft);
            alti.text = "TYT Biyoloji · Umut-1 Mars Kolonisi";
            Konum(alti.rectTransform, 0.055f, 0.60f, 0.40f, 0.67f, 0, 0, 0, 0);

            MenuDugme("Yeni Oyun", 0.50f, Tema.YaziOrta, () => { HaritaKayit.Sifirla(); KartKumesi.Sifirla(); oyun.YeniOyun(); HaritaAc(); }); // tüm ilerleme sıfırlanır; harita gündüze döner
            MenuDugme("Devam Et", 0.41f, Tema.YaziOrta, () => { if (oyun.KayitVarMi && oyun.DevamEt()) { Uygula(); Goster(false); } });
            MenuDugme("Görev 1 · Yüz Çekmece", 0.32f, Tema.YaziKucuk, () => GorevAc("KT01"));
            MenuDugme("Görev 2 · Islak Toprakta Susuzluk", 0.245f, Tema.YaziKucuk, () => GorevAc("KT04"));
            MenuDugme("Görev 3 · Kırk Yedi", 0.16f, Tema.YaziKucuk, () => GorevAc("KT08"));
            Text zt;
            var zb = Dugme("Zorluk", menu.transform, "Zorluk: Normal", Tema.KontrolAlt, out zt, "secenek_butonu_normal_9slice");
            zt.fontSize = Tema.YaziKucuk;
            Konum((RectTransform)zb.transform, 0.05f, 0.065f, 0.30f, 0.135f, 0, 0, 0, 0);
            zb.onClick.AddListener(() =>
            {
                if (zorlukCarpan < 0.75f) { zorlukCarpan = 1f; zt.text = "Zorluk: Normal"; }
                else if (zorlukCarpan < 1.25f) { zorlukCarpan = 1.5f; zt.text = "Zorluk: Zor"; }
                else { zorlukCarpan = 0.5f; zt.text = "Zorluk: Kolay"; }
                Uygula();
            });
            menuNot = Yazi("MenuNot", menu.transform, Tema.YaziKucuk, Tema.Soluk, TextAnchor.MiddleCenter);
            menuNot.alignment = TextAnchor.LowerLeft;
            Konum(menuNot.rectTransform, 0.05f, 0.005f, 0.7f, 0.06f, 0, 0, 0, 0);
            menuNot.text = "Boşluk/Enter: devam · 1-4: seçenek · H: ipucu · E: etkileşim";
            if (oyun.IcerikHatalari.Count > 0) menuNot.text = "İÇERİK HATASI: " + oyun.IcerikHatalari[0];

            // koloni haritası: en son kurulur, böylece her şeyin üstünde durur
            harita = gameObject.AddComponent<HaritaEkrani>();
            harita.Kur(kok, font, fontKalin, fontBaslik, y);
            harita.GorevSecildi = HaritaGorevSecildi;
            harita.MenuIstendi = () => { harita.Kapat(); Goster(true); };
            harita.VideoOynat = (dosya, bitince) => VideoOynat(dosya, harita.Kok, true, bitince);
        }

        // ---------- koloni haritası ----------
        void HaritaAc()
        {
            if (harita == null) { Goster(true); return; }
            VideoIptal();
            menu.SetActive(false);
            oyunEkrani.SetActive(false);
            Uygula();
            harita.Ac();
        }

        void HaritaGorevSecildi(string id)
        {
            VideoIptal();
            harita.Kapat();
            Uygula();
            if (oyun.GoreveGit(id)) Goster(false);
            else HaritaAc(); // bilinmeyen görev: haritada kal
        }

        void GorevAc(string id)
        {
            Uygula();
            if (oyun.GoreveGit(id)) Goster(false);
        }

        void Uygula() { y.CezaCarpani = zorlukCarpan; }

        void MenuDugme(string yazi, float yPos, int boyut, UnityEngine.Events.UnityAction act)
        {
            Text t;
            var b = Dugme("M_" + yazi, menu.transform, yazi, Tema.Kontrol, out t, "secenek_butonu_normal_9slice");
            t.fontSize = boyut; t.alignment = TextAnchor.MiddleLeft;
            t.rectTransform.offsetMin = new Vector2(Tema.MetinSol, 0); t.rectTransform.offsetMax = new Vector2(-Tema.MetinSag, 0);
            var cb = b.colors; cb.highlightedColor = Tema.Hover; cb.pressedColor = Tema.Basili; b.colors = cb;
            Konum((RectTransform)b.transform, 0.05f, yPos, 0.34f, yPos + (boyut >= Tema.YaziOrta ? 0.08f : 0.07f), 0, 0, 0, 0);
            b.onClick.AddListener(act);
        }

        // ---------- arayüz hareketi ----------
        static float YavasBitir(float f) { f = Mathf.Clamp01(f); return 1f - (1f - f) * (1f - f); }

        void Hareket(float dt)
        {
            // daktilo
            if (daktiloAkiyor)
            {
                daktiloT += dt;
                int n = Tema.DaktiloHiz > 0f ? (int)(daktiloT / Tema.DaktiloHiz) : metinTam.Length;
                if (n >= metinTam.Length) DaktiloTamamla();
                else if (n != daktiloGorunen) { daktiloGorunen = n; DaktiloYaz(n); }
            }
            // diyalog paneli
            if (panelT < Tema.PanelGirisSure) { panelT += dt; PanelUygula(); }
            // portre: kayma ve (konuşan değişince) parlama
            if (portreT < Tema.PortreGirisSure || portreParlamaT < Tema.PortreParlamaSure)
            {
                portreT += dt; portreParlamaT += dt;
                PortreUygula();
            }
            // tohum duvarı: yumuşak kayma, sarsıntı, kırmızı parlama
            if (duvarKaymaT < Tema.DuvarKaymaSure)
            {
                duvarKaymaT += dt;
                duvarGoster = Mathf.Lerp(duvarKaynak, duvarHedef, Mathf.SmoothStep(0f, 1f, duvarKaymaT / Tema.DuvarKaymaSure));
                DuvarDolguAyarla(duvarGoster);
            }
            if (duvarKapRect != null)
            {
                if (duvarSarsintiT < Tema.DuvarSarsintiSure)
                {
                    duvarSarsintiT += dt;
                    float k = Mathf.Clamp01(1f - duvarSarsintiT / Tema.DuvarSarsintiSure);
                    duvarKapRect.anchoredPosition = Random.insideUnitCircle * Tema.DuvarSarsintiGuc * k;
                }
                else if (duvarKapRect.anchoredPosition != Vector2.zero) duvarKapRect.anchoredPosition = Vector2.zero;
                if (duvarParlamaT < Tema.DuvarParlamaSure)
                {
                    duvarParlamaT += dt;
                    Color taban = duvarSpriteli ? Tema.Beyaz : Tema.Kontrol;
                    duvarKapGorsel.color = Color.Lerp(Tema.Hata, taban, YavasBitir(duvarParlamaT / Tema.DuvarParlamaSure));
                }
            }
            // kavram kartı
            if (kartT < Tema.KartGirisSure) { kartT += dt; KartUygula(); }
        }

        void DaktiloYaz(int n)
        {
            // gizli kısım şeffaf yazılır: satır kaymaz, kelimeler sıçramaz
            metinYazi.text = metinTam.Substring(0, n) + "<color=#00000000>" + metinTam.Substring(n) + "</color>";
        }

        void DaktiloBaslat(string metin)
        {
            metinTam = metin;
            daktiloT = 0f;
            daktiloGorunen = -1;
            daktiloAkiyor = metin.Length > 0 && Tema.DaktiloHiz > 0f;
            if (daktiloAkiyor) DaktiloYaz(0);
            else metinYazi.text = metin;
        }

        /// <summary>Akıyorsa metni hemen tamamlar; akıyordu ise true döner.</summary>
        bool DaktiloTamamla()
        {
            if (!daktiloAkiyor) return false;
            daktiloAkiyor = false;
            metinYazi.text = metinTam;
            return true;
        }

        // Boşluk/Enter daktilo sürerken yalnızca metni tamamlar, adımı ilerletmez.
        bool DaktiloKilidi()
        {
            if (!daktiloAkiyor) return false;
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null || !(k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) return false;
            DaktiloTamamla();
            return true;
        }

        void PanelUygula()
        {
            if (altGrup == null || altRect == null) return;
            float f = YavasBitir(panelT / Tema.PanelGirisSure);
            altGrup.alpha = f;
            altRect.anchoredPosition = new Vector2(0f, -Tema.PanelGirisMesafe * (1f - f));
        }

        void PortreGiris(string konusan)
        {
            if (portreRect == null) return;
            portreT = 0f;
            portreParlamaT = konusan == sonKonusan ? 999f : 0f; // parlama yalnızca konuşan değişince
            sonKonusan = konusan;
            PortreUygula();
        }

        void PortreUygula()
        {
            if (portre == null || portreRect == null) return;
            float g = YavasBitir(portreT / Tema.PortreGirisSure);
            portreRect.anchoredPosition = portreTaban + new Vector2(-Tema.PortreGirisMesafe * (1f - g), 0f);
            float p = YavasBitir(portreParlamaT / Tema.PortreParlamaSure);
            portre.color = Color.Lerp(Tema.PortreParlamaBaslangic, Tema.Beyaz, p);
            portreRect.localScale = Vector3.one * Mathf.Lerp(Tema.PortreParlamaOlcek, 1f, p);
        }

        void KartUygula()
        {
            if (kartKok == null || kartGrup == null) return;
            float f = YavasBitir(kartT / Tema.KartGirisSure);
            kartKok.localScale = Vector3.one * Mathf.Lerp(Tema.KartBaslangicOlcek, 1f, f);
            kartKok.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(Tema.KartBaslangicAci, 0f, f));
            kartGrup.alpha = f;
            if (kartPaneliGorsel != null)
            {
                var c = Tema.Orten; c.a *= f; // arka plan kararır
                kartPaneliGorsel.color = c;
            }
        }

        void Goster(bool menuGoster)
        {
            if (menuGoster) VideoIptal();
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
            y.KavramKartiAcildi += k => { KartKumesi.Ekle(k.id); kartBaslik.text = k.baslik; kartMetin.text = k.konu + "\n\n" + k.metin; var ks = Spr("Kartlar/" + k.id); kartGorsel.sprite = ks; kartGorsel.gameObject.SetActive(ks != null); kartPaneli.SetActive(true); kartT = 0f; KartUygula(); };
            y.SimulasyonGuncellendi += SimCiz;
            y.OyunBitti += o => SonGoster(o);
            y.GorevBitti += g => { if (harita != null) { harita.GorevTamamlandi(g.id); HaritaAc(); } };
        }

        // ---------- sinematik sahne ----------
        // Katmanlı sahne (Parallax/<ad>_arka_plan_dolu, _orta_plan, _on_plan) varsa onu, yoksa tek resmi kurar.
        SahneSlot SlotKur(Adim a, bool giris)
        {
            var s = new SahneSlot();
            var go = Bos("Sahne_" + a.gorsel, sahne.transform);
            s.kok = (RectTransform)go.transform;
            Doldur(s.kok);
            s.grup = go.AddComponent<CanvasGroup>();
            s.grup.blocksRaycasts = false; s.grup.interactable = false;
            s.ad = a.gorsel;
            s.kamera = string.IsNullOrEmpty(a.kamera) ? KameraTipi.Itme : a.kamera;
            s.sure = a.kameraSure > 0f ? a.kameraSure : Tema.KameraSure;
            s.basla = Time.unscaledTime;
            s.giris = giris;

            var arka = Spr("Parallax/" + a.gorsel + "_arka_plan_dolu");
            var orta = Spr("Parallax/" + a.gorsel + "_orta_plan");
            var on = Spr("Parallax/" + a.gorsel + "_on_plan");
            if (arka != null && orta != null)
            {
                var ks = new[] { arka, orta, on };
                for (int i = 0; i < ks.Length; i++)
                {
                    if (ks[i] == null) continue;
                    var im = Yuz("Katman" + i, s.kok, Color.white);
                    im.sprite = ks[i]; im.raycastTarget = false; im.preserveAspect = false;
                    var r = im.rectTransform;
                    r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                    r.offsetMin = new Vector2(-Tema.ParalaksSahne, -Tema.ParalaksSahne); r.offsetMax = new Vector2(Tema.ParalaksSahne, Tema.ParalaksSahne);
                    s.katman.Add(r); s.derinlik.Add(i);
                }
                return s;
            }
            var spr = Resources.Load<Sprite>("Gorseller/Sahneler/" + a.gorsel);
            if (spr == null) spr = Resources.Load<Sprite>("Gorseller/" + a.gorsel);
            if (spr != null)
            {
                var im = Yuz("Resim", s.kok, Color.white);
                im.sprite = spr; im.raycastTarget = false; im.preserveAspect = false;
                Doldur(im.rectTransform);
                s.katman.Add(im.rectTransform); s.derinlik.Add(-1);
            }
            return s; // resim de yoksa slot boş kalır, arkadaki Tema.Sahne rengi görünür
        }

        // Zamanla kendi oynayan kamera: itme / cekme / kaydirmaSol / kaydirmaSag / sabit.
        void KameraUygula(SahneSlot s, float simdi)
        {
            float e = simdi - s.basla;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / s.sure));
            float yakin = s.giris ? 1f + Tema.GirisYakinlik * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / Tema.GirisCekilmeSure))) : 1f;
            for (int i = 0; i < s.katman.Count; i++)
            {
                var r = s.katman[i];
                if (r == null) continue;
                int d = s.derinlik[i];
                bool tek = d < 0;
                float hedef = tek ? Tema.KenBurnsOlcek : Tema.KatmanOlcek[d];
                float kay = tek ? Tema.KenBurnsKaydirma : Tema.KaydirmaOfset[d];
                float olcek = 1f, x = 0f;
                switch (s.kamera)
                {
                    case KameraTipi.Cekme: olcek = Mathf.Lerp(hedef, 1f, t); if (tek) x = Mathf.Lerp(Tema.KenBurnsKayma, 0f, t); break;
                    case KameraTipi.KaydirmaSol: olcek = hedef; x = Mathf.Lerp(-kay, kay, t); break; // kamera sola → içerik sağa
                    case KameraTipi.KaydirmaSag: olcek = hedef; x = Mathf.Lerp(kay, -kay, t); break;
                    case KameraTipi.Sabit: break;
                    default: olcek = Mathf.Lerp(1f, hedef, t); if (tek) x = Mathf.Lerp(0f, Tema.KenBurnsKayma, t); break; // itme
                }
                Vector2 p = new Vector2(x, 0f);
                if (!tek) p += s.fare * Tema.ParalaksGuc[d];
                r.anchoredPosition = p;
                r.localScale = Vector3.one * (olcek * yakin);
            }
        }

        void SahneDegistir(Adim a)
        {
            if (string.IsNullOrEmpty(a.gorsel)) return; // görsel yoksa önceki sahne kamerasıyla sürer
            string gecis = string.IsNullOrEmpty(a.gecis) ? GecisTipi.Capraz : a.gecis;
            var onceki = slotlar.Count > 0 ? slotlar[slotlar.Count - 1] : null;
            if (onceki != null && onceki.ad == a.gorsel && gecis == GecisTipi.Capraz) return; // aynı resim: kesme yok
            GecisiBitir();
            var yeni = SlotKur(a, onceki != null);
            slotlar.Add(yeni);
            KameraUygula(yeni, Time.unscaledTime);
            if (onceki == null) return;
            yeni.grup.alpha = 0f;
            switch (gecis)
            {
                case GecisTipi.Karart: gecisRutin = StartCoroutine(KarartRutin(yeni, null)); break;
                case GecisTipi.HaftalarSonra: gecisRutin = StartCoroutine(KarartRutin(yeni, Tema.HaftalarSonraMetni)); break;
                case GecisTipi.Sarsinti: gecisRutin = StartCoroutine(CaprazRutin(yeni, true)); break;
                default: gecisRutin = StartCoroutine(CaprazRutin(yeni, false)); break;
            }
        }

        // Eski sahne kamerasını sürdürürken yenisi üstte erir; istenirse ekran sarsılır.
        IEnumerator CaprazRutin(SahneSlot yeni, bool sars)
        {
            float t0 = Time.unscaledTime;
            float son = sars ? Mathf.Max(Tema.CaprazSure, Tema.SarsintiSure) : Tema.CaprazSure;
            for (float e = 0f; e < son; e = Time.unscaledTime - t0)
            {
                yeni.grup.alpha = Mathf.Clamp01(e / Tema.CaprazSure);
                if (sars) ekran.anchoredPosition = e < Tema.SarsintiSure ? Random.insideUnitCircle * Tema.SarsintiGuc * (1f - e / Tema.SarsintiSure) : Vector2.zero;
                yield return null;
            }
            gecisRutin = null;
            GecisiBitir();
        }

        // Siyaha erir, sahneyi değiştirir; metin verilirse ortada yazı kartı gösterir, sonra açılır.
        IEnumerator KarartRutin(SahneSlot yeni, string metin)
        {
            yield return Gecir(Tema.KarartSure, f => karartGrup.alpha = f);
            EskileriSil();
            yeni.grup.alpha = 1f;
            yeni.basla = Time.unscaledTime; // kamera karanlıktan çıkınca başlasın
            if (!string.IsNullOrEmpty(metin))
            {
                gecisYazi.text = metin;
                yield return Gecir(Tema.YaziKartGiris, YaziAlfa);
                yield return new WaitForSecondsRealtime(Tema.HaftalarBekle);
                yield return Gecir(Tema.YaziKartGiris, f => YaziAlfa(1f - f));
                gecisYazi.text = "";
            }
            yield return Gecir(Tema.KarartSure, f => karartGrup.alpha = 1f - f);
            gecisRutin = null;
            GecisiBitir();
        }

        IEnumerator Gecir(float sure, System.Action<float> uygula)
        {
            float t0 = Time.unscaledTime;
            for (float e = 0f; e < sure; e = Time.unscaledTime - t0)
            {
                uygula(e / sure);
                yield return null;
            }
            uygula(1f);
        }

        void YaziAlfa(float a)
        {
            var c = Tema.Beyaz; c.a = a;
            gecisYazi.color = c;
        }

        // Yarım kalan geçişi anında tamamlar: yalnızca en yeni sahne kalır, örtü ve sarsıntı sıfırlanır.
        void GecisiBitir()
        {
            if (gecisRutin != null) { StopCoroutine(gecisRutin); gecisRutin = null; }
            EskileriSil();
            if (slotlar.Count > 0) slotlar[slotlar.Count - 1].grup.alpha = 1f;
            karartGrup.alpha = 0f;
            gecisYazi.text = "";
            ekran.anchoredPosition = Vector2.zero;
        }

        void EskileriSil()
        {
            while (slotlar.Count > 1)
            {
                if (slotlar[0].kok != null) Destroy(slotlar[0].kok.gameObject);
                slotlar.RemoveAt(0);
            }
        }

        // ---------- video ----------
        // StreamingAssets/Video/<dosya> sessiz oynar; yoksa ya da hazırlanamazsa uyarı yazıp adımın resmine düşer.
        void VideoBaslat(string dosya) { VideoOynat(dosya, oyunEkrani.transform, false, null); }

        // Başladıysa true. Harita (sessiz) için dosya yoksa uyarı yazmaz; hata olursa bitince(true) ile haber verir.
        bool VideoOynat(string dosya, Transform ata, bool sessiz, System.Action<bool> bitince)
        {
            VideoIptal();
            if (string.IsNullOrEmpty(dosya)) return false;
            string yol = Application.streamingAssetsPath + "/Video/" + dosya;
            if (!yol.Contains("://") && !System.IO.File.Exists(yol)) // WebGL/Android'de yol URL'dir, denetlenemez
            {
                if (!sessiz) Debug.LogWarning("KizilTohum: video bulunamadı, sahne resmi kullanılıyor: " + yol);
                return false;
            }
            videoBitince = bitince;
            var zemin = Yuz("Video", ata, Tema.Base);
            videoKok = zemin.gameObject;
            Doldur(zemin.rectTransform);
            if (ata == oyunEkrani.transform) videoKok.transform.SetSiblingIndex(karartGrup.transform.GetSiblingIndex());
            else videoKok.transform.SetAsLastSibling();
            var atla = videoKok.AddComponent<Button>();
            atla.transition = Selectable.Transition.None;
            atla.onClick.AddListener(() => VideoBitir());
            var gor = Bos("Goruntu", videoKok.transform);
            videoGorsel = gor.AddComponent<RawImage>();
            videoGorsel.raycastTarget = false;
            videoGorsel.enabled = false;
            var oran = gor.AddComponent<AspectRatioFitter>();
            oran.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            var vp = videoKok.AddComponent<VideoPlayer>();
            video = vp;
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.skipOnDrop = true;
            vp.waitForFirstFrame = true;
            vp.source = VideoSource.Url;
            vp.url = yol;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.APIOnly;
            vp.errorReceived += (p, m) =>
            {
                if (p != video) return;
                Debug.LogWarning("KizilTohum: video oynatılamadı (" + dosya + "): " + m);
                VideoBitir(true);
            };
            vp.prepareCompleted += p =>
            {
                if (p != video) return;
                if (videoZaman != null) { StopCoroutine(videoZaman); videoZaman = null; }
                if (p.width == 0 || p.height == 0) { Debug.LogWarning("KizilTohum: video boyutu okunamadı: " + dosya); VideoBitir(true); return; }
                oran.aspectRatio = (float)p.width / p.height;
                p.Play();
            };
            vp.loopPointReached += p => { if (p == video) VideoBitir(); };
            vp.Prepare();
            videoZaman = StartCoroutine(VideoZamanAsimi(vp, dosya));
            return true;
        }

        IEnumerator VideoZamanAsimi(VideoPlayer vp, string dosya)
        {
            yield return new WaitForSecondsRealtime(Tema.VideoHazirlikSiniri);
            videoZaman = null;
            if (vp == video && !vp.isPrepared)
            {
                Debug.LogWarning("KizilTohum: video zamanında hazırlanamadı: " + dosya);
                VideoBitir(true);
            }
        }

        // Video bitince, tıklanınca ya da Boşluk/Enter'a basılınca kapanır; altındaki sahne zaten kurulu.
        void VideoBitir(bool hata = false)
        {
            if (videoZaman != null) { StopCoroutine(videoZaman); videoZaman = null; }
            if (videoKok == null) return;
            if (video != null) video.Stop();
            Destroy(videoKok);
            videoKok = null; video = null; videoGorsel = null;
            videoKapandiKare = Time.frameCount;
            var b = videoBitince; videoBitince = null;
            if (b != null) b(hata);
        }

        // Dışarıdan iptal (menü, harita, yeni video): bitince geri çağrısı tetiklenmez.
        void VideoIptal()
        {
            videoBitince = null;
            VideoBitir();
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
            float f = Mathf.Clamp01(v / 100f);
            if (duvarGoster < 0f) { duvarGoster = f; duvarKaynak = f; duvarHedef = f; duvarKaymaT = 999f; DuvarDolguAyarla(f); }
            else if (!Mathf.Approximately(f, duvarHedef)) { duvarKaynak = duvarGoster; duvarHedef = f; duvarKaymaT = 0f; } // çubuk Hareket()'te kayar
            if (duvarSpriteli) duvarDolgu.color = v >= 85 ? Tema.CubukIyi : v >= 60 ? Tema.CubukOrta : Tema.CubukDusuk;
            else duvarDolgu.color = v >= 85 ? Tema.Basari : v >= 60 ? Tema.Uyari : Tema.Hata;
        }

        void DuvarDolguAyarla(float f)
        {
            var r = duvarDolgu.rectTransform;
            if (duvarSpriteli)
            {
                r.anchorMin = new Vector2(0.013f, 0.13f);
                r.anchorMax = new Vector2(0.013f + 0.974f * f, 0.87f);
            }
            else
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = new Vector2(f, 1);
            }
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void AdimCiz(AdimSunumu s)
        {
            aktif = s.adim;
            DuvarCiz(s.saglik);
            gorevBaslik.text = s.gorev.id + " · " + s.gorev.baslik + "   (" + s.adimNo + "/" + s.toplamAdim + ")";
            konusanYazi.text = string.IsNullOrEmpty(s.adim.konusan) ? "" : s.adim.konusan;
            DaktiloBaslat(s.adim.metin ?? "");
            PortreSec(s.adim.konusan, null);
            PortreGiris(s.adim.konusan);
            panelT = 0f; PanelUygula();
            tepkiYazi.text = ""; ipucuYazi.text = ""; linYazi.text = "";
            SahneDegistir(s.adim);
            if (harita == null || !harita.Acik) VideoBaslat(s.adim.video);
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
                var b = Dugme("Sec" + idx, secenekKap, (idx + 1) + ") " + so.metin, Tema.Kontrol, out t, "secenek_butonu_normal_9slice");
                t.fontSize = Tema.YaziKucuk; t.alignment = TextAnchor.MiddleLeft;
                t.rectTransform.offsetMin = new Vector2(Tema.MetinSol, 0); t.rectTransform.offsetMax = new Vector2(-Tema.MetinSag, 0);
                SpriteDurumlari(b);
                b.gameObject.AddComponent<CanvasGroup>();
                b.gameObject.AddComponent<DugmeHareketi>().gecikme = secBtn.Count * Tema.SecenekAralik;
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
                if (so.elendi) SecDurum(i, "secenek_butonu_elenmis_9slice", Tema.Elenmis);
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
                tepkiYazi.color = Tema.Basari;
                tepkiYazi.text = string.IsNullOrEmpty(c.tepki) ? "Doğru!" : c.tepki;
                if (aktif != null) PortreSec(aktif.konusan, "dogru");
                SecDurum(c.secilenIndeks, "secenek_butonu_dogru_9slice", Tema.Dogru);
            }
            else
            {
                tepkiYazi.color = Tema.Hata;
                string e = string.IsNullOrEmpty(c.tepki) ? "Bu doğru değil." : c.tepki;
                if (c.ceza > 0) e += "  (−" + c.ceza + ")";
                tepkiYazi.text = e;
                if (aktif != null) PortreSec(aktif.konusan, "yanlis");
                duvarSarsintiT = 0f; duvarParlamaT = 0f;
                SecDurum(c.secilenIndeks, "secenek_butonu_yanlis_9slice", Tema.Yanlis);
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
            bool hucreAcikti = hucre != null && hucre.gameObject.activeSelf;
            if (hucre != null) hucre.gameObject.SetActive(false);
            if (s.tur == "osmoz" && s.osmoz.cumle != null)
            {
                var o = s.osmoz;
                if (hucre == null && !hucreDenendi)
                {
                    hucreDenendi = true;
                    hucre = HucreGorunumu.Kur(simPaneli.transform, fontKalin);
                    if (hucre != null) hucre.transform.SetSiblingIndex(simKap.GetSiblingIndex() + 1); // yazının altında kalsın
                }
                if (hucre != null)
                {
                    hucre.gameObject.SetActive(true);
                    hucre.Ayarla(o, !hucreAcikti);
                    simYazi.text = "Dış/iç derişim: " + o.oran.ToString("0.00") + " · " + o.cumle + (s.hedefeUlasti ? "\n✓ Hedefe ulaştın!" : "");
                    return;
                }
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
                var cep = Yuz("Cep", simKap, Tema.Cep);
                Konum(cep.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, -w / 2, -w / 2, w / 2, w / 2);
                // protoplast (küçülür)
                var pro = Yuz("Proto", simKap, Tema.Protoplast);
                float p = w * 0.9f * Mathf.Clamp(o.protoplastOlcegi, 0.3f, 1f);
                Konum(pro.rectTransform, 0.5f, 0.64f, 0.5f, 0.64f, -p / 2, -p / 2, p / 2, p / 2);
                simYazi.text = "Dış/iç derişim: " + o.oran.ToString("0.00") + " · " + o.cumle + (s.hedefeUlasti ? "\n✓ Hedefe ulaştın!" : "");
            }
            else if (s.tur == "co2" && s.canliEgri != null)
            {
                Cubuklar(s.canliEgri, Tema.Basari, 0.0f, 0.5f);
                if (s.kontrolEgri != null) Cubuklar(s.kontrolEgri, Tema.Uyari, 0.5f, 1f);
                simYazi.text = "CO₂ (temsili) — yeşil: canlı bitki" + (s.kontrolEgri != null ? " · sarı: kontrol" : "");
            }
            else if (s.tur == "punnett" && s.punnett.genotipler != null)
            {
                var pn = s.punnett;
                for (int i = 0; i < 4 && i < pn.genotipler.Length; i++)
                {
                    float x = 0.3f + (i % 2) * 0.2f, yy = 0.55f - (i / 2) * 0.25f;
                    var k = Yuz("P" + i, simKap, pn.direncli != null && pn.direncli[i] ? Tema.DirencliHucre : Tema.Kontrol);
                    Konum(k.rectTransform, x, yy, x + 0.19f, yy + 0.23f, 0, 0, 0, 0);
                    var tx = Yazi("T", k.transform, Tema.YaziBaslik, Tema.Beyaz, TextAnchor.MiddleCenter);
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
                    var f = Yuz("F" + i, simKap, d ? Tema.Basari : Tema.FideLekeli);
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
            if (sz != null) { sonZemin.sprite = sz; sonZemin.color = Tema.SonTon; }
            KartKumesi.Birlestir(y.AcilanKartlar);
            string m = o.son != null ? o.son.ad + "\n\n" + o.son.metin : "";
            m += "\n\nTohum Duvarı: " + o.saglik + "/100";
            m += "\nKavram kartları: " + KartKumesi.Sayi + "/" + (y.Icerik != null ? y.Icerik.kartlar.Count : 14);
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

        // kaplamaAd verilirse düğme rengi ve sprite'ı tek çağrıda kurulur (sprite yoksa düz renk).
        Button Dugme(string ad, Transform ata, string yazi, Color c, out Text t, string kaplamaAd = null)
        {
            var im = Yuz(ad, ata, c);
            if (kaplamaAd != null) Kaplama(im, kaplamaAd, c);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            t = Yazi("Yazi", im.transform, Tema.YaziOrta, Tema.Beyaz, TextAnchor.MiddleCenter);
            t.font = fontKalin;
            Doldur(t.rectTransform);
            t.text = yazi;
            var cb = b.colors;
            cb.disabledColor = Tema.Pasif;
            b.colors = cb;
            return b;
        }

        // Hover/basılı durumları sprite ile taşınır; sprite'lar yoksa renk geçişi (eski davranış) kalır.
        void SpriteDurumlari(Button b)
        {
            var uz = Spr("Arayuz/secenek_butonu_uzerinde_9slice");
            var ba = Spr("Arayuz/secenek_butonu_basili_9slice");
            if (b.image.sprite == null || uz == null || ba == null) return;
            b.transition = Selectable.Transition.SpriteSwap;
            var ss = b.spriteState;
            ss.highlightedSprite = uz;
            ss.pressedSprite = ba;
            b.spriteState = ss;
        }

        Slider SliderYap(Transform ata)
        {
            var kok = Bos("Slider", ata);
            var arka = Yuz("Arka", kok.transform, Tema.KontrolAlt);
            Konum(arka.rectTransform, 0, 0.35f, 1, 0.65f, 0, 0, 0, 0);
            var alan = Bos("DolguAlani", kok.transform);
            Konum((RectTransform)alan.transform, 0, 0.35f, 1, 0.65f, 0, 0, 0, 0);
            var dolgu = Yuz("Dolgu", alan.transform, Tema.Bilgi);
            dolgu.rectTransform.anchorMin = new Vector2(0, 0);
            dolgu.rectTransform.anchorMax = new Vector2(0, 1);
            dolgu.rectTransform.offsetMin = dolgu.rectTransform.offsetMax = Vector2.zero;
            var tutAlan = Bos("TutAlani", kok.transform);
            Doldur((RectTransform)tutAlan.transform);
            var tut = Yuz("Tutamac", tutAlan.transform, Tema.Marka);
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
