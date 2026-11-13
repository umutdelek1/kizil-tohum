using System;
using System.Collections.Generic;
using UnityEngine;

namespace KizilTohum
{
    /// <summary>
    /// Oyunun durum makinesi. Unity'ye bağımlı değildir (yalnızca JsonUtility); arayüz bu sınıfın olaylarını dinler
    /// ve çağrılarını yapar (bkz. ARAYUZ_SOZLESMESI).
    /// </summary>
    public class GorevYoneticisi
    {
        public const int BaslangicSagligi = 100;
        public const int SoruBasinaEnCokCeza = 2;

        readonly IcerikVerisi icerik;
        /// <summary>Zorluk: Kolay 0.5 · Normal 1 · Zor 1.5. Cezası olan her yanlış en az 1 ışık söndürür.</summary>
        public float CezaCarpani { get; set; } = 1f;
        OyunDurumu durum = new OyunDurumu();

        // ---- Mantık -> arayüz olayları ----
        public event Action<Gorev> GorevBasladi;
        public event Action<AdimSunumu> AdimBasladi;
        public event Action<AdimSunumu> AdimGuncellendi;     // ilerlenebilir oldu, vb.
        public event Action<CevapSonucu> Cevaplandi;
        public event Action<string> IpucuGosterildi;
        public event Action<string> LinNotuGosterildi;
        public event Action<int, int> SaglikDegisti;          // yeni değer, fark
        public event Action<Kart> KavramKartiAcildi;
        public event Action<SimBilgisi> SimulasyonGuncellendi;
        public event Action<Gorev> GorevBitti;
        public event Action<OyunSonu> OyunBitti;
        public event Action<string> BayrakDegisti;
        public event Action<string> SesOynat;
        public event Action KayitDegisti;

        public GorevYoneticisi(IcerikVerisi icerik)
        {
            this.icerik = icerik;
        }

        // ---- Okuma ----
        public IcerikVerisi Icerik { get { return icerik; } }
        public int Saglik { get { return durum.saglik; } }
        public bool Bitti { get { return durum.bitti; } }
        public int Seed { get { return durum.seed; } }
        public int DefterPuani { get { return durum.defterPuani; } }
        public IList<string> AcilanKartlar { get { return durum.acilanKartlar; } }
        public IList<string> Bayraklar { get { return durum.bayraklar; } }
        public IList<SecimKaydi> Secimler { get { return durum.secimler; } }
        public IList<string> TakilanSorular { get { return durum.takilanSorular; } }
        public bool BayrakVarMi(string ad) { return durum.bayraklar.Contains(ad); }

        public Gorev AktifGorev
        {
            get
            {
                if (durum.gorevIndeksi < 0 || durum.gorevIndeksi >= icerik.gorevler.Count) return null;
                return icerik.gorevler[durum.gorevIndeksi];
            }
        }

        public Adim AktifAdim
        {
            get
            {
                Gorev g = AktifGorev;
                if (g == null) return null;
                if (durum.adimIndeksi < 0 || durum.adimIndeksi >= g.adimlar.Count) return null;
                return g.adimlar[durum.adimIndeksi];
            }
        }

        public bool Ilerlenebilir
        {
            get { return !durum.bitti && durum.calisma.tamam; }
        }

        public Adim AdimBul(string id)
        {
            for (int g = 0; g < icerik.gorevler.Count; g++)
                for (int a = 0; a < icerik.gorevler[g].adimlar.Count; a++)
                    if (icerik.gorevler[g].adimlar[a].id == id) return icerik.gorevler[g].adimlar[a];
            return null;
        }

        // ---- Arayüz -> mantık çağrıları ----

        public void YeniOyun(int seed)
        {
            durum = new OyunDurumu();
            durum.seed = seed;
            durum.saglik = BaslangicSagligi;
            if (icerik.gorevler.Count == 0) return;
            BaslatGorev(0);
        }

        /// <summary>İşler Kitabevi QR bağlantısı (?gorev=KT04). Geçersiz id false döner.</summary>
        public bool GoreveGit(string gorevId, int seed)
        {
            int gi = icerik.GorevIndeksi(gorevId);
            if (gi < 0) return false;
            durum = new OyunDurumu();
            durum.seed = seed;
            durum.saglik = BaslangicSagligi;
            BaslatGorev(gi);
            return true;
        }

