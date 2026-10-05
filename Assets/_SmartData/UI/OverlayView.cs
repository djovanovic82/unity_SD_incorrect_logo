using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>Uvek iznad svega: debug tekst, vizualizacija dodira, skrivena admin zona.</summary>
    public class OverlayView : MonoBehaviour
    {
        public RectTransform hiddenHotspot;
        public TMP_Text debugText;
        public RectTransform touchLayer;

        private readonly List<Image> touchPool = new List<Image>();

        public void ApplyStyle(FindFakeApp app, float fontScale)
        {
            if (hiddenHotspot != null)
            {
                hiddenHotspot.anchorMin = new Vector2(0f, 1f);
                hiddenHotspot.anchorMax = new Vector2(0f, 1f);
                hiddenHotspot.pivot = new Vector2(0f, 1f);
                hiddenHotspot.anchoredPosition = Vector2.zero;
                hiddenHotspot.sizeDelta = new Vector2(app.admin.hotspotSize, app.admin.hotspotSize);
                var img = hiddenHotspot.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false; // ne blokira polja ispod; detekcija ide preko InputGuard-a
                    img.color = new Color(1f, 0f, 1f, app.debug.showDebugOverlay ? 0.25f : 0f);
                }
            }
            UiStyle.Text(debugText, app.typography, 22f, 1f, Color.yellow);
            if (debugText != null && debugText.gameObject.activeSelf != app.debug.showDebugOverlay)
                debugText.gameObject.SetActive(app.debug.showDebugOverlay);
        }

        public bool HotspotContains(Vector2 screenPoint)
        {
            return hiddenHotspot != null && RectTransformUtility.RectangleContainsScreenPoint(hiddenHotspot, screenPoint, null);
        }

        public void SetDebug(string text)
        {
            UiStyle.SetText(debugText, text);
        }

        public void TickTouches(bool show, Sprite circle)
        {
            if (touchLayer == null) return;
            int count = show ? InputGuard.TouchCount : 0;
            for (int i = 0; i < count; i++)
            {
                if (i >= touchPool.Count)
                {
                    var go = new GameObject("Touch_" + i, typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(touchLayer, false);
                    var img = go.GetComponent<Image>();
                    img.raycastTarget = false;
                    img.sprite = circle;
                    img.color = new Color(1f, 1f, 0f, 0.5f);
                    img.rectTransform.sizeDelta = new Vector2(90f, 90f);
                    touchPool.Add(img);
                }
                Image dot = touchPool[i];
                if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(touchLayer, InputGuard.TouchPosition(i), null, out local))
                    dot.rectTransform.anchoredPosition = local;
            }
            for (int i = count; i < touchPool.Count; i++)
            {
                if (touchPool[i].gameObject.activeSelf) touchPool[i].gameObject.SetActive(false);
            }
        }
    }
}
