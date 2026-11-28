# Changelog — com.smartdata.core

## 2.3.0 — 2026-11-28
- Odluka: odbrojavanje je **jedan** zaobljeni kvadrat sa brojem 3-2-1 i „pop“ animacijom (kao `HubCountdownBadge` u hubu):
  `SDCountdownView.singleSquare = true` po podrazumevanju (kvadrat = 20 % kraće strane).
- `SDBrand.Custom(...)` / `SDBrand.SetCurrent(SDBrand)`: hub prosleđuje boje i dugme svoje početne strane — jedan izvor izgleda.

## 2.2.2 — 2026-11-28
- Odbrojavanje, natpisi i zastor nose `SDUISkip` u imenu: `SmartDataUIStandard` ih više ne dira. Ranije je kvadratima odbrojavanja
  dodavao „pozadinski“ fitter, pa su se sva tri kvadrata razvukla u jedan veliki koji se smanjivao (video sa testa).

## 2.2.1 — 2026-11-28
- Bez poskoka posle 2.2.0: zaobljenje dugmeta više ne zavisi od trenutne visine (bilo je drugačije pre i posle slaganja UI-a) —
  sada je isto kao u Memory (multiplier 1). Pulsiranje START-a u Attract statusu isključeno po podrazumevanju.

## 2.2.0 — 2026-11-28
- `SDBrand`: jedinstven izgled brenda za sve igre — Memory dugme brenda (sprite) i boje iz Memory tema; SPREMAN = zamenjene boje
  dugmeta i teksta; odbrojavanje u boji dugmeta brenda. Kad hub postavi `SDBrand.Current`, boje igara se ne koriste.

## 2.1.0 — 2026-11-27
- `SDCountdownView`: samo izgled odbrojavanja (kvadrati popunjavaju svoj pravougaonik) — igra vodi vreme i zove `SetRemaining` / `ShowGo`.
  `SDCountdown` sada koristi `SDCountdownView`.
- `SDLook`: izgled SPREMAN / čekanja za dugmad koja nisu `UnityEngine.UI.Button` (Image + ručni hit-test).
- `SDButton`: inicijalizacija posle podešavanja iz koda; Confirmed radi i preko obojenog sprite-a (svetla ispuna + ivica);
  `onPress` (zanemaruje dupli tap i Locked/Disabled).
- `SDTouchTracker`: radi samo uz stari Input Manager (`ENABLE_LEGACY_INPUT_MANAGER`), bez greške u projektima sa Input System-om.

## 2.0.0 — 2026-11-27
- Prva verzija po standardu v2.0 (tura 0): SDScreenBoot (zastor bez poskoka + profil ekrana), SDButton (statusi bez kvačica),
  SDReadyPair (START za dva igrača, IGRAJ SAM), SDCountdown (kvadrati u boji brenda), SDZones/SDTouchTracker, SDStats (CSV),
  SDShapes, SDStandard (mere iz Memory igre), SDCoreSettings.
