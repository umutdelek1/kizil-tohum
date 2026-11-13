using System.Collections.Generic;
using UnityEngine;

namespace KizilTohum
{
    /// <summary>
    /// Görevler arasında biriken kavram kartı kimlikleri (PlayerPrefs, virgülle ayrılmış). Aynı kart bir kez sayılır.
    /// Görev yeniden başlayınca silinmez; yalnızca "Yeni Oyun" Sifirla() çağırır.
    /// </summary>
    public static class KartKumesi
    {
        const string Anahtar = "kizil_tohum_kartlar_v1";

        public static HashSet<string> Oku()
        {
            var k = new HashSet<string>();
            string s = PlayerPrefs.GetString(Anahtar, "");
            if (!string.IsNullOrEmpty(s))
            {
                var parca = s.Split(',');
                for (int i = 0; i < parca.Length; i++) if (parca[i].Length > 0) k.Add(parca[i]);
            }
            return k;
        }

        public static int Sayi { get { return Oku().Count; } }

        public static void Ekle(string kartId)
        {
            if (string.IsNullOrEmpty(kartId)) return;
            Birlestir(new[] { kartId });
        }

        /// <summary>Verilen kimlikleri kümeye ekler (yeni varsa yazar). Eski kayıttan gelen kartları da yakalar.</summary>
        public static void Birlestir(IEnumerable<string> kartIdleri)
        {
            if (kartIdleri == null) return;
            var k = Oku();
            bool degisti = false;
            foreach (var id in kartIdleri) if (!string.IsNullOrEmpty(id) && k.Add(id)) degisti = true;
            if (!degisti) return;
            PlayerPrefs.SetString(Anahtar, string.Join(",", new List<string>(k).ToArray()));
            PlayerPrefs.Save();
        }

        public static void Sifirla()
        {
            PlayerPrefs.DeleteKey(Anahtar);
            PlayerPrefs.Save();
        }
    }
}
