// SmartData Core — kojoj zoni igrača pripada tačka na ekranu (standard v2.0, odeljak 0.3).
// SideBySide: dve polovine po dužoj strani (levo/desno u landscape, gore/dole u portrait).
// FaceToFace: dve polovine po dužoj strani, igrač 2 je naspram igrača 1 (njegov UI je okrenut 180°).
// Single: uvek igrač 0. Prst se prati po fingerId — zona se određuje na početku dodira i ne menja se.
using System.Collections.Generic;
using UnityEngine;

namespace SmartData.Core
{
    public static class SDZones
    {
        /// <summary>Zona (0 ili 1) za tačku u pikselima ekrana (y od dole).</summary>
        public static int ZoneOf(Vector2 screenPoint, SDLayout layout)
        {
            if (layout == SDLayout.Single) return 0;
            bool landscape = Screen.width >= Screen.height;
            if (landscape) return screenPoint.x < Screen.width * 0.5f ? 0 : 1;
            return screenPoint.y < Screen.height * 0.5f ? 0 : 1;
        }

        /// <summary>Pravougaonik zone u pikselima ekrana.</summary>
        public static Rect ZoneRect(int zone, SDLayout layout)
        {
            if (layout == SDLayout.Single) return new Rect(0, 0, Screen.width, Screen.height);
            bool landscape = Screen.width >= Screen.height;
            if (landscape) return new Rect(zone == 0 ? 0 : Screen.width * 0.5f, 0, Screen.width * 0.5f, Screen.height);
            return new Rect(0, zone == 0 ? 0 : Screen.height * 0.5f, Screen.width, Screen.height * 0.5f);
        }
    }

    /// <summary>
    /// Prati prste po fingerId: zona se dodeljuje na TouchPhase.Began i važi do kraja dodira,
    /// pa prst koji pređe granicu ne utiče na drugog igrača. Dodir duži od palmSeconds bez pomeranja se zanemaruje (dlan).
    /// Miš se koristi samo kad nema dodira (bez dupliranja touch + mouse). Radi samo sa starim Input Manager-om (Active Input Handling: Old ili Both).
    /// </summary>
    public class SDTouchTracker
    {
        public struct Contact { public int id; public int zone; public Vector2 position; public TouchPhase phase; }

        readonly Dictionary<int, int> _zoneById = new Dictionary<int, int>();
        readonly Dictionary<int, float> _startById = new Dictionary<int, float>();
        readonly Dictionary<int, Vector2> _startPosById = new Dictionary<int, Vector2>();
        readonly List<Contact> _contacts = new List<Contact>();

        public SDLayout layout = SDLayout.SideBySide;
        public float palmSeconds = 3f;
        public float palmMoveTolerance = 12f;

        /// <summary>Pozovi jednom po frejmu; vraća aktivne dodire sa dodeljenom zonom.</summary>
        public List<Contact> Poll()
        {
            _contacts.Clear();
#if ENABLE_LEGACY_INPUT_MANAGER
            float now = Time.unscaledTime;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        _zoneById[t.fingerId] = SDZones.ZoneOf(t.position, layout);
                        _startById[t.fingerId] = now;
                        _startPosById[t.fingerId] = t.position;
                    }
                    int zone;
                    if (!_zoneById.TryGetValue(t.fingerId, out zone)) continue;
                    bool palm = now - _startById[t.fingerId] > palmSeconds
                        && (t.position - _startPosById[t.fingerId]).magnitude < palmMoveTolerance;
                    if (!palm) _contacts.Add(new Contact { id = t.fingerId, zone = zone, position = t.position, phase = t.phase });
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        _zoneById.Remove(t.fingerId); _startById.Remove(t.fingerId); _startPosById.Remove(t.fingerId);
                    }
                }
            }
            else
            {
                const int mouseId = -1;
                Vector2 p = Input.mousePosition;
                TouchPhase ph;
                if (Input.GetMouseButtonDown(0)) { ph = TouchPhase.Began; _zoneById[mouseId] = SDZones.ZoneOf(p, layout); }
                else if (Input.GetMouseButtonUp(0)) ph = TouchPhase.Ended;
                else if (Input.GetMouseButton(0)) ph = TouchPhase.Moved;
                else return _contacts;
                int zone;
                if (_zoneById.TryGetValue(mouseId, out zone)) _contacts.Add(new Contact { id = mouseId, zone = zone, position = p, phase = ph });
                if (ph == TouchPhase.Ended) _zoneById.Remove(mouseId);
            }
#endif
            return _contacts;
        }

        public void Clear() { _zoneById.Clear(); _startById.Clear(); _startPosById.Clear(); _contacts.Clear(); }
    }
}
