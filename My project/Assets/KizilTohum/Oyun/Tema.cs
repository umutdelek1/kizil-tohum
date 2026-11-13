using UnityEngine;

namespace KizilTohum
{
    /// <summary>
    /// Arayüzün tek tasarım kaynağı: renkler, yazı boyutları, boşluklar ve hareket sabitleri.
    /// Renk adları rol adıdır (ne işe yaradığını söyler, hangi renk olduğunu değil).
    /// </summary>
    public static class Tema
    {
        // ---------- panel alfa seviyeleri ----------
        public const float PanelAlfa = 0.94f;
        public const float PanelHafifAlfa = 0.88f;

        // ---------- yüzey renkleri ----------
        public static readonly Color Base = Hex("0C0A0C");
        public static readonly Color Panel = Hex("171316", PanelAlfa);
        public static readonly Color PanelHafif = Hex("171316", PanelHafifAlfa);
        public static readonly Color Kontrol = Hex("2B2428");
        public static readonly Color KontrolAlt = Hex("3A3036");
        public static readonly Color Kart = Hex("221B20");
        public static readonly Color Sahne = Hex("2A2226");
        public static readonly Color Sim = Hex("120F12", 0.92f);
        public static readonly Color Ipucu = Hex("1B2A33");
        public static readonly Color Lin = Hex("2A1B33");

        // katman / örtü yüzeyleri
        public static readonly Color Saydam = new Color(0f, 0f, 0f, 0f);
        public static readonly Color Orten = Hex("000000", 0.75f);
        public static readonly Color SonZemin = Hex("0C0A0C", 0.96f);
        public static readonly Color MenuZemin = Hex("0C0A0C", 0.98f);

        // sprite üzerine binen renk dokunuşları
        public static readonly Color KapakTon = new Color(0.55f, 0.55f, 0.55f, 1f);
        public static readonly Color SonTon = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color LinKutuTon = new Color(0.85f, 0.7f, 1f, 1f);
        public static readonly Color LogoPlakaTon = new Color(1f, 1f, 1f, 0.92f);

        // ---------- metin / vurgu ----------
        public static readonly Color Beyaz = Color.white;
        public static readonly Color Soluk = Hex("999999");
        public static readonly Color Marka = Hex("E0643B");
        public static readonly Color Konusan = Hex("FFD35A");
        public static readonly Color Uyari = Konusan;
        public static readonly Color Bilgi = Hex("8FD8FF");
        public static readonly Color LinRengi = Hex("B47CFF");
        public static readonly Color Basari = Hex("5DFFA6");
        public static readonly Color Hata = Hex("FF5A52");

        // ---------- durum renkleri ----------
        public static readonly Color Dogru = Hex("1E6B45");
        public static readonly Color Yanlis = Hex("6B1E1E");
        public static readonly Color Elenmis = Hex("3A3A3A");
        public static readonly Color Hover = Hex("FFE2CC");
        public static readonly Color Basili = Hex("C9B3A4");
        public static readonly Color Pasif = Hex("666666", 0.6f);
        public static readonly Color IpucuDugme = Hex("2F6F8F");

        // Tohum Duvarı çubuğu (sprite'lı görünümde ton, sprite'sız görünümde Basari/Uyari/Hata)
        public static readonly Color CubukIyi = Hex("FFFFFF");
        public static readonly Color CubukOrta = Hex("FFE08A");
        public static readonly Color CubukDusuk = Hex("FF8A80");

        // simülasyon çizimleri (sprite yokken yer tutucu)
        public static readonly Color Cep = Hex("5D8F3A");
        public static readonly Color Protoplast = Hex("7FD1A0");
        public static readonly Color FideLekeli = Hex("6B8F5A");
        public static readonly Color DirencliHucre = Hex("5DFFA6", 0.5f);

        // ---------- yazı boyutları ----------
        public const int YaziKucuk = 22;
        public const int YaziOrta = 28;
        public const int YaziBuyuk = 32;
        public const int YaziBaslik = 40;
        public const int YaziLogo = 90;
        public const int YaziMenuBaslik = 72;      // menü emblemi yanındaki KIZIL TOHUM
        public const int YaziMenuAlt = 24;         // TYT BİYOLOJİ
        public const float EmblemYuk = 190f;
        public const float MenuPanelGenislik = 0.32f;   // soldaki buton paneli: ekran genişliğinin %32'si
        public const float MenuPanelOpaklik = 0.80f;    // solda %80 koyu, sağ kenarda şeffaf
        public const float EmblemAralik = 24f;

        // ---------- boşluk ----------
        public const float MetinSol = 56f;
        public const float MetinSag = 36f;
        public const float CerceveKenar = 36f;
        public const float ParalaksSahne = 60f;
        public const float ParalaksMenu = 80f;

        // ---------- hareket ----------
        public static readonly float[] ParalaksGuc = { 10f, 22f, 40f };
        public const float Takip = 4f;
        public const float MenuKayma = 0.35f;
        public const float MenuYaklasma = 0.2f;
        public const float MenuZoom = 0.015f;