        public string Kaydet()
        {
            return JsonUtility.ToJson(durum);
        }

        /// <summary>Kayıttan devam. Bozuk/eski kayıtta false döner ve mevcut durum değişmez.</summary>
        public bool Yukle(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            OyunDurumu d = null;
            try { d = JsonUtility.FromJson<OyunDurumu>(json); }
            catch (Exception) { return false; }
            if (d == null || d.surum != 1) return false;
            if (d.gorevIndeksi < 0 || d.gorevIndeksi >= icerik.gorevler.Count) return false;
            Gorev g = icerik.gorevler[d.gorevIndeksi];
            if (d.adimIndeksi < 0 || d.adimIndeksi >= g.adimlar.Count) return false;
            if (d.saglik < 0 || d.saglik > 100) return false;
            if (d.calisma == null) d.calisma = new AdimCalisma();

            durum = d;
            if (GorevBasladi != null) GorevBasladi(g);
            Adim adim = AktifAdim;
            if (durum.bitti)
            {
                if (OyunBitti != null) OyunBitti(OyunSonuOlustur());
                return true;
            }
            if (durum.calisma.adimId != adim.id)
            {
                // çalışma durumu bu adıma ait değil: adımı temiz başlat
                BaslatAdim();
            }
            else
            {
                if (AdimBasladi != null) AdimBasladi(SunumOlustur());
                SimulasyonuYay(adim, false);
            }
            return true;
        }

        /// <summary>Devam butonu / Boşluk / Enter. Yalnızca "ilerlenebilir" iken etkilidir.</summary>
        public void Ilerle()
        {
            if (durum.bitti) return;
            if (!durum.calisma.tamam) return;
            Adim adim = AktifAdim;
            if (adim == null) return;
            if (!durum.tamamlananAdimlar.Contains(adim.id)) durum.tamamlananAdimlar.Add(adim.id);

            Gorev g = AktifGorev;
            durum.adimIndeksi++;
            if (durum.adimIndeksi >= g.adimlar.Count)
            {
                if (GorevBitti != null) GorevBitti(g);
                int sonraki = durum.gorevIndeksi + 1;
                if (sonraki >= icerik.gorevler.Count)
                {
                    OyunuBitir(false);
                    return;
                }
                BaslatGorev(sonraki);
            }
            else
            {
                BaslatAdim();
            }
        }

        /// <summary>Etkileşim nesnesiyle iş bitti (ör. "Cekmece47"). Doğru nesne ise adım kendiliğinden ilerler.</summary>
        public void EtkilesimTamamlandi(string nesne)
        {
            if (durum.bitti) return;
            Adim adim = AktifAdim;
            if (adim == null || adim.tip != AdimTipi.Etkilesim) return;
            if (durum.calisma.tamam) return; // ikinci kez tetiklenmez
            if (!string.IsNullOrEmpty(adim.nesne) && adim.nesne != nesne) return;
            durum.calisma.tamam = true;
            Ilerle();
        }

