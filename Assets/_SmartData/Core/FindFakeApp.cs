using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    [Serializable]
    public class SceneRefs
    {
        public Camera mainCamera;
        public Canvas canvas;
        public CanvasScaler scaler;
        public Image backgroundColor;
        public Image backgroundImage;
        public SafeAreaFitter safeArea;
        public AttractView attract;
        public StartView start;
        public GameView game;
        public ResultView result;
        public AdminView admin;
        public OverlayView overlay;
        public SoundPlayer sound;
    }

    /// <summary>
    /// SmartDataApp za "Pronađi pogrešan logo". Jedina ulazna tačka konfiguracije,
    /// jedini vlasnik stanja (AppState) i jedini put za full reset.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public partial class FindFakeApp : MonoBehaviour
    {
        public static FindFakeApp Instance { get; private set; }

        public ProjectBlock project = new ProjectBlock();
        public DisplayBlock display = new DisplayBlock();
        public ContentBlock content = new ContentBlock();
        public GameplayBlock gameplay = new GameplayBlock();
        public LayoutBlock layout = new LayoutBlock();
        public VisualBlock visuals = new VisualBlock();
        public TypographyBlock typography = new TypographyBlock();
        public AnimationBlock animations = new AnimationBlock();
        public InputBlock input = new InputBlock();
        public TimingBlock timing = new TimingBlock();
        public SoundBlock sound = new SoundBlock();
        public DataBlock data = new DataBlock();
        public AdminBlock admin = new AdminBlock();
        public KioskBlock kiosk = new KioskBlock();
        public BuildBlock build = new BuildBlock();
        public DebugBlock debug = new DebugBlock();
        public SceneRefs refs = new SceneRefs();

        // ---------- runtime stanje (sve se vraća u ResetMatchState) ----------
        private readonly InputGuard guard = new InputGuard();
        private MatchDataStore store;
        private int flowToken;
        private int score1, score2, round;
        private int fake1 = -1, fake2 = -1;
        private LogoData currentLogo, lastLogo;
        private readonly List<LogoData> deck = new List<LogoData>();
        private bool ready1, ready2;
        private int roundMisses1, roundMisses2, matchMisses1, matchMisses2;
        private int roundWinner;
        private float roundStartTime;
        private float lastInteraction;
        private int lastTimerSecond = -1;
        private MatchRecord currentMatch;
        private readonly List<float> hiddenTaps = new List<float>();
        private readonly List<Vector2> pointerDowns = new List<Vector2>();
        private float resetArmedUntil = -1f;
        private string adminMessage = "";
        private int activeProfileIndex;
        private Vector2Int lastScreenSize;
        private Vector2 lastCanvasSize;
        private float smoothedFps = 60f;
        private static TextSet fallbackTexts;

        public AppState State { get; private set; } = AppState.Attract;
        public InputGuard Guard => guard;

        public TextSet Texts
        {
            get
            {
                if (content.languages != null && content.languages.Count > 0)
                    return content.languages[Mathf.Clamp(content.activeLanguage, 0, content.languages.Count - 1)];
                if (fallbackTexts == null) fallbackTexts = TextSet.Serbian();
                return fallbackTexts;
            }
        }

        public DisplayProfile ActiveProfile
        {
            get
            {
                if (display.profiles == null || display.profiles.Count == 0) return new DisplayProfile();
                return display.profiles[Mathf.Clamp(activeProfileIndex, 0, display.profiles.Count - 1)];
            }
        }

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[FindFake] Pronađen duplikat FindFakeApp – uklanjam ga.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            store = new MatchDataStore(data.folderName);
            InputGuard.ConfigurePlatform(input);
            string module = EventSystemGuard.Ensure();
            if (module.StartsWith("GREŠKA")) Debug.LogError("[FindFake] " + module);
            else if (debug.logInput) Debug.Log("[FindFake] UI input modul: " + module);
            KioskController.Apply(kiosk, display);
            BindViews();

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            ApplyAll(ResolveProfileIndex());
            EnterState(AppState.Attract, true);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BindViews()
        {
            if (refs.attract != null) refs.attract.Bind(OnAttractTap);
            if (refs.start != null) refs.start.Bind(OnReadyPressed);
            if (refs.game != null)
            {
                if (refs.game.player1.board != null) refs.game.player1.board.Bind(1, OnTileDown);
                if (refs.game.player2.board != null) refs.game.player2.board.Bind(2, OnTileDown);
            }
            if (refs.result != null) refs.result.Bind(OnPlayAgain);
            if (refs.admin != null)
                refs.admin.Bind(OnAdminResume, OnAdminNewGame, OnAdminLanguage, OnAdminExport, OnAdminReset, OnAdminQuit);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (guard.Tick()) lastInteraction = now;

            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen != lastScreenSize)
            {
                lastScreenSize = screen;
                ApplyAll(ResolveProfileIndex());
            }
            else if (refs.canvas != null)
            {
                // CanvasScaler primenjuje skaliranje frejm kasnije – tada ponovo poravnaj raspored i pozadinu.
                Vector2 canvasSize = ((RectTransform)refs.canvas.transform).rect.size;
                if ((canvasSize - lastCanvasSize).sqrMagnitude > 0.5f)
                {
                    lastCanvasSize = canvasSize;
                    RefreshLayout();
                }
            }

            switch (State)
            {
                case AppState.Attract:
                    if (refs.attract != null) refs.attract.Tick(animations);
                    break;
                case AppState.Playing:
                    TickRoundTimer(now);
                    break;
                case AppState.Admin:
                    if (resetArmedUntil > 0f && now > resetArmedUntil)
                    {
                        resetArmedUntil = -1f;
                        RefreshAdmin();
                    }
                    break;
            }

            TickHiddenHotspot(now);
            TickIdle(now);
            TickDebug();
        }

        // =====================================================================
        // Primena konfiguracije (radi i u Edit Mode-u)
        // =====================================================================

        public int ResolveProfileIndex()
        {
            if (!Application.isPlaying) return display.editorPreviewProfileIndex;
            if (display.selection == ProfileSelectionMode.Forced) return display.forcedProfileIndex;
            return Mathf.Max(0, LayoutResolver.SelectProfileIndex(display.profiles, new Vector2(Screen.width, Screen.height)));
        }

        /// <summary>Primenjuje celu konfiguraciju (profil, raspored, boje, fontove, tekstove). Ne kreira objekte.</summary>
        public void ApplyAll(int profileIndex)
        {
            int count = display.profiles != null ? display.profiles.Count : 0;
            activeProfileIndex = count > 0 ? Mathf.Clamp(profileIndex, 0, count - 1) : 0;
            DisplayProfile profile = ActiveProfile;

            if (refs.scaler != null)
            {
                refs.scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                refs.scaler.referenceResolution = new Vector2(Mathf.Max(1, profile.resolution.x), Mathf.Max(1, profile.resolution.y));
                refs.scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                refs.scaler.matchWidthOrHeight = profile.matchWidthOrHeight;
            }
            if (refs.safeArea != null)
            {
                refs.safeArea.apply = display.useSafeArea;
                refs.safeArea.Refresh(true);
            }
            Canvas.ForceUpdateCanvases();

            ApplyLayout(profile);
            ApplyVisuals(profile);
            ApplyTexts();
            if (refs.sound != null) refs.sound.Configure(sound);
        }

        /// <summary>Ponovo računa raspored i pozadinu bez diranja stila i teksta.</summary>
        public void RefreshLayout()
        {
            DisplayProfile profile = ActiveProfile;
            ApplyLayout(profile);
            if (refs.backgroundImage != null) BackgroundFitter.Fit(refs.backgroundImage, visuals.backgroundFit);
        }

        private void ApplyLayout(DisplayProfile profile)
        {
            ResolvedLayout r = LayoutResolver.Resolve(profile, layout);
            if (refs.canvas != null)
            {
                foreach (PlayerHalf half in refs.canvas.GetComponentsInChildren<PlayerHalf>(true))
                    LayoutResolver.ConfigureHalf(half, r, layout);
            }
            if (refs.game != null) refs.game.ApplyLayout(r, layout);
        }

        private void ApplyVisuals(DisplayProfile profile)
        {
            float fs = profile.fontScale;
            if (refs.mainCamera != null)
            {
                refs.mainCamera.clearFlags = CameraClearFlags.SolidColor;
                refs.mainCamera.backgroundColor = visuals.backgroundColor;
            }
            if (refs.backgroundColor != null)
            {
                refs.backgroundColor.color = visuals.backgroundColor;
                refs.backgroundColor.raycastTarget = false;
            }
            if (refs.backgroundImage != null)
            {
                Sprite bg = profile.backgroundOverride != null ? profile.backgroundOverride : visuals.backgroundSprite;
                refs.backgroundImage.sprite = bg;
                refs.backgroundImage.color = Color.white;
                refs.backgroundImage.raycastTarget = false;
                refs.backgroundImage.enabled = bg != null;
                BackgroundFitter.Fit(refs.backgroundImage, visuals.backgroundFit);
            }

            foreach (ScreenView v in AllViews())
            {
                if (v != null) v.ApplyStyle(this, fs);
            }
            if (refs.overlay != null) refs.overlay.ApplyStyle(this, fs);
        }

        private void ApplyTexts()
        {
            TextSet t = Texts;
            foreach (ScreenView v in AllViews())
            {
                if (v != null) v.ApplyTexts(t);
            }
        }

        private IEnumerable<ScreenView> AllViews()
        {
            yield return refs.attract;
            yield return refs.start;
            yield return refs.game;
            yield return refs.result;
            yield return refs.admin;
        }

        private ScreenView ViewFor(AppState state)
        {
            switch (state)
            {
                case AppState.Attract: return refs.attract;
                case AppState.Start: return refs.start;
                case AppState.Countdown:
                case AppState.Playing:
                case AppState.RoundResult: return refs.game;
                case AppState.MatchResult: return refs.result;
                case AppState.Admin: return refs.admin;
                default: return null;
            }
        }

        // =====================================================================
        // State machine
        // =====================================================================

        private void EnterState(AppState next, bool instant = false)
        {
            AppState previous = State;
            State = next;
            flowToken++;
            StopAllCoroutines();
            guard.LockFor(input.inputLockAfterStateChange);
            if (debug.logStateChanges) Debug.Log("[FindFake] " + previous + " → " + next);

            ShowView(ViewFor(next), ViewFor(previous), instant);

            switch (next)
            {
                case AppState.Attract: OnEnterAttract(); break;
                case AppState.Start: OnEnterStart(); break;
                case AppState.Countdown: StartCoroutine(CountdownRoutine(flowToken)); break;
                case AppState.Playing: OnEnterPlaying(); break;
                case AppState.RoundResult: OnEnterRoundResult(); break;
                case AppState.MatchResult: OnEnterMatchResult(); break;
                case AppState.Admin: OnEnterAdmin(); break;
            }
        }

        private void ShowView(ScreenView target, ScreenView previous, bool instant)
        {
            bool fade = !instant && animations.screenFade > 0f && target != previous && Application.isPlaying;
            foreach (ScreenView v in AllViews())
            {
                if (v == null) continue;
                if (v == target) v.SetVisible(true, fade ? 0f : 1f);
                else v.SetVisible(false);
            }
            if (fade && target != null) StartCoroutine(FadeIn(target, flowToken));
        }

        private IEnumerator FadeIn(ScreenView view, int token)
        {
            float t = 0f;
            while (t < animations.screenFade)
            {
                if (token != flowToken) yield break;
                t += Time.unscaledDeltaTime;
                view.SetAlpha(Mathf.Clamp01(t / animations.screenFade));
                yield return null;
            }
            view.SetAlpha(1f);
        }

        private static WaitForSecondsRealtime Wait(float seconds)
        {
            return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
        }

        // ---------- Attract ----------

        private void OnEnterAttract()
        {
            ResetMatchState();
        }

        private void OnAttractTap()
        {
            if (State != AppState.Attract) return;
            lastInteraction = Time.unscaledTime;
            PlaySound(sound.uiClick);
            EnterState(AppState.Start);
        }

        // ---------- Start ----------

        private void OnEnterStart()
        {
            ready1 = false;
            ready2 = false;
            if (refs.start != null) refs.start.Refresh(Texts, gameplay.startMode, false, false);
        }

        private void OnReadyPressed(int player)
        {
            if (State != AppState.Start) return;
            lastInteraction = Time.unscaledTime;
            PlaySound(sound.uiClick);

            bool editorSolo = Application.isEditor && debug.editorSinglePlayerStart;
            if (gameplay.startMode == StartMode.AnyPlayerStarts || editorSolo)
            {
                if (editorSolo && gameplay.startMode == StartMode.BothPlayersReady)
                    Debug.Log("[FindFake] Editor: jedan pritisak pokreće meč (DEBUG → editorSinglePlayerStart). U buildu su potrebna oba igrača.");
                BeginMatch();
                return;
            }

            if (player == 1) ready1 = true;
            else ready2 = true;
            if (refs.start != null) refs.start.Refresh(Texts, gameplay.startMode, ready1, ready2);
            if (ready1 && ready2) BeginMatch();
        }

        private void BeginMatch()
        {
            ResetMatchState();
            lastInteraction = Time.unscaledTime;
            currentMatch = new MatchRecord
            {
                matchId = Guid.NewGuid().ToString("N").Substring(0, 12),
                startedAt = MatchDataStore.Now(),
                language = Texts.languageCode,
                displayProfile = ActiveProfile.Label,
                targetWins = gameplay.targetWins
            };
            EnterState(AppState.Countdown);
        }

        // ---------- Countdown ----------

        private IEnumerator CountdownRoutine(int token)
        {
            round++;
            ForEachBoard(b =>
            {
                b.ResetBoard();
                b.ShowBlank();
                b.SetLocked(true);
            });
            UpdateScores();
            SetStatusBoth(UiStyle.Format(Texts.roundLabel, round));
            yield return Wait(timing.countdownStep);

            for (int i = timing.countdownFrom; i >= 1; i--)
            {
                if (token != flowToken) yield break;
                SetStatusBoth(i.ToString());
                PlaySound(sound.countdownTick);
                yield return Wait(timing.countdownStep);
            }
            if (token != flowToken) yield break;

            if (!SetupRound())
            {
                Debug.LogError("[FindFake] Nema ispravnih logoa u bazi (oba sprite-a moraju biti dodeljena). Pokreni Validate.");
                EnterState(AppState.Attract);
                yield break;
            }
            SetStatusBoth(Texts.go);
            PlaySound(sound.go);
            EnterState(AppState.Playing);
        }

        private bool SetupRound()
        {
            LogoData logo = DrawLogo();
            if (logo == null) return false;
            currentLogo = logo;
            lastLogo = logo;

            int count = gameplay.TileCount;
            fake1 = UnityEngine.Random.Range(0, count);
            fake2 = gameplay.distinctFakePositions && count > 1
                ? (fake1 + UnityEngine.Random.Range(1, count)) % count
                : UnityEngine.Random.Range(0, count);

            roundMisses1 = 0;
            roundMisses2 = 0;
            roundWinner = 0;
            PlayerBoard b1 = Board(1);
            PlayerBoard b2 = Board(2);
            if (b1 != null) b1.ShowRound(logo, fake1);
            if (b2 != null) b2.ShowRound(logo, fake2);
            return true;
        }

        private LogoData DrawLogo()
        {
            if (content.logoDatabase == null) return null;
            List<LogoData> playable = content.logoDatabase.GetPlayable();
            if (playable.Count == 0) return null;

            if (!gameplay.avoidLogoRepeats) return playable[UnityEngine.Random.Range(0, playable.Count)];

            deck.RemoveAll(l => l == null || !playable.Contains(l));
            if (deck.Count == 0)
            {
                deck.AddRange(playable);
                for (int i = deck.Count - 1; i > 0; i--)
                {
                    int r = UnityEngine.Random.Range(0, i + 1);
                    LogoData tmp = deck[i];
                    deck[i] = deck[r];
                    deck[r] = tmp;
                }
                if (deck.Count > 1 && deck[0] == lastLogo)
                {
                    int swap = UnityEngine.Random.Range(1, deck.Count);
                    LogoData tmp = deck[0];
                    deck[0] = deck[swap];
                    deck[swap] = tmp;
                }
            }
            LogoData next = deck[0];
            deck.RemoveAt(0);
            return next;
        }

        // ---------- Playing ----------

        private void OnEnterPlaying()
        {
            roundStartTime = Time.unscaledTime;
            lastTimerSecond = -1;
            ForEachBoard(b => b.SetLocked(false));
            StartCoroutine(ClearGoMessage(flowToken));
        }

        private IEnumerator ClearGoMessage(int token)
        {
            yield return Wait(timing.goMessageDuration);
            if (token != flowToken || State != AppState.Playing) yield break;
            if (gameplay.roundTimeLimit <= 0f || !gameplay.showRoundTimer) SetStatusBoth("");
        }

        private void TickRoundTimer(float now)
        {
            if (gameplay.roundTimeLimit <= 0f) return;
            float elapsed = now - roundStartTime;
            float remaining = gameplay.roundTimeLimit - elapsed;
            if (remaining <= 0f)
            {
                EndRound(0);
                return;
            }
            if (!gameplay.showRoundTimer || elapsed < timing.goMessageDuration) return;
            int sec = Mathf.CeilToInt(remaining);
            if (sec == lastTimerSecond) return;
            lastTimerSecond = sec;
            SetStatusBoth(UiStyle.Format(Texts.timeLeftFormat, sec));
        }

        private void OnTileDown(PlayerBoard board, LogoTile tile, PointerEventData eventData)
        {
            if (State != AppState.Playing || board == null || tile == null) return;
            int player = board.PlayerIndex;
            if (!guard.AcceptGameTap(eventData, player, input))
            {
                if (debug.logInput) Debug.Log("[FindFake] Dodir igrača " + player + " ODBIJEN: " + guard.LastRejectReason);
                return;
            }
            if (debug.logInput) Debug.Log("[FindFake] Dodir igrača " + player + " na polje " + tile.index);
            lastInteraction = Time.unscaledTime;

            tile.Punch(animations.tapPunchScale, animations.tapPunchDuration);
            int fake = player == 1 ? fake1 : fake2;
            if (tile.index == fake)
            {
                PlaySound(sound.correctTap);
                EndRound(player);
                return;
            }

            int misses;
            if (player == 1)
            {
                roundMisses1++;
                matchMisses1++;
                misses = roundMisses1;
            }
            else
            {
                roundMisses2++;
                matchMisses2++;
                misses = roundMisses2;
            }
            PlaySound(sound.wrongTap);
            board.FlashWrong(tile.index, visuals, timing.wrongFlashDuration);
            board.Freeze(FreezeDuration(misses), Texts.frozen);
        }

        private float FreezeDuration(int missesThisRound)
        {
            float d = gameplay.wrongTapFreeze;
            if (gameplay.escalatingPenalty)
                d = Mathf.Min(gameplay.maxFreeze, d + gameplay.penaltyStep * Mathf.Max(0, missesThisRound - 1));
            return d;
        }

        private void EndRound(int winner)
        {
            if (State != AppState.Playing) return;
            float reaction = Time.unscaledTime - roundStartTime;
            roundWinner = winner;
            if (winner == 1) score1++;
            else if (winner == 2) score2++;

            if (currentMatch != null)
            {
                currentMatch.rounds.Add(new RoundRecord
                {
                    round = round,
                    logoId = currentLogo != null ? currentLogo.SafeId : "",
                    winner = winner,
                    reactionSeconds = winner != 0 ? reaction : 0f,
                    player1WrongTaps = roundMisses1,
                    player2WrongTaps = roundMisses2,
                    endedAt = MatchDataStore.Now()
                });
            }
            EnterState(AppState.RoundResult);
        }

        // ---------- Round result ----------

        private void OnEnterRoundResult()
        {
            ForEachBoard(b =>
            {
                b.SetLocked(true);
                b.ClearFreeze();
            });
            ShowRoundOutcome(Texts);
            UpdateScores();
            if (roundWinner != 0) PlaySound(sound.roundWin);
            StartCoroutine(RoundResultRoutine(flowToken));
        }

        private void ShowRoundOutcome(TextSet t)
        {
            string detail = "";
            if (content.showDifferenceDescription && currentLogo != null && !string.IsNullOrEmpty(currentLogo.differenceDescription))
                detail = "\n<size=65%>" + t.differencePrefix + currentLogo.differenceDescription + "</size>";

            for (int p = 1; p <= 2; p++)
            {
                PlayerBoard b = Board(p);
                string status;
                if (roundWinner == 0)
                {
                    status = t.timeUp;
                    if (gameplay.revealFakeOnRoundEnd && b != null) b.RevealFake(visuals, animations);
                }
                else if (roundWinner == p)
                {
                    status = t.youFound;
                    if (b != null) b.MarkCorrect(visuals, animations);
                }
                else
                {
                    status = t.opponentFound;
                    if (gameplay.revealFakeOnRoundEnd && b != null) b.RevealFake(visuals, animations);
                }
                SetStatus(p, status + detail);
            }
        }

        private IEnumerator RoundResultRoutine(int token)
        {
            yield return Wait(timing.roundResultDuration);
            if (token != flowToken) yield break;
            int target = Mathf.Max(1, gameplay.targetWins);
            EnterState(score1 >= target || score2 >= target ? AppState.MatchResult : AppState.Countdown);
        }

        // ---------- Match result ----------

        private void OnEnterMatchResult()
        {
            int winner = score1 >= score2 ? 1 : 2;
            if (refs.result != null) refs.result.Show(winner, score1, score2, Texts);
            PlaySound(sound.matchWin);
            SaveMatch(winner);
            if (timing.autoReturnToAttract) StartCoroutine(AutoReturn(flowToken));
        }

        private void SaveMatch(int winner)
        {
            if (currentMatch == null) return;
            currentMatch.endedAt = MatchDataStore.Now();
            currentMatch.winner = winner;
            currentMatch.player1Score = score1;
            currentMatch.player2Score = score2;
            currentMatch.player1WrongTaps = matchMisses1;
            currentMatch.player2WrongTaps = matchMisses2;
            if (data.recordMatches && store != null) store.Add(currentMatch);
            currentMatch = null; // sprečava dvostruki upis
        }

        private IEnumerator AutoReturn(int token)
        {
            yield return Wait(timing.matchResultDuration);
            if (token == flowToken) EnterState(AppState.Attract);
        }

        private void OnPlayAgain()
        {
            if (State != AppState.MatchResult) return;
            lastInteraction = Time.unscaledTime;
            PlaySound(sound.uiClick);
            EnterState(AppState.Start);
        }

        // ---------- Admin ----------

        private void TickHiddenHotspot(float now)
        {
            if (!admin.enableHiddenAdmin || refs.overlay == null) return;
            // Admin gest radi samo van aktivne igre, da ne smeta poljima u uglu.
            if (State != AppState.Attract && State != AppState.Start && State != AppState.MatchResult) return;

            guard.CollectPointerDowns(pointerDowns, input);
            for (int i = 0; i < pointerDowns.Count; i++)
            {
                if (!refs.overlay.HotspotContains(pointerDowns[i])) continue;
                hiddenTaps.Add(now);
            }
            hiddenTaps.RemoveAll(t => now - t > admin.hiddenTapWindow);
            if (hiddenTaps.Count >= admin.hiddenTapCount)
            {
                hiddenTaps.Clear();
                EnterState(AppState.Admin);
            }
        }

        private void OnEnterAdmin()
        {
            adminMessage = "";
            resetArmedUntil = -1f;
            RefreshAdmin();
        }

        private void RefreshAdmin()
        {
            if (refs.admin == null) return;
            MatchStats stats = store != null ? store.GetStats() : new MatchStats();
            refs.admin.Refresh(Texts, stats, adminMessage, resetArmedUntil > 0f, admin.allowQuit, project.version);
        }

        private void OnAdminResume()
        {
            if (State == AppState.Admin) EnterState(AppState.Attract);
        }

        private void OnAdminNewGame()
        {
            if (State == AppState.Admin) EnterState(AppState.Start);
        }

        private void OnAdminLanguage()
        {
            if (State != AppState.Admin || content.languages == null || content.languages.Count == 0) return;
            content.activeLanguage = (content.activeLanguage + 1) % content.languages.Count;
            ApplyTexts();
            RefreshAdmin();
        }

        private void OnAdminExport()
        {
            if (State != AppState.Admin || store == null) return;
            string path = store.ExportCsv(data.csvSeparator);
            adminMessage = path != null ? UiStyle.Format(Texts.exportDone, path) : Texts.exportFailed;
            RefreshAdmin();
        }

        private void OnAdminReset()
        {
            if (State != AppState.Admin || store == null) return;
            float now = Time.unscaledTime;
            if (resetArmedUntil > 0f && now <= resetArmedUntil)
            {
                store.Clear();
                resetArmedUntil = -1f;
                adminMessage = Texts.resetDone;
            }
            else
            {
                resetArmedUntil = now + admin.resetConfirmWindow;
            }
            RefreshAdmin();
        }

        private void OnAdminQuit()
        {
            if (State != AppState.Admin || !admin.allowQuit) return;
            Debug.Log("[FindFake] Izlaz iz aplikacije.");
            Application.Quit();
        }

        // ---------- Idle / debug ----------

        private void TickIdle(float now)
        {
            if (timing.idleTimeout <= 0f || State == AppState.Attract || State == AppState.Admin) return;
            if (now - lastInteraction > timing.idleTimeout) EnterState(AppState.Attract);
        }

        private void TickDebug()
        {
            if (refs.overlay == null) return;
            refs.overlay.TickTouches(debug.showTouches, visuals.circleSprite);
            if (!debug.showDebugOverlay) return;
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smoothedFps = Mathf.Lerp(smoothedFps, 1f / dt, 0.1f);
            refs.overlay.SetDebug(State + " | " + ActiveProfile.Label + " | " + Screen.width + "x" + Screen.height
                                  + " | " + Mathf.RoundToInt(smoothedFps) + " FPS | touches " + InputGuard.TouchCount);
        }

        // =====================================================================
        // Full reset – NOVA IGRA mora biti identična svežem pokretanju
        // =====================================================================

        private void ResetMatchState()
        {
            score1 = 0;
            score2 = 0;
            round = 0;
            fake1 = -1;
            fake2 = -1;
            currentLogo = null;
            ready1 = false;
            ready2 = false;
            roundMisses1 = roundMisses2 = matchMisses1 = matchMisses2 = 0;
            roundWinner = 0;
            lastTimerSecond = -1;
            currentMatch = null;
            hiddenTaps.Clear();
            resetArmedUntil = -1f;
            guard.ResetPlayers();

            ForEachBoard(b =>
            {
                b.ResetBoard();
                b.ShowBlank();
            });
            SetStatusBoth("");
            UpdateScores();
            if (refs.attract != null) refs.attract.ResetAnimation();
            if (refs.start != null) refs.start.Refresh(Texts, gameplay.startMode, false, false);
        }

        // =====================================================================
        // Helperi
        // =====================================================================

        private PlayerBoard Board(int player)
        {
            if (refs.game == null) return null;
            return refs.game.Get(player).board;
        }

        private void ForEachBoard(Action<PlayerBoard> action)
        {
            PlayerBoard b1 = Board(1);
            PlayerBoard b2 = Board(2);
            if (b1 != null) action(b1);
            if (b2 != null) action(b2);
        }

        private void SetStatus(int player, string text)
        {
            if (refs.game != null) refs.game.SetStatus(player, text);
        }

        private void SetStatusBoth(string text)
        {
            SetStatus(1, text);
            SetStatus(2, text);
        }

        private void UpdateScores()
        {
            if (refs.game == null) return;
            refs.game.SetScore(1, score1, score2, Texts.scoreFormat);
            refs.game.SetScore(2, score2, score1, Texts.scoreFormat);
        }

        private void PlaySound(AudioClip clip)
        {
            if (refs.sound != null) refs.sound.Play(clip);
        }

        // =====================================================================
        // Edit Mode preview (poziva Custom Inspector; ne kreira i ne briše objekte)
        // =====================================================================

        public void PreviewState(AppState state)
        {
            ApplyAll(display.editorPreviewProfileIndex);
            ScreenView target = ViewFor(state);
            foreach (ScreenView v in AllViews())
            {
                if (v != null) v.SetVisible(v == target);
            }

            TextSet t = Texts;
            switch (state)
            {
                case AppState.Start:
                    if (refs.start != null) refs.start.Refresh(t, gameplay.startMode, true, false);
                    break;
                case AppState.Countdown:
                case AppState.Playing:
                case AppState.RoundResult:
                    PreviewGame(state, t);
                    break;
                case AppState.MatchResult:
                    if (refs.result != null) refs.result.Show(1, gameplay.targetWins, Mathf.Max(0, gameplay.targetWins - 2), t);
                    break;
                case AppState.Admin:
                    if (refs.admin != null)
                    {
                        var sample = new MatchStats { matches = 12, rounds = 41, player1Wins = 7, player2Wins = 5, averageReaction = 3.2f };
                        refs.admin.Refresh(t, sample, "", false, admin.allowQuit, project.version);
                    }
                    break;
            }
        }

        private void PreviewGame(AppState state, TextSet t)
        {
            LogoData logo = content.logoDatabase != null ? content.logoDatabase.FirstPlayable() : null;
            int count = gameplay.TileCount;
            int f1 = count / 3;
            int f2 = Mathf.Min(count - 1, count * 2 / 3);

            score1 = 1;
            score2 = 0;
            UpdateScores();
            score1 = 0;

            PlayerBoard b1 = Board(1);
            PlayerBoard b2 = Board(2);
            ForEachBoard(b => b.ResetBoard());

            if (state == AppState.Countdown)
            {
                ForEachBoard(b => b.ShowBlank());
                SetStatusBoth("3");
                return;
            }

            if (b1 != null) b1.ShowRound(logo, f1);
            if (b2 != null) b2.ShowRound(logo, f2);

            if (state == AppState.Playing)
            {
                SetStatusBoth(UiStyle.Format(t.timeLeftFormat, Mathf.CeilToInt(gameplay.roundTimeLimit > 0 ? gameplay.roundTimeLimit : 15)));
                return;
            }

            if (b1 != null) b1.MarkCorrect(visuals, animations);
            if (b2 != null && gameplay.revealFakeOnRoundEnd) b2.RevealFake(visuals, animations);
            string detail = content.showDifferenceDescription && logo != null && !string.IsNullOrEmpty(logo.differenceDescription)
                ? "\n<size=65%>" + t.differencePrefix + logo.differenceDescription + "</size>"
                : "";
            SetStatus(1, t.youFound + detail);
            SetStatus(2, t.opponentFound + detail);
        }
    }
}
