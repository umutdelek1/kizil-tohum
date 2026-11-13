# Kızıl Tohum

TYT Biyoloji odaklı, Mars kolonisinde geçen hikâyeli bir bilim gizemi oyunu.
İşler Teknoloji EdTech Hackathon · 13–14 Kasım 2026.

Unity projesi `My project/` klasöründedir (Unity 6000.6.9f1, URP, WebGL).

## Kurulum

- **Klonlamak için `git lfs install` gerekir.** Görseller, videolar ve fontlar Git LFS ile tutulur.
- WebGL yayını: Unity'de **Kızıl Tohum › WebGL Ayarlarını Uygula**, sonra File › Build Profiles › WebGL › Build. Çıktı klasörüne `Dagitim/vercel.json` kopyalanır (Sıkıştırma kapalıdır (Vercel `.br` dosyalarını çift sıkıştırdığı için Brotli bırakıldı); `Dagitim/vercel.json` yalnızca Content-Type ve `/Build/*` için `max-age=0, must-revalidate` (ETag ile 304; veriyi Unity kendi IndexedDB önbelleğinde tutar)).

## Kaynaklar ve lisanslar

- **Inter** ve **Space Grotesk** fontları SIL Open Font License (OFL) ile lisanslı, önceden hazır bileşenlerdir.
- Görseller ChatGPT ve Gemini ile üretilmiştir.
