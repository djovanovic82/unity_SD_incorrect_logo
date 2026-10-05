using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartData.FindFake
{
    /// <summary>
    /// Obezbeđuje tačno jedan EventSystem sa input modulom koji odgovara aktivnom
    /// Input backend-u projekta. Bez ispravnog modula UI ne prima nijedan klik.
    /// </summary>
    public static class EventSystemGuard
    {
        private const string NewModuleType = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        public static bool UsesNewInputSystemOnly
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Vraća opis stanja (za log/self-test). Kreira EventSystem ako ne postoji.</summary>
        public static string Ensure()
        {
            EventSystem[] all = UnityEngine.Object.FindObjectsOfType<EventSystem>(true);
            EventSystem es = all.Length > 0 ? all[0] : null;
            if (es == null)
            {
                es = new GameObject("EventSystem").AddComponent<EventSystem>();
                Debug.LogWarning("[FindFake] EventSystem nije postojao – kreiran u runtime-u. Pokreni Repair Scene.");
            }
            for (int i = 1; i < all.Length; i++)
            {
                Debug.LogWarning("[FindFake] Duplikat EventSystem-a isključen: " + all[i].name);
                all[i].gameObject.SetActive(false);
            }
            if (!es.gameObject.activeSelf) es.gameObject.SetActive(true);
            if (!es.enabled) es.enabled = true;

            if (UsesNewInputSystemOnly)
            {
                Type moduleType = Type.GetType(NewModuleType);
                if (moduleType == null)
                    return "GREŠKA: Active Input Handling je 'Input System Package (New)', a paket Input System nije instaliran. Postavi 'Input Manager (Old)' ili 'Both'.";

                StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    legacy.enabled = false;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(legacy);
                    else UnityEngine.Object.DestroyImmediate(legacy);
                }
                if (es.GetComponent(moduleType) == null) es.gameObject.AddComponent(moduleType);
                return "InputSystemUIInputModule (novi Input System)";
            }

            BaseInputModule module = es.GetComponent<BaseInputModule>();
            if (module == null) module = es.gameObject.AddComponent<StandaloneInputModule>();
            if (!module.enabled) module.enabled = true;
            return module.GetType().Name;
        }
    }
}
