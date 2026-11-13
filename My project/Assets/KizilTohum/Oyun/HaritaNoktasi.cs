using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KizilTohum
{
    /// <summary>Harita noktasının fare olayları: üzerine gelme, çıkma, tıklama.</summary>
    public class HaritaNoktasi : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Action Girdi, Cikti, Tiklandi;

        public void OnPointerEnter(PointerEventData e) { if (Girdi != null) Girdi(); }
        public void OnPointerExit(PointerEventData e) { if (Cikti != null) Cikti(); }
        public void OnPointerClick(PointerEventData e) { if (Tiklandi != null) Tiklandi(); }
    }
}
