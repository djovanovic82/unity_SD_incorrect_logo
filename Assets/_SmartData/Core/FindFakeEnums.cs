namespace SmartData.FindFake
{
    /// <summary>Jedino autoritativno stanje aplikacije.</summary>
    public enum AppState
    {
        Attract,
        Start,
        Countdown,
        Playing,
        RoundResult,
        MatchResult,
        Admin
    }

    /// <summary>Kako igrači stoje u odnosu na ekran.</summary>
    public enum SeatingMode
    {
        /// <summary>Horizontalni touch sto, igrači stoje jedan naspram drugog.</summary>
        TableOpposite,
        /// <summary>Uspravan ekran, oba igrača stoje ispred ekrana.</summary>
        ScreenSideBySide
    }

    public enum SplitMode
    {
        Auto,
        TopBottom,
        LeftRight
    }

    /// <summary>
    /// Smer podele kada je SplitMode = Auto, nezavisno od orijentacije ekrana.
    /// AlongLongSide: linija podele je paralelna dužoj strani – svaki igrač dobija dužu ivicu.
    /// AlongShortSide: linija podele je paralelna kraćoj strani – svaki igrač dobija kraću ivicu.
    /// </summary>
    public enum SplitDirection
    {
        Auto,
        AlongLongSide,
        AlongShortSide
    }

    public enum ZoneRotation
    {
        Deg0 = 0,
        Deg90 = 90,
        Deg180 = 180,
        Deg270 = 270
    }

    public enum HudPlacement
    {
        Auto,
        Top,
        Side
    }

    public enum ProfileSelectionMode
    {
        AutoByAspect,
        Forced
    }

    public enum BackgroundFitMode
    {
        Cover,
        Contain,
        Stretch
    }

    public enum StartMode
    {
        AnyPlayerStarts,
        BothPlayersReady
    }

    public enum BuildOrientation
    {
        Landscape,
        Portrait,
        AutoLandscape,
        AutoAll
    }
}
