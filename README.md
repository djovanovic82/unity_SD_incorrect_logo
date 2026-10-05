# Pronađi pogrešan logo — SmartData Unity

Duel 1 na 1 za touch ekrane. Svaki igrač ima svoju tablu sa istim logoom ponovljenim na svim poljima, osim na jednom polju gde je verzija sa greškom. Pobeđuje igrač koji prvi dodirne lažni logo. Pogrešan dodir zamrzava tablu. Meč traje do zadatog broja pobeda.

- **Unity:** 2022.3.19f1 LTS
- **Platforme:** Windows x64, Android (ARM64, IL2CPP), iPad (Xcode projekat)
- **Scena:** jedna produkciona scena `Assets/Scenes/Main.unity`
- **Verzija:** vidi `VERSION` i `CHANGELOG.md`

---

## Instalacija (macOS, jedna komanda)

```bash
mkdir -p ~/UnityProjects && unzip -o ~/Downloads/unity_SD_pronadji_logo_v1.2.0.zip -d ~/UnityProjects/ && open -a "Unity Hub"
```

Zatim u Unity Hub-u izaberi **Add → Add project from disk → ~/UnityProjects/unity_SD_pronadji_logo**, sa verzijom 2022.3.19f1.

Pri prvom otvaranju projekat sam ponudi **Setup Project**. Setup radi sledeće:
1. uvozi TMP Essential Resources (ako nedostaju) i posle uvoza se sam nastavlja,
2. kreira `Main.unity` sa celom hijerarhijom,
3. generiše 7 demo parova logoa (`Assets/Art/Logos/Demo`, `Assets/Data/Logos`),
4. dodaje scenu u Build Settings i Game View rezolucije za sve profile (prefiks `SD`).

Isto je dostupno ručno iz menija **SmartData → Pronađi pogrešan logo → Setup Project**.

> **Active Input Handling** mora biti `Input Manager (Old)` ili `Both`. To je podrazumevano za nov 2022.3 projekat, a Validator to proverava.

---

## Display profili

Sve ide kroz jednu scenu, bez scene po rezoluciji:

| Profil | Rezolucija | Napomena |
|---|---|---|
| Full HD Landscape | 1920×1080 | |
| Full HD Portrait | 1080×1920 | |
| Landscape 16:10 | 1920×1200 | kanonski |
| iPad 4:3 | 2048×1536 | font ×1.1 |
| High-DPI 16:10 | 2880×1800 | font ×1.5 |
| Portrait 10:16 | 1200×1920 | kanonski |

U runtime-u se profil bira automatski po aspektu ekrana (`DISPLAY → selection = AutoByAspect`) ili se fiksira (`Forced`). Svaki profil može da ima sopstvenu pozadinu, font scale i **layout override** (podela, rotacije, HUD, razmak). Važi pravilo globalno → lokalno.

### Kako se raspoređuju igrači

**`LAYOUT → seating`**
- **TableOpposite** (podrazumevano) je horizontalni touch sto sa igračima jedan naspram drugog. Zone se rotiraju tako da svaki igrač čita svoju polovinu uspravno.
- **ScreenSideBySide** je uspravan ekran sa igračima jedan pored drugog, bez rotacije.

**`LAYOUT → splitDirection`** određuje uz koje ivice ekrana stoje igrači, isto u landscape-u i portrait-u:
- **AlongLongSide**: igrači su uz **duže** ivice (linija podele je paralelna dužoj strani).
- **AlongShortSide**: igrači su uz **kraće** ivice (linija podele je paralelna kraćoj strani).
- **Auto**: sto u landscape-u koristi duže ivice, a sve ostalo kraće ivice (ponašanje iz v1.0).

| Profil (sto) | Duže ivice | Kraće ivice |
|---|---|---|
| 1920×1080 Landscape | gore/dole, 0°/180°, mreža 8×2, polje ≈177 px | levo/desno, 270°/90°, 4×4, ≈166 px |
| 1080×1920 Portrait | levo/desno, 270°/90°, 8×2, ≈177 px | gore/dole, 0°/180°, 4×4, ≈166 px |

