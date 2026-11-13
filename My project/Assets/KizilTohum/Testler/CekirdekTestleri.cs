using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace KizilTohum.Testler
{
    public class CekirdekTestleri
    {
        enum Strateji { Dogru, Yanlis }

        static IcerikVerisi icerik;

        [OneTimeSetUp]
        public void Hazirla()
        {
            var hatalar = new List<string>();
            icerik = IcerikYukleyici.YukleResources(hatalar);
            Assert.IsEmpty(hatalar, string.Join("\n", hatalar));
        }

        static GorevYoneticisi Yeni(int seed = 7)
        {
            var y = new GorevYoneticisi(icerik);
            y.YeniOyun(seed);
            return y;
        }

        // ---- sürücü yardımcıları ----
        static void SecenekCoz(GorevYoneticisi y, Strateji s)
        {
            int guard = 0;
            while (!y.Ilerlenebilir && guard++ < 10)
            {
                int hedef = -1;
                if (s == Strateji.Yanlis)
                {
                    for (int i = 0; i < y.SunulanSayisi; i++)
                        if (!y.Elendi(i) && !y.SunulanSecenekBul(i).dogru) { hedef = i; break; }
                }
                if (hedef < 0)
                {
                    for (int i = 0; i < y.SunulanSayisi; i++)
                        if (y.SunulanSecenekBul(i).dogru) { hedef = i; break; }
                }
                y.CevapVer(hedef);
            }
        }

        static void AdimOyna(GorevYoneticisi y, Strateji s)
        {
            Adim a = y.AktifAdim;
            switch (a.tip)
            {
                case AdimTipi.Etkilesim:
                    y.EtkilesimTamamlandi(a.nesne);
                    break;
                case AdimTipi.Kaydirici:
                    y.KaydiriciDegisti(a.hedefMin + 0.1f);
                    y.Ilerle();
                    break;
                default:
                    if (a.SecenekliMi) SecenekCoz(y, s);
                    y.Ilerle();
                    break;
            }
        }

        static void Oyna(GorevYoneticisi y, Strateji s, Func<Adim, bool> dur = null)
        {
            int guard = 0;
            while (!y.Bitti && guard++ < 3000)
            {
                if (dur != null && dur(y.AktifAdim)) return;
                AdimOyna(y, s);
            }
            Assert.Less(guard, 3000, "Oyun bitmedi (sonsuz döngü).");
        }

        // ---- içerik ----
        [Test]
        public void Icerik_Sayilar()
        {
            Assert.AreEqual(3, icerik.gorevler.Count);
            Assert.AreEqual(14, icerik.kartlar.Count);
            Assert.AreEqual(15, icerik.tekrar.Count);
            int soru = 0, karar = 0;
            foreach (var g in icerik.gorevler)
                foreach (var a in g.adimlar)
                {
                    if (a.tip == AdimTipi.Soru || a.tip == AdimTipi.Punnett) soru++;
                    if (a.tip == AdimTipi.Karar) karar++;
                }
            Assert.AreEqual(14, soru);
            Assert.AreEqual(5, karar);
        }

        [Test]
        public void Icerik_EksikAlanHataVerir()
        {
            var bozuk = "{\"surum\":1,\"gorevler\":[{\"id\":\"X\",\"adimlar\":[{\"id\":\"X-1\",\"tip\":\"soru\",\"secenekler\":[]}]}]}";
            var hatalar = new List<string>();
            IcerikYukleyici.Yukle(bozuk, hatalar);
            Assert.IsNotEmpty(hatalar);
        }

        // ---- ceza / sağlık ----
        [Test]
        public void HepsiDogru_YesilKubbe_Saglik100_14Kart()
        {
            var y = Yeni();
            int kartOlayi = 0;
            y.KavramKartiAcildi += k => kartOlayi++;
            OyunSonu sonu = null;
            y.OyunBitti += o => sonu = o;
            Oyna(y, Strateji.Dogru);
            Assert.IsTrue(y.Bitti);
            Assert.AreEqual(100, y.Saglik);
            Assert.AreEqual("Yeşil Kubbe", sonu.son.ad);
            Assert.AreEqual(14, kartOlayi);
            Assert.AreEqual(14, y.AcilanKartlar.Count);
        }

        [Test]
        public void HepsiYanlis_OyunBitmez_TekCekmece_Saglik40()
        {
            var y = Yeni();
            int enDusuk = 100;
            y.SaglikDegisti += (yeni, fark) => { if (yeni < enDusuk) enDusuk = yeni; Assert.Less(fark, 0); };
            OyunSonu sonu = null;
            y.OyunBitti += o => sonu = o;
            Oyna(y, Strateji.Yanlis);
            Assert.IsTrue(y.Bitti);
            Assert.AreEqual(40, y.Saglik);
            Assert.AreEqual(40, enDusuk);
            Assert.AreEqual("Tek Çekmece", sonu.son.ad);
            Assert.GreaterOrEqual(y.Saglik, 0);
        }

        [Test]
        public void Sonlar_SinirDegerler()
        {
            var y = Yeni();
            Assert.AreEqual("Yeşil Kubbe", y.SonBul(100).ad);
            Assert.AreEqual("Yeşil Kubbe", y.SonBul(85).ad);
            Assert.AreEqual("Yarım Sera", y.SonBul(84).ad);
            Assert.AreEqual("Yarım Sera", y.SonBul(60).ad);
            Assert.AreEqual("Tek Çekmece", y.SonBul(59).ad);
            Assert.AreEqual("Tek Çekmece", y.SonBul(0).ad);
        }

        static GorevYoneticisi SoruyaGit(string soruId, int seed = 3)
        {
            var y = Yeni(seed);
            Oyna(y, Strateji.Dogru, a => a.id == soruId);
            Assert.AreEqual(soruId, y.AktifAdim.id);
            return y;
        }

        [Test]
        public void IpucuMerdiveni_UcAsama()
        {
            var y = SoruyaGit("KT01-S1");
            int sagOnce = y.Saglik;
            string ipucu = null, lin = null;
            y.IpucuGosterildi += t => ipucu = t;
            y.LinNotuGosterildi += t => lin = t;

            // 1. yanlış: yalnız tepki
            int w1 = -1;
            for (int i = 0; i < y.SunulanSayisi; i++) if (!y.SunulanSecenekBul(i).dogru) { w1 = i; break; }
            var r1 = y.CevapVer(w1);
            Assert.IsFalse(r1.dogru);
            Assert.AreEqual(2, r1.ceza);
            Assert.IsFalse(r1.ipucuGoster);
            Assert.IsFalse(r1.linNotuGoster);
            Assert.AreEqual(2, r1.kalanSecenekIndeksleri.Count);
            Assert.IsTrue(y.Elendi(w1));
            Assert.IsNull(ipucu);

            // 2. yanlış: ipucu + Lin notu birlikte, geriye yalnız doğru kalır
            int w2 = -1;
            for (int i = 0; i < y.SunulanSayisi; i++) if (!y.Elendi(i) && !y.SunulanSecenekBul(i).dogru) { w2 = i; break; }
            var r2 = y.CevapVer(w2);
            Assert.IsFalse(r2.dogru);
            Assert.AreEqual(2, r2.ceza);
            Assert.IsTrue(r2.ipucuGoster);
            Assert.IsTrue(r2.linNotuGoster);
            Assert.AreEqual(1, r2.kalanSecenekIndeksleri.Count);
            Assert.IsNotNull(ipucu);
            Assert.IsNotNull(lin);
            Assert.AreEqual(sagOnce - 4, y.Saglik);

            // doğru: ceza yok, kart bir kez açılır
            int kart = 0;
            y.KavramKartiAcildi += k => kart++;
            var r3 = y.CevapVer(r2.kalanSecenekIndeksleri[0]);
            Assert.IsTrue(r3.dogru);
            Assert.AreEqual(0, r3.ceza);
            Assert.AreEqual(sagOnce - 4, y.Saglik);
            Assert.AreEqual(1, kart);
            Assert.IsTrue(y.Ilerlenebilir);
        }

        [Test]
        public void AyniYanlisSecenegeTekrarBasmakCezaVermez()
        {
            var y = SoruyaGit("KT01-S1");
            int w = -1;
            for (int i = 0; i < y.SunulanSayisi; i++) if (!y.SunulanSecenekBul(i).dogru) { w = i; break; }
            y.CevapVer(w);
            int sag = y.Saglik;
            var tekrar = y.CevapVer(w);
            Assert.IsFalse(tekrar.gecerli);
            Assert.AreEqual(sag, y.Saglik);
            Assert.IsTrue(y.Elendi(w));
        }

        [Test]
        public void IpucuDugmesi_BirinciIpucu_IkinciLin_CezaYok()
        {
            var y = SoruyaGit("KT01-S2");
            int sag = y.Saglik;
            var olaylar = new List<string>();
            y.IpucuGosterildi += t => olaylar.Add("ipucu");
            y.LinNotuGosterildi += t => olaylar.Add("lin");
            y.IpucuIste();
            y.IpucuIste();
            y.IpucuIste();
            CollectionAssert.AreEqual(new[] { "ipucu", "lin" }, olaylar);
            Assert.AreEqual(sag, y.Saglik);
        }

        [Test]
        public void KararCezasi_BirKez_Elenir_PuansizKararlardaCezaYok()
        {
            var y = SoruyaGit("KT01-K1");
            int sag = y.Saglik;
            int w = -1;
            for (int i = 0; i < y.SunulanSayisi; i++) if (!y.SunulanSecenekBul(i).dogru) { w = i; break; }
            var r = y.CevapVer(w);
            Assert.AreEqual(3, r.ceza);
            Assert.AreEqual(sag - 3, y.Saglik);
            Assert.IsTrue(y.Elendi(w));
            y.CevapVer(w);
            Assert.AreEqual(sag - 3, y.Saglik);

            var y2 = SoruyaGit("KT08-K1");
            int sag2 = y2.Saglik;
            var r2 = y2.CevapVer(0);
            Assert.IsTrue(r2.dogru);
            Assert.AreEqual(sag2, y2.Saglik);

            var y3 = SoruyaGit("KT08-K3");
            int sag3 = y3.Saglik;
            int w3 = -1;
            for (int i = 0; i < y3.SunulanSayisi; i++) if (!y3.SunulanSecenekBul(i).dogru) { w3 = i; break; }
            var r3 = y3.CevapVer(w3);
            Assert.IsFalse(r3.dogru);
            Assert.AreEqual(0, r3.ceza);
            Assert.AreEqual(sag3, y3.Saglik);
        }

        [Test]
        public void Bayraklar_SonaYansir()
        {
            var y = SoruyaGit("KT08-K1");
            for (int i = 0; i < y.SunulanSayisi; i++)
                if (y.SunulanSecenekBul(i).bayrak == "tohum7_sinirli") { y.CevapVer(i); break; }
            Assert.IsTrue(y.BayrakVarMi("tohum7_sinirli"));
            Assert.IsFalse(y.BayrakVarMi("tohum7_kapali"));
            OyunSonu sonu = null;
            y.OyunBitti += o => sonu = o;
            y.Ilerle();
            Oyna(y, Strateji.Dogru);
            CollectionAssert.Contains(sonu.bayraklar, "tohum7_sinirli");
        }

        // ---- karıştırma ----
        [Test]
        public void Karistirma_AyniSeedAyniSira_SabitSoruDegismez()
        {
            var a = SoruyaGit("KT01-S1", 11);
            var b = SoruyaGit("KT01-S1", 11);
            CollectionAssert.AreEqual(a.SunulanSira(), b.SunulanSira());

            var p = SoruyaGit("KT08-S2", 11);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, p.SunulanSira());
            // doğru cevap her zaman doğru çözülür
            for (int seed = 1; seed <= 20; seed++)
            {
                var y = SoruyaGit("KT01-S1", seed);
                int d = -1;
                for (int i = 0; i < y.SunulanSayisi; i++) if (y.SunulanSecenekBul(i).dogru) d = i;
                Assert.IsTrue(y.CevapVer(d).dogru);
            }
        }

        [Test]
        public void Karistirma_PermutasyonGecerli()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                int[] s = Karistirici.Sira(3, seed, "x", false);
                var kume = new HashSet<int>(s);
                Assert.AreEqual(3, kume.Count);
                Assert.IsTrue(kume.Contains(0) && kume.Contains(1) && kume.Contains(2));
            }
        }

        // ---- sinematik atlama ----
        [Test]
        public void SinematikAtlamak_SaglikKartTetiklemez()
        {
            var y = Yeni();
            int kart = 0, saglik = 0;
            y.KavramKartiAcildi += k => kart++;
            y.SaglikDegisti += (a, b) => saglik++;
            Assert.AreEqual("sinematik", y.AktifAdim.tip);
            string onceki = y.AktifAdim.id;
            y.Ilerle();
            Assert.AreNotEqual(onceki, y.AktifAdim.id);
            Assert.AreEqual(0, kart);
            Assert.AreEqual(0, saglik);
            Assert.AreEqual(100, y.Saglik);
        }

        [Test]
        public void Etkilesim_YanlisNesneIlerletmez_IkinciKezTetiklenmez()
        {
            var y = Yeni();
            Oyna(y, Strateji.Dogru, a => a.tip == AdimTipi.Etkilesim);
            string id = y.AktifAdim.id;
            y.EtkilesimTamamlandi("YanlisNesne");
            Assert.AreEqual(id, y.AktifAdim.id);
            y.EtkilesimTamamlandi(y.AktifAdim.nesne);
            string sonraki = y.AktifAdim.id;
            Assert.AreNotEqual(id, sonraki);
            y.EtkilesimTamamlandi("TohumDuvari"); // eski çağrı yeni adımı bozmaz
            Assert.AreEqual(sonraki, y.AktifAdim.id);
        }

        // ---- kayıt ----
        [Test]
        public void Kayit_KaydetYukle_Esitlik()
        {
            var y = SoruyaGit("KT04-S2", 5);
            // bir yanlış yap, sonra kaydet (yanlış sonrası durum da korunmalı)
            int w = -1;
            for (int i = 0; i < y.SunulanSayisi; i++) if (!y.SunulanSecenekBul(i).dogru) { w = i; break; }
            y.CevapVer(w);
            string json = y.Kaydet();

            var y2 = new GorevYoneticisi(icerik);
            Assert.IsTrue(y2.Yukle(json));
            Assert.AreEqual(y.Saglik, y2.Saglik);
            Assert.AreEqual(y.AktifAdim.id, y2.AktifAdim.id);
            CollectionAssert.AreEqual(y.AcilanKartlar, y2.AcilanKartlar);
            CollectionAssert.AreEqual(y.Bayraklar, y2.Bayraklar);
            Assert.IsTrue(y2.Elendi(w));
            CollectionAssert.AreEqual(y.SunulanSira(), y2.SunulanSira());
        }

        [Test]
        public void Kayit_BozukKayitCokmez()
        {
            var y = new GorevYoneticisi(icerik);
            Assert.IsFalse(y.Yukle(null));
            Assert.IsFalse(y.Yukle(""));
            Assert.IsFalse(y.Yukle("bu json değil"));
            Assert.IsFalse(y.Yukle("{\"surum\":2}"));
            Assert.IsFalse(y.Yukle("{\"surum\":1,\"gorevIndeksi\":99}"));
            Assert.IsFalse(y.Yukle("{\"surum\":1,\"saglik\":500}"));
        }

        [Test]
        public void DerinBaglanti()
        {
            var y = new GorevYoneticisi(icerik);
            Assert.IsTrue(y.GoreveGit("KT04", 1));
            Assert.AreEqual("KT04", y.AktifGorev.id);
            Assert.IsTrue(y.GoreveGit("KT08", 1));
            Assert.AreEqual("KT08", y.AktifGorev.id);
            Assert.IsTrue(y.GoreveGit("KT01", 1));
            Assert.IsFalse(y.GoreveGit("KT99", 1));
            Assert.IsFalse(y.GoreveGit("", 1));
        }

        // ---- simülasyonlar ----
        [Test]
        public void Osmoz_Kurallar()
        {
            var bir = Osmoz.Hesapla(1.0f);
            Assert.AreEqual(0, bir.suYonu);
            Assert.AreEqual("gevsek", bir.durum);
            for (float r = 0.2f; r <= 3.0f; r += 0.1f)
            {
                var s = Osmoz.Hesapla(r);
                Assert.AreEqual(1f, s.ceperOlcegi, "çeper sabit olmalı");
                Assert.GreaterOrEqual(s.protoplastOlcegi, 0.55f - 0.0001f);
                Assert.LessOrEqual(s.protoplastOlcegi, 1f);
            }
            Assert.AreEqual("turgorlu", Osmoz.Hesapla(0.4f).durum);
            Assert.AreEqual(1, Osmoz.Hesapla(0.4f).suYonu);
            Assert.AreEqual("plazmoliz_basliyor", Osmoz.Hesapla(1.5f).durum);
            Assert.AreEqual("plazmolize", Osmoz.Hesapla(2.2f).durum);
            Assert.AreEqual(-1, Osmoz.Hesapla(2.2f).suYonu);
            Assert.IsTrue(Osmoz.HedefteMi(1.8f, 1.8f, 3.0f));
            Assert.IsFalse(Osmoz.HedefteMi(1.7f, 1.8f, 3.0f));
        }

        [Test]
        public void Kaydirici_BasariBirKezTetiklenir()
        {
            var y = Yeni();
            Oyna(y, Strateji.Dogru, a => a.tip == AdimTipi.Kaydirici);
            int basariOlayi = 0;
            y.AdimGuncellendi += s => basariOlayi++;
            Assert.IsFalse(y.Ilerlenebilir);
            y.KaydiriciDegisti(1.0f);
            Assert.IsFalse(y.Ilerlenebilir);
            y.KaydiriciDegisti(1.5f);
            Assert.IsFalse(y.Ilerlenebilir);
            y.KaydiriciDegisti(1.9f);
            Assert.IsTrue(y.Ilerlenebilir);
            Assert.AreEqual(1, basariOlayi);
            y.KaydiriciDegisti(2.5f);
            y.KaydiriciDegisti(0.5f);
            Assert.AreEqual(1, basariOlayi);
            Assert.IsTrue(y.Ilerlenebilir);
        }

        [Test]
        public void Co2_Egrileri()
        {
            float[] canli = Co2.Canli();
            float[] kontrol = Co2.Kontrol();
            Assert.AreEqual(24, canli.Length);
            Assert.AreEqual(24, kontrol.Length);
            for (int i = 1; i < 24; i++)
            {
                Assert.GreaterOrEqual(canli[i], canli[i - 1]);
                Assert.GreaterOrEqual(kontrol[i], kontrol[i - 1]);
            }
            Assert.Greater(canli[23], kontrol[23]);
            Assert.Greater(kontrol[23], kontrol[0]);
            for (int i = 0; i < 24; i++)
            {
                Assert.GreaterOrEqual(Co2.Normalize(canli[i]), 0f);
                Assert.LessOrEqual(Co2.Normalize(canli[i]), 1f);
            }
        }

        [Test]
        public void Punnett_Cekinik()
        {
            var f1 = Punnett.Hesapla("AA", "aa");
            Assert.AreEqual(0, f1.direncliSayisi);
            foreach (var g in f1.genotipler) Assert.AreEqual("Aa", g);

            var f2 = Punnett.Hesapla("Aa", "Aa");
            Assert.AreEqual(1, f2.direncliSayisi);
            int aa = 0, Aa = 0, AA = 0;
            foreach (var g in f2.genotipler)
            {
                if (g == "aa") aa++;
                else if (g == "Aa") Aa++;
                else if (g == "AA") AA++;
            }
            Assert.AreEqual(1, AA);
            Assert.AreEqual(2, Aa);
            Assert.AreEqual(1, aa);
        }

        [Test]
        public void F2_Sabit()
        {
            Assert.AreEqual(16, F2.Toplam);
            int say = 0;
            for (int i = 0; i < 16; i++) if (F2.DirencliMi(i)) say++;
            Assert.AreEqual(3, say);
            Assert.IsTrue(F2.DirencliMi(2));
            Assert.IsTrue(F2.DirencliMi(9));
            Assert.IsTrue(F2.DirencliMi(13));
            CollectionAssert.AreEqual(new[] { 2, 9, 13 }, F2.Direncliler());
        }
    }
}
