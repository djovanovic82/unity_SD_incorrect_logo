using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Platformska i kiosk podešavanja u runtime-u. Bez Android JNI.</summary>
    public static class KioskController
    {
        public static void Apply(KioskBlock kiosk, DisplayBlock display)
        {
            Application.targetFrameRate = display.targetFrameRate;
            Application.runInBackground = kiosk.runInBackground;
            Screen.sleepTimeout = kiosk.preventSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

            bool hideCursor = kiosk.kioskMode && kiosk.hideCursorInKiosk && !Application.isEditor;
            Cursor.visible = !hideCursor;

#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.fullScreen = true;
#endif
        }
    }
}
