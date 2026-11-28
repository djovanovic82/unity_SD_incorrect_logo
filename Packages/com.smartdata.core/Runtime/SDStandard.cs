// SmartData Core — vrednosti standarda v2.0 na jednom mestu.
// Mere su u referentnom okviru Memory igre (kraća strana 1200 px), isto kao SmartDataUIStandard.
// Na tabletu 1800x2880 se množe sa 1.5 (kraća strana 1800).
using UnityEngine;

namespace SmartData.Core
{
    public enum SDButtonType { Primary, Secondary, Key, Icon }

    public enum SDButtonState { Normal, Attract, Confirmed, Waiting, Locked, Disabled, Selected }

    public enum SDLayout { Single, SideBySide, FaceToFace }

    public static class SDStandard
    {
        public const string Version = "2.3.0";

        /// <summary>Referentna kraća strana (Memory).</summary>
        public const float ReferenceShortSide = 1200f;

        // dugmad (Memory: 360 x 88, font 32)
        public static readonly Vector2 PrimarySize = new Vector2(360f, 88f);
        public static readonly Vector2 SecondarySize = new Vector2(240f, 64f);
        public static readonly Vector2 KeySize = new Vector2(80f, 80f);
        public static readonly Vector2 IconSize = new Vector2(96f, 96f);
        public const float PrimaryFont = 32f;
        public const float SecondaryFont = 26f;
        public const float KeyFont = 36f;
        public const float ConfirmedBorder = 5f;

        // tekstovi (Memory)
        public const float TitleFont = 92f;
        public const float SubtitleFont = 56f;
        public const float BodyFont = 34f;

        // odbrojavanje
        public const float CountdownSquare = 130f;
        public const float CountdownGap = 26f;
        public const float CountdownFont = 80f;

        // animacije (sekunde)
        public const float FadeTime = 0.3f;
        public const float PressTime = 0.08f;
        public const float PressScale = 0.95f;
        public const float AttractScale = 1.04f;
        public const float AttractPeriod = 1.2f;
        public const float ShakeTime = 0.2f;
        public const float DoubleTapIgnore = 0.3f;

        /// <summary>Faktor iz referentnih jedinica u piksele ekrana.</summary>
        public static float PixelScale
        {
            get { return Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height)) / ReferenceShortSide; }
        }

        public static Vector2 SizeOf(SDButtonType t)
        {
            switch (t)
            {
                case SDButtonType.Secondary: return SecondarySize;
                case SDButtonType.Key: return KeySize;
                case SDButtonType.Icon: return IconSize;
                default: return PrimarySize;
            }
        }
    }
}
