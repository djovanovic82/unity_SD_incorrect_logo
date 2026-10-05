using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SmartData.FindFake
{
    /// <summary>
    /// Centralna zaštita ulaza: globalni lock posle promene stanja, suzbijanje
    /// sintetičkog miša posle dodira, anti-spam po igraču. Jedini fajl koji
    /// direktno čita legacy Input API.
    /// </summary>
    public class InputGuard
    {
        private float lockUntil = -1f;
        private float lastTouchTime = -999f;
        private readonly float[] lastPlayerTap = { -999f, -999f, -999f };

        public bool IsLocked => Time.unscaledTime < lockUntil;

        public void LockFor(float seconds)
        {
            if (seconds <= 0f) return;
            lockUntil = Mathf.Max(lockUntil, Time.unscaledTime + seconds);
        }

        public void ResetAll()
        {
            lockUntil = -1f;
            ResetPlayers();
        }

        public void ResetPlayers()
        {
            for (int i = 0; i < lastPlayerTap.Length; i++) lastPlayerTap[i] = -999f;
        }

        /// <summary>Poziva se jednom po frejmu. Vraća true ako je bilo ikakve aktivnosti pokazivača.</summary>
        public bool Tick()
        {
            bool activity = false;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                lastTouchTime = Time.unscaledTime;
                activity = true;
            }
            if (Input.GetMouseButtonDown(0)) activity = true;
#endif
            return activity;
        }

        /// <summary>Razlog poslednjeg odbijanja (za DEBUG → logInput i self-test).</summary>
        public string LastRejectReason { get; private set; } = "";

        private bool Reject(string reason)
        {
            LastRejectReason = reason;
            return false;
        }

        private bool PassesPointerType(PointerEventData e, InputBlock cfg)
        {
            if (e == null) return true;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Novi Input System koristi drugačije pointerId vrednosti – filtriranje tipa pokazivača se preskače.
            return true;
#else
            if (e.pointerId < 0)
            {
                // -1 = levi taster miša; desni/srednji se ne prihvataju.
                if (e.pointerId != -1) return Reject("desni/srednji taster miša");
                if (!cfg.allowMouse) return Reject("miš je isključen (INPUT → allowMouse)");
                if (Time.unscaledTime - lastTouchTime < cfg.touchMouseSuppression) return Reject("sintetički miš posle dodira");
                return true;
            }
            lastTouchTime = Time.unscaledTime;
            return true;
#endif
        }

        public bool AcceptGameTap(PointerEventData e, int player, InputBlock cfg)
        {
            if (IsLocked) return Reject("input lock posle promene stanja");
            if (!PassesPointerType(e, cfg)) return false;
            int p = Mathf.Clamp(player, 0, lastPlayerTap.Length - 1);
            if (Time.unscaledTime - lastPlayerTap[p] < cfg.tapDebounce) return Reject("anti-spam (tapDebounce)");
            lastPlayerTap[p] = Time.unscaledTime;
            return true;
        }

        public bool AcceptUi(PointerEventData e, InputBlock cfg)
        {
            if (IsLocked) return Reject("input lock posle promene stanja");
            return PassesPointerType(e, cfg);
        }

        /// <summary>Pozicije svih novih dodira/klikova u ovom frejmu (za skrivenu admin zonu).</summary>
        public void CollectPointerDowns(List<Vector2> results, InputBlock cfg)
        {
            results.Clear();
#if ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) results.Add(t.position);
            }
            if (cfg.allowMouse && Input.touchCount == 0 && Input.GetMouseButtonDown(0)
                && Time.unscaledTime - lastTouchTime >= cfg.touchMouseSuppression)
            {
                results.Add(Input.mousePosition);
            }
#endif
        }

        public static int TouchCount
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.touchCount;
#else
                return 0;
#endif
            }
        }

        public static Vector2 TouchPosition(int index)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetTouch(index).position;
#else
            return Vector2.zero;
#endif
        }

        public static void ConfigurePlatform(InputBlock cfg)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Input.multiTouchEnabled = cfg.multiTouch;
            Input.simulateMouseWithTouches = false;
#endif
        }
    }
}
