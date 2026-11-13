#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace KizilTohum.Editor
{
    /// <summary>Son WebGL build'in raporunu (en büyük kalemler, kategori toplamları) ve doku importer durumunu masaüstüne yazar.</summary>
    public static class BuildRaporu
    {
        [MenuItem("Kızıl Tohum/Build Raporu Yaz")]
        public static void Yaz()
        {
            var sb = new StringBuilder();
            var r = BuildReport.GetLatestReport();
            if (r == null) sb.AppendLine("Son build raporu bulunamadı.");
            else
            {
                sb.AppendLine("Build: " + r.summary.outputPath + " · sonuç " + r.summary.result + " · toplam " + MB(r.summary.totalSize) + " MB");
                sb.AppendLine("--- Çıktı dosyaları (role bazlı) ---");
                foreach (var f in r.GetFiles().OrderByDescending(x => (long)x.size).Take(15))
                    sb.AppendLine(string.Format("{0,10:F2} MB  {1}  [{2}]", MB(f.size), f.path.Replace('\\', '/'), f.role));
                sb.AppendLine("--- packedAssets: kategori toplamları ---");
                var tum = new List<KeyValuePair<string, ulong>>();
                var kat = new Dictionary<string, ulong>();
                foreach (var pa in r.packedAssets)
                    foreach (var c in pa.contents)
                    {
                        string t = c.type != null ? c.type.Name : "?";
                        ulong sz = c.packedSize;
                        ulong o; kat.TryGetValue(t, out o); kat[t] = o + sz;
                        tum.Add(new KeyValuePair<string, ulong>(c.sourceAssetPath + " (" + t + ")", sz));
                    }
                foreach (var k in kat.OrderByDescending(x => x.Value).Take(12)) sb.AppendLine(string.Format("{0,10:F2} MB  {1}", MB(k.Value), k.Key));
                sb.AppendLine("--- En büyük 25 kalem ---");
                foreach (var k in tum.OrderByDescending(x => x.Value).Take(25)) sb.AppendLine(string.Format("{0,10:F2} MB  {1}", MB(k.Value), k.Key));
            }

            sb.AppendLine("--- Doku importer: Resources/Gorseller (WebGL) ---");
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Gorseller" });
            var sayi = new Dictionary<string, int>();
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                var ps = ti.GetPlatformTextureSettings("WebGL");
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                string fmt = tex != null ? tex.format.ToString() : "?";
                string anahtar = "crunch=" + ti.crunchedCompression + " q=" + ti.compressionQuality + " overridden=" + ps.overridden + " fmt=" + ps.format + " max=" + ps.maxTextureSize + " | imp.max=" + ti.maxTextureSize + " comp=" + ti.textureCompression + " mip=" + ti.mipmapEnabled + " → asset format " + fmt;
                int o; sayi.TryGetValue(anahtar, out o); sayi[anahtar] = o + 1;
                if (fmt.Contains("RGBA32") || fmt.Contains("RGB24") || tex != null && (tex.width % 4 != 0 || tex.height % 4 != 0))
                    sb.AppendLine("  SIKIŞTIRMASIZ/NPOT? " + p + " " + (tex != null ? tex.width + "x" + tex.height : "") + " " + fmt);
            }
            foreach (var k in sayi.OrderByDescending(x => x.Value)) sb.AppendLine(string.Format("{0,5} adet  {1}", k.Value, k.Key));

            string yol = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "KizilTohum_BuildRaporu.txt");
            File.WriteAllText(yol, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[KızılTohum] Build raporu yazıldı: " + yol);
        }

        /// <summary>
        /// Boyutu 4'ün katı olmayan PNG'ler DXT ile sıkıştırılamaz (RGBA32/RGB24 kalır). Bu araç son satır/sütunu
        /// çoğaltarak boyutu 4'ün katına tamamlar (en fazla 3 px; görsel fark yok).
        /// </summary>
        [MenuItem("Kızıl Tohum/Doku Boyunu 4'ün Katına Tamamla")]
        public static void BoyutTamamla()
        {
            var sb = new StringBuilder();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Gorseller" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                var tam = Path.GetFullPath(p);
                var kaynak = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(kaynak, File.ReadAllBytes(tam), false)) { UnityEngine.Object.DestroyImmediate(kaynak); continue; }
                int w = kaynak.width, h = kaynak.height;
                int nw = (w + 3) / 4 * 4, nh = (h + 3) / 4 * 4;
                if (nw == w && nh == h) { UnityEngine.Object.DestroyImmediate(kaynak); continue; }
                var px = kaynak.GetPixels32();
                var yeni = new Color32[nw * nh];
                for (int y = 0; y < nh; y++)
                    for (int x = 0; x < nw; x++)
                        yeni[y * nw + x] = px[Mathf.Min(y, h - 1) * w + Mathf.Min(x, w - 1)];
                var hedef = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
                hedef.SetPixels32(yeni); hedef.Apply();
                File.WriteAllBytes(tam, hedef.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(kaynak); UnityEngine.Object.DestroyImmediate(hedef);
                sb.AppendLine(p + ": " + w + "x" + h + " -> " + nw + "x" + nh);
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("[KızılTohum] Boyut tamamlama: " + (sb.Length > 0 ? sb.ToString() : "değişen dosya yok"));
        }

        /// <summary>Resources/Gorseller altındaki tüm dokuları yeniden içe aktarır (importer kuralları değişince).</summary>
        [MenuItem("Kızıl Tohum/Görselleri Yeniden İçe Aktar")]
        public static void YenidenIceAktar()
        {
            AssetDatabase.ImportAsset("Assets/Resources/Gorseller", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[KızılTohum] Görseller yeniden içe aktarıldı.");
        }

        static double MB(ulong b) { return b / 1048576.0; }
    }
}
#endif
