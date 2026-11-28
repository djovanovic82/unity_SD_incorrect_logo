# SmartData Core (`com.smartdata.core`) — v2.3.0

Zajednički delovi svih SmartData Unity igara po **standardu v2.0** (projekat „sd Hub" → `standard/SD_GAME_SCREEN_STANDARD.md` i `standard/PLAN_I_ODLUKE.md`).

Paket **ne menja** arhitekturu igara: svaka igra zadržava svoj state machine i scenu, a iz paketa koristi gotove delove.
Mere su u referentnom okviru Memory igre (kraća strana 1200), isto kao `SmartDataUIStandard`.

## Šta radi samo od sebe (bez ikakvog podešavanja)

| Deo | Šta radi |
|---|---|
| `SDScreenBoot` | Zastor preko ekrana pri pokretanju i pri svakom učitavanju scene dok se ekran ne smiri i UI složi → **nema poskoka**. Profil ekrana se pamti (`persistentDataPath/SmartData/screen_profile.json`); sledeći put, ako je ekran isti, zastor traje 2 frejma i ne vidi se. Ako se ekran promeni u radu (rotacija), zastor se ponovo spušta. |

Podešavanja (opciono): **Assets ▸ Create ▸ SmartData ▸ Core Settings**, asset u bilo koji `Resources` folder pod imenom `SDCoreSettings`.
Isključivanje: `bootCover = false` ili pokretanje sa argumentom `-sdboot-off`.

## Komponente za igre

| Komponenta | Upotreba |
|---|---|
| `SDButton` | Dodaje se na postojeći `Button`. Statusi: Normal, Attract, Confirmed („SPREMAN"), Waiting („ČEKAMO PROTIVNIKA"), Locked, Disabled, Selected + `Error()` i `Cooldown(s)`. **Bez kvačica**, veličina se ne menja. Igra sluša `onPress` (zanemaruje dupli tap i dodir u Locked/Disabled). |
| `SDReadyPair` | START za dva igrača: SPREMAN / ČEKAMO PROTIVNIKA, poništavanje ponovnim dodirom, IGRAJ SAM posle 10 s, `onBothReady`, `onSolo(igrač)`. |
| `SDCountdown` | Odbrojavanje u zaobljenim kvadratima u boji brenda: `Play(3, onDone)` na početku, `Play(2, onDone)` posle prekida (gol, runda, nivo). |
| `SDCountdownView` | Samo izgled odbrojavanja, kad igra sama vodi vreme: `SDCountdownView.Create(roditelj, min, max, boja, font)`, pa `SetRemaining(n, ukupno)` / `ShowGo("KRENI!")` / `Hide()`. |
| `SDLook` | SPREMAN / čekanje za dugmad koja nisu `Button`: `SDLook.Confirmed(img, label, boja, true)`, `SDLook.Caption(rect, "ČEKAMO PROTIVNIKA", boja, font, true)`. |
| `SDZones` / `SDTouchTracker` | Zona igrača za tačku na ekranu; praćenje prstiju po `fingerId` (zona se ne menja kad prst pređe granicu), filtriranje dlana, miš samo kad nema dodira. |
| `SDBrand` | **Jedan izgled brenda u svim igrama**: dugme = Memory dugme brenda (`Resources/SDBrands/<brend>/Button_normal|pressed`), boje iz Memory tema (Heineken #00683D, Camel #0D4A93, Coca-Cola bela/#AF071C, Intesa #09562C, Wargaming #B8222C). Hub postavlja `SDBrand.Current` za igre sa liste standarda; tada SDButton, SDLook, SDCountdownView i `SmartDataUIStandard` koriste samo te boje i to dugme. SPREMAN = zamenjene boje dugmeta i teksta. |
| `SDStats` | CSV statistika partija bez ličnih podataka: `SDStats.Record(igra, brend, mod, igrači, "34|28", trajanje, nagrada)`. |
| `SDShapes` | Zaobljeni pravougaonik / okvir kao 9-slice sprite bez spoljnih slika. |
| `SDStandard` | Sve mere i vremena standarda na jednom mestu. |

## Igra sa sopstvenim asmdef-om

U `references` asmdef-a igre dodaj `"SmartData.Core"`.

## Igra van huba (samostalni repo)

Do pravljenja posebnog repoa `unity_SD_core` paket živi u hubu (`Packages/com.smartdata.core`). Samostalna igra ga dobija preko
`Packages/manifest.json`:

```json
"com.smartdata.core": "https://github.com/djovanovic82/unity_sd_hub.git?path=/Packages/com.smartdata.core#core-v2.3.0"
```
