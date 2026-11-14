using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>Menü soldan kayarak girer: etkinleşince sol kenardan yerine yumuşakça oturur ve belirir. Sunum katmanı, oyun mantığına dokunmaz.</summary>
    public class MenuGirisHareketi : MonoBehaviour
    {
        public float sure = 0.5f, mesafe = 520f;
        RectTransform rt;
        CanvasGroup cg;
        float t;

        void Awake() { rt = (RectTransform)transform; cg = GetComponent<CanvasGroup>(); if (cg == null) cg = gameObject.AddComponent<CanvasGroup>(); }
        void OnEnable() { if (rt == null) Awake(); t = 0f; Uygula(0f); }

        void Update()
        {
            if (t >= sure) return;
            t += Time.unscaledDeltaTime;
            Uygula(Mathf.Clamp01(t / sure));
        }

        void Uygula(float f)
        {
            float e = 1f - (1f - f) * (1f - f) * (1f - f);   // yavaşlayarak biter
            rt.anchoredPosition = new Vector2(-mesafe * (1f - e), 0f);
            cg.alpha = Mathf.Clamp01(f * 2.2f);
        }
    }

    /// <summary>Düğmenin üzerine gelince kehribar bir parlama katmanı belirir; çıkınca söner.</summary>
    public class MenuParlama : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image parlama;
        public float azami = 0.34f, hiz = 9f;
        float hedef, simdi;

        public void OnPointerEnter(PointerEventData e) { hedef = azami; }
        public void OnPointerExit(PointerEventData e) { hedef = 0f; }
        void OnDisable() { hedef = simdi = 0f; Boya(); }

        void Update()
        {
            if (parlama == null) return;
            simdi = Mathf.Lerp(simdi, hedef, 1f - Mathf.Exp(-hiz * Time.unscaledDeltaTime));
            Boya();
        }

        void Boya() { if (parlama != null) { var c = parlama.color; c.a = simdi; parlama.color = c; } }
    }
}
