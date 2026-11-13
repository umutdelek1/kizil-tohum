using System.Collections.Generic;

namespace KizilTohum
{
    /// <summary>Platformdan bağımsız, aynı seed için her zaman aynı sırayı veren karıştırıcı.</summary>
    public static class Karistirici
    {
        // FNV-1a: string.GetHashCode() çalıştırmadan çalıştırmaya değiştiği için kullanılmaz.
        public static uint Ozet(string s)
        {
            uint h = 2166136261u;
            if (s == null) return h;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }

        /// <summary>0..n-1 permütasyonu. sabit=true ise karıştırmaz.</summary>
        public static int[] Sira(int n, int seed, string anahtar, bool sabit)
        {
            var dizi = new int[n];
            for (int i = 0; i < n; i++) dizi[i] = i;
            if (sabit || n < 2) return dizi;
            uint durum = (uint)seed * 2654435761u + Ozet(anahtar) + 12345u;
            for (int i = n - 1; i > 0; i--)
            {
                durum = durum * 1664525u + 1013904223u;
                int j = (int)((durum >> 8) % (uint)(i + 1));
                int gecici = dizi[i]; dizi[i] = dizi[j]; dizi[j] = gecici;
            }
            return dizi;
        }
    }
}
