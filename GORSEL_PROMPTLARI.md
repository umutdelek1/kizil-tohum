# Kızıl Tohum · Görsel üretim promptları (ChatGPT)

Hackathon süresince üretilir. Her görsel **ayrı mesajla**, aynı sohbette üretilirse tutarlılık artar. İndirirken ekran görüntüsü değil, **"İndir"** düğmesiyle tam çözünürlüklü PNG al.

## 0. Sabit stil bloğu (HER prompta en başa yapıştır)

> Stil: sinematik, boyanmış gerçekçi dijital illüstrasyon; koyu ve sıcak Mars atmosferi. Renk paleti: Mars kızılı #E0643B, yaşam yeşili #5DFFA6, buz mavisi #8FD8FF, kubbe/yapay zekâ moru #B47CFF, uyarı sarısı #FFD35A; koyu arka plan #0F1720. Yumuşak hacimsel ışık, hafif film greni. Görselde YAZI, LOGO, FİLİGRAN veya HARF OLMASIN (belirtilen etiketler hariç). Gerçek kişi veya telifli karakter benzetme.

## 1. Karakter tarifleri (portrelerde aynen kullan)

- **Ada:** 17 yaşında kız, kısa dağınık koyu saç, çilli yüz, bej tişört üstüne petrol yeşili tulum, iş eldiveni, elinde tablet.
- **Komutan Okan:** 50'li yaşlarda erkek, kır saçlı ve sakallı, koyu lacivert iş ceketi, turuncu omuz şeridi.
- **Dr. Lin:** 50'li yaşlarda kadın, kır saç tutamlı topuz, gözlük, haki laboratuvar önlüğü; hafif soğuk ışık.
- **TOHUM-7:** insan yüzü YOK; duvara gömülü yuvarlak mercek ve etrafında ince ışık halkası.

## 2. Parti 1 · Zorunlu (oyun bunlarsız oynanmaz)

Sahneler: **16:9, en az 1920×1080, üzerinde HİÇBİR yazı yok.**

| Dosya adı | Prompt (stil bloğundan sonra) |
|---|---|
| `kubbe_dis.png` | Kızıl çöl vadisinde, gün batımında, Mars kolonisi Umut-1'in büyük cam sera kubbesi; kubbenin içinde hafif mor ışık; ön planda toz fırtınasının yeni dindiği ince kızıl toz. |
| `dis_koloni.png` | Umut-1 kolonisinin dış yürüyüş yolu: ayak izleri, tanklar, kubbeye giden patika; sakin ve hafif tedirgin. |
| `tohum_duvari.png` | Koloninin Tohum Duvarı: yüzlerce küçük numaralı çekmece, bir kısmının ışığı sarı; ortada TOHUM-7'nin mor ışık halkalı yuvarlak merceği; köşede 47 numaralı çekmece belirgin. |
| `lab_masasi.png` | Laboratuvar masası: iki kapalı cam kavanoz, mikroskop, açık bir defter ve tablet; soğuk beyaz-mavi masa lambası. |
| `sera_ici.png` | Sera içi: bitki sıraları, mor büyütme lambaları, nemli koyu toprak; yapraklar sarkık ve soluk. |
| `pompa_odasi.png` | Pompa odası: kalın borular, büyük kırmızı vana, su tankları, loş endüstriyel ışık. |
| `ada_47.png` | Ada, 47 numaralı çekmeceyi dikkatle açıyor; çekmecenin içinde küçük bir tohum parlıyor; yüzü yarı gölgede. |
| `sarkik_bitki.png` | Çok ıslak toprak ve onun içinde sarkık, susuz görünen bir bitki; çelişkiyi vurgulayan yakın plan. |

Portreler: **1024×1024, şeffaf arka plan, bel üstü, yüz öne dönük, yazı yok.**

