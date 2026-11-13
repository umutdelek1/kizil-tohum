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

        static Color Hex(string h, float a = 1f)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + h, out c);
            c.a = a;
            return c;
        }
    }
}
