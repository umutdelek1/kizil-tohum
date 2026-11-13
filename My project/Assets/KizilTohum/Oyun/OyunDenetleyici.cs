using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KizilTohum
{
    /// <summary>
    /// Unity tarafındaki tek giriş noktası: içeriği yükler, GorevYoneticisi'ni kurar, kaydı PlayerPrefs'e yazar,
    /// klavyeyi (Boşluk/Enter, 1-4, H) mantığa bağlar. Arayüz bu sınıfın Yonetici olaylarını dinler.
    /// Sahneye bir kez eklenir (boş bir GameObject + bu bileşen).
    /// </summary>
    public class OyunDenetleyici : MonoBehaviour
    {
        const string KayitAnahtari = "kizil_tohum_kayit_v1";

        public static OyunDenetleyici Instance { get; private set; }
        public GorevYoneticisi Yonetici { get; private set; }
        public readonly List<string> IcerikHatalari = new List<string>();

        public bool KayitVarMi { get { return PlayerPrefs.HasKey(KayitAnahtari); } }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            IcerikVerisi icerik = IcerikYukleyici.YukleResources(IcerikHatalari);
            for (int i = 0; i < IcerikHatalari.Count; i++) Debug.LogError("[KızılTohum] içerik: " + IcerikHatalari[i]);
            Yonetici = new GorevYoneticisi(icerik);
            Yonetici.KayitDegisti += KaydiYaz;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static int YeniSeed()
        {
            return (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
        }

        public void YeniOyun()
        {
            PlayerPrefs.DeleteKey(KayitAnahtari);
            Yonetici.YeniOyun(YeniSeed());
        }

        public bool DevamEt()
        {
            return Yonetici.Yukle(PlayerPrefs.GetString(KayitAnahtari, ""));
        }

        public bool GoreveGit(string gorevId)
        {
            return Yonetici.GoreveGit(gorevId, YeniSeed());
        }

        /// <summary>Adres çubuğundaki ?gorev=KT04 değeri (yoksa boş).</summary>
        public static string DerinBaglantiGorevi()
        {
            string url = Application.absoluteURL;
            if (string.IsNullOrEmpty(url)) return "";
            int i = url.IndexOf("gorev=", StringComparison.Ordinal);
            if (i < 0) return "";
            string s = url.Substring(i + 6);
            int son = s.IndexOfAny(new[] { '&', '#' });
            if (son >= 0) s = s.Substring(0, son);
            return s.Trim();
        }

        void KaydiYaz()
        {
            PlayerPrefs.SetString(KayitAnahtari, Yonetici.Kaydet());
            PlayerPrefs.Save();
        }

        void Update()
        {
            if (Yonetici == null) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;

            if (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
                Yonetici.Ilerle();

            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Yonetici.CevapVer(0);
            if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Yonetici.CevapVer(1);
            if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Yonetici.CevapVer(2);
            if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) Yonetici.CevapVer(3);

            if (k.hKey.wasPressedThisFrame) Yonetici.IpucuIste();

            if (k.eKey.wasPressedThisFrame)
            {
                Adim a = Yonetici.AktifAdim;
                if (a != null && a.tip == AdimTipi.Etkilesim) Yonetici.EtkilesimTamamlandi(a.nesne);
            }
        }
    }
}
