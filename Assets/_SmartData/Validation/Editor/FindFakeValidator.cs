using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SmartData.FindFake.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartData.FindFake.Validation
{
    public enum Severity
    {
        Error,
        Warning,
        Info
    }

    public struct Issue
    {
        public Severity severity;
        public string message;
        public Object context;

        public Issue(Severity s, string m, Object c = null)
        {
            severity = s;
            message = m;
            context = c;
        }
    }

    /// <summary>Provera projekta pre builda. Greške (Error) blokiraju build.</summary>
    public static class FindFakeValidator
    {
        [MenuItem(FindFakeSetup.MenuRoot + "Validate", false, 20)]
        public static void ValidateMenu()
        {
            FindFakeApp app = Object.FindObjectOfType<FindFakeApp>(true);
            ValidatorWindow.ShowResults(Run(app, EditorUserBuildSettings.activeBuildTarget));
        }

        public static bool HasErrors(List<Issue> issues)
        {
            return issues.Any(i => i.severity == Severity.Error);
        }

        public static List<Issue> Run(FindFakeApp app, BuildTarget target)
        {
            var issues = new List<Issue>();

            // ---- Scena ----
            FindFakeApp[] apps = Object.FindObjectsOfType<FindFakeApp>(true);
            if (apps.Length == 0)
            {
                issues.Add(new Issue(Severity.Error, "U otvorenoj sceni nema SmartDataApp (FindFakeApp). Pokreni Setup Project."));
                return issues;
            }
            if (apps.Length > 1) issues.Add(new Issue(Severity.Error, "Postoji više FindFakeApp objekata (" + apps.Length + ")."));
            if (app == null) app = apps[0];

            int eventSystems = Object.FindObjectsOfType<EventSystem>(true).Length;
            if (eventSystems == 0) issues.Add(new Issue(Severity.Error, "Nema EventSystem objekta. Pokreni Repair Scene."));
            if (eventSystems > 1) issues.Add(new Issue(Severity.Error, "Više EventSystem objekata (" + eventSystems + ") – duplira ulaz."));

            string scenePath = app.gameObject.scene.path;
            if (scenePath != FindFakeSetup.ScenePath)
                issues.Add(new Issue(Severity.Warning, "Otvorena scena nije " + FindFakeSetup.ScenePath + " (trenutno: " + scenePath + ")."));
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == FindFakeSetup.ScenePath))
                issues.Add(new Issue(Severity.Warning, "Main scena nije u Build Settings – build alat će je dodati."));

            // ---- Reference ----
            SceneRefs r = app.refs;
            Require(issues, r.canvas, "Canvas", app);
            Require(issues, r.scaler, "CanvasScaler", app);
            Require(issues, r.attract, "AttractScreen", app);
            Require(issues, r.start, "StartScreen", app);
            Require(issues, r.game, "GameScreen", app);
            Require(issues, r.result, "ResultScreen", app);
            Require(issues, r.admin, "AdminScreen", app);
            Require(issues, r.overlay, "OverlayRoot", app);
            Require(issues, r.sound, "SoundPlayer", app);

            if (r.attract != null && r.attract.tapArea != null)
            {
                var tapImg = r.attract.tapArea.GetComponent<UnityEngine.UI.Image>();
                if (tapImg == null || !tapImg.raycastTarget)
                    issues.Add(new Issue(Severity.Error, "Attract TapArea nema Image sa Raycast Target – dodir ne pokreće igru.", r.attract.tapArea));
            }

            if (r.game != null)
            {
                for (int p = 1; p <= 2; p++)
                {
                    GameView.Half h = r.game.Get(p);
                    if (h.board == null)
                    {
                        issues.Add(new Issue(Severity.Error, "Igrač " + p + ": nedostaje PlayerBoard.", r.game));
                        continue;
                    }
                    int active = h.board.tiles.Count(t => t != null);
                    if (active < app.gameplay.TileCount)
                        issues.Add(new Issue(Severity.Error, "Igrač " + p + ": tabla ima " + active + " polja, potrebno " + app.gameplay.TileCount + ". Klikni 'Sync Tiles'.", h.board));
                    if (h.half == null || h.status == null || h.score == null)
                        issues.Add(new Issue(Severity.Error, "Igrač " + p + ": HUD reference nedostaju. Pokreni Repair Scene.", r.game));
                }
            }

            // ---- Sadržaj ----
            LogoDatabase db = app.content.logoDatabase;
            if (db == null)
            {
                issues.Add(new Issue(Severity.Error, "CONTENT → Logo Database nije dodeljena.", app));
            }
            else
            {
                var ids = new HashSet<string>();
                int playable = 0;
                for (int i = 0; i < db.logos.Count; i++)
                {
                    LogoData l = db.logos[i];
                    if (l == null)
                    {
                        issues.Add(new Issue(Severity.Warning, "Logo Database: prazan slot #" + i + ".", db));
                        continue;
                    }
                    if (l.correctSprite == null || l.fakeSprite == null)
                        issues.Add(new Issue(Severity.Error, "Logo '" + l.name + "': nedostaje originalni ili lažni sprite.", l));
                    else if (l.correctSprite == l.fakeSprite)
                        issues.Add(new Issue(Severity.Error, "Logo '" + l.name + "': original i greška su isti sprite.", l));
                    else if (l.correctSprite.rect.size != l.fakeSprite.rect.size)
                        issues.Add(new Issue(Severity.Warning, "Logo '" + l.name + "': original i greška nisu iste dimenzije – razlika se može uočiti po veličini.", l));
                    if (string.IsNullOrEmpty(l.id))
                        issues.Add(new Issue(Severity.Warning, "Logo '" + l.name + "': prazan ID (statistika koristi ime asseta).", l));
                    else if (!ids.Add(l.id))
                        issues.Add(new Issue(Severity.Error, "Dupli logo ID: " + l.id, l));
                    if (l.active && l.IsValid) playable++;
                }
                if (playable == 0) issues.Add(new Issue(Severity.Error, "Nema nijednog aktivnog i ispravnog logoa.", db));
                int maxRounds = app.gameplay.targetWins * 2 - 1;
                if (playable > 0 && playable < maxRounds)
                    issues.Add(new Issue(Severity.Warning, "Samo " + playable + " logoa za meč do " + maxRounds + " rundi – logoi će se ponavljati.", db));
            }

            if (app.content.languages == null || app.content.languages.Count == 0)
                issues.Add(new Issue(Severity.Warning, "Nema definisanih jezika – koristi se ugrađeni srpski.", app));

            // ---- Gameplay / display ----
            if (app.gameplay.TileCount < 2) issues.Add(new Issue(Severity.Error, "Mreža mora imati najmanje 2 polja.", app));
            if (app.display.profiles == null || app.display.profiles.Count == 0)
            {
                issues.Add(new Issue(Severity.Error, "DISPLAY: nema nijednog profila.", app));
            }
            else
            {
                foreach (DisplayProfile p in app.display.profiles)
                    if (p.resolution.x <= 0 || p.resolution.y <= 0)
                        issues.Add(new Issue(Severity.Error, "Profil '" + p.name + "' ima neispravnu rezoluciju.", app));
                if (!app.display.profiles.Any(p => p.resolution == new Vector2Int(1920, 1080)))
                    issues.Add(new Issue(Severity.Info, "Nema Full HD Landscape (1920×1080) profila.", app));
                if (!app.display.profiles.Any(p => p.resolution == new Vector2Int(1080, 1920)))
                    issues.Add(new Issue(Severity.Info, "Nema Full HD Portrait (1080×1920) profila.", app));
            }
            if (app.gameplay.roundTimeLimit > 0f && app.gameplay.roundTimeLimit < 3f)
                issues.Add(new Issue(Severity.Warning, "Tajmer runde je kraći od 3 s.", app));

            // ---- Projekat / platforma ----
            if (!Regex.IsMatch(app.project.bundleIdentifier ?? "", @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$"))
                issues.Add(new Issue(Severity.Error, "Bundle identifier nije ispravan: " + app.project.bundleIdentifier, app));
            if (!Regex.IsMatch(app.project.version ?? "", @"^\d+(\.\d+){0,3}$"))
                issues.Add(new Issue(Severity.Error, "Verzija mora biti oblika 1.0.0: " + app.project.version, app));

#if !ENABLE_LEGACY_INPUT_MANAGER
            issues.Add(new Issue(Severity.Warning, "Active Input Handling je 'Input System Package (New)'. UI radi preko InputSystemUIInputModule, ali skrivena admin zona i vizualizacija dodira ne rade. Preporuka: 'Both' ili 'Input Manager (Old)'."));
            if (System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") == null)
                issues.Add(new Issue(Severity.Error, "Active Input Handling je 'New', a paket Input System nije instaliran – UI neće primati klikove."));
#endif
            EventSystem esCheck = Object.FindObjectOfType<EventSystem>(true);
            if (esCheck != null && esCheck.GetComponent<BaseInputModule>() == null)
                issues.Add(new Issue(Severity.Error, "EventSystem nema input modul – klikovi ne rade. Pokreni Repair Scene.", esCheck));
            if (!FindFakeSetup.TmpEssentialsReady())
                issues.Add(new Issue(Severity.Error, "TMP Essential Resources nisu uvezeni (Window → TextMeshPro)."));
            if (app.typography.font == null)
                issues.Add(new Issue(Severity.Info, "TYPOGRAPHY: koristi se podrazumevani TMP font. Proveri prikaz slova Č Ć Š Ž Đ.", app));

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                issues.Add(new Issue(Severity.Error, "Build modul za " + target + " nije instaliran (Unity Hub → Add Modules)."));

            if (app.kiosk.kioskMode && app.debug.showDebugOverlay)
                issues.Add(new Issue(Severity.Warning, "Kiosk režim je uključen zajedno sa Debug overlay-em.", app));

            return issues;
        }

        private static void Require(List<Issue> issues, Object obj, string label, Object context)
        {
            if (obj == null) issues.Add(new Issue(Severity.Error, "Nedostaje referenca: " + label + ". Pokreni Repair Scene.", context));
        }
    }

    public class ValidatorWindow : EditorWindow
    {
        private List<Issue> issues = new List<Issue>();
        private Vector2 scroll;

        public static void ShowResults(List<Issue> results)
        {
            var w = GetWindow<ValidatorWindow>("FindFake Validator");
            w.issues = results ?? new List<Issue>();
            w.minSize = new Vector2(460f, 260f);
            w.Show();
        }

        private void OnGUI()
        {
            int errors = issues.Count(i => i.severity == Severity.Error);
            int warnings = issues.Count(i => i.severity == Severity.Warning);
            EditorGUILayout.HelpBox(
                errors == 0 ? "Nema grešaka koje blokiraju build. Upozorenja: " + warnings : "Greške: " + errors + " (build je blokiran). Upozorenja: " + warnings,
                errors == 0 ? MessageType.Info : MessageType.Error);

            if (GUILayout.Button("Ponovo proveri"))
                issues = FindFakeValidator.Run(Object.FindObjectOfType<FindFakeApp>(true), EditorUserBuildSettings.activeBuildTarget);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (Issue issue in issues.OrderBy(i => i.severity))
            {
                MessageType type = issue.severity == Severity.Error ? MessageType.Error
                    : issue.severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(issue.message, type);
                if (issue.context != null && GUILayout.Button("Select", GUILayout.Width(60f), GUILayout.Height(38f)))
                {
                    Selection.activeObject = issue.context;
                    EditorGUIUtility.PingObject(issue.context);
                }
                EditorGUILayout.EndHorizontal();
            }
            if (issues.Count == 0) EditorGUILayout.LabelField("Sve provere su prošle.");
            EditorGUILayout.EndScrollView();
        }
    }
}
