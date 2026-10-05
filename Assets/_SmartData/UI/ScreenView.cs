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
            // CanvasGroup se postavlja PRE aktivacije, da Selectable-i u OnEnable vide ispravno stanje.
            CanvasGroup g = Group;
            if (g != null)
            {
                g.alpha = visible ? alpha : 0f;
                g.interactable = visible;
                g.blocksRaycasts = visible;
            }
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
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