        /// <summary>Seçenek butonu veya 1-4 tuşu. Parametre GÖRÜNTÜ sırasındaki indekstir.</summary>
        public CevapSonucu CevapVer(int goruntuIndeksi)
        {
            if (durum.bitti) return null;
            Adim adim = AktifAdim;
            AdimCalisma c = durum.calisma;
            if (adim == null || !adim.SecenekliMi || c.tamam) return null;
            if (goruntuIndeksi < 0 || goruntuIndeksi >= c.sunulanSira.Count) return null;
            if (c.elenenler.Contains(goruntuIndeksi))
            {
                var gecersiz = new CevapSonucu();
                gecersiz.gecerli = false;
                gecersiz.secilenIndeks = goruntuIndeksi;
                gecersiz.kalanSecenekIndeksleri = KalanSecenekler();
                return gecersiz;
            }

            Secenek sec = adim.secenekler[c.sunulanSira[goruntuIndeksi]];
            var sonuc = new CevapSonucu();
            sonuc.secilenIndeks = goruntuIndeksi;
            sonuc.tepki = sec.tepki;

            if (sec.dogru)
            {
                sonuc.dogru = true;
                sonuc.ceza = 0;
                c.tamam = true;
                if (!string.IsNullOrEmpty(sec.bayrak)) BayrakEkle(sec.bayrak);
                KararKaydet(adim, sec, c.yanlisSayisi);
                if (c.yanlisSayisi > 0 && !durum.takilanSorular.Contains(adim.id)) durum.takilanSorular.Add(adim.id);
                sonuc.kalanSecenekIndeksleri = KalanSecenekler();
                sonuc.adimTamam = true;

                Kart acilacak = null;
                if (!string.IsNullOrEmpty(adim.kartId) && !durum.acilanKartlar.Contains(adim.kartId))
                {
                    acilacak = icerik.KartBul(adim.kartId);
                    if (acilacak != null) durum.acilanKartlar.Add(adim.kartId);
                }
                if (SesOynat != null) SesOynat("dogru");
                if (Cevaplandi != null) Cevaplandi(sonuc);
                if (AdimGuncellendi != null) AdimGuncellendi(SunumOlustur());
                if (acilacak != null && KavramKartiAcildi != null) KavramKartiAcildi(acilacak);
                Kayit();
                return sonuc;
            }

            // yanlış seçenek
            sonuc.dogru = false;
            c.yanlisSayisi++;
            c.elenenler.Add(goruntuIndeksi);
            int ceza = 0;
            if (sec.ceza > 0 && c.cezaAdedi < SoruBasinaEnCokCeza)
            {
                ceza = (int)Math.Round(sec.ceza * CezaCarpani, MidpointRounding.AwayFromZero);
                if (ceza < 1) ceza = 1;
                c.cezaAdedi++;
            }
            sonuc.ceza = ceza;
            if (!durum.takilanSorular.Contains(adim.id)) durum.takilanSorular.Add(adim.id);

            bool merdivenIki = c.yanlisSayisi >= 2;
            if (merdivenIki)
            {
                // 2. yanlış: ipucu + Lin notu birlikte, geriye yalnız doğru seçenek kalır
                for (int i = 0; i < c.sunulanSira.Count; i++)
                {
                    if (!adim.secenekler[c.sunulanSira[i]].dogru && !c.elenenler.Contains(i)) c.elenenler.Add(i);
                }
            }
            sonuc.kalanSecenekIndeksleri = KalanSecenekler();
            sonuc.ipucu = adim.ipucu;
            sonuc.linNotu = adim.linNotu;
            sonuc.ipucuGoster = merdivenIki && !string.IsNullOrEmpty(adim.ipucu);
            sonuc.linNotuGoster = merdivenIki && !string.IsNullOrEmpty(adim.linNotu);

            if (SesOynat != null) SesOynat("yanlis");
            if (Cevaplandi != null) Cevaplandi(sonuc);
            if (sonuc.ipucuGoster && !c.ipucuGosterildi)
            {
                c.ipucuGosterildi = true;
                if (IpucuGosterildi != null) IpucuGosterildi(adim.ipucu);
            }
            if (sonuc.linNotuGoster && !c.linNotuGosterildi)
            {
                c.linNotuGosterildi = true;
                if (LinNotuGosterildi != null) LinNotuGosterildi(adim.linNotu);
            }
            if (ceza > 0) SaglikDegistir(-ceza);
            Kayit();
            return sonuc;
        }

        /// <summary>H tuşu: 1. basış ipucu, 2. basış Lin notu. Ceza vermez.</summary>
        public void IpucuIste()
        {
            if (durum.bitti) return;
            Adim adim = AktifAdim;
            AdimCalisma c = durum.calisma;
            if (adim == null || !adim.SecenekliMi || c.tamam) return;
            if (!c.ipucuGosterildi && !string.IsNullOrEmpty(adim.ipucu))
            {
                c.ipucuGosterildi = true;
                if (IpucuGosterildi != null) IpucuGosterildi(adim.ipucu);
                Kayit();
            }
            else if (!c.linNotuGosterildi && !string.IsNullOrEmpty(adim.linNotu))
            {
                c.linNotuGosterildi = true;
                if (LinNotuGosterildi != null) LinNotuGosterildi(adim.linNotu);
                Kayit();
            }
        }

