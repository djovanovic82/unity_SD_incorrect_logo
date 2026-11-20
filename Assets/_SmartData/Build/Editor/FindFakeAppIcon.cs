using UnityEditor;
using UnityEngine;

namespace SmartData.FindFake.BuildTools
{
    /// <summary>SmartData app icon (diagonal split: SmartData mark + game motif). Generator: Tools~/icon/make_icon.py</summary>
    public static class FindFakeAppIcon
    {
        public const string AppIcon = "Assets/Art/Icon/AppIcon.png";

        /// <summary>Sets AppIcon.png as the Default Icon for all platforms. Called from the project's player-settings step.</summary>
        public static void Apply()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIcon);
            if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            else Debug.LogWarning("[SmartData] App icon not found: " + AppIcon);
        }
    }
}
