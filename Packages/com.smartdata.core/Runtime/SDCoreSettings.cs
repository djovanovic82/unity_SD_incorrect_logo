// SmartData Core — podešavanja (opciono). Napravi: Assets > Create > SmartData > Core Settings,
// i stavi u bilo koji Resources folder pod imenom "SDCoreSettings". Bez asseta važe podrazumevane vrednosti.
using UnityEngine;

namespace SmartData.Core
{
    [CreateAssetMenu(menuName = "SmartData/Core Settings", fileName = "SDCoreSettings")]
    public class SDCoreSettings : ScriptableObject
    {
        [Header("ZASTOR (bez poskoka)")]
        [Tooltip("Zastor preko celog ekrana dok se ekran ne smiri i raspored ne složi.")]
        public bool bootCover = true;
        [Tooltip("Boja zastora. Igra/hub može da je promeni pre učitavanja scene (SDScreenBoot.CoverColor).")]
        public Color coverColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        [Tooltip("Logo na zastoru (opciono).")]
        public Sprite coverLogo;
        [Tooltip("Koliko uzastopnih frejmova ekran mora biti isti da bi se smatrao stabilnim.")]
        [Range(1, 10)] public int stableFramesRequired = 3;
        [Tooltip("Dodatni frejmovi posle stabilnog ekrana — da se UI (SmartDataUIStandard, LayoutGroup) složi pre prikaza.")]
        [Range(1, 10)] public int settleFrames = 3;
        [Tooltip("Dodatni frejmovi kad je ekran isti kao zapamćeni profil (brže).")]
        [Range(1, 10)] public int settleFramesKnown = 2;
        [Tooltip("Najduže čekanje na stabilan ekran (rotacija na Android-u ume da traje).")]
        [Range(0.2f, 3f)] public float maxCalibrationSeconds = 1.5f;
        [Tooltip("Trajanje skidanja zastora.")]
        [Range(0f, 1f)] public float coverFadeSeconds = 0.25f;
        [Tooltip("Zastor i kad se ekran promeni tokom rada (rotacija, safe area).")]
        public bool coverOnScreenChange = true;

        static SDCoreSettings _inst;
        public static SDCoreSettings Instance
        {
            get
            {
                if (_inst == null) _inst = Resources.Load<SDCoreSettings>("SDCoreSettings");
                if (_inst == null) { _inst = CreateInstance<SDCoreSettings>(); _inst.hideFlags = HideFlags.DontSave; }
                return _inst;
            }
        }
    }
}
