using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Osnova svakog ekrana. Vidljivost isključivo preko CanvasGroup + SetActive.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ScreenView : MonoBehaviour
    {
        public CanvasGroup group;

        private CanvasGroup Group
        {
            get
            {
                if (group == null) group = GetComponent<CanvasGroup>();
                return group;
            }
        }

        public void SetVisible(bool visible, float alpha = 1f)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            CanvasGroup g = Group;
            if (g == null) return;
            g.alpha = visible ? alpha : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
        }

        public void SetAlpha(float alpha)
        {
            CanvasGroup g = Group;
            if (g != null) g.alpha = alpha;
        }

        public abstract void ApplyStyle(FindFakeApp app, float fontScale);
        public abstract void ApplyTexts(TextSet texts);
    }
}