| Dosya adı | Prompt |
|---|---|
| `ada_endiseli.png` | Ada (tarif yukarıda), endişeli, kaşları hafif kalkık. |
| `ada_dusunceli.png` | Ada, düşünceli, çenesi elinde. |
| `ada_gulumseyen.png` | Ada, hafifçe gülümseyen, rahatlamış. |
| `okan_notr.png` | Komutan Okan, nötr ve yorgun bakış. |
| `okan_sert.png` | Komutan Okan, sert ve kararlı bakış. |
| `lin_portre.png` | Dr. Lin, sakin, hafif soğuk ışık. |
| `mercek_normal.png` | TOHUM-7 mercek, mor (#B47CFF) ışık halkası, sakin. Şeffaf arka plan. |
| `mercek_olcuyor.png` | Aynı mercek, iç halkada ince tarama çizgisi, daha parlak. |
| `mercek_uyari.png` | Aynı mercek, halka sarı (#FFD35A). |

Hücre (kaydırıcı): **dört görsel, 1024×1024, aynı kompozisyon ve aynı çeper konumu, şeffaf arka plan.** Bilim kuralı: çeper dört görselde de **aynı yerde ve aynı boyutta**.

| Dosya adı | Prompt |
|---|---|
| `hucre_turgorlu.png` | Bitki hücresi kesiti: kalın çeper, zar çepere tam yaslı, büyük merkezî koful (hacmin %80'i), ince sitoplazma, kenarda çekirdek ve kloroplastlar. Gergin. |
| `hucre_gevsek.png` | Aynı hücre, koful biraz küçük, zar çepere hafifçe değiyor, gevşek. |
| `hucre_plazmoliz_basliyor.png` | Aynı hücre, zar yalnızca köşelerden çeperden ayrılmış; çeper değişmemiş. |
| `hucre_plazmolize.png` | Aynı hücre, zar ve sitoplazma belirgin şekilde büzülmüş ve çeperden ayrılmış; çeper ile zar arasında açık mavi-gri dış çözelti; çeper aynı yerde. |

Fide ve kavanoz (küçük sprite, **şeffaf arka plan, 512×512**):

| Dosya adı | Prompt |
|---|---|
| `fide_saglikli.png` | Tek bir yeşil, sağlıklı genç fide, yaprakları temiz. |
| `fide_lekeli.png` | Aynı fide, yapraklarında koyu mantar lekeleri (hassas). |
| `fide_direncli.png` | Aynı fide, yaprakları temiz ve canlı yeşil (dirençli), çevresinde çok ince yeşil ışık. |
| `kavanoz_canli.png` | Kapalı cam kavanoz, içinde nemli toprakta altı tohum, ikisi filiz vermiş; kapağında küçük CO₂ sensörü. |
| `kavanoz_kaynatilmis.png` | Aynı kavanoz, tohumlar soluk ve filizsiz; yanında küçük tencere-buhar simgesi (yazısız). |

## 3. Parti 2 · Sonra (zorunlu değil)

- Sonlar için sera içi varyantları: `final_yesil_kubbe.png` (bol yeşil), `final_yarim_sera.png` (yarı yeşil), `final_tek_cekmece.png` (yalnız 47. çekmece).
- `lin_revir.png` (Lin, revir yatağında bağlantıda), `okan_rahatlamis.png`, `ada_kararli.png`.
- Sinematik kareler: kubbe_dis_toz, cekmece47_ic, vana_yakin (üzerinde "03.14" ekranı), tozlastirma_yakin (iki fırça, yüz yok), modul_101.
- Sunum kapağı, logo, favicon.

## 4. Kavram kartı ve arayüz

- Kavram kartları ve arayüz **oyunda kodla çizilecek** (koyu panel, renkli kenar, metin). Görsel istemiyoruz; sadece Parti 1 yeter.

## 5. Kurallar (tekrar)

- Gelen her görseli bana göster; bilimsel hata (ör. hücre çeperi kayması, CO₂ baloncuğu, yanlış alel etiketi) kontrolü ben yaparım.
- Dosyaları yukarıdaki adlarla `Assets/Resources/Gorseller/` içine koyacağız (ben yerleştiririm).
