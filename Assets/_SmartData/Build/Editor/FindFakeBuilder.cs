using System;
using System.IO;
using System.Linq;
using SmartData.FindFake.EditorTools;
using SmartData.FindFake.Validation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SmartData.FindFake.BuildTools
{
    /// <summary>
    /// Build kroz eksplicitan alat: backup scene → primena Player Settings → validacija
    /// (greške blokiraju) → build u Builds/&lt;platforma&gt;/.
    /// </summary>
    public static class FindFakeBuilder
    {
        private const string BuildMenu = FindFakeSetup.MenuRoot + "Build/";
        private const int MaxBackups = 10;

        [MenuItem(BuildMenu + "Windows x64", false, 60)]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64);
        }

        [MenuItem(BuildMenu + "Android (APK, ARM64)", false, 61)]
        public static void BuildAndroid()
        {
            Build(BuildTarget.Android);
        }

        [MenuItem(BuildMenu + "iPad (Xcode projekat)", false, 62)]
        public static void BuildIPad()
        {
            Build(BuildTarget.iOS);
        }

        [MenuItem(BuildMenu + "Apply Player Settings (aktivna platforma)", false, 80)]
        public static void ApplySettingsMenu()
        {
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            if (app == null) return;
            ApplyPlayerSettings(app, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("[FindFake] Player Settings primenjeni za " + EditorUserBuildSettings.activeBuildTarget + ".");
        }

        public static bool Build(BuildTarget target)
        {
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            if (app == null)
            {
                EditorUtility.DisplayDialog("Build", "Otvori Main scenu (SmartDataApp nije pronađen).", "OK");
                return false;
            }

            var scene = app.gameObject.scene;
            if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
            {
                EditorUtility.DisplayDialog("Build", "Scena nije sačuvana – build otkazan.", "OK");
                return false;
            }
            if (app.build.backupSceneBeforeBuild) BackupScene(scene.path);

            BuildSettingsUtil.EnsureSceneInBuild(FindFakeSetup.ScenePath);
            ApplyPlayerSettings(app, target);

            var issues = FindFakeValidator.Run(app, target);
            if (FindFakeValidator.HasErrors(issues))
            {
                ValidatorWindow.ShowResults(issues);
                EditorUtility.DisplayDialog("Build blokiran", "Validator je pronašao greške. Pogledaj FindFake Validator prozor.", "OK");
                return false;
            }

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            if (EditorUserBuildSettings.activeBuildTarget != target && !EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                EditorUtility.DisplayDialog("Build", "Prebacivanje na platformu " + target + " nije uspelo.", "OK");
                return false;
            }

            string path = OutputPath(app, target);
            Directory.CreateDirectory(target == BuildTarget.iOS ? path : Path.GetDirectoryName(path) ?? path);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { FindFakeSetup.ScenePath },
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = app.build.developmentBuild ? BuildOptions.Development : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[FindFake] Build uspešan: " + path + " (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " + summary.totalTime + ")");
                EditorUtility.RevealInFinder(path);
                return true;
            }

            Debug.LogError("[FindFake] Build nije uspeo: " + summary.result + ", greške: " + summary.totalErrors);
            EditorUtility.DisplayDialog("Build", "Build nije uspeo (" + summary.result + "). Pogledaj Console.", "OK");
            return false;
        }

        public static void ApplyPlayerSettings(FindFakeApp app, BuildTarget target)
        {
            ProjectBlock p = app.project;
            PlayerSettings.companyName = p.companyName;
            PlayerSettings.productName = p.productName;
            PlayerSettings.bundleVersion = p.version;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, p.bundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, p.bundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, p.bundleIdentifier);
            PlayerSettings.runInBackground = app.kiosk.runInBackground;

            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                    PlayerSettings.fullScreenMode = app.build.windowsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                    PlayerSettings.defaultIsNativeResolution = true;
                    PlayerSettings.visibleInBackground = true;
                    PlayerSettings.resizableWindow = !app.build.windowsFullscreen;
                    break;

                case BuildTarget.Android:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    PlayerSettings.Android.bundleVersionCode = p.buildNumber;
                    EditorUserBuildSettings.buildAppBundle = false;
                    ApplyOrientation(app.build.androidOrientation);
                    break;

                case BuildTarget.iOS:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
                    PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
                    PlayerSettings.iOS.buildNumber = p.buildNumber.ToString();
                    ApplyOrientation(app.build.iPadOrientation);
                    break;
            }
        }

        private static void ApplyOrientation(BuildOrientation orientation)
        {
            switch (orientation)
            {
                case BuildOrientation.Landscape:
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
                    break;
                case BuildOrientation.Portrait:
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                    break;
                case BuildOrientation.AutoLandscape:
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                    PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    PlayerSettings.allowedAutorotateToPortrait = false;
                    PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                    break;
                case BuildOrientation.AutoAll:
                    PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                    PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                    PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    PlayerSettings.allowedAutorotateToPortrait = true;
                    PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
                    break;
            }
        }

        private static string OutputPath(FindFakeApp app, BuildTarget target)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            string file = string.IsNullOrEmpty(app.build.buildFileName) ? "PronadjiPogresanLogo" : app.build.buildFileName;
            string ver = app.project.version;
            string root = Path.Combine(projectRoot, string.IsNullOrEmpty(app.build.outputFolder) ? "Builds" : app.build.outputFolder);

            switch (target)
            {
                case BuildTarget.Android:
                    return Path.Combine(root, "Android", file + "_" + ver + "_b" + app.project.buildNumber + ".apk");
                case BuildTarget.iOS:
                    return Path.Combine(root, "iPad", file + "_" + ver);
                default:
                    return Path.Combine(root, "Windows", file + "_" + ver, file + ".exe");
            }
        }

        private static void BackupScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return;
            const string folder = "Assets/_Backups";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "_Backups");
            string name = Path.GetFileNameWithoutExtension(scenePath);
            string dest = folder + "/" + name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
            AssetDatabase.CopyAsset(scenePath, dest);

            string[] old = AssetDatabase.FindAssets(name + "_ t:Scene", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderByDescending(x => x)
                .Skip(MaxBackups)
                .ToArray();
            foreach (string o in old) AssetDatabase.DeleteAsset(o);
        }
    }
}
