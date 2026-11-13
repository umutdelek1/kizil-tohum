using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KizilTohum
{
    /// <summary>
    /// Seçenek düğmesi hareketi: gecikmeyle belirir, üzerine gelince hafif büyür, basılınca küçülür.
    /// Tüm süre/ölçekler Tema'dan gelir; zaman unscaledDeltaTime ile akar.
    /// </summary>
    public class DugmeHareketi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float gecikme;

        CanvasGroup grup;
        Selectable sel;
        float t;
        float olcek = 1f;
        bool uzerinde, basili;

        void Awake()
        {
            grup = GetComponent<CanvasGroup>();
            sel = GetComponent<Selectable>();
            Uygula();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            bool etkin = sel == null || sel.IsInteractable();
            float hedef = !etkin ? 1f : basili ? Tema.SecenekBasiliOlcek : uzerinde ? Tema.SecenekHoverOlcek : 1f;
            olcek = Mathf.Lerp(olcek, hedef, 1f - Mathf.Exp(-Tema.SecenekOlcekHiz * dt));
            Uygula();
        }

        void Uygula()
        {
            float f = Mathf.Clamp01((t - gecikme) / Tema.SecenekGirisSure);
            f = 1f - (1f - f) * (1f - f); // ease-out
            if (grup != null) grup.alpha = f;
            transform.localScale = Vector3.one * (olcek * Mathf.Lerp(Tema.SecenekGirisOlcek, 1f, f));
        }

        public void OnPointerEnter(PointerEventData e) { uzerinde = true; }
        public void OnPointerExit(PointerEventData e) { uzerinde = false; basili = false; }
        public void OnPointerDown(PointerEventData e) { basili = true; }
        public void OnPointerUp(PointerEventData e) { basili = false; }
    }
}
