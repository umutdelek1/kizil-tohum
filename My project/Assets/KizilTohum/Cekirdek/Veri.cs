using System;
using System.Collections.Generic;
using UnityEngine;

namespace KizilTohum
{
    // icerik.json'un birebir karşılığı (JsonUtility alan adlarına göre eşler).

    [Serializable]
    public class IcerikVerisi
    {
        public int surum;
        public List<Son> sonlar = new List<Son>();
        public List<Kart> kartlar = new List<Kart>();
        public List<Gorev> gorevler = new List<Gorev>();
        public List<TekrarSorusu> tekrar = new List<TekrarSorusu>();

        public Kart KartBul(string id)
        {
            for (int i = 0; i < kartlar.Count; i++) if (kartlar[i].id == id) return kartlar[i];
            return null;
        }

        public int GorevIndeksi(string id)
        {
            for (int i = 0; i < gorevler.Count; i++) if (gorevler[i].id == id) return i;
            return -1;
        }
    }

    [Serializable]
    public class Son
    {
        public string ad;
        public int min;      // sağlık >= min ise bu son
        public string metin;
        public string gorsel;
    }

    [Serializable]
    public class Kart
    {
        public string id;
        public string baslik;
        public string konu;
        public string metin;
    }

    [Serializable]
    public class Gorev
    {
        public string id;
        public string baslik;
        public string slogan;
        public string hedef;
        public List<Adim> adimlar = new List<Adim>();
    }

    public static class AdimTipi
    {
        public const string Sinematik = "sinematik";
        public const string Diyalog = "diyalog";
        public const string Etkilesim = "etkilesim";
        public const string Simulasyon = "simulasyon";
        public const string Soru = "soru";
        public const string Karar = "karar";
        public const string Kaydirici = "kaydirici";
        public const string Punnett = "punnett";
        public const string Son = "son";
    }

    [Serializable]
    public class Adim
    {
        public string id;
        public string no;
        public string tip;
        public string konusan;
        public string metin;
        public string gorsel;
        public string anim;
        public string nesne;
        public string kod;
        public bool puansiz;
        public bool sabitSira;
        public List<Secenek> secenekler = new List<Secenek>();
        public string ipucu;
        public string linNotu;
        public string kartId;
        public string simTur;
        public string simDeger;
        public string punnettA;
        public string punnettB;
        public float kMin;
        public float kMax;
        public float kBaslangic;
        public float hedefMin;
        public float hedefMax;
        public string basariMetni;
        public bool ek;

        public bool SecenekliMi
        {
            get { return tip == AdimTipi.Soru || tip == AdimTipi.Karar || tip == AdimTipi.Punnett; }
        }
    }

    [Serializable]
    public class Secenek
    {
        public string metin;
        public bool dogru;
        public int ceza;
        public string tepki;
        public string bayrak;
    }

    [Serializable]
    public class TekrarSorusu
    {
        public string id;
        public string gorevId;
        public string konusan;
        public string metin;
        public List<Secenek> secenekler = new List<Secenek>();
        public string ipucu;
        public string linNotu;
        public string kartId;
        public string defterNotu;
    }

    public static class IcerikYukleyici
    {
        /// <summary>JSON'u okur ve doğrular. Hata varsa hatalar listesine yazar (null dönmez).</summary>
        public static IcerikVerisi Yukle(string json, List<string> hatalar)
        {
            IcerikVerisi veri = null;
            try { veri = JsonUtility.FromJson<IcerikVerisi>(json); }
            catch (Exception e) { hatalar.Add("JSON okunamadı: " + e.Message); }
            if (veri == null) { hatalar.Add("İçerik boş."); return new IcerikVerisi(); }
            Dogrula(veri, hatalar);
            return veri;
        }

        public static IcerikVerisi YukleResources(List<string> hatalar)
        {
            TextAsset ta = Resources.Load<TextAsset>("KizilTohum/icerik");
            if (ta == null) { hatalar.Add("Resources/KizilTohum/icerik.json bulunamadı."); return new IcerikVerisi(); }
            return Yukle(ta.text, hatalar);
        }

        public static void Dogrula(IcerikVerisi v, List<string> hatalar)
        {
            if (v.gorevler.Count == 0) hatalar.Add("Görev yok.");
            if (v.sonlar.Count == 0) hatalar.Add("Son yok.");
            var adimIdleri = new HashSet<string>();
            for (int g = 0; g < v.gorevler.Count; g++)
            {
                Gorev gr = v.gorevler[g];
                if (string.IsNullOrEmpty(gr.id)) hatalar.Add("Görev id boş.");
                if (gr.adimlar.Count == 0) hatalar.Add(gr.id + ": adım yok.");
                for (int a = 0; a < gr.adimlar.Count; a++)
                {
                    Adim ad = gr.adimlar[a];
                    string kim = gr.id + "/" + ad.id;
                    if (string.IsNullOrEmpty(ad.id)) hatalar.Add(gr.id + ": adım id boş.");
                    else if (!adimIdleri.Add(ad.id)) hatalar.Add(kim + ": tekrarlanan adım id.");
                    if (string.IsNullOrEmpty(ad.tip)) hatalar.Add(kim + ": tip yok.");
                    if (ad.SecenekliMi)
                    {
                        if (ad.secenekler.Count < 2) hatalar.Add(kim + ": en az 2 seçenek gerekir.");
                        int dogru = 0;
                        for (int s = 0; s < ad.secenekler.Count; s++) if (ad.secenekler[s].dogru) dogru++;
                        if (dogru == 0) hatalar.Add(kim + ": doğru seçenek yok.");
                        if (!ad.puansiz && dogru != 1) hatalar.Add(kim + ": tam 1 doğru seçenek olmalı.");
                        if (ad.tip != AdimTipi.Karar)
                        {
                            if (string.IsNullOrEmpty(ad.ipucu)) hatalar.Add(kim + ": ipucu yok.");
                            if (string.IsNullOrEmpty(ad.linNotu)) hatalar.Add(kim + ": Lin notu yok.");
                        }
                        if (!string.IsNullOrEmpty(ad.kartId) && v.KartBul(ad.kartId) == null)
                            hatalar.Add(kim + ": kart bulunamadı: " + ad.kartId);
                    }
                    if (ad.tip == AdimTipi.Kaydirici && ad.kMax <= ad.kMin) hatalar.Add(kim + ": kaydırıcı aralığı geçersiz.");
                }
            }
            for (int t = 0; t < v.tekrar.Count; t++)
            {
                TekrarSorusu q = v.tekrar[t];
                if (v.GorevIndeksi(q.gorevId) < 0) hatalar.Add(q.id + ": görev yok " + q.gorevId);
                if (q.secenekler.Count < 2) hatalar.Add(q.id + ": seçenek yetersiz.");
                if (!string.IsNullOrEmpty(q.kartId) && v.KartBul(q.kartId) == null)
                    hatalar.Add(q.id + ": kart bulunamadı: " + q.kartId);
            }
        }
    }
}
