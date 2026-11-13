using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>
    /// KT04 osmoz simülasyonu için hücre görünümü: hucre_izotonik / hucre_turgor / hucre_plazmoliz aynı
    /// RectTransform'da üst üste durur ve OsmozSonucu'na göre çapraz erir. Duvar üç görselde aynıdır ve hiç ölçeklenmez.
    /// Su parçacıkları saydam beyaz-yeşil parıltıdır (mavi yok); plazmolizde dışa, turgorda içe akar. CO₂ kabarcığı yok.
    /// Etiketler (hücre duvarı, hücre zarı, vakuol) kodla UI olarak çizilir. Görsel yoksa Kur null döner (eski çizime düşülür).
    /// </summary>
    public class HucreGorunumu : MonoBehaviour
    {
        class Parcacik
        {
            public RectTransform r;
            public Image im;
            public float aci, faz, hiz;
        }

        class Etiket
        {
            public RectTransform yazi, cizgi;
            public Vector2 yaziKonum, hedef;
        }

        RectTransform kutu, govde;
        Image turgor, plazmoliz;
        float wT, wP, hT, hP;
        int suYonu;
        readonly List<Parcacik> parcaciklar = new List<Parcacik>();
        readonly List<Etiket> etiketler = new List<Etiket>();
        static Sprite parilti;

        public static HucreGorunumu Kur(Transform ata, Font font)
        {
            var si = Resources.Load<Sprite>("Gorseller/Hucre/hucre_izotonik");
            if (si == null) return null;
            var go = new GameObject("HucreGorunumu", typeof(RectTransform));
            go.transform.SetParent(ata, false);
            var kok = (RectTransform)go.transform;
            kok.anchorMin = new Vector2(0f, Tema.HucreAltSinir);   // panelin yüksekliğine yayılır; en-boy oranı kutuda korunur
            kok.anchorMax = new Vector2(1f, 1f);
            kok.offsetMin = Vector2.zero; kok.offsetMax = new Vector2(0f, -8f);
            var h = go.AddComponent<HucreGorunumu>();
            h.Olustur(si, Resources.Load<Sprite>("Gorseller/Hucre/hucre_turgor"), Resources.Load<Sprite>("Gorseller/Hucre/hucre_plazmoliz"), font);
            return h;
        }

        void Olustur(Sprite si, Sprite st, Sprite sp, Font font)
        {
            // görselin oranında kutu: çocukların normalize konumları görsel koordinatıdır
            kutu = (RectTransform)Bos("Kutu", transform).transform;
            var oran = kutu.gameObject.AddComponent<AspectRatioFitter>();
            oran.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            oran.aspectRatio = si.rect.width / Mathf.Max(1f, si.rect.height);

            govde = (RectTransform)Bos("Govde", kutu).transform;
            Doldur(govde);
            Katman("Izotonik", si);              // taban hep opak; üstteki iki katman erir
            turgor = Katman("Turgor", st);
            plazmoliz = Katman("Plazmoliz", sp);

            if (parilti == null) parilti = PariltiUret();
            for (int i = 0; i < Tema.HucreParcacikSayisi; i++)
            {
                var im = Bos("Su" + i, kutu).AddComponent<Image>();
                im.sprite = parilti; im.raycastTarget = false; im.color = Tema.Saydam;
                var r = im.rectTransform;
                r.sizeDelta = new Vector2(Tema.HucreParcacikBoyu, Tema.HucreParcacikBoyu);
                parcaciklar.Add(new Parcacik { r = r, im = im, aci = Random.value * Mathf.PI * 2f, faz = Random.value, hiz = Random.Range(0.75f, 1.25f) });
            }

            // etiketler hücrenin dışında (kenar boşluğunda), çizgiler hücre silüetinin dışında biter
            EtiketEkle("Hücre duvarı", new Vector2(0.03f, 0.30f), new Vector2(0.20f, 0.40f), font);
            EtiketEkle("Hücre zarı", new Vector2(0.03f, 0.82f), new Vector2(0.33f, 0.77f), font);
            EtiketEkle("Vakuol", new Vector2(0.33f, 0.99f), new Vector2(0.45f, 0.94f), font);
        }

        Image Katman(string ad, Sprite s)
        {
            if (s == null) return null;
            var im = Bos(ad, govde).AddComponent<Image>();
            im.sprite = s; im.raycastTarget = false; im.preserveAspect = false;
            Doldur(im.rectTransform);
            return im;
        }

        void EtiketEkle(string metin, Vector2 yaziKonum, Vector2 hedef, Font font)
        {
            var e = new Etiket { yaziKonum = yaziKonum, hedef = hedef };
            var cz = Bos("Cizgi", kutu).AddComponent<Image>();
            cz.color = Tema.HucreEtiketCizgi; cz.raycastTarget = false;
            e.cizgi = cz.rectTransform;
            e.cizgi.anchorMin = e.cizgi.anchorMax = hedef;
            e.cizgi.pivot = new Vector2(0f, 0.5f);   // hedefte başlar, yazıya doğru döner
            var t = Bos("Etiket", kutu).AddComponent<Text>();
            t.font = font; t.fontSize = Tema.YaziKucuk; t.color = Tema.Beyaz; t.raycastTarget = false;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = metin;
            t.gameObject.AddComponent<Shadow>().effectColor = Tema.Orten;
            e.yazi = t.rectTransform;
            e.yazi.anchorMin = e.yazi.anchorMax = yaziKonum;
            e.yazi.pivot = new Vector2(1f, 0.5f);   // yazı konumdan sola doğru uzar, çizgi yazının sağ ucundan çıkar
            t.alignment = TextAnchor.MiddleRight;
            e.yazi.sizeDelta = new Vector2(200f, 30f);
            etiketler.Add(e);
        }

        /// <summary>Hedef durumu ayarlar; aninda ise erime beklenmeden yerine oturur (panel yeni açıldığında).</summary>
        public void Ayarla(OsmozSonucu o, bool aninda)
        {
            hT = turgor != null ? o.turgor : 0f;
            hP = plazmoliz != null ? o.plazmoliz : 0f;
            suYonu = o.suYonu;
            if (aninda) { wT = hT; wP = hP; Uygula(); }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float adim = dt / Tema.HucreGecisSure;   // tam geçiş 0.8 sn
            wT = Mathf.MoveTowards(wT, hT, adim);
            wP = Mathf.MoveTowards(wP, hP, adim);
            Uygula();

            // nefes: Tema.HucreNefesOlcek 1 iken kapalı (duvar ölçeklenmesin diye)
            float nefes = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * 2f * Mathf.PI / Tema.HucreNefesSure);
            govde.localScale = Vector3.one * Mathf.Lerp(1f, Tema.HucreNefesOlcek, nefes);

            // su: saydam beyaz-yeşil parıltı; plazmolizde dışa (-1), turgorda içe (+1), dengede duvar çevresinde salınır
            float yogun = suYonu == 0 ? Tema.HucreParcacikDengeAlfa : Mathf.Lerp(Tema.HucreParcacikDengeAlfa * 1.5f, 1f, Mathf.Max(wT, wP));
            for (int i = 0; i < parcaciklar.Count; i++)
            {
                var p = parcaciklar[i];
                p.faz += dt * Tema.HucreParcacikHiz * p.hiz;
                if (p.faz >= 1f) { p.faz -= 1f; p.aci = Random.value * Mathf.PI * 2f; }
                float r = suYonu < 0 ? Mathf.Lerp(Tema.HucreParcacikIc, Tema.HucreParcacikDis, p.faz)
                        : suYonu > 0 ? Mathf.Lerp(Tema.HucreParcacikDis, Tema.HucreParcacikIc, p.faz)
                        : 1f + 0.08f * Mathf.Sin(p.faz * 2f * Mathf.PI);
                var konum = Tema.HucreMerkez + new Vector2(Mathf.Cos(p.aci) * Tema.HucreYaricap.x, Mathf.Sin(p.aci) * Tema.HucreYaricap.y) * r;
                p.r.anchorMin = p.r.anchorMax = konum;
                p.r.anchoredPosition = Vector2.zero;
                var c = Tema.HucreSu; c.a *= Mathf.Sin(p.faz * Mathf.PI) * yogun;
                p.im.color = c;
            }

            // etiket çizgileri: hedeften yazıya
            Vector2 boy = kutu.rect.size;
            for (int i = 0; i < etiketler.Count; i++)
            {
                var e = etiketler[i];
                Vector2 fark = Vector2.Scale(e.yaziKonum - e.hedef, boy);
                e.cizgi.sizeDelta = new Vector2(fark.magnitude, 2f);
                e.cizgi.anchoredPosition = Vector2.zero;
                e.cizgi.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fark.y, fark.x) * Mathf.Rad2Deg);
            }
        }

        void Uygula()
        {
            if (turgor != null) turgor.color = new Color(1f, 1f, 1f, wT);
            if (plazmoliz != null) plazmoliz.color = new Color(1f, 1f, 1f, wP);
        }

        static Sprite PariltiUret()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
            var c = new Vector2(n * 0.5f, n * 0.5f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        static GameObject Bos(string ad, Transform ata)
        {
            var g = new GameObject(ad, typeof(RectTransform));
            g.transform.SetParent(ata, false);
            return g;
        }

        static void Doldur(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
