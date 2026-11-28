// SmartData Core — čitanje/pisanje teksta za legacy Text i TextMeshPro (bez zavisnosti od TMP asembla).
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SmartData.Core
{
    public static class SDText
    {
        public static Component Find(Transform root)
        {
            if (root == null) return null;
            var t = root.GetComponentInChildren<Text>(true);
            if (t != null) return t;
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "TextMeshProUGUI" || n == "TextMeshPro") return c;
            }
            return null;
        }

        public static string Get(Component c)
        {
            if (c == null) return null;
            var t = c as Text;
            if (t != null) return t.text;
            var p = c.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            return p != null ? p.GetValue(c, null) as string : null;
        }

        public static void Set(Component c, string value)
        {
            if (c == null || value == null) return;
            var t = c as Text;
            if (t != null) { t.text = value; return; }
            var p = c.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite) p.SetValue(c, value, null);
        }

        public static void SetColor(Component c, Color color)
        {
            var g = c as Graphic;
            if (g != null) g.color = color;
        }

        public static Color GetColor(Component c)
        {
            var g = c as Graphic;
            return g != null ? g.color : Color.white;
        }
    }
}
