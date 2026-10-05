using System.Linq;
using SmartData.FindFake.BuildTools;
using SmartData.FindFake.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmartData.FindFake.EditorTools
{
    [CustomEditor(typeof(FindFakeApp))]
    public class FindFakeAppEditor : UnityEditor.Editor
    {
        private static readonly string[] Fields =
        {
            "project", "display", "content", "gameplay", "layout", "visuals", "typography", "animations",
            "input", "timing", "sound", "data", "admin", "kiosk", "build", "debug", "refs"
        };

        private static readonly string[] Titles =
        {
            "PROJECT", "DISPLAY", "CONTENT", "GAMEPLAY", "LAYOUT", "VISUALS", "TYPOGRAPHY", "ANIMATION",
            "INPUT", "TIMING", "AUDIO", "DATA", "ADMIN", "KIOSK", "BUILD", "DEBUG", "SCREENS / SCENE REFERENCES"
        };

        private const string LiveApplyKey = "SmartData.FindFake.LiveApply";
        private const string PreviewKey = "SmartData.FindFake.LastPreview";

        private static AppState LastPreview
        {
            get => (AppState)SessionState.GetInt(PreviewKey, (int)AppState.Attract);
            set => SessionState.SetInt(PreviewKey, (int)value);
        }

        public override void OnInspectorGUI()
        {
            var app = (FindFakeApp)target;
            serializedObject.Update();

            DrawHeaderBox(app);
            DrawPreview(app);
            DrawTools(app);
            DrawBuild(app);

            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < Fields.Length; i++) DrawSection(Fields[i], Titles[i]);
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (changed && !Application.isPlaying && EditorPrefs.GetBool(LiveApplyKey, true))
            {
                EditorApplication.delayCall += () =>
                {
                    if (app != null) Apply(app);
                };
            }
        }

        private static void DrawHeaderBox(FindFakeApp app)
        {
            EditorGUILayout.LabelField(app.project.productName + "  v" + app.project.version, EditorStyles.boldLabel);
            if (app.refs.canvas == null || app.refs.game == null || app.refs.attract == null)
                EditorGUILayout.HelpBox("Nedostaju reference scene. Klikni 'Repair Scene'.", MessageType.Warning);
            if (app.content.logoDatabase == null)
                EditorGUILayout.HelpBox("CONTENT → Logo Database nije dodeljena.", MessageType.Warning);
        }

        private void DrawPreview(FindFakeApp app)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("EDIT MODE PREVIEW", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                string[] labels = app.display.profiles.Select(p => p.Label).ToArray();
                if (labels.Length > 0)
                {
                    int current = Mathf.Clamp(app.display.editorPreviewProfileIndex, 0, labels.Length - 1);
                    int next = EditorGUILayout.Popup("Preview profil", current, labels);
                    if (next != app.display.editorPreviewProfileIndex)
                    {
                        Undo.RecordObject(app, "Preview Profile");
                        app.display.editorPreviewProfileIndex = next;
                        EditorUtility.SetDirty(app);
                        Apply(app);
                    }
                }

                var seating = (SeatingMode)EditorGUILayout.EnumPopup("Igrači", app.layout.seating);
                var direction = (SplitDirection)EditorGUILayout.EnumPopup("Podela ekrana", app.layout.splitDirection);
                if (seating != app.layout.seating || direction != app.layout.splitDirection)
                {
                    Undo.RecordObject(app, "Layout");
                    app.layout.seating = seating;
                    app.layout.splitDirection = direction;
                    EditorUtility.SetDirty(app);
                    Apply(app);
                }
                if (app.layout.split != SplitMode.Auto)
                    EditorGUILayout.HelpBox("LAYOUT → split je fiksno " + app.layout.split + ", pa se 'Podela ekrana' ne primenjuje.", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                PreviewButton(app, "ATTRACT", AppState.Attract);
                PreviewButton(app, "START", AppState.Start);
                PreviewButton(app, "COUNTDOWN", AppState.Countdown);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                PreviewButton(app, "GAME", AppState.Playing);
                PreviewButton(app, "ROUND RESULT", AppState.RoundResult);
                PreviewButton(app, "MATCH RESULT", AppState.MatchResult);
                PreviewButton(app, "ADMIN", AppState.Admin);
                EditorGUILayout.EndHorizontal();

                bool live = EditorPrefs.GetBool(LiveApplyKey, true);
                bool newLive = EditorGUILayout.ToggleLeft("Live apply (primeni promene odmah u sceni)", live);
                if (newLive != live) EditorPrefs.SetBool(LiveApplyKey, newLive);
            }
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode – stanje: " + app.State + " | profil: " + app.ActiveProfile.Label, MessageType.None);
                using (new EditorGUI.DisabledScope(app.SelfTestRunning))
                {
                    if (GUILayout.Button(app.SelfTestRunning ? "Self-test je u toku..." : "Pokreni FLOW SELF-TEST")) app.RunSelfTest();
                }
                if (!string.IsNullOrEmpty(app.SelfTestReport))
                    EditorGUILayout.HelpBox(app.SelfTestReport, app.SelfTestReport.Contains("FAIL") ? MessageType.Error : MessageType.Info);
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("FLOW SELF-TEST: uđi u Play Mode i klikni dugme koje se ovde pojavi.", MessageType.None);
            }
        }

        private static void PreviewButton(FindFakeApp app, string label, AppState state)
        {
            GUI.backgroundColor = LastPreview == state ? new Color(0.6f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(label))
            {
                LastPreview = state;
                Apply(app);
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DrawTools(FindFakeApp app)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("ALATI", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Apply")) Apply(app);
                if (GUILayout.Button("Sync Tiles")) SyncTiles(app);
                if (GUILayout.Button("Repair Scene")) FindFakeSetup.RepairMenu();
            }
            if (GUILayout.Button("Validate"))
                ValidatorWindow.ShowResults(FindFakeValidator.Run(app, EditorUserBuildSettings.activeBuildTarget));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Game View presets")) GameViewPresets.Install(app.display.profiles, false);
            if (GUILayout.Button("Otvori folder podataka"))
            {
                string folder = System.IO.Path.Combine(Application.persistentDataPath, app.data.folderName);
                System.IO.Directory.CreateDirectory(folder);
                EditorUtility.RevealInFinder(folder);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawBuild(FindFakeApp app)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("BUILD", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Windows x64")) EditorApplication.delayCall += () => FindFakeBuilder.Build(BuildTarget.StandaloneWindows64);
                if (GUILayout.Button("Android")) EditorApplication.delayCall += () => FindFakeBuilder.Build(BuildTarget.Android);
                if (GUILayout.Button("iPad")) EditorApplication.delayCall += () => FindFakeBuilder.Build(BuildTarget.iOS);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSection(string field, string title)
        {
            SerializedProperty prop = serializedObject.FindProperty(field);
            if (prop == null) return;
            string key = "SmartData.FindFake.Foldout." + field;
            bool open = SessionState.GetBool(key, false);
            bool next = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
            if (next != open) SessionState.SetBool(key, next);
            if (!next) return;

            EditorGUI.indentLevel++;
            SerializedProperty it = prop.Copy();
            SerializedProperty end = it.GetEndProperty();
            bool enter = true;
            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                EditorGUILayout.PropertyField(it, true);
                enter = false;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4f);
        }

        private static void Apply(FindFakeApp app)
        {
            if (app == null || Application.isPlaying) return;
            app.PreviewState(LastPreview);
            // CanvasScaler se primenjuje u sledećem editor update-u – tada još jednom poravnaj raspored.
            EditorApplication.delayCall += () =>
            {
                if (app != null && !Application.isPlaying) app.RefreshLayout();
            };
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
        }

        private static void SyncTiles(FindFakeApp app)
        {
            if (app.refs.game == null) return;
            int created = 0;
            for (int p = 1; p <= 2; p++)
            {
                PlayerBoard board = app.refs.game.Get(p).board;
                if (board == null) continue;
                Undo.RecordObject(board, "Sync Tiles");
                foreach (LogoTile tile in board.Configure(app.gameplay, app.layout))
                {
                    Undo.RegisterCreatedObjectUndo(tile.gameObject, "Create Tile");
                    created++;
                }
                EditorUtility.SetDirty(board);
            }
            Apply(app);
            Debug.Log("[FindFake] Sync Tiles: kreirano " + created + " novih polja (višak se samo deaktivira).");
        }
    }
}
