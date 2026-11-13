#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KizilTohum.Editor
{
    /// <summary>
    /// Kızıl Tohum/Otomatik Ekran Görüntüsü: Play moduna girer, oyunu kendi kendine sürer ve ekran görüntüleri alır.
    /// Çıktı: Masaüstü/KizilTohum_Ekranlar/ (01_menu.png ... ekran_log.txt). Oyun kodu değişmez; arayüzün özel
    /// metotlarına (HaritaAc, GorevAc, VideoIptal) yansıma ile, oyun mantığına GorevYoneticisi'nin public API'siyle ulaşır.
    /// Oyuncunun kaydı ve harita/kart ilerlemesi çalışma başında yedeklenir, sonunda geri yazılır.
    /// </summary>
    [InitializeOnLoad]
    public static class OtomatikEkranGoruntusu
    {
        const string Bayrak = "KizilTohum_OtoEkran_Calisiyor";
        static readonly string[] YedekAnahtarlar = { "kizil_tohum_kayit_v1", "kizil_tohum_harita_tamam_v1", "kizil_tohum_kartlar_v1" };

        static IEnumerator<object> akis;
        static double bekleBitis, kosulBitis;
        static Func<bool> beklenen;
        static string klasor;
        static StreamWriter log;
        static int uyari, hata;
        static string konsolYolu;
        static long konsolBaslangic;
        static readonly Dictionary<string, string> yedek = new Dictionary<string, string>();
        static readonly List<string> cekilenler = new List<string>();

        static OtomatikEkranGoruntusu()
        {
            EditorApplication.playModeStateChanged += ModDegisti;
        }

        [MenuItem("Kızıl Tohum/Otomatik Ekran Görüntüsü")]
        static void MenudenBaslat()
        {
            SessionState.SetBool(Bayrak, true);
            if (EditorApplication.isPlaying) Kur();
            else EditorApplication.EnterPlaymode();
        }

        /// <summary>Komut satırından: -executeMethod KizilTohum.Editor.OtomatikEkranGoruntusu.KomutSatirindan</summary>
        public static void KomutSatirindan() { MenudenBaslat(); }

        static void ModDegisti(PlayModeStateChange d)
        {
            if (d == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Bayrak, false)) Kur();
            if (d == PlayModeStateChange.ExitingPlayMode && akis != null) Bitir("Play modundan çıkıldı");
        }

        // ---------- kurulum / bitiş ----------
        static void Kur()
        {
            SessionState.SetBool(Bayrak, false);
            klasor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "KizilTohum_Ekranlar");
            Directory.CreateDirectory(klasor);
            log = new StreamWriter(Path.Combine(klasor, "ekran_log.txt"), false, new UTF8Encoding(false));
            log.AutoFlush = true;
            uyari = hata = 0; cekilenler.Clear();
            log.WriteLine("Kızıl Tohum otomatik ekran görüntüsü · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · Unity " + Application.unityVersion);
            log.WriteLine("Yalnızca Warning / Error / Exception / Assert kayıtları ve [ARAÇ] notları.");
            log.WriteLine();
            Application.logMessageReceivedThreaded += KayitGeldi;
            // yerel (native) uyarılar, örn. video çözücü, yönetilen olaylara düşmez: editör log dosyasından da okunur
            konsolYolu = Application.consoleLogPath;
            try { konsolBaslangic = !string.IsNullOrEmpty(konsolYolu) && File.Exists(konsolYolu) ? new FileInfo(konsolYolu).Length : 0; }
            catch (Exception) { konsolBaslangic = 0; } // video çözücü uyarıları başka iş parçacığından gelir

            yedek.Clear();
            for (int i = 0; i < YedekAnahtarlar.Length; i++)
                if (PlayerPrefs.HasKey(YedekAnahtarlar[i])) yedek[YedekAnahtarlar[i]] = PlayerPrefs.GetString(YedekAnahtarlar[i]);

            akis = Akis().GetEnumerator();
            bekleBitis = EditorApplication.timeSinceStartup; beklenen = null;
            EditorApplication.update += Guncelle;
        }

        static void Bitir(string neden)
        {
            if (akis == null) return;
            akis = null;
            EditorApplication.update -= Guncelle;
            Application.logMessageReceivedThreaded -= KayitGeldi;
            // oyuncunun kaydını ve ilerlemesini geri yaz
            for (int i = 0; i < YedekAnahtarlar.Length; i++)
            {
                string a = YedekAnahtarlar[i];
                if (yedek.ContainsKey(a)) PlayerPrefs.SetString(a, yedek[a]); else PlayerPrefs.DeleteKey(a);
            }
            PlayerPrefs.Save();
            int yerel = YerelKayitlariEkle();
            lock (kilit) if (log != null)
            {
                log.WriteLine();
                if (yerel > 0) log.WriteLine("Editör log dosyasından eklenen yerel uyarı/hata satırı: " + yerel);
                log.WriteLine("ÖZET: " + neden + " · görüntü " + cekilenler.Count + " · uyarı " + uyari + " · hata/istisna " + hata);
                log.WriteLine("Görüntüler: " + string.Join(", ", cekilenler.ToArray()));
                log.Close(); log = null;
            }
            Debug.Log("[Otomatik Ekran Görüntüsü] " + neden + ". Çıktı: " + klasor);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        static readonly object kilit = new object();

        static void KayitGeldi(string mesaj, string iz, LogType tur)
        {
            if (tur == LogType.Log) return;
            lock (kilit)
            {
                if (log == null) return;
                if (tur == LogType.Warning) uyari++; else hata++;
                log.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + tur + ": " + mesaj);
                if (!string.IsNullOrEmpty(iz)) log.WriteLine(iz.TrimEnd());
                log.WriteLine();
            }
        }

        // Çalışma süresince editör log dosyasına eklenen uyarı/hata satırlarını kopyalar (stack trace'i olanlarla birlikte).
        static int YerelKayitlariEkle()
        {
            if (string.IsNullOrEmpty(konsolYolu) || !File.Exists(konsolYolu)) return 0;
            string metin;
            try
            {
                using (var fs = new FileStream(konsolYolu, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fs.Length <= konsolBaslangic) return 0;
                    fs.Seek(konsolBaslangic, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs)) metin = sr.ReadToEnd();
                }
            }
            catch (Exception e) { Not("Editör log dosyası okunamadı: " + e.Message); return 0; }
            var satirlar = metin.Split('\n');
            int n = 0;
            lock (kilit)
            {
                if (log == null) return 0;
                log.WriteLine();
                log.WriteLine("---- Editör log dosyasından (" + konsolYolu + ") ----");
                foreach (var ham in satirlar)
                {
                    string s = ham.TrimEnd('\r');
                    string k = s.ToLowerInvariant();
                    if (k.Contains("warning") || k.Contains("error") || k.Contains("exception") || k.Contains("unsupported") || k.Contains("unexpected") || k.Contains("failed"))
                    {
                        log.WriteLine(s);
                        n++;
                    }
                }
            }
            return n;
        }

        static void Not(string s) { lock (kilit) if (log != null) log.WriteLine("[ARAÇ] " + s); }

        // ---------- sürücü: yield 'float' = saniye bekle, 'Func<bool>' = koşulu bekle (en çok 25 sn) ----------
        static void Guncelle()
        {
            if (akis == null) return;
            if (!EditorApplication.isPlaying) { Bitir("Play modu kapandı"); return; }
            double t = EditorApplication.timeSinceStartup;
            if (beklenen != null)
            {
                bool tamam;
                try { tamam = beklenen(); } catch (Exception) { tamam = false; }
                if (!tamam && t < kosulBitis) return;
                if (!tamam) Not("koşul beklenirken zaman aşımı");
                beklenen = null;
            }
            if (t < bekleBitis) return;
            bool devam;
            try { devam = akis.MoveNext(); }
            catch (Exception e) { Not("AKIŞ HATASI: " + e); Bitir("Akış hatayla durdu"); return; }
            if (!devam) { Bitir("Tamamlandı"); return; }
            object o = akis.Current;
            if (o is float) bekleBitis = t + (float)o;
            else if (o is Func<bool>) { beklenen = (Func<bool>)o; kosulBitis = t + 25.0; }
        }

        // ---------- akış ----------
        static IEnumerable<object> Akis()
        {
            // temiz başlangıç: harita gündüz olsun (yedek sonunda geri yazılır)
            PlayerPrefs.DeleteKey("kizil_tohum_harita_tamam_v1");
            yield return (Func<bool>)(() => Arayuz != null && Yonetici != null);
            if (Arayuz == null)
            {
                Not("Sahnede KizilTohumArayuz yok, ekleniyor.");
                new GameObject("KizilTohum_OtoEkran").AddComponent<KizilTohumArayuz>();
                yield return 2f;
            }
            yield return 3f;

            // a) ana menü
            Cek("01_menu");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            yield return 1.5f;

            // b) Koloni Haritası (gündüz)
            Cagir("HaritaAc");
            yield return 3f;
            Cek("02_harita");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            yield return 1.5f;

            // c) haritadan Tohum Duvarı (KT04) dalışı ve sonrasında ilk adım
            var pin = NoktaBul("tohum_duvari");
            if (pin == null) Not("Haritada tohum_duvari noktası bulunamadı (c atlandı).");
            else
            {
                if (pin.Tiklandi != null) pin.Tiklandi();
                yield return 1.2f;
                Cek("03_harita_dalis");
                yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
                yield return (Func<bool>)(() => Harita == null || !Harita.Acik);
                yield return (Func<bool>)(() => !VideoAcik);
                yield return 2.5f;
                Cek("04_kt04_ilk_adim");
                yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            }
            yield return 1.5f;

            // d) KT01 açılışı (K02_acilis.mp4)
            Cagir("GorevAc", "KT01");
            yield return 2f;
            Cek("05_kt01_acilis_2sn");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            yield return 3f;
            Cek("06_kt01_acilis_5sn");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            if (!VideoAcik) Not("KT01 açılışında 5. saniyede video açık değildi (bitti ya da yüklenemedi).");
            yield return 1f;

            // e) KT04 hücre: izotonik, turgor, plazmoliz
            Cagir("VideoIptal");
            Cagir("GorevAc", "KT04");
            yield return 1.5f;
            foreach (var x in Gec(a => a.tip == AdimTipi.Kaydirici)) yield return x;
            KartKapat();
            if (Yonetici.AktifAdim == null || Yonetici.AktifAdim.tip != AdimTipi.Kaydirici) Not("KT04 kaydırıcı adımına ulaşılamadı (e atlandı).");
            else
            {
                Yonetici.KaydiriciDegisti(1.0f);
                yield return 2f;
                KartKapat();
                Cek("07_hucre_izotonik");
                yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
                Yonetici.KaydiriciDegisti(0.3f);
                yield return 2f;
                KartKapat();
                Cek("08_hucre_turgor");
                yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
                Yonetici.KaydiriciDegisti(2.8f);
                yield return 2f;
                KartKapat();
                Cek("09_hucre_plazmoliz");
                yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            }
            yield return 1f;

            // f) bir diyalog adımı ve bir seçenek ekranı (KT01)
            Cagir("GorevAc", "KT01");
            yield return 0.5f;
            Cagir("VideoIptal");
            foreach (var x in Gec(a => a.tip == AdimTipi.Diyalog)) yield return x;
            yield return 3f;   // daktilo metni tamamlansın
            KartKapat();
            Cek("10_diyalog");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            foreach (var x in Gec(a => a.SecenekliMi)) yield return x;
            yield return 2.5f; // seçenekler sırayla belirsin
            KartKapat();
            Cek("11_secenek");
            yield return 0.4f; // çekim kare sonunda alınır; sonraki adım görüntüye karışmasın
            // g) KT08 ilk adım (pompa odası)
            Cagir("VideoIptal");
            Cagir("GorevAc", "KT08");
            yield return 4f;
            KartKapat();
            Cek("12_kt08_ilk_adim");
            yield return 0.4f;

            // h) KT01 tohum_duvari_3d ve lab_masasi_3d: açılış anı ve zoom sonu (10. sn)
            Cagir("VideoIptal");
            Cagir("GorevAc", "KT01");
            yield return 0.5f;
            Cagir("VideoIptal");
            foreach (var x in Gec(a => a.id == "KT01-5")) yield return x;
            yield return 1.5f; KartKapat();
            Cek("13_tohum_duvari_acilis");
            yield return 8.8f; KartKapat();
            Cek("14_tohum_duvari_10sn");
            yield return 0.4f;
            foreach (var x in Gec(a => a.id == "KT01-11")) yield return x;
            yield return 1.5f; KartKapat();
            Cek("15_lab_masasi_acilis");
            yield return 8.8f; KartKapat();
            Cek("16_lab_masasi_10sn");
            yield return 0.4f;

            // i) harita dalış klipleri: son kare ve klipten sonraki ilk sahne
            foreach (var id in new[] { "tohum_duvari", "kubbe_sera" })
            {
                Cagir("VideoIptal");
                Cagir("HaritaAc");
                yield return 2f;
                var pn = NoktaBul(id);
                if (pn == null) { Not("harita noktası yok: " + id); continue; }
                pn.Tiklandi();
                yield return 4.55f;
                Cek("17_dalis_" + id + "_son_kare");
                yield return 0.4f;
                yield return 2.2f;
                Cek("18_dalis_" + id + "_sonrasi");
                yield return 0.4f;
            }

            // j) Ada portreleri: normal (KT01-3), yanlış ve doğru cevap (KT04-S3)
            Cagir("VideoIptal");
            Cagir("GorevAc", "KT01");
            yield return 0.5f;
            Cagir("VideoIptal");
            foreach (var x in Gec(a => a.id == "KT01-3")) yield return x;
            yield return 2.5f; KartKapat();
            Cek("19_ada_normal");
            yield return 0.4f;
            Cagir("GorevAc", "KT04");
            yield return 0.5f;
            Cagir("VideoIptal");
            foreach (var x in Gec(a => a.id == "KT04-S3")) yield return x;
            yield return 1.5f; KartKapat();
            for (int si = 0; si < 3 && Yonetici.AktifAdim != null && Yonetici.AktifAdim.id == "KT04-S3"; si++)
            {
                var sonuc = Yonetici.CevapVer(si);
                yield return 1.2f; KartKapat();
                Cek("20_ada_cevap_" + si + (sonuc != null && sonuc.dogru ? "_dogru" : "_yanlis"));
                yield return 0.4f;
                if (sonuc != null && sonuc.dogru) break;
            }

            // l) KT08: Ada (saskin), Okan, Lin (normal / yanlış / doğru) ve yeni sahneler
            Cagir("VideoIptal");
            Cagir("GorevAc", "KT08");
            yield return 0.5f;
            Cagir("VideoIptal");
            foreach (var id8 in new[] { "KT08-2", "KT08-4", "KT08-6", "KT08-10", "KT08-S1", "KT08-13", "KT08-17", "KT08-21", "KT08-22" })
            {
                string hedefId = id8;
                foreach (var x in Gec(a => a.id == hedefId)) yield return x;
                yield return 2.6f; KartKapat();
                if (hedefId == "KT08-S1")
                {
                    int nSec = Yonetici.AktifAdim != null ? Yonetici.AktifAdim.secenekler.Count : 0;
                    for (int si = nSec - 1; si >= 0 && Yonetici.AktifAdim != null && Yonetici.AktifAdim.id == "KT08-S1"; si--)   // ters sıra: önce yanlış
                    {
                        var sonuc = Yonetici.CevapVer(si);
                        yield return 1.2f; KartKapat();
                        Cek("30_lin_cevap_" + si + (sonuc != null && sonuc.dogru ? "_dogru" : "_yanlis"));
                        yield return 0.4f;
                        if (sonuc != null && sonuc.dogru) break;
                    }
                }
                else
                {
                    Cek("30_kt08_" + hedefId.Substring(5));
                    yield return 0.4f;
                }
            }

            // k) final ekranı (en iyi son: final_fidan_3d) ve Bekçi Defteri
            var ic = Yonetici.Icerik;
            var son = new OyunSonu { son = ic.sonlar[0], saglik = 100 };
            Cagir("SonGoster", son);
            yield return 1.5f;
            Cek("21_final_yesil_kubbe");
            yield return 0.4f;
            Cagir("SonGoster", new OyunSonu { son = ic.sonlar[2], saglik = 40 });
            yield return 1.5f;
            Cek("23_final_tek_cekmece");
            yield return 0.4f;
            Cagir("GorevAc", "KT01");
            yield return 0.4f;
            Cagir("VideoIptal");
            var ui = Arayuz;
            var df = typeof(KizilTohumArayuz).GetField("defter", BindingFlags.Instance | BindingFlags.NonPublic);
            var d = ui != null && df != null ? df.GetValue(ui) as DefterEkrani : null;
            if (d == null) Not("Defter ekranı bulunamadı");
            else
            {
                d.Ac();
                yield return 1.5f;
                Cek("22_defter");
                yield return 0.4f;
                d.Kapat();
            }
            yield return 1.5f;
        }

        // Hedef adıma gelene kadar ilerler: etkileşimi tamamlar, soruyu doğru seçenek bulunana kadar dener.
        static IEnumerable<object> Gec(Func<Adim, bool> hedef)
        {
            for (int i = 0; i < 80; i++)
            {
                var y = Yonetici;
                var a = y != null ? y.AktifAdim : null;
                if (a == null || hedef(a)) yield break;
                if (!y.Ilerlenebilir)
                {
                    if (a.tip == AdimTipi.Etkilesim) y.EtkilesimTamamlandi(a.nesne);
                    else if (a.SecenekliMi) for (int s = 0; s < a.secenekler.Count && !y.Ilerlenebilir; s++) y.CevapVer(s);
                    else if (a.tip == AdimTipi.Kaydirici) y.KaydiriciDegisti((a.hedefMin + a.hedefMax) * 0.5f);
                }
                if (y.Ilerlenebilir && y.AktifAdim == a) y.Ilerle();
                yield return 0.3f;
            }
            Not("Hedef adıma 80 denemede ulaşılamadı.");
        }

        // ---------- yardımcılar ----------
        static KizilTohumArayuz Arayuz { get { return UnityEngine.Object.FindAnyObjectByType<KizilTohumArayuz>(); } }
        static HaritaEkrani Harita { get { return UnityEngine.Object.FindAnyObjectByType<HaritaEkrani>(); } }
        static GorevYoneticisi Yonetici { get { return OyunDenetleyici.Instance != null ? OyunDenetleyici.Instance.Yonetici : null; } }

        static bool VideoAcik
        {
            get
            {
                var ui = Arayuz;
                if (ui == null) return false;
                var f = typeof(KizilTohumArayuz).GetField("videoKok", BindingFlags.Instance | BindingFlags.NonPublic);
                var v = f != null ? f.GetValue(ui) as GameObject : null;
                return v != null;
            }
        }

        static void Cagir(string ad, params object[] arg)
        {
            var ui = Arayuz;
            var m = typeof(KizilTohumArayuz).GetMethod(ad, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (ui == null || m == null) { Not("Çağrılamadı: " + ad); return; }
            m.Invoke(ui, arg);
        }

        static HaritaNoktasi NoktaBul(string id)
        {
            var hepsi = Resources.FindObjectsOfTypeAll<HaritaNoktasi>();
            for (int i = 0; i < hepsi.Length; i++)
            {
                var ata = hepsi[i].transform.parent;
                if (ata != null && ata.name == "Nokta_" + id) return hepsi[i];
            }
            return null;
        }

        // Akış sırasında açılan kavram kartı ekranı kapatmasın
        static void KartKapat()
        {
            var ui = Arayuz;
            var f = typeof(KizilTohumArayuz).GetField("kartPaneli", BindingFlags.Instance | BindingFlags.NonPublic);
            var p = ui != null && f != null ? f.GetValue(ui) as GameObject : null;
            if (p != null && p.activeSelf) { p.SetActive(false); Not("açık kavram kartı kapatıldı"); }
        }

        static void Cek(string ad)
        {
            string yol = Path.Combine(klasor, ad + ".png");
            if (File.Exists(yol)) File.Delete(yol);
            ScreenCapture.CaptureScreenshot(yol, 1);
            cekilenler.Add(ad + ".png");
            var y = Yonetici;
            Not(ad + ".png · adım: " + (y != null && y.AktifAdim != null ? y.AktifAdim.id + " (" + y.AktifAdim.tip + ")" : "-") + " · video: " + (VideoAcik ? "açık" : "yok"));
        }
    }
}
#endif
