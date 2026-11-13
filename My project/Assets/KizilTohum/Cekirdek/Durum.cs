using System;
using System.Collections.Generic;

namespace KizilTohum
{
    /// <summary>Kaydedilen oyun durumu (JsonUtility ile serileşir, sözlük yok).</summary>
    [Serializable]
    public class OyunDurumu
    {
        public int surum = 1;
        public int seed;
        public int saglik = 100;
        public int gorevIndeksi;
        public int adimIndeksi;
        public bool bitti;
        public string sonAdi = "";
        public List<string> acilanKartlar = new List<string>();
        public List<string> bayraklar = new List<string>();
        public List<SecimKaydi> secimler = new List<SecimKaydi>();
        public List<string> takilanSorular = new List<string>();   // en az bir yanlış yapılan soru/karar id'leri
        public List<string> tamamlananAdimlar = new List<string>();
        public AdimCalisma calisma = new AdimCalisma();
        public int defterPuani;
    }

    [Serializable]
    public class SecimKaydi
    {
        public string adimId;
        public string metin;   // seçilen doğru seçeneğin metni (özet için)
        public int yanlisSayisi;
    }

    /// <summary>Şu anki adımın çalışma durumu; adım değişince sıfırlanır.</summary>
    [Serializable]
    public class AdimCalisma
    {
        public string adimId = "";
        public List<int> sunulanSira = new List<int>();     // görüntü sırası -> özgün seçenek indeksi
        public List<int> elenenler = new List<int>();       // elenen GÖRÜNTÜ indeksleri
        public int yanlisSayisi;
        public int cezaAdedi;                               // bu adımda uygulanan ceza adedi
        public bool ipucuGosterildi;
        public bool linNotuGosterildi;
        public bool tamam;                                  // adım ilerlenebilir mi
        public bool basari;                                 // kaydırıcı hedefine ulaşıldı
        public float kaydiriciDegeri;
    }

    // ---- Arayüze giden bilgiler --------------------------------------------
    public class SunulanSecenek
    {
        public int indeks;       // görüntü sırası
        public string metin;
        public bool elendi;
    }

    public class AdimSunumu
    {
        public Gorev gorev;
        public Adim adim;
        public int adimNo;         // 1 tabanlı, görev içinde
        public int toplamAdim;
        public List<SunulanSecenek> secenekler = new List<SunulanSecenek>();
        public bool ilerlenebilir;
        public int saglik;
    }

    public class CevapSonucu
    {
        public bool dogru;
        public bool gecerli = true;       // elenmiş seçeneğe basıldıysa false
        public int secilenIndeks;         // görüntü sırası
        public string tepki;
        public int ceza;
        public string ipucu;
        public string linNotu;
        public bool ipucuGoster;
        public bool linNotuGoster;
        public List<int> kalanSecenekIndeksleri = new List<int>();
        public bool adimTamam;
    }

    public class OyunSonu
    {
        public Son son;
        public int saglik;
        public List<Kart> kartlar = new List<Kart>();
        public List<string> takilanlar = new List<string>();
        public List<SecimKaydi> secimler = new List<SecimKaydi>();
        public List<string> bayraklar = new List<string>();
        public int defterPuani;
    }
}
