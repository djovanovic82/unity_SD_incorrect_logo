using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>
    /// Jedan display profil. Scena je ista za sve profile; profil menja samo
    /// CanvasScaler, raspored zona i opcione override vrednosti.
    /// </summary>
    [Serializable]
    public class DisplayProfile
    {
        public string name = "Full HD Landscape";
        public Vector2Int resolution = new Vector2Int(1920, 1080);
        [Range(0f, 1f)] public float matchWidthOrHeight = 0.5f;
        [Tooltip("Množilac za sve veličine fonta u ovom profilu.")]
        [Range(0.5f, 2f)] public float fontScale = 1f;

        [Header("Pozadina (prazno = globalna)")]
        public Sprite backgroundOverride;

        [Header("Layout override (globalno → lokalno)")]
        public bool overrideLayout;
        public SplitMode split = SplitMode.Auto;
        [Tooltip("Koristi se kada je split = Auto. Auto = globalno LAYOUT podešavanje.")]
        public SplitDirection splitDirection = SplitDirection.Auto;
        [Tooltip("Ako je isključeno, rotacije se računaju automatski iz seating moda i podele.")]
        public bool overrideRotations;
        public ZoneRotation player1Rotation = ZoneRotation.Deg0;
        public ZoneRotation player2Rotation = ZoneRotation.Deg180;
        public HudPlacement hudPlacement = HudPlacement.Auto;
        [Range(0f, 0.2f)] public float centerGap = 0.02f;
        [Range(0.08f, 0.4f)] public float hudSize = 0.16f;

        public bool IsPortrait => resolution.y > resolution.x;
        public float Aspect => resolution.y <= 0 ? 1f : (float)resolution.x / resolution.y;
        public string Label => name + " (" + resolution.x + "x" + resolution.y + ")";

        public DisplayProfile() { }

        public DisplayProfile(string profileName, int width, int height, float textScale = 1f)
        {
            name = profileName;
            resolution = new Vector2Int(width, height);
            fontScale = textScale;
        }

        public static List<DisplayProfile> CreateDefaults()
        {
            return new List<DisplayProfile>
            {
                new DisplayProfile("Full HD Landscape", 1920, 1080),
                new DisplayProfile("Full HD Portrait", 1080, 1920),
                new DisplayProfile("Landscape 16:10", 1920, 1200),
                new DisplayProfile("iPad 4:3", 2048, 1536, 1.1f),
                new DisplayProfile("High-DPI 16:10", 2880, 1800, 1.5f),
                new DisplayProfile("Portrait 10:16", 1200, 1920)
            };
        }
    }

    /// <summary>Konačne vrednosti rasporeda posle primene globalnih i profilnih podešavanja.</summary>
    public struct ResolvedLayout
    {
        public SplitMode split;              // nikad Auto
        public ZoneRotation player1Rotation;
        public ZoneRotation player2Rotation;
        public bool player1First;            // true = P1 dole (TopBottom) / levo (LeftRight)
        public float centerGap;
        public float hudSize;
        public HudPlacement hudPlacement;    // može ostati Auto; rešava se po aspektu zone
    }
}
