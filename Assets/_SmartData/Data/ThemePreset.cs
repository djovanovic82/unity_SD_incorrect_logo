using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SmartData.FindFake
{
    [Serializable]
    public class ThemeText
    {
        public string languageCode = "SR";
        public string title;
        public string subtitle;
    }

    /// <summary>
    /// Dizajn tema (brend): boje, pozadina, polja, font, naslovi i sopstvena baza logoa.
    /// Primena kopira vrednosti u VISUALS / CONTENT / LAYOUT root Inspectora (globalno → lokalno ostaje).
    /// </summary>
    [CreateAssetMenu(fileName = "NewTheme", menuName = "SmartData/Pronađi pogrešan logo/Theme Preset")]
    public class ThemePreset : ScriptableObject
    {
        public string themeName = "Tema";
        public LogoDatabase logoDatabase;
        public VisualBlock visuals = new VisualBlock();
        [Tooltip("Prazno = zadržava trenutni font.")]
        public TMP_FontAsset font;
        [Range(0.5f, 4f)] public float tileAspect = 1f;
        public List<ThemeText> texts = new List<ThemeText>();

        public ThemeText TextFor(string languageCode)
        {
            foreach (ThemeText t in texts)
                if (t != null && string.Equals(t.languageCode, languageCode, StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }
    }
}
