using UnityEngine;

namespace SmartData.FindFake
{
    public partial class FindFakeApp
    {
        private const string ThemePrefKey = "SmartData.FindFake.Theme";

        /// <summary>U buildu vraća temu izabranu u admin ekranu (preživljava restart). U Editoru važi Inspector.</summary>
        private void RestoreSavedTheme()
        {
            if (Application.isEditor || content.themes == null) return;
            int saved = PlayerPrefs.GetInt(ThemePrefKey, -1);
            if (saved >= 0 && saved < content.themes.Count && saved != content.activeTheme) ApplyTheme(saved);
        }

        public ThemePreset ActiveTheme
        {
            get
            {
                if (content.themes == null || content.activeTheme < 0 || content.activeTheme >= content.themes.Count) return null;
                return content.themes[content.activeTheme];
            }
        }

        /// <summary>
        /// Kopira temu u VISUALS / CONTENT / LAYOUT / TYPOGRAPHY i naslove u jezike.
        /// Ne kreira i ne briše objekte; posle primene poziva ApplyAll.
        /// </summary>
        public void ApplyTheme(int index)
        {
            if (content.themes == null || index < 0 || index >= content.themes.Count || content.themes[index] == null) return;
            ThemePreset theme = content.themes[index];
            content.activeTheme = index;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(theme.visuals), visuals);
            if (theme.logoDatabase != null) content.logoDatabase = theme.logoDatabase;
            if (theme.font != null) typography.font = theme.font;
            layout.tileAspect = theme.tileAspect;

            if (content.languages != null)
            {
                foreach (TextSet set in content.languages)
                {
                    ThemeText t = theme.TextFor(set.languageCode);
                    if (t == null) continue;
                    if (!string.IsNullOrEmpty(t.title)) set.title = t.title;
                    if (!string.IsNullOrEmpty(t.subtitle)) set.subtitle = t.subtitle;
                }
            }

            deck.Clear();
            lastLogo = null;
            ApplyAll(Application.isPlaying ? ResolveProfileIndex() : display.editorPreviewProfileIndex);
            if (debug.logStateChanges) Debug.Log("[FindFake] Tema: " + theme.themeName);
        }

        private string ThemeLabel()
        {
            if (content.themes == null || content.themes.Count == 0) return null;
            ThemePreset t = ActiveTheme;
            return t != null ? t.themeName : "-";
        }

        private void OnAdminTheme()
        {
            if (State != AppState.Admin || content.themes == null || content.themes.Count == 0) return;
            int next = (content.activeTheme + 1) % content.themes.Count;
            if (next < 0) next = 0;
            ApplyTheme(next);
            PlayerPrefs.SetInt(ThemePrefKey, next);
            PlayerPrefs.Save();
            RefreshAdmin();
        }
    }
}
