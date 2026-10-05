using UnityEngine;

namespace SmartData.FindFake
{
    /// <summary>Jedan logo: original (prikazuje se na svim poljima osim jednog) i verzija sa greškom.</summary>
    [CreateAssetMenu(fileName = "NewLogoData", menuName = "SmartData/Pronađi pogrešan logo/Logo Data")]
    public class LogoData : ScriptableObject
    {
        [Tooltip("Stabilan ID za statistiku i izvoz. Ne menjati posle produkcije.")]
        public string id;
        public string brandName;
        public bool active = true;
        public Sprite correctSprite;
        public Sprite fakeSprite;
        [Tooltip("Opis greške (prikazuje se na kraju runde ako je uključeno).")]
        public string differenceDescription;

        public bool IsValid => correctSprite != null && fakeSprite != null && correctSprite != fakeSprite;

        public string SafeId => string.IsNullOrEmpty(id) ? name : id;
    }
}
