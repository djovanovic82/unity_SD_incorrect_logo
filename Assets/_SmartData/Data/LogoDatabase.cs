using System.Collections.Generic;
using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Lista logoa koji ulaze u igru. Redosled je bitan samo za preview.</summary>
    [CreateAssetMenu(fileName = "LogoDatabase", menuName = "SmartData/Pronađi pogrešan logo/Logo Database")]
    public class LogoDatabase : ScriptableObject
    {
        public List<LogoData> logos = new List<LogoData>();

        public List<LogoData> GetPlayable()
        {
            var result = new List<LogoData>();
            for (int i = 0; i < logos.Count; i++)
            {
                LogoData logo = logos[i];
                if (logo != null && logo.active && logo.IsValid) result.Add(logo);
            }
            return result;
        }

        public LogoData FirstPlayable()
        {
            List<LogoData> list = GetPlayable();
            return list.Count > 0 ? list[0] : null;
        }
    }
}
