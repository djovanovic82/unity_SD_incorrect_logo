using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>
    /// Polovina ekrana jednog igrača. Spoljašnji RectTransform se sidri na polovinu,
    /// a "content" se rotira (0/90/180/270) i dimenzioniše tako da je za igrača
    /// uvek "uspravan". Isti mehanizam koriste svi ekrani.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class PlayerHalf : MonoBehaviour
    {
        [Range(1, 2)] public int player = 1;
        public RectTransform content;

        [SerializeField] private ZoneRotation rotation = ZoneRotation.Deg0;
        [SerializeField] private float padding = 24f;

        public ZoneRotation Rotation => rotation;
        public Vector2 ContentSize => content != null ? content.rect.size : Vector2.zero;

        public void Configure(Vector2 anchorMin, Vector2 anchorMax, ZoneRotation zoneRotation, float zonePadding)
        {
            var rt = (RectTransform)transform;
            UiStyle.Anchors(rt, anchorMin, anchorMax);
            if (rt.pivot != new Vector2(0.5f, 0.5f)) rt.pivot = new Vector2(0.5f, 0.5f);
            if (rt.localRotation != Quaternion.identity) rt.localRotation = Quaternion.identity;
            if (rt.localScale != Vector3.one) rt.localScale = Vector3.one;
            rotation = zoneRotation;
            padding = zonePadding;
            UpdateContent();
        }

        public void UpdateContent()
        {
            if (content == null) return;
            var rt = (RectTransform)transform;
            Vector2 size = rt.rect.size;
            bool swap = rotation == ZoneRotation.Deg90 || rotation == ZoneRotation.Deg270;
            float w = Mathf.Max(0f, (swap ? size.y : size.x) - padding * 2f);
            float h = Mathf.Max(0f, (swap ? size.x : size.y) - padding * 2f);

            var center = new Vector2(0.5f, 0.5f);
            if (content.anchorMin != center) content.anchorMin = center;
            if (content.anchorMax != center) content.anchorMax = center;
            if (content.pivot != center) content.pivot = center;
            if (content.anchoredPosition != Vector2.zero) content.anchoredPosition = Vector2.zero;
            var target = new Vector2(w, h);
            if ((content.sizeDelta - target).sqrMagnitude > 0.01f) content.sizeDelta = target;
            Quaternion rot = Quaternion.Euler(0f, 0f, (int)rotation);
            if (content.localRotation != rot) content.localRotation = rot;
            if (content.localScale != Vector3.one) content.localScale = Vector3.one;
        }

        private void OnEnable()
        {
            UpdateContent();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) UpdateContent();
        }
    }
}
