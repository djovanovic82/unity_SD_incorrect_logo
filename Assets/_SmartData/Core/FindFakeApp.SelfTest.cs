using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartData.FindFake
{
    /// <summary>
    /// Automatski test celog toka igre u Play Mode-u:
    /// Attract → Start → odbrojavanje → igra (pogrešan dodir, zamrzavanje, pogodak) → rezultat runde → rezultat meča → Attract.
    /// Pored logike proverava i raycast: da li stvarni klik na centar dugmeta/polja stiže baš do njega.
    /// </summary>
    public partial class FindFakeApp
    {
        public bool SelfTestRunning { get; private set; }
        public string SelfTestReport { get; private set; } = "";

        public void RunSelfTest()
        {
            if (!Application.isPlaying || SelfTestRunning) return;
            var go = new GameObject("FindFake_SelfTest");
            var runner = go.AddComponent<FindFakeSelfTestRunner>();
            runner.StartCoroutine(SelfTestRoutine(go));
        }

        private IEnumerator SelfTestRoutine(GameObject host)
        {
            SelfTestRunning = true;
            var log = new StringBuilder();
            int fails = 0;

            void Check(bool ok, string message)
            {
                if (!ok) fails++;
                log.Append(ok ? "  OK    " : "  FAIL  ").Append(message).Append('\n');
            }

            // Snimi podešavanja i ubrzaj tajminge samo za test.
            TimingBlock savedTiming = JsonUtility.FromJson<TimingBlock>(JsonUtility.ToJson(timing));
            AnimationBlock savedAnim = JsonUtility.FromJson<AnimationBlock>(JsonUtility.ToJson(animations));
            InputBlock savedInput = JsonUtility.FromJson<InputBlock>(JsonUtility.ToJson(input));
            GameplayBlock savedGameplay = JsonUtility.FromJson<GameplayBlock>(JsonUtility.ToJson(gameplay));
            bool savedRecord = data.recordMatches;

            timing.countdownStep = 0.05f;
            timing.goMessageDuration = 0.05f;
            timing.roundResultDuration = 0.15f;
            timing.matchResultDuration = 0.3f;
            timing.autoReturnToAttract = true;
            timing.idleTimeout = 0f;
            animations.screenFade = 0.05f;
            input.inputLockAfterStateChange = 0.05f;
            gameplay.wrongTapFreeze = 0.4f;
            gameplay.escalatingPenalty = false;
            gameplay.roundTimeLimit = 0f;
            data.recordMatches = false;

            const float step = 0.15f;
            try
            {
                log.Append("[FindFake] FLOW SELF-TEST\n");

                // ---------- Okruženje ----------
                Check(EventSystem.current != null, "EventSystem.current postoji");
                if (EventSystem.current != null)
                {
                    BaseInputModule module = EventSystem.current.GetComponent<BaseInputModule>();
                    Check(module != null && module.enabled, "EventSystem ima aktivan input modul (" + (module != null ? module.GetType().Name : "nema") + ")");
                }
                Check(refs.attract != null && refs.start != null && refs.game != null && refs.result != null, "Reference ekrana su dodeljene");
                int playable = content.logoDatabase != null ? content.logoDatabase.GetPlayable().Count : 0;
                Check(playable > 0, "Logo baza ima ispravnih logoa: " + playable);
                if (refs.attract == null || refs.start == null || refs.game == null || refs.result == null || playable == 0) yield break;

                // ---------- Attract ----------
                EnterState(AppState.Attract);
                yield return new WaitForSecondsRealtime(step);
                Check(refs.attract.gameObject.activeInHierarchy, "Attract ekran je vidljiv");
                Check(Raycast(refs.attract.tapArea != null ? refs.attract.tapArea.gameObject : null, "Attract TapArea"), lastRaycast);
                if (refs.attract.tapArea != null) refs.attract.tapArea.onClick.Invoke();
                yield return WaitFor(AppState.Start, 2f);
                Check(State == AppState.Start, "Dodir na Attract vodi na Start (stanje: " + State + ")");
                if (State != AppState.Start) yield break;

                // ---------- Start ----------
                yield return new WaitForSecondsRealtime(step);
                for (int p = 1; p <= 2; p++)
                {
                    StartView.Half h = refs.start.Get(p);
                    Check(Raycast(h.button != null ? h.button.gameObject : null, "Start dugme igrača " + p), lastRaycast);
                }
                if (refs.start.player1.button != null) refs.start.player1.button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(step);
                if (State == AppState.Start)
                {
                    Check(ready1, "Igrač 1 je označen kao spreman, čeka se igrač 2");
                    if (refs.start.player2.button != null) refs.start.player2.button.onClick.Invoke();
                }
                yield return WaitFor(AppState.Playing, 3f);
                Check(State == AppState.Playing, "Start → odbrojavanje → igra (stanje: " + State + ")");
                if (State != AppState.Playing) yield break;

                // ---------- Tabla ----------
                PlayerBoard b1 = Board(1);
                PlayerBoard b2 = Board(2);
                Check(b1 != null && b2 != null, "Obe table postoje");
                if (b1 == null || b2 == null) yield break;
                Check(b1.tiles.Count >= gameplay.TileCount && b2.tiles.Count >= gameplay.TileCount, "Broj polja ≥ " + gameplay.TileCount);
                Check(fake1 != fake2 || !gameplay.distinctFakePositions, "Lažni logo je na različitim pozicijama (" + fake1 + " / " + fake2 + ")");
                LogoTile f1 = b1.Get(fake1);
                Check(f1 != null && currentLogo != null && f1.icon != null && f1.icon.sprite == currentLogo.fakeSprite, "Igrač 1: lažni sprite je na polju " + fake1);
                LogoTile other1 = b1.Get(fake1 == 0 ? 1 : 0);
                Check(other1 != null && other1.icon != null && other1.icon.sprite == currentLogo.correctSprite, "Igrač 1: ostala polja imaju originalni sprite");
                Check(Raycast(f1 != null ? f1.gameObject : null, "Polje igrača 1"), lastRaycast);
                Check(Raycast(b2.Get(0) != null ? b2.Get(0).gameObject : null, "Polje igrača 2"), lastRaycast);

                // ---------- Pogrešan dodir + zamrzavanje ----------
                yield return new WaitForSecondsRealtime(step);
                LogoTile wrong2 = b2.Get(fake2 == 0 ? 1 : 0);
                b2.HandleTileDown(wrong2, null);
                yield return null;
                Check(b2.IsFrozen, "Pogrešan dodir zamrzava tablu igrača 2");
                Check(!b1.IsFrozen, "Tabla igrača 1 nije zamrznuta");
                b2.HandleTileDown(b2.Get(fake2), null);
                yield return null;
                Check(State == AppState.Playing, "Dodir na zamrznutoj tabli se ignoriše");

                // ---------- Runde do kraja meča ----------
                int target = Mathf.Max(1, gameplay.targetWins);
                int safety = target * 3;
                while (State != AppState.MatchResult && safety-- > 0)
                {
                    yield return WaitFor(AppState.Playing, 3f);
                    if (State != AppState.Playing) break;
                    yield return new WaitForSecondsRealtime(step);
                    int before = score1;
                    b1.HandleTileDown(b1.Get(fake1), null);
                    yield return null;
                    Check(score1 == before + 1, "Runda " + round + ": pogodak igrača 1 → rezultat " + score1 + ":" + score2);
                    Check(State == AppState.RoundResult || State == AppState.MatchResult, "Posle pogotka: RoundResult (stanje: " + State + ")");
                    yield return new WaitForSecondsRealtime(timing.roundResultDuration + step);
                }
                yield return WaitFor(AppState.MatchResult, 3f);
                Check(State == AppState.MatchResult || State == AppState.Attract, "Meč završen (stanje: " + State + ")");
                Check(score1 >= target || State == AppState.Attract, "Igrač 1 je dostigao " + target + " pobede");
                if (State == AppState.MatchResult)
                {
                    Check(refs.result.gameObject.activeInHierarchy, "Ekran rezultata je vidljiv");
                    Check(Raycast(refs.result.player1.playAgain != null ? refs.result.player1.playAgain.gameObject : null, "NOVA IGRA"), lastRaycast);
                }

                // ---------- Povratak ----------
                yield return WaitFor(AppState.Attract, 3f);
                Check(State == AppState.Attract, "Automatski povratak na Attract");
                Check(score1 == 0 && score2 == 0 && round == 0, "Full reset: rezultat i runda su vraćeni na 0");
            }
            finally
            {
                timing = savedTiming;
                animations = savedAnim;
                input = savedInput;
                gameplay = savedGameplay;
                data.recordMatches = savedRecord;
                if (Application.isPlaying) EnterState(AppState.Attract);

                log.Append(fails == 0 ? "REZULTAT: SVE PROŠLO" : "REZULTAT: " + fails + " GREŠAKA");
                SelfTestReport = log.ToString();
                if (fails == 0) Debug.Log(SelfTestReport);
                else Debug.LogError(SelfTestReport);
                SelfTestRunning = false;
                if (host != null) Destroy(host);
            }
        }

        private IEnumerator WaitFor(AppState wanted, float timeout)
        {
            float end = Time.unscaledTime + timeout;
            while (State != wanted && Time.unscaledTime < end) yield return null;
        }

        private string lastRaycast = "";

        /// <summary>Simulira klik na centar objekta i proverava da li ga baš on prima (ništa ga ne pokriva).</summary>
        private bool Raycast(GameObject target, string label)
        {
            if (target == null)
            {
                lastRaycast = label + ": objekat ne postoji";
                return false;
            }
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                lastRaycast = label + ": nema EventSystem-a";
                return false;
            }
            var rt = (RectTransform)target.transform;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
            var pointer = new PointerEventData(es) { position = screen };
            var hits = new List<RaycastResult>();
            es.RaycastAll(pointer, hits);
            if (hits.Count == 0)
            {
                lastRaycast = label + ": klik ne pogađa ništa (GraphicRaycaster / CanvasGroup.blocksRaycasts / raycastTarget)";
                return false;
            }
            GameObject top = hits[0].gameObject;
            if (top == target || top.transform.IsChildOf(target.transform))
            {
                lastRaycast = label + ": klik stiže do objekta";
                return true;
            }
            lastRaycast = label + ": klik BLOKIRA '" + PathOf(top.transform) + "'";
            return false;
        }

        private static string PathOf(Transform t)
        {
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
            return p;
        }
    }
}
