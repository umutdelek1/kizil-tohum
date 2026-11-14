namespace KizilTohum
{
    /// <summary>
    /// Haritadaki kilitli işaretlere tıklayınca açılan "Yakında" kartlarının verisi (icerik.json'dan bağımsız küçük dizi).
    /// noktaId: Tema.HaritaNoktalari içindeki kimlik. Konular TYT Biyoloji; ipucu tek satır ve merak uyandırıcıdır.
    /// </summary>
    public static class YakindaKartlari
    {
        public struct Kart
        {
            public string noktaId, yer, konu, ipucu;
            public Kart(string noktaId, string yer, string konu, string ipucu)
            { this.noktaId = noktaId; this.yer = yer; this.konu = konu; this.ipucu = ipucu; }
        }

        public static readonly Kart[] Liste =
        {
            // Enzimler: biyolojik katalizör, tepkime sonunda tükenmez; optimum sıcaklık/pH vardır, aşırı sıcaklık yapıyı bozar.
            new Kart("laboratuvar", "Laboratuvar", "Enzimler",
                "Aynı enzim neden her sıcaklıkta ve her pH'ta çalışmaz?"),

            // Canlıların sınıflandırılması: ikili (binominal) adlandırma, cins + tür adı; Linnaeus.
            new Kart("komuta", "Radar İstasyonu", "Canlıların sınıflandırılması",
                "Dünyanın her yerinde aynı geçerli olan, cins ve tür adından oluşan iki kelimelik canlı adı nasıl çalışır?"),

            // Ekosistem: enerji akışı tek yönlüdür; her basamakta enerjinin büyük kısmı kaybolur.
            new Kart("inis_pisti", "İniş Pisti", "Ekosistem",
                "Besin zincirinde enerji bir basamaktan diğerine geçerken büyük kısmı nereye gider?"), // KONTROL ET: soruda sayı vermedim; "yaklaşık %10 aktarılır" kuralı kitaba göre değişebilir

            // Fotosentez: ışık enerjisi kimyasal enerjiye çevrilir; su ışığa bağımlı evrede parçalanır, O2 açığa çıkar.
            new Kart("yol_kavsagi", "Yer İstasyonu", "Fotosentez",
                "Bir yaprak ışığı yakalarken atmosfere hangi gazı verir ve bu gaz hangi molekülden gelir?"),
        };

        public static bool Bul(string noktaId, out Kart kart)
        {
            for (int i = 0; i < Liste.Length; i++)
                if (Liste[i].noktaId == noktaId) { kart = Liste[i]; return true; }
            kart = default(Kart);
            return false;
        }
    }
}