        // ---------- sinematik kamera ----------
        public const float KameraSure = 8f;                                   // kameraSure verilmezse
        public static readonly float[] KatmanOlcek = { 1.05f, 1.10f, 1.18f }; // arka / orta / ön hedef ölçek
        public static readonly float[] KaydirmaOfset = { 20f, 50f, 90f };    // kaydırmada katman başına x ofseti
        public const float KenBurnsOlcek = 1.08f;                             // tek resimli sahne
        public const float KenBurnsKayma = 25f;                               // itme/çekmede hafif x kayması
        public const float KenBurnsKaydirma = 50f;                            // tek resimde kaydırma ofseti
        public const float GirisYakinlik = 0.04f;                             // yeni sahne bu kadar yakın başlar
        public const float GirisCekilmeSure = 1.2f;                           // ...ve bu sürede geri çekilir

        // ---------- arayüz hareketi (süreler sn, unscaled) ----------
        public const float DaktiloHiz = 0.02f;                // karakter başına
        public const float PanelGirisSure = 0.3f;             // diyalog paneli aşağıdan kayar + solar
        public const float PanelGirisMesafe = 120f;
        public const float PortreGirisSure = 0.35f;           // portre soldan kayar
        public const float PortreGirisMesafe = 220f;
        public const float PortreParlamaSure = 0.3f;          // konuşan değişince kısa parlama
        public const float PortreParlamaOlcek = 1.06f;
        public static readonly Color PortreParlamaBaslangic = new Color(0.55f, 0.55f, 0.55f, 1f);
        public const float SecenekAralik = 0.08f;             // seçenekler sırayla belirir
        public const float SecenekGirisSure = 0.2f;
        public const float SecenekGirisOlcek = 0.94f;
        public const float SecenekHoverOlcek = 1.03f;
        public const float SecenekBasiliOlcek = 0.97f;
        public const float SecenekOlcekHiz = 16f;             // ölçek yaklaşma hızı (1/sn)
        public const float DuvarSarsintiSure = 0.35f;
        public const float DuvarSarsintiGuc = 10f;
        public const float DuvarParlamaSure = 0.5f;           // yanlışta kırmızı parlama
        public const float DuvarKaymaSure = 0.4f;             // can azalırken çubuk yumuşak kayar
        public const float KartGirisSure = 0.4f;              // kavram kartı ölçeklenip dönerek gelir
        public const float KartBaslangicOlcek = 0.6f;
        public const float KartBaslangicAci = 8f;

        // ---------- diyalog paneli yerleşimi (canvas birimi, referans 1080 yükseklik) ----------
        public const float DiyalogOran = 0.22f;          // tek satırlık metinde panel yüksekliği / ekran
        public const float PanelUstBosluk = 34f;
        public const float KonusanYuk = 44f;
        public const float MetinUst = 80f;
        public const float SecenekMinYuk = 60f;          // 28 px yazıya göre kutu; uzun seçenek kutuyu büyütür
        public const float SecenekBosluk = 8f;
        public const float SecenekYatayDolgu = 24f;
        public const float TepkiYuk = 80f;
        public const float PanelAltBosluk = 12f;
        public const float AltSatirYuk = 66f;            // Devam / İpucu düğmeleri
        public const float AltSatirKenar = 16f;
        public const float SimPanelAralik = 0.02f;

        // ---------- Bekçi Defteri (kâğıt üstü mürekkep renkleri) ----------
        public static readonly Color DefterMurekkep = Hex("2B1F14");
        public static readonly Color DefterBaslik = Hex("7A2E12");

        // ---------- koloni haritası ----------
        public static readonly Color Kehribar = Hex("FFB347");
        public static readonly string[] HaritaResimleri = { "Harita/harita_gunduz", "Harita/harita_gunbatimi", "Harita/harita_gece" }; // 0 / 1 / 2+ tamamlanan görev
        public const float HaritaCaprazSure = 1.5f;       // gündüz → gün batımı → gece erimesi
        public const float HaritaDalisSure = 0.9f;        // tıklanan noktaya kamera dalışı
        public const float HaritaDalisOlcek = 2.4f;
        public const float HaritaKararmaBasi = 0.55f;     // dalışın bu oranından sonra siyaha erir
        public const float HaritaSurukle = 12f;           // kendi kendine yavaş sürüklenme (px)
        public const float HaritaSurukleHiz = 0.3f;
        public const float HaritaParalaks = 20f;          // fare paralaksı (px)
        public const float HaritaTemelOlcek = 1.07f;      // sürüklenirken kenarlar görünmesin
        public const float HaritaNoktaBoyu = 96f;
        public const float HaritaHoverOlcek = 1.25f;
        public const float HaritaOlcekHiz = 14f;
        public const float HaritaNabizSure = 1.6f;
        public const float HaritaNabizOlcek = 1.9f;
        public const float HaritaBilgiGenislik = 460f;
        public const float HaritaBilgiYukseklik = 120f;
        public const float HaritaBilgiUzaklik = 24f;      // simgenin kenarından panele boşluk (panel simgenin yanında)
        public const float HaritaSimgeNabizOlcek = 1.08f; // pin_acik sprite'ı: 1.0 → 1.08 hafif nabız
        public const string HaritaBaslik = "KOLONİ HARİTASI";
        public const string HaritaKapaliMetni = "Demoda kapalı";

