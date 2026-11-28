// SmartData Core — zapamćeni profil ekrana (standard v2.0, odeljak 0.1).
// Jedan zapis po orijentaciji (portrait / landscape) u persistentDataPath/SmartData/screen_profile.json.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SmartData.Core
{
    [Serializable]
    public class SDScreenProfile
    {
        public string key;
        public int width;
        public int height;
        public Rect safeArea;
        public string deviceModel;
        public string standardVersion;

        public static string KeyFor(int w, int h) { return w >= h ? "landscape" : "portrait"; }

        public static SDScreenProfile Current()
        {
            return new SDScreenProfile
            {
                key = KeyFor(Screen.width, Screen.height),
                width = Screen.width,
                height = Screen.height,
                safeArea = Screen.safeArea,
                deviceModel = SystemInfo.deviceModel,
                standardVersion = SDStandard.Version
            };
        }

        public bool SameAs(SDScreenProfile o)
        {
            if (o == null) return false;
            return width == o.width && height == o.height && safeArea == o.safeArea
                && deviceModel == o.deviceModel && standardVersion == o.standardVersion;
        }
    }

    public static class SDScreenProfileStore
    {
        [Serializable] class Wrapper { public List<SDScreenProfile> items = new List<SDScreenProfile>(); }

        static Wrapper _cache;

        static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "SmartData", "screen_profile.json"); }
        }

        static Wrapper Data
        {
            get
            {
                if (_cache != null) return _cache;
                _cache = new Wrapper();
                try
                {
                    if (File.Exists(FilePath)) _cache = JsonUtility.FromJson<Wrapper>(File.ReadAllText(FilePath)) ?? new Wrapper();
                }
                catch (Exception e) { Debug.LogWarning("[SD Core] Profil ekrana nije učitan: " + e.Message); _cache = new Wrapper(); }
                if (_cache.items == null) _cache.items = new List<SDScreenProfile>();
                return _cache;
            }
        }

        public static SDScreenProfile Get(string key)
        {
            foreach (var p in Data.items) if (p != null && p.key == key) return p;
            return null;
        }

        public static void Save(SDScreenProfile profile)
        {
            if (profile == null) return;
            var d = Data;
            d.items.RemoveAll(p => p == null || p.key == profile.key);
            d.items.Add(profile);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonUtility.ToJson(d, true));
            }
            catch (Exception e) { Debug.LogWarning("[SD Core] Profil ekrana nije snimljen: " + e.Message); }
        }

        /// <summary>Briše sve zapise (servisni ekran: „Ponovo izmeri ekran“).</summary>
        public static void Clear()
        {
            _cache = new Wrapper();
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch (Exception) { }
        }
    }
}
