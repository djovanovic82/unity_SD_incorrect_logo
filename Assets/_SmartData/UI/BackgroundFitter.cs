using UnityEngine;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>Cover / Contain / Stretch za pozadinsku sliku – nikad proizvoljno razvlačenje.</summary>
    public static class BackgroundFitter
    {
        public static void Fit(Image image, BackgroundFitMode mode)
        {
            if (image == null) return;
            RectTransform rt = image.rectTransform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;

            if (image.sprite == null || mode == BackgroundFitMode.Stretch)
            {
                UiStyle.Anchors(rt, Vector2.zero, Vector2.one);
                return;
            }

            Vector2 ps = parent.rect.size;
            Rect sr = image.sprite.rect;
            if (ps.x <= 0f || ps.y <= 0f || sr.height <= 0f) return;

            float spriteAspect = sr.width / sr.height;
            float parentAspect = ps.x / ps.y;
            Vector2 size;
            if (mode == BackgroundFitMode.Cover)
                size = parentAspect > spriteAspect ? new Vector2(ps.x, ps.x / spriteAspect) : new Vector2(ps.y * spriteAspect, ps.y);
            else
                size = parentAspect > spriteAspect ? new Vector2(ps.y * spriteAspect, ps.y) : new Vector2(ps.x, ps.x / spriteAspect);

            var center = new Vector2(0.5f, 0.5f);
            rt.anchorMin = center;
            rt.anchorMax = center;
            rt.pivot = center;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            image.preserveAspect = false;
        }
    }
}