Isto podešavanje je dostupno na vrhu Inspectora ("Igrači", "Podela ekrana"), a po profilu kroz `overrideLayout → splitDirection`. Fiksno `split = TopBottom / LeftRight` ima prednost nad smerom. Ručne rotacije po profilu važe samo uz `overrideRotations`.

Ovaj isti mehanizam (`PlayerHalf`) koriste svi ekrani: Attract, Start, Game i Result. Zato svaki igrač uvek čita tekst uspravno sa svoje strane.

### Mreža polja

`GAMEPLAY → columns × rows` određuje **broj** polja (podrazumevano 4×4 = 16). Kada je uključen `autoGridShape`, isti broj polja se preraspoređuje da polja budu najveća. Na primer, Full HD landscape sto daje 8×2 sa poljima od oko 177 px, umesto 4×4 sa oko 99 px. HUD (ime, status, rezultat) ide gore ili sa strane, zavisno od oblika zone (`hudPlacement = Auto`).

---

## Inspector (SmartDataApp)

Sve što operater menja nalazi se na objektu **SmartDataApp**, nigde u kodu:

`PROJECT · DISPLAY · CONTENT · GAMEPLAY · LAYOUT · VISUALS · TYPOGRAPHY · ANIMATION · INPUT · TIMING · AUDIO · DATA · ADMIN · KIOSK · BUILD · DEBUG · SCREENS`

U vrhu Inspectora se nalaze:
- **Preview profil** i dugmad **ATTRACT / START / COUNTDOWN / GAME / ROUND RESULT / MATCH RESULT / ADMIN**. Rade u Edit Mode-u i ne kreiraju niti brišu objekte.
- **Live apply**: svaka izmena u Inspectoru se odmah vidi u sceni.
- **Apply**, **Sync Tiles** (posle promene broja polja), **Repair Scene**, **Validate**, **Game View presets**, **Otvori folder podataka**.
- **Build**: Windows x64, Android i iPad.

### Najvažnija podešavanja

| Sekcija | Polje | Podrazumevano |
|---|---|---|
| GAMEPLAY | targetWins | 3 |
| GAMEPLAY | wrongTapFreeze / escalatingPenalty | 2 s / isključeno |
| GAMEPLAY | roundTimeLimit (0 = bez tajmera) | 20 s |
| GAMEPLAY | distinctFakePositions | uključeno |
| GAMEPLAY | startMode | BothPlayersReady |
| TIMING | countdownFrom / countdownStep | 3 / 0.7 s |
| TIMING | idleTimeout | 45 s |
| INPUT | tapDebounce / touchMouseSuppression / inputLockAfterStateChange | 0.08 / 0.2 / 0.25 s |
| CONTENT | languages (SR, EN), showDifferenceDescription | |

---

## Dodavanje logoa

1. Ubaci dva PNG-a istih dimenzija: original i verziju sa greškom. Texture Type mora biti `Sprite (2D and UI)`.
2. **Create → SmartData → Pronađi pogrešan logo → Logo Data**. Popuni `id` (stabilan, za statistiku), `brandName`, oba sprite-a i `differenceDescription` (npr. "Pogrešna nijansa crvene").
3. Dodaj taj asset u `Assets/Data/LogoDatabase.asset`. Demo logoe isključi sa `active = false` ili ih ukloni iz liste.

Validator upozorava ako original i greška nisu iste dimenzije, jer bi se razlika tada videla po veličini.

---

## Pravila igre i zaštita ulaza

- Dodir se registruje na **pritisak** (pointer down), ne na otpuštanje prsta. Cela površina polja je aktivna.
- Svaki igrač ima sopstvenu tablu i sopstveni anti-spam. Jedan dodir ne može da aktivira oba igrača.
- Sintetički miš koji Windows generiše posle dodira se ignoriše.
- Posle svake promene stanja ulaz je zaključan 250 ms (centralni lock, ne samo `interactable`).
- Pogrešan dodir zamrzava samo tablu tog igrača i prikazuje odbrojavanje na tabli.
- Lažni logo kod igrača 2 nikad nije na istoj poziciji kao kod igrača 1, pa gledanje u protivnikovu ruku ne pomaže.
- Na kraju runde pobednik dobija zeleni okvir, a protivniku se otkriva gde je bio lažni logo (zlatni okvir), uz opcioni opis greške.
- **NOVA IGRA** prolazi kroz isti full reset kao svež start: rezultat, tajmeri, korutine, zamrzavanja, okviri, skala i lock.

