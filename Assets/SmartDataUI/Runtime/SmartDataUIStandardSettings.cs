// Settings for SmartDataUIStandard (put one in a Resources folder, named SmartDataUIStandardSettings).
using UnityEngine;

namespace SmartData.UIStandard
{
    [CreateAssetMenu(menuName = "SmartData/UI Standard Settings", fileName = "SmartDataUIStandardSettings")]
    public class SmartDataUIStandardSettings : ScriptableObject
    {
        [Tooltip("Master switch.")] public bool enabled = true;
        [Header("Reference frame (Memory): sizes below are in this frame, short side")]
        public float referenceShortSide = 1200f;
        [Header("Buttons")]
        public bool standardizeButtons = true;
        public Vector2 buttonSize = new Vector2(360f, 88f);
        public float buttonFontSize = 32f;
        public float buttonGap = 24f;
        [Header("Texts")]
        public bool standardizeTexts = true;
        public float titleFontSize = 92f;
        public float subtitleFontSize = 56f;
        public float bodyFontSize = 34f;
        [Tooltip("Texts at or above this size (reference units) count as title.")] public float titleFrom = 70f;
        [Tooltip("Texts at or above this size count as subtitle.")] public float subtitleFrom = 46f;
        [Tooltip("Auto-fit may shrink a text down to this share of its role size.")] [Range(0.2f, 1f)] public float minShrink = 0.45f;
        [Header("Font (empty = Resources/SmartDataUI_Font)")]
        public bool uniformFont = true;
        public Font font;
        [Header("Contrast on light designs")]
        public bool fixContrast = true;
        [Tooltip("Empty (alpha 0) = the most common dark text colour of the scene.")] public Color darkTextColor = new Color(0, 0, 0, 0);
        [Header("Backgrounds")]
        public bool coverBackgrounds = true;
        [Header("Skip objects whose name contains (case-insensitive)")]
        public string[] excludeNames = { "Admin", "Debug", "Numbers" };
    }

}
