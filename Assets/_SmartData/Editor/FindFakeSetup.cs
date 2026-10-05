using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartData.FindFake.EditorTools
{
    /// <summary>
    /// Setup Project: kreira scenu samo ako ne postoji.
    /// Repair Scene (preserve data): kreira SAMO objekte/komponente/reference koji nedostaju.
    /// Postojeći objekti, sidra, boje, sprite-ovi i tekstovi se nikad ne menjaju.
    /// </summary>
    public static class FindFakeSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string MenuRoot = "SmartData/Pronađi pogrešan logo/";

        private static readonly List<string> createdLog = new List<string>();

        // =====================================================================
        // Meniji
        // =====================================================================

        [MenuItem(MenuRoot + "Setup Project", false, 1)]
        public static void SetupProject()
        {
            if (!TmpEssentialsReady())
            {
                ImportTmpEssentials();
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            bool firstTime = !File.Exists(ScenePath);
            if (firstTime)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            FindFakeApp app = RepairScene(firstTime);
            DemoContent.EnsureDemoContent(app);
            BuildSettingsUtil.EnsureSceneInBuild(ScenePath);

            app.PreviewState(AppState.Attract);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            EditorSceneManager.SaveScene(app.gameObject.scene);
            GameViewPresets.Install(app.display.profiles, true);
            Selection.activeGameObject = app.gameObject;

            Debug.Log("[FindFake] Setup završen. Scena: " + ScenePath + (createdLog.Count > 0 ? "\nKreirano:\n - " + string.Join("\n - ", createdLog) : ""));
        }

        [MenuItem(MenuRoot + "Repair Scene (preserve data)", false, 2)]
        public static void RepairMenu()
        {
            if (!TmpEssentialsReady())
            {
                ImportTmpEssentials();
                return;
            }
            FindFakeApp app = RepairScene(false);
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            Selection.activeGameObject = app.gameObject;
            string msg = createdLog.Count == 0
                ? "Ništa nije nedostajalo. Scena je netaknuta."
                : "Dodato (" + createdLog.Count + "):\n - " + string.Join("\n - ", createdLog.Take(25));
            EditorUtility.DisplayDialog("Repair Scene", msg, "OK");
        }

        // =====================================================================
        // TMP Essentials
        // =====================================================================

        public static bool TmpEssentialsReady()
        {
            return Resources.Load<TMP_Settings>("TMP Settings") != null;
        }

        private static void ImportTmpEssentials()
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageCompleted += OnTmpImported;
            Debug.Log("[FindFake] Uvozim TMP Essential Resources – Setup se nastavlja automatski posle uvoza.");

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro/package.json");
            if (info != null)
            {
                string pkg = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(pkg))
                {
                    AssetDatabase.ImportPackage(pkg, false);
                    return;
                }
            }

            Type importer = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("TMPro.TMP_PackageResourceImporter"))
                .FirstOrDefault(t => t != null);
            MethodInfo method = importer != null ? importer.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static) : null;
            if (method != null && method.GetParameters().Length == 3)
            {
                method.Invoke(null, new object[] { true, false, false });
                return;
            }

            if (!EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources"))
                EditorUtility.DisplayDialog("TextMeshPro", "Uvezi ručno: Window → TextMeshPro → Import TMP Essential Resources, pa ponovo pokreni Setup.", "OK");
        }

        private static void OnTmpImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.Refresh();
                if (TmpEssentialsReady()) SetupProject();
            };
        }

        // =====================================================================
        // Repair (idempotentno, hirurški)
        // =====================================================================

        private static void EnsureFolders()
        {
            foreach (string f in new[]
                     {
                         "Assets/Scenes", "Assets/Data", "Assets/Data/Logos", "Assets/Art", "Assets/Art/Logos",
                         "Assets/Art/Logos/Demo", "Assets/Art/Backgrounds", "Assets/Audio", "Assets/_Backups"
                     })
            {
                if (AssetDatabase.IsValidFolder(f)) continue;
                string parent = Path.GetDirectoryName(f)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
            }
        }

        public static FindFakeApp RepairScene(bool firstTime)
        {
            createdLog.Clear();

            // ---- SmartDataApp ----
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            if (app == null)
            {
                GameObject go = NewObject("SmartDataApp", null);
                app = Undo.AddComponent<FindFakeApp>(go);
                firstTime = true;
            }
            Undo.RecordObject(app, "Repair FindFakeApp");
            SceneRefs refs = app.refs;

            if (firstTime) AssignBuiltinDefaults(app);

            // ---- Zvuk ----
            if (refs.sound == null) refs.sound = GetOrAdd<SoundPlayer>(app.gameObject);
            if (refs.sound.sfx == null) refs.sound.sfx = GetOrAdd<AudioSource>(Child(app.transform, "SFX", false).gameObject);
            if (refs.sound.music == null) refs.sound.music = GetOrAdd<AudioSource>(Child(app.transform, "Music", false).gameObject);

            // ---- Kamera ----
            if (refs.mainCamera == null)
            {
                Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
                if (cam == null)
                {
                    GameObject camGo = NewObject("Main Camera", null);
                    cam = Undo.AddComponent<Camera>(camGo);
                    camGo.tag = "MainCamera";
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = app.visuals.backgroundColor;
                    camGo.transform.position = new Vector3(0f, 0f, -10f);
                }
                refs.mainCamera = cam;
            }

            // ---- EventSystem (nikad duplikat) ----
            EventSystem es = UnityEngine.Object.FindObjectOfType<EventSystem>(true);
            if (es == null)
            {
                GameObject esGo = NewObject("EventSystem", null);
                es = Undo.AddComponent<EventSystem>(esGo);
            }
            if (es.GetComponent<BaseInputModule>() == null) Undo.AddComponent<StandaloneInputModule>(es.gameObject);

            // ---- Canvas ----
            if (refs.canvas == null)
            {
                Transform existing = FindRoot("MainCanvas");
                GameObject canvasGo = existing != null ? existing.gameObject : NewObject("MainCanvas", null, typeof(RectTransform));
                Canvas canvas = GetOrAdd<Canvas>(canvasGo);
                if (existing == null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.pixelPerfect = false;
                }
                refs.canvas = canvas;
            }
            Transform canvasT = refs.canvas.transform;
            if (refs.scaler == null) refs.scaler = GetOrAdd<CanvasScaler>(refs.canvas.gameObject);
            GetOrAdd<GraphicRaycaster>(refs.canvas.gameObject);

            if (refs.backgroundColor == null) refs.backgroundColor = ImageChild(canvasT, "BackgroundColor", app.visuals.backgroundColor, 0, null);
            if (refs.backgroundImage == null) refs.backgroundImage = ImageChild(canvasT, "BackgroundImage", Color.white, 1, null);

            RectTransform safeRoot = RectChild(canvasT, "SafeAreaRoot", 2);
            if (refs.safeArea == null) refs.safeArea = GetOrAdd<SafeAreaFitter>(safeRoot.gameObject);

            refs.attract = BuildAttract(safeRoot, refs.attract);
            refs.start = BuildStart(safeRoot, refs.start);
            refs.game = BuildGame(safeRoot, refs.game, app);
            refs.result = BuildResult(safeRoot, refs.result);
            refs.admin = BuildAdmin(safeRoot, refs.admin);
            refs.overlay = BuildOverlay(canvasT, refs.overlay);

            EditorUtility.SetDirty(app);
            return app;
        }

        private static void AssignBuiltinDefaults(FindFakeApp app)
        {
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (app.visuals.buttonSprite == null) app.visuals.buttonSprite = uiSprite;
            if (app.visuals.tileSprite == null) app.visuals.tileSprite = uiSprite;
            if (app.visuals.circleSprite == null) app.visuals.circleSprite = knob;
        }

        // ---------------------------------------------------------------------
        // Ekrani
        // ---------------------------------------------------------------------

        private static T ScreenRoot<T>(RectTransform parent, string name, T existing) where T : ScreenView
        {
            if (existing != null) return existing;
            RectTransform rt = RectChild(parent, name, -1);
            GetOrAdd<CanvasGroup>(rt.gameObject);
            T view = GetOrAdd<T>(rt.gameObject);
            if (view.group == null) view.group = rt.GetComponent<CanvasGroup>();
            return view;
        }

        private static PlayerHalf HalfRoot(Transform screen, int player, PlayerHalf existing)
        {
            if (existing != null) return existing;
            RectTransform rt = RectChild(screen, "P" + player + "Half", -1, out bool created);
            PlayerHalf half = GetOrAdd<PlayerHalf>(rt.gameObject);
            if (created) half.player = player;
            if (half.content == null) half.content = RectChild(rt, "Content", -1);
            return half;
        }

        private static AttractView BuildAttract(RectTransform root, AttractView view)
        {
            view = ScreenRoot(root, "AttractScreen", view);
            if (view.tapArea == null)
            {
                Image img = ImageChild(view.transform, "TapArea", new Color(0f, 0f, 0f, 0f), 0, null);
                view.tapArea = GetOrAdd<SmartButton>(img.gameObject);
                view.tapArea.targetGraphic = img;
            }
            for (int p = 1; p <= 2; p++)
            {
                AttractView.Half h = p == 1 ? view.player1 : view.player2;
                h.half = HalfRoot(view.transform, p, h.half);
                Transform c = h.half.content;
                if (h.title == null) h.title = TextChild(c, "Title", "PRONAĐI POGREŠAN LOGO", new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.92f));
                if (h.subtitle == null) h.subtitle = TextChild(c, "Subtitle", "Dodirni ekran za početak", new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.42f));
            }
            return view;
        }

        private static StartView BuildStart(RectTransform root, StartView view)
        {
            view = ScreenRoot(root, "StartScreen", view);
            for (int p = 1; p <= 2; p++)
            {
                StartView.Half h = view.Get(p);
                h.half = HalfRoot(view.transform, p, h.half);
                Transform c = h.half.content;
                if (h.playerName == null) h.playerName = TextChild(c, "PlayerName", "IGRAČ " + p, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.96f));
                if (h.hint == null) h.hint = TextChild(c, "Hint", "", new Vector2(0.05f, 0.56f), new Vector2(0.95f, 0.72f));
                if (h.button == null) h.button = ButtonChild(c, "ReadyButton", "SPREMAN", new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.5f));
                if (h.status == null) h.status = TextChild(c, "Status", "", new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.2f));
            }
            return view;
        }

        private static GameView BuildGame(RectTransform root, GameView view, FindFakeApp app)
        {
            view = ScreenRoot(root, "GameScreen", view);
            if (view.divider == null) view.divider = ImageChild(view.transform, "Divider", new Color(1f, 1f, 1f, 0.25f), 0, null);

            for (int p = 1; p <= 2; p++)
            {
                GameView.Half h = view.Get(p);
                h.half = HalfRoot(view.transform, p, h.half);
                Transform c = h.half.content;

                if (h.hud == null) h.hud = RectChild(c, "Hud", -1);
                if (h.hudBackground == null) h.hudBackground = ImageChild(h.hud, "HudBackground", new Color(0f, 0f, 0f, 0.35f), 0, app.visuals.buttonSprite);
                if (h.playerName == null) h.playerName = TextChild(h.hud, "PlayerName", "IGRAČ " + p, Vector2.zero, Vector2.one);
                if (h.status == null) h.status = TextChild(h.hud, "Status", "", Vector2.zero, Vector2.one);
                if (h.score == null) h.score = TextChild(h.hud, "Score", "0 : 0", Vector2.zero, Vector2.one);

                if (h.board == null)
                {
                    RectTransform boardRt = RectChild(c, "Board", -1);
                    h.board = GetOrAdd<PlayerBoard>(boardRt.gameObject);
                }
                RepairBoard(h.board, app);
            }
            return view;
        }

        private static void RepairBoard(PlayerBoard board, FindFakeApp app)
        {
            Undo.RecordObject(board, "Repair Board");
            if (board.tilesRoot == null) board.tilesRoot = RectChild(board.transform, "Tiles", 0);

            board.tiles.RemoveAll(t => t == null);
            if (board.tiles.Count == 0)
            {
                LogoTile existing = board.tilesRoot.GetComponentInChildren<LogoTile>(true);
                board.tiles.Add(existing != null ? existing : CreateTile(board.tilesRoot, app));
                if (existing != null)
                {
                    foreach (LogoTile t in board.tilesRoot.GetComponentsInChildren<LogoTile>(true))
                        if (!board.tiles.Contains(t)) board.tiles.Add(t);
                }
            }

            if (board.lockOverlay == null)
            {
                board.lockOverlay = ImageChild(board.transform, "LockOverlay", new Color(0.8f, 0.05f, 0.05f, 0.45f), -1, app.visuals.buttonSprite);
                board.lockOverlay.gameObject.SetActive(false);
            }
            if (board.lockLabel == null) board.lockLabel = TextChild(board.lockOverlay.transform, "LockLabel", "ZAMRZNUTO", Vector2.zero, Vector2.one);

            foreach (LogoTile created in board.Configure(app.gameplay, app.layout))
            {
                Undo.RegisterCreatedObjectUndo(created.gameObject, "Create Tile");
                createdLog.Add(PathOf(created.transform));
            }
            EditorUtility.SetDirty(board);
        }

        private static LogoTile CreateTile(Transform parent, FindFakeApp app)
        {
            RectTransform rt = RectChild(parent, "Tile_00", -1);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 200f);
            Image frame = GetOrAdd<Image>(rt.gameObject);
            frame.sprite = app.visuals.tileSprite;
            frame.type = frame.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            LogoTile tile = GetOrAdd<LogoTile>(rt.gameObject);
            tile.frame = frame;
            tile.background = ImageChild(rt, "Background", Color.white, 0, app.visuals.tileSprite);
            tile.icon = ImageChild(rt, "Icon", Color.white, 1, null);
            tile.icon.preserveAspect = true;
            return tile;
        }

        private static ResultView BuildResult(RectTransform root, ResultView view)
        {
            view = ScreenRoot(root, "ResultScreen", view);
            for (int p = 1; p <= 2; p++)
            {
                ResultView.Half h = view.Get(p);
                h.half = HalfRoot(view.transform, p, h.half);
                Transform c = h.half.content;
                if (h.headline == null) h.headline = TextChild(c, "Headline", "POBEDA!", new Vector2(0.05f, 0.56f), new Vector2(0.95f, 0.94f));
                if (h.caption == null) h.caption = TextChild(c, "Caption", "Konačan rezultat", new Vector2(0.1f, 0.46f), new Vector2(0.9f, 0.56f));
                if (h.score == null) h.score = TextChild(c, "Score", "3 : 1", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.46f));
                if (h.playAgain == null) h.playAgain = ButtonChild(c, "PlayAgainButton", "NOVA IGRA", new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.22f));
            }
            return view;
        }

        private static AdminView BuildAdmin(RectTransform root, AdminView view)
        {
            view = ScreenRoot(root, "AdminScreen", view);
            if (view.dim == null) view.dim = ImageChild(view.transform, "Dim", new Color(0f, 0f, 0f, 0.6f), 0, null);
            if (view.panel == null)
            {
                view.panel = ImageChild(view.transform, "Panel", new Color(0.1f, 0.11f, 0.14f, 0.97f), 1, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
                SetAnchors(view.panel.rectTransform, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.94f));
            }
            Transform panel = view.panel.transform;
            if (view.title == null) view.title = TextChild(panel, "Title", "ADMIN", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f));
            if (view.stats == null)
            {
                view.stats = TextChild(panel, "Stats", "", new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.85f));
                view.stats.alignment = TextAlignmentOptions.Left;
            }
            if (view.message == null) view.message = TextChild(panel, "Message", "", new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.56f));

            RectTransform buttons = RectChild(panel, "Buttons", -1, out bool created);
            if (created)
            {
                SetAnchors(buttons, new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.47f));
                var layoutGroup = Undo.AddComponent<VerticalLayoutGroup>(buttons.gameObject);
                layoutGroup.spacing = 12f;
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = true;
            }
            if (view.resume == null) view.resume = ButtonChild(buttons, "ResumeButton", "NAZAD", Vector2.zero, Vector2.one);
            if (view.newGame == null) view.newGame = ButtonChild(buttons, "NewGameButton", "NOVA IGRA", Vector2.zero, Vector2.one);
            if (view.language == null) view.language = ButtonChild(buttons, "LanguageButton", "JEZIK", Vector2.zero, Vector2.one);
            if (view.export == null) view.export = ButtonChild(buttons, "ExportButton", "IZVEZI CSV", Vector2.zero, Vector2.one);
            if (view.resetStats == null) view.resetStats = ButtonChild(buttons, "ResetStatsButton", "OBRIŠI STATISTIKU", Vector2.zero, Vector2.one);
            if (view.quit == null) view.quit = ButtonChild(buttons, "QuitButton", "IZLAZ", Vector2.zero, Vector2.one);
            return view;
        }

        private static OverlayView BuildOverlay(Transform canvas, OverlayView view)
        {
            if (view == null)
            {
                RectTransform rt = RectChild(canvas, "OverlayRoot", -1);
                view = GetOrAdd<OverlayView>(rt.gameObject);
            }
            view.transform.SetAsLastSibling();
            if (view.touchLayer == null) view.touchLayer = RectChild(view.transform, "TouchLayer", 0);
            if (view.debugText == null) view.debugText = TextChild(view.transform, "DebugText", "", new Vector2(0.15f, 0.95f), new Vector2(0.85f, 1f));
            if (view.hiddenHotspot == null)
            {
                Image img = ImageChild(view.transform, "HiddenHotspot", new Color(1f, 0f, 1f, 0f), -1, null);
                img.raycastTarget = false;
                view.hiddenHotspot = img.rectTransform;
            }
            return view;
        }

        // =====================================================================
        // Niski nivo – kreiraju samo ono što ne postoji
        // =====================================================================

        private static GameObject NewObject(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, components);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            if (parent != null) go.transform.SetParent(parent, false);
            createdLog.Add(PathOf(go.transform));
            return go;
        }

        private static Transform FindRoot(string name)
        {
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }

        private static Transform Child(Transform parent, string name, bool rect)
        {
            Transform t = parent.Find(name);
            if (t != null) return t;
            return rect ? NewObject(name, parent, typeof(RectTransform)).transform : NewObject(name, parent).transform;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null)
            {
                c = Undo.AddComponent<T>(go);
                createdLog.Add(PathOf(go.transform) + " [" + typeof(T).Name + "]");
            }
            return c;
        }

        private static RectTransform RectChild(Transform parent, string name, int siblingIndex)
        {
            return RectChild(parent, name, siblingIndex, out _);
        }

        private static RectTransform RectChild(Transform parent, string name, int siblingIndex, out bool created)
        {
            Transform t = parent.Find(name);
            created = t == null;
            if (!created) return (RectTransform)t;
            var rt = (RectTransform)NewObject(name, parent, typeof(RectTransform)).transform;
            Stretch(rt);
            if (siblingIndex >= 0) rt.SetSiblingIndex(siblingIndex);
            return rt;
        }

        private static Image ImageChild(Transform parent, string name, Color color, int siblingIndex, Sprite sprite)
        {
            RectTransform rt = RectChild(parent, name, siblingIndex, out bool created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            if (created)
            {
                img.color = color;
                img.sprite = sprite;
                img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                img.raycastTarget = false;
            }
            return img;
        }

        private static TextMeshProUGUI TextChild(Transform parent, string name, string text, Vector2 min, Vector2 max)
        {
            RectTransform rt = RectChild(parent, name, -1, out bool created);
            TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(rt.gameObject);
            if (created)
            {
                SetAnchors(rt, min, max);
                tmp.text = text;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = true;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = 48f;
                tmp.fontSizeMin = 14f;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }
            return tmp;
        }

        private static SmartButton ButtonChild(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            RectTransform rt = RectChild(parent, name, -1, out bool created);
            Image img = GetOrAdd<Image>(rt.gameObject);
            SmartButton button = GetOrAdd<SmartButton>(rt.gameObject);
            if (created)
            {
                SetAnchors(rt, min, max);
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                img.raycastTarget = true;
                button.targetGraphic = img;
                TextChild(rt, "Label", label, Vector2.zero, Vector2.one);
            }
            return button;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

    /// <summary>Jednokratna ponuda za Setup kada scena još ne postoji.</summary>
    [InitializeOnLoad]
    internal static class FindFakeFirstRun
    {
        private const string SessionKey = "SmartData.FindFake.FirstRunAsked";

        static FindFakeFirstRun()
        {
            EditorApplication.delayCall += Check;
        }

        private static void Check()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(FindFakeSetup.ScenePath) || SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            if (EditorUtility.DisplayDialog("Pronađi pogrešan logo",
                    "Glavna scena još ne postoji.\nPokrenuti Setup Project sada?", "Da", "Kasnije"))
            {
                FindFakeSetup.SetupProject();
            }
        }
    }

    public static class BuildSettingsUtil
    {
        public static void EnsureSceneInBuild(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            int idx = scenes.FindIndex(s => s.path == path);
            if (idx >= 0)
            {
                if (idx == 0 && scenes[0].enabled) return;
                scenes.RemoveAt(idx);
            }
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
