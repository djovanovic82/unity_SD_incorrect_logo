using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SmartData.FindFake
{
    // Svaka klasa ispod je jedna foldout sekcija u root Inspectoru (SmartDataApp).

    [Serializable]
    public class ProjectBlock
    {
        public string productName = "Pronađi pogrešan logo";
        public string companyName = "SmartData";
        public string version = "1.1.0";
        [Min(1)] public int buildNumber = 1;
        public string bundleIdentifier = "com.smartdata.pronadjilogo";
    }

    [Serializable]
    public class DisplayBlock
    {
        public ProfileSelectionMode selection = ProfileSelectionMode.AutoByAspect;
        [Tooltip("Koristi se kada je selection = Forced.")]
        public int forcedProfileIndex;
        [Tooltip("Profil koji Inspector koristi za Edit Mode preview.")]
        public int editorPreviewProfileIndex;
        public bool useSafeArea = true;
        [Range(30, 120)] public int targetFrameRate = 60;
        public List<DisplayProfile> profiles = DisplayProfile.CreateDefaults();
    }

    [Serializable]
    public class ContentBlock
    {
        public LogoDatabase logoDatabase;
        public int activeLanguage;
        public List<TextSet> languages = new List<TextSet> { TextSet.Serbian(), TextSet.English() };
        [Tooltip("Na kraju runde prikaži opis greške iz LogoData.")]
        public bool showDifferenceDescription = true;
    }

    [Serializable]
    public class GameplayBlock
    {
        [Range(1, 10)] public int columns = 4;
        [Range(1, 10)] public int rows = 4;
        [Tooltip("Isti broj polja (kolone × redovi) se automatski preraspoređuje da polja budu što veća (npr. 4×4 → 8×2 u širokoj zoni).")]
        public bool autoGridShape = true;
        [Range(1, 15)] public int targetWins = 3;
        [Tooltip("Lažni logo kod igrača 2 nikad nije na istoj poziciji kao kod igrača 1.")]
        public bool distinctFakePositions = true;
        public StartMode startMode = StartMode.BothPlayersReady;
        public bool avoidLogoRepeats = true;
        public bool revealFakeOnRoundEnd = true;

        [Header("Kazna za pogrešan dodir")]
        [Min(0f)] public float wrongTapFreeze = 2f;
        public bool escalatingPenalty;
        [Min(0f)] public float penaltyStep = 0.5f;
        [Min(0f)] public float maxFreeze = 5f;

        [Header("Tajmer runde (0 = isključen)")]
        [Min(0f)] public float roundTimeLimit = 20f;
        public bool showRoundTimer = true;

        public int TileCount => Mathf.Clamp(columns * rows, 2, 100);
    }

    [Serializable]
    public class LayoutBlock
    {
        public SeatingMode seating = SeatingMode.TableOpposite;
        [Tooltip("Auto = koristi 'splitDirection'. TopBottom / LeftRight = fiksno, bez obzira na orijentaciju.")]
        public SplitMode split = SplitMode.Auto;
        [Tooltip("AlongLongSide: igrači stoje uz duže ivice ekrana. AlongShortSide: igrači stoje uz kraće ivice.\nAuto: sto u landscape-u = duže ivice, inače kraće ivice.")]
        public SplitDirection splitDirection = SplitDirection.Auto;
        public HudPlacement hudPlacement = HudPlacement.Auto;
        [Tooltip("Zamena: igrač 1 gore/desno umesto dole/levo.")]
        public bool swapPlayers;
        [Range(0f, 0.2f)] public float centerGap = 0.02f;
        [Range(0.08f, 0.4f)] public float hudSize = 0.16f;
        [Tooltip("Ako je odnos širine i visine zone veći od ovoga, HUD ide sa strane (Auto).")]
        [Min(1f)] public float sideHudAspectThreshold = 2.2f;
        [Min(0f)] public float halfPadding = 24f;
        [Min(0f)] public float boardPadding = 16f;
        [Min(0f)] public float tileSpacing = 18f;
        [Min(20f)] public float maxTileSize = 320f;
        public bool showDivider = true;
        [Min(0f)] public float dividerThickness = 6f;
    }

    [Serializable]
    public class VisualBlock
    {
        [Header("Pozadina")]
        public Color backgroundColor = new Color(0.07f, 0.08f, 0.11f, 1f);
        public Sprite backgroundSprite;
        public BackgroundFitMode backgroundFit = BackgroundFitMode.Cover;

        [Header("Igrači")]
        public Color player1Accent = new Color(0.16f, 0.62f, 1f, 1f);
        public Color player2Accent = new Color(1f, 0.42f, 0.2f, 1f);

        [Header("Polja")]
        public Sprite tileSprite;
        public Color tileColor = Color.white;
        public Color tileFrameColor = new Color(1f, 1f, 1f, 0.15f);
        [Min(0f)] public float tileFrameThickness = 6f;
        [Range(0f, 0.4f)] public float tileIconPadding = 0.1f;
        public Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        public Color wrongColor = new Color(0.95f, 0.2f, 0.2f, 1f);
        public Color revealColor = new Color(1f, 0.8f, 0.15f, 1f);
        public Color lockOverlayColor = new Color(0.8f, 0.05f, 0.05f, 0.45f);
        public Color dividerColor = new Color(1f, 1f, 1f, 0.25f);

        [Header("Dugmad i tekst")]
        public Sprite buttonSprite;
        public Color buttonColor = new Color(0.16f, 0.62f, 1f, 1f);
        public Color buttonTextColor = Color.white;
        public Color textColor = Color.white;
        public Color secondaryTextColor = new Color(1f, 1f, 1f, 0.7f);
        public Color hudColor = new Color(0f, 0f, 0f, 0.35f);
        public Color adminPanelColor = new Color(0.1f, 0.11f, 0.14f, 0.97f);
        public Sprite circleSprite;
    }

    [Serializable]
    public class TypographyBlock
    {
        [Tooltip("Prazno = podrazumevani TMP font.")]
        public TMP_FontAsset font;
        [Min(8f)] public float titleSize = 84f;
        [Min(8f)] public float subtitleSize = 40f;
        [Min(8f)] public float nameSize = 30f;
        [Min(8f)] public float statusSize = 44f;
        [Min(8f)] public float scoreSize = 56f;
        [Min(8f)] public float buttonSize = 40f;
        [Min(8f)] public float lockSize = 48f;
        [Min(8f)] public float resultSize = 110f;
        [Min(8f)] public float adminSize = 32f;
        [Range(0.2f, 1f)] public float autoSizeMinRatio = 0.45f;
    }

    [Serializable]
    public class AnimationBlock
    {
        [Min(0f)] public float screenFade = 0.25f;
        [Range(0.5f, 1f)] public float tapPunchScale = 0.9f;
        [Min(0.01f)] public float tapPunchDuration = 0.12f;
        [Range(1f, 1.5f)] public float revealPulseScale = 1.12f;
        [Min(0.05f)] public float revealPulseDuration = 0.6f;
        public bool attractPulse = true;
        [Min(0f)] public float attractPulseSpeed = 2f;
        [Range(0f, 0.3f)] public float attractPulseAmount = 0.06f;
    }

    [Serializable]
    public class TimingBlock
    {
        [Range(1, 10)] public int countdownFrom = 3;
        [Min(0.1f)] public float countdownStep = 0.7f;
        [Min(0f)] public float goMessageDuration = 0.6f;
        [Min(0f)] public float wrongFlashDuration = 0.35f;
        [Min(0f)] public float roundResultDuration = 2.5f;
        [Min(0f)] public float matchResultDuration = 8f;
        public bool autoReturnToAttract = true;
        [Tooltip("Bez dodira ovoliko sekundi → povratak na Attract (0 = isključeno).")]
        [Min(0f)] public float idleTimeout = 45f;
    }

    [Serializable]
    public class InputBlock
    {
        [Min(0f)] public float inputLockAfterStateChange = 0.25f;
        [Min(0f)] public float touchMouseSuppression = 0.2f;
        [Tooltip("Minimalni razmak između dva prihvaćena dodira istog igrača (anti-spam).")]
        [Min(0f)] public float tapDebounce = 0.08f;
        [Min(0f)] public float buttonDebounce = 0.3f;
        public bool allowMouse = true;
        public bool multiTouch = true;
    }

    [Serializable]
    public class SoundBlock
    {
        public bool enabled = true;
        [Range(0f, 1f)] public float masterVolume = 1f;
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.4f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        public AudioClip uiClick;
        public AudioClip countdownTick;
        public AudioClip go;
        public AudioClip correctTap;
        public AudioClip wrongTap;
        public AudioClip roundWin;
        public AudioClip matchWin;
    }

    [Serializable]
    public class DataBlock
    {
        public bool recordMatches = true;
        public string folderName = "PronadjiLogo";
        [Tooltip("Separator za CSV (';' radi sa srpskim Excel podešavanjima).")]
        public string csvSeparator = ";";
    }

    [Serializable]
    public class AdminBlock
    {
        public bool enableHiddenAdmin = true;
        [Range(2, 10)] public int hiddenTapCount = 5;
        [Min(0.5f)] public float hiddenTapWindow = 3f;
        [Min(40f)] public float hotspotSize = 140f;
        public bool allowQuit = true;
        [Min(0.5f)] public float resetConfirmWindow = 3f;
    }

    [Serializable]
    public class KioskBlock
    {
        [Tooltip("Produkcijski kiosk režim (skriva kursor u buildu).")]
        public bool kioskMode;
        public bool hideCursorInKiosk = true;
        public bool preventSleep = true;
        public bool runInBackground = true;
    }

    [Serializable]
    public class BuildBlock
    {
        public string buildFileName = "PronadjiPogresanLogo";
        public string outputFolder = "Builds";
        public BuildOrientation androidOrientation = BuildOrientation.AutoAll;
        public BuildOrientation iPadOrientation = BuildOrientation.AutoAll;
        public bool windowsFullscreen = true;
        public bool developmentBuild;
        public bool backupSceneBeforeBuild = true;
    }

    [Serializable]
    public class DebugBlock
    {
        public bool showDebugOverlay;
        public bool showTouches;
        public bool logStateChanges = true;
    }
}
