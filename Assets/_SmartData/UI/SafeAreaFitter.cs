using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Prilagođava SafeAreaRoot bezbednoj zoni uređaja (iPad, telefoni). Opciono.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        public bool apply = true;

        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void Update()
        {
            if (Application.isPlaying) Refresh(false);
        }

        public void Refresh(bool force)
        {
            var rt = (RectTransform)transform;
            if (!apply || !Application.isPlaying)
            {
                UiStyle.Anchors(rt, Vector2.zero, Vector2.one);
                return;
            }

            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafeArea && screen == lastScreen) return;
            lastSafeArea = safe;
            lastScreen = screen;
            if (screen.x <= 0 || screen.y <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= screen.x; min.y /= screen.y;
            max.x /= screen.x; max.y /= screen.y;
            UiStyle.Anchors(rt, min, max);
        }
    }
}