        /// <summary>Haritadaki bir nokta. x,y normalize (sol üst 0,0); gorevId boşsa kilitli.</summary>
        public struct HaritaNoktaTanimi
        {
            public string id, yer, gorevId;
            public float x, y;
            public HaritaNoktaTanimi(string id, string yer, float x, float y, string gorevId)
            { this.id = id; this.yer = yer; this.x = x; this.y = y; this.gorevId = gorevId; }
        }

        // Konumları buradan değiştir.
        public static readonly HaritaNoktaTanimi[] HaritaNoktalari =
        {
            new HaritaNoktaTanimi("kubbe_sera",   "Ana Kubbe Serası", 0.190f, 0.42f, "KT01"),
            new HaritaNoktaTanimi("laboratuvar",  "Laboratuvar",      0.400f, 0.29f, ""),
            new HaritaNoktaTanimi("tohum_duvari", "Tohum Duvarı",     0.646f, 0.27f, "KT04"),
            new HaritaNoktaTanimi("su_tanklari",  "Su Tankları",      0.835f, 0.32f, "KT08"),
            new HaritaNoktaTanimi("komuta",       "Komuta Merkezi",   0.610f, 0.74f, ""),
            new HaritaNoktaTanimi("inis_pisti",   "İniş Pisti",       0.842f, 0.59f, ""),
            new HaritaNoktaTanimi("yol_kavsagi",  "Yol Kavşağı",      0.113f, 0.73f, ""),
        };

        // ---------- KT04 hücre (osmoz) ----------
        public const float HucreGecisSure = 0.8f;                 // izotonik ↔ turgor / plazmoliz çapraz erime
        public const float HucreNefesOlcek = 1f;                  // 1.015 nefes alır; duvar da ölçeklendiği için kapalı (bkz. rapor)
        public const float HucreNefesSure = 3f;
        public const float HucreAltSinir = 0.27f;                 // hücre alanı: panelin bu oranından yukarısı (altta yazı ve kaydırıcı)
        public const int HucreParcacikSayisi = 18;
        public const float HucreParcacikBoyu = 14f;
        public const float HucreParcacikHiz = 0.45f;              // tur/sn
        public const float HucreParcacikIc = 0.55f;               // yarıçap oranı: zarın içi
        public const float HucreParcacikDis = 1.25f;              // ...duvarın dışı
        public const float HucreParcacikDengeAlfa = 0.25f;
        public static readonly Color HucreSu = new Color(0.88f, 1f, 0.9f, 0.75f);   // saydam beyaz-yeşil (mavi değil)
        public static readonly Color HucreEtiketCizgi = new Color(1f, 1f, 1f, 0.7f);
        public const float HucreNoktaBoyu = 6f;                   // etiket çizgisi ucundaki nokta
        // Etiket hedefleri: hücre görselinin normalize koordinatları (0..1, sol-üst orijin, 1920x1071 çerçevesi).
        // Sıra: izotonik, turgor, plazmoliz. Durum geçişinde uç 0.8 sn'de yeni hedefe kayar (HucreGecisSure).
        public static readonly Vector2[] HucreHedefVakuol = { new Vector2(0.45f, 0.45f), new Vector2(0.45f, 0.45f), new Vector2(0.41f, 0.45f) };
        public static readonly Vector2[] HucreHedefDuvar = { new Vector2(0.19f, 0.58f), new Vector2(0.19f, 0.58f), new Vector2(0.19f, 0.58f) };
        public static readonly Vector2[] HucreHedefZar = { new Vector2(0.27f, 0.43f), new Vector2(0.27f, 0.43f), new Vector2(0.245f, 0.45f) };
        public static readonly Vector2 HucreMerkez = new Vector2(0.51f, 0.48f);    // görsel içinde (sol alt 0,0)
        public static readonly Vector2 HucreYaricap = new Vector2(0.33f, 0.42f);

        // ---------- geçişler ----------
        public const float CaprazSure = 0.6f;
        public const float SarsintiSure = 0.4f;
        public const float SarsintiGuc = 14f;
        public const float KarartSure = 0.5f;
        public const float YaziKartGiris = 0.4f;
        public const float HaftalarBekle = 1.6f;
        public const string HaftalarSonraMetni = "Haftalar sonra…";
        public const float VideoHazirlikSiniri = 6f;                          // bu sürede hazırlanmazsa resme düş

        static Color Hex(string h, float a = 1f)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + h, out c);
            c.a = a;
            return c;
        }
    }
}