        /// <summary>Osmoz kaydırıcısı oynadığında.</summary>
        public void KaydiriciDegisti(float deger)
        {
            if (durum.bitti) return;
            Adim adim = AktifAdim;
            if (adim == null || adim.tip != AdimTipi.Kaydirici) return;
            if (deger < adim.kMin) deger = adim.kMin;
            if (deger > adim.kMax) deger = adim.kMax;
            AdimCalisma c = durum.calisma;
            c.kaydiriciDegeri = deger;

            bool hedefte = Osmoz.HedefteMi(deger, adim.hedefMin, adim.hedefMax);
            bool ilkBasari = hedefte && !c.basari;
            if (ilkBasari)
            {
                c.basari = true;
                c.tamam = true;
            }
            var sim = new SimBilgisi();
            sim.tur = "osmoz";
            sim.osmoz = Osmoz.Hesapla(deger);
            sim.hedefeUlasti = c.basari;
            if (SimulasyonGuncellendi != null) SimulasyonGuncellendi(sim);
            if (ilkBasari)
            {
                if (SesOynat != null) SesOynat("dogru");
                if (AdimGuncellendi != null) AdimGuncellendi(SunumOlustur());
            }
            Kayit();
        }

        public void DefterPuaniEkle(int puan)
        {
            if (puan == 0) return;
            durum.defterPuani += puan;
            Kayit();
        }

        // ---- Sunulan seçenekler (test ve arayüz yardımcıları) ----
        public int SunulanSayisi { get { return durum.calisma.sunulanSira.Count; } }

        public Secenek SunulanSecenekBul(int goruntuIndeksi)
        {
            AdimCalisma c = durum.calisma;
            Adim adim = AktifAdim;
            if (adim == null || goruntuIndeksi < 0 || goruntuIndeksi >= c.sunulanSira.Count) return null;
            return adim.secenekler[c.sunulanSira[goruntuIndeksi]];
        }

        public bool Elendi(int goruntuIndeksi) { return durum.calisma.elenenler.Contains(goruntuIndeksi); }

        public int[] SunulanSira()
        {
            return durum.calisma.sunulanSira.ToArray();
        }

        // ---- İç işleyiş ----

        void BaslatGorev(int gorevIndeksi)
        {
            durum.gorevIndeksi = gorevIndeksi;
            durum.adimIndeksi = 0;
            if (GorevBasladi != null) GorevBasladi(icerik.gorevler[gorevIndeksi]);
            BaslatAdim();
        }

        void BaslatAdim()
        {
            Adim adim = AktifAdim;
            var c = new AdimCalisma();
            c.adimId = adim.id;
            if (adim.SecenekliMi)
            {
                int[] sira = Karistirici.Sira(adim.secenekler.Count, durum.seed, adim.id, adim.sabitSira);
                for (int i = 0; i < sira.Length; i++) c.sunulanSira.Add(sira[i]);
            }
            switch (adim.tip)
            {
                case AdimTipi.Sinematik:
                case AdimTipi.Diyalog:
                case AdimTipi.Simulasyon:
                case AdimTipi.Son:
                    c.tamam = true;
                    break;
                default:
                    c.tamam = false;
                    break;
            }
            if (adim.tip == AdimTipi.Kaydirici) c.kaydiriciDegeri = adim.kBaslangic;
            durum.calisma = c;

            if (adim.tip == AdimTipi.Son)
            {
                OyunuBitir(true);
                return;
            }
            if (AdimBasladi != null) AdimBasladi(SunumOlustur());
            SimulasyonuYay(adim, true);
            Kayit();
        }

        void SimulasyonuYay(Adim adim, bool yeni)
        {
            SimBilgisi sim = null;
            switch (adim.tip)
            {
                case AdimTipi.Simulasyon:
                    sim = new SimBilgisi();
                    sim.tur = adim.simTur;
                    sim.deger = adim.simDeger;
                    if (adim.simTur == "co2")
                    {
                        sim.canliEgri = Co2.Canli();
                        if (adim.simDeger == "kontrol") sim.kontrolEgri = Co2.Kontrol();
                        sim.notMetni = "Temsili değer";
                    }
                    else if (adim.simTur == "f2")
                    {
                        sim.direncliIndeksler = F2.Direncliler();
                        int n;
                        sim.toplamFide = int.TryParse(adim.simDeger, out n) ? n : F2.Toplam;
                    }
                    break;
                case AdimTipi.Punnett:
                    sim = new SimBilgisi();
                    sim.tur = "punnett";
                    sim.punnett = Punnett.Hesapla(adim.punnettA, adim.punnettB);
                    break;
                case AdimTipi.Kaydirici:
                    sim = new SimBilgisi();
                    sim.tur = "osmoz";
                    sim.osmoz = Osmoz.Hesapla(durum.calisma.kaydiriciDegeri);
                    sim.hedefeUlasti = durum.calisma.basari;
                    break;
            }
            if (sim != null && SimulasyonGuncellendi != null) SimulasyonGuncellendi(sim);
        }

