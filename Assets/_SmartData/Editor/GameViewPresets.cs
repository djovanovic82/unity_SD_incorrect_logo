using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartData.FindFake.EditorTools
{
    /// <summary>
    /// Dodaje fiksne Game View rezolucije za sve display profile (Standalone, Android, iOS).
    /// Koristi interni Unity API preko refleksije – ako ga verzija ne podržava, samo prijavi.
    /// </summary>
    public static class GameViewPresets
    {
        [MenuItem(FindFakeSetup.MenuRoot + "Install Game View Presets", false, 30)]
        public static void InstallMenu()
        {
            FindFakeApp app = UnityEngine.Object.FindObjectOfType<FindFakeApp>(true);
            List<DisplayProfile> profiles = app != null ? app.display.profiles : DisplayProfile.CreateDefaults();
            Install(profiles, false);
        }

        public static void Install(List<DisplayProfile> profiles, bool silent)
        {
            try
            {
                Assembly editorAsm = typeof(Editor).Assembly;
                Type sizesType = editorAsm.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAsm.GetType("UnityEditor.GameViewSize");
                Type sizeKindType = editorAsm.GetType("UnityEditor.GameViewSizeType");
                Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object instance = singletonType.GetProperty("instance").GetValue(null, null);
                MethodInfo getGroup = sizesType.GetMethod("GetGroup");
                ConstructorInfo ctor = sizeType.GetConstructor(new[] { sizeKindType, typeof(int), typeof(int), typeof(string) });
                object fixedKind = Enum.ToObject(sizeKindType, 1); // FixedResolution

                int added = 0;
                foreach (GameViewSizeGroupType groupType in new[] { GameViewSizeGroupType.Standalone, GameViewSizeGroupType.Android, GameViewSizeGroupType.iOS })
                {
                    object group = getGroup.Invoke(instance, new object[] { (int)groupType });
                    MethodInfo getTexts = group.GetType().GetMethod("GetDisplayTexts");
                    MethodInfo addCustom = group.GetType().GetMethod("AddCustomSize");
                    var existing = new HashSet<string>((string[])getTexts.Invoke(group, null));

                    foreach (DisplayProfile p in profiles)
                    {
                        string label = "SD " + p.name;
                        bool exists = false;
                        foreach (string e in existing)
                            if (e.StartsWith(label, StringComparison.Ordinal)) exists = true;
                        if (exists) continue;
                        object size = ctor.Invoke(new object[] { fixedKind, p.resolution.x, p.resolution.y, label });
                        addCustom.Invoke(group, new[] { size });
                        added++;
                    }
                }
                sizesType.GetMethod("SaveToHDD")?.Invoke(instance, null);
                if (!silent) EditorUtility.DisplayDialog("Game View", "Dodato rezolucija: " + added + ".\nIzaberi ih u Game View padajućem meniju (prefiks 'SD').", "OK");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FindFake] Game View preseti nisu dodati automatski (" + e.Message + "). Dodaj ih ručno u Game View.");
            }
        }
    }
}