---

## Provera toka (FLOW SELF-TEST)

U Play Mode-u, na SmartDataApp Inspectoru, klikni **Pokreni FLOW SELF-TEST**. Test ubrzava tajminge (samo za vreme testa) i prolazi ceo tok: Attract → Start → odbrojavanje → pogrešan dodir i zamrzavanje → pogoci do kraja meča → rezultat → povratak na Attract → full reset. Za svako dugme i polje simulira klik na centar i javlja da li ga neki drugi objekat blokira. Izveštaj se pojavljuje u Inspectoru i u Console.

Ako klik i dalje ne radi, uključi **DEBUG → logInput**. Console će tada za svaki klik ispisati da li je prihvaćen, a ako nije, zbog čega (input lock, debounce, sintetički miš...).

**Testiranje mišem:** `DEBUG → editorSinglePlayerStart` je podrazumevano uključen, pa u Editoru jedan klik na SPREMAN pokreće meč. U buildu je i dalje potrebno da oba igrača pritisnu SPREMAN.

## Admin, podaci, kiosk

- **Skriveni admin:** 5 dodira u gornjem levom uglu u roku od 3 s, na Attract, Start ili Result ekranu (`ADMIN` sekcija). Zona ne blokira polja ispod sebe.
- **Admin ekran:** statistika, promena jezika, CSV izvoz, brisanje statistike (dvostruka potvrda) i izlaz.
- **Podaci:** `Application.persistentDataPath/PronadjiLogo/matches.json`. Vreme se upisuje u trenutku nastanka zapisa, ne pri izvozu.
- **CSV:** folder `exports/`, separator `;`, UTF-8 sa BOM (otvara se direktno u Excel-u).
- **Kiosk:** `KIOSK → kioskMode` skriva kursor u buildu i sprečava gašenje ekrana. Windows build je Fullscreen Window.

---

## Build

Inspector → **BUILD** ili meni **SmartData → Pronađi pogrešan logo → Build**:

1. sačuva scenu i napravi backup u `Assets/_Backups` (čuva se poslednjih 10),
2. primeni Player Settings (ime, bundle ID, verzija, IL2CPP/ARM64, orijentacija, iPad only),
3. pokrene Validator (greške blokiraju build),
4. builduje u `Builds/Windows`, `Builds/Android` ili `Builds/iPad`.

Za Windows build na Mac-u potreban je modul **Windows Build Support (Mono)** iz Unity Hub-a. Validator to proverava.

---

## Struktura

```
Assets/_SmartData/
  Core/        FindFakeApp (SmartDataApp), AppState i enumi
  Config/      sekcije Inspectora, DisplayProfile, TextSet (jezici)
  Data/        LogoData, LogoDatabase, MatchDataStore (JSON/CSV)
  Features/FindFake/  PlayerBoard, LogoTile
  UI/          ekrani (Attract/Start/Game/Result/Admin/Overlay), PlayerHalf, LayoutResolver,
               SafeAreaFitter, BackgroundFitter, SmartButton, UiStyle
  Input/       InputGuard (jedino mesto sa legacy Input API)
  Platform/    KioskController
  Runtime/     SoundPlayer
  Editor/      Setup/Repair, Custom Inspector, demo logoi, Game View preseti
  Validation/Editor/  Validator
  Build/Editor/       Build alat
```

Runtime kod nema nijednu `UnityEditor` referencu, a Editor kod je isključivo u `Editor` folderima.

## Repair i ručne izmene

**Repair Scene (preserve data)** dodaje samo objekte, komponente i reference koji nedostaju. Postojeće pozicije, boje, sprite-ove, fontove i tekstove ne dira. Iz Inspectora se kontrolišu samo elementi koje vodi LAYOUT/VISUALS: sidra polovina igrača, HUD, mreža polja, boje i fontovi. Sve ostalo (naslovi, dugmad unutar polovina, admin panel) slobodno se pomera ručno i ostaje sačuvano.