        AdimSunumu SunumOlustur()
        {
            var s = new AdimSunumu();
            s.gorev = AktifGorev;
            s.adim = AktifAdim;
            s.adimNo = durum.adimIndeksi + 1;
            s.toplamAdim = s.gorev.adimlar.Count;
            s.ilerlenebilir = durum.calisma.tamam;
            s.saglik = durum.saglik;
            if (s.adim.SecenekliMi)
            {
                for (int i = 0; i < durum.calisma.sunulanSira.Count; i++)
                {
                    var o = new SunulanSecenek();
                    o.indeks = i;
                    o.metin = s.adim.secenekler[durum.calisma.sunulanSira[i]].metin;
                    o.elendi = durum.calisma.elenenler.Contains(i);
                    s.secenekler.Add(o);
                }
            }
            return s;
        }

        List<int> KalanSecenekler()
        {
            var kalan = new List<int>();
            for (int i = 0; i < durum.calisma.sunulanSira.Count; i++)
                if (!durum.calisma.elenenler.Contains(i)) kalan.Add(i);
            return kalan;
        }

        void KararKaydet(Adim adim, Secenek sec, int yanlisSayisi)
        {
            for (int i = 0; i < durum.secimler.Count; i++)
                if (durum.secimler[i].adimId == adim.id) return;
            var k = new SecimKaydi();
            k.adimId = adim.id;
            k.metin = sec.metin;
            k.yanlisSayisi = yanlisSayisi;
            durum.secimler.Add(k);
        }

        void BayrakEkle(string ad)
        {
            if (durum.bayraklar.Contains(ad)) return;
            durum.bayraklar.Add(ad);
            if (BayrakDegisti != null) BayrakDegisti(ad);
        }

        void SaglikDegistir(int fark)
        {
            int eski = durum.saglik;
            int yeni = eski + fark;
            if (yeni < 0) yeni = 0;
            if (yeni > BaslangicSagligi) yeni = BaslangicSagligi;
            if (yeni == eski) return;
            durum.saglik = yeni;
            if (SaglikDegisti != null) SaglikDegisti(yeni, yeni - eski);
        }

        public Son SonBul(int saglik)
        {
            Son secilen = null;
            for (int i = 0; i < icerik.sonlar.Count; i++)
            {
                Son s = icerik.sonlar[i];
                if (saglik >= s.min && (secilen == null || s.min > secilen.min)) secilen = s;
            }
            if (secilen == null && icerik.sonlar.Count > 0)
            {
                // hiçbir eşik tutmadıysa (olmamalı) en düşük eşikli sonu ver
                secilen = icerik.sonlar[0];
                for (int i = 1; i < icerik.sonlar.Count; i++) if (icerik.sonlar[i].min < secilen.min) secilen = icerik.sonlar[i];
            }
            return secilen;
        }

        OyunSonu OyunSonuOlustur()
        {
            var o = new OyunSonu();
            o.saglik = durum.saglik;
            o.son = SonBul(durum.saglik);
            for (int i = 0; i < durum.acilanKartlar.Count; i++)
            {
                Kart k = icerik.KartBul(durum.acilanKartlar[i]);
                if (k != null) o.kartlar.Add(k);
            }
            o.takilanlar.AddRange(durum.takilanSorular);
            o.secimler.AddRange(durum.secimler);
            o.bayraklar.AddRange(durum.bayraklar);
            o.defterPuani = durum.defterPuani;
            return o;
        }

        void OyunuBitir(bool sonAdimindan)
        {
            if (durum.bitti) return;
            Gorev g = AktifGorev;
            durum.bitti = true;
            Son son = SonBul(durum.saglik);
            durum.sonAdi = son != null ? son.ad : "";
            if (sonAdimindan && g != null && GorevBitti != null) GorevBitti(g);
            if (OyunBitti != null) OyunBitti(OyunSonuOlustur());
            Kayit();
        }

        void Kayit()
        {
            if (KayitDegisti != null) KayitDegisti();
        }
    }
}
