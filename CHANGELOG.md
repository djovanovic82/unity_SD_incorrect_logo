# Changelog

## 1.3.0 — 2026-10-21
- Dizajn teme (ThemePreset): Camel, Coca-Cola, Heineken – boje, pozadina (Cover, radi za landscape i portrait), polja, dugmad, naslovi SR/EN i sopstvena baza logoa.
- Po brendu 6 rundi sa različitim greškama (boja, ogledalo, nagib, spljošten, deo koji nedostaje, brend-specifična greška).
- LAYOUT → tileAspect: pravougaona polja za široke logoe (tema ga postavlja sama).
- Inspector → TEME: primena teme jednim klikom; "Kreiraj / dopuni brend teme" pravi assete iz Assets/Art/Brands (postojeće ne prepisuje).
- Admin → TEMA: promena teme na licu mesta; u buildu se pamti posle restarta.
- Raspored mreže izbegava nepotpun poslednji red kad god je razlika u veličini polja mala.

## 1.2.0 — 2026-10-05
- ISPRAVKA (glavni uzrok "klik ne radi"): providna Attract TapArea nije primala klikove jer je u Unity 2022
  "Cull Transparent Mesh" podrazumevano uključen. Sada se isključuje u runtime-u (radi i na postojećoj sceni) i u Setup/Repair.
- ISPRAVKA: EventSystem uvek dobija input modul koji odgovara aktivnom Input backend-u (Old / Both / New Input System).
- ISPRAVKA: CanvasGroup se postavlja pre aktivacije ekrana (Selectable-i odmah vide ispravno interactable stanje).
- Start ekran: SPREMAN postaje zelen, protivnik vidi "Protivnik je spreman!"; dugme se više ne sivi.
- DEBUG → editorSinglePlayerStart (podrazumevano uključeno): u Editoru jedan pritisak pokreće meč. Build se ne menja.
- DEBUG → logInput: loguje svaki klik/dodir i razlog odbijanja.
- FLOW SELF-TEST (Play Mode, dugme u Inspectoru): automatski prolazi ceo tok i raycast-om proverava da li išta blokira dugmad i polja.
- Validator: provera input modula, TapArea i Input System podešavanja.

## 1.1.0 — 2026-10-05
- Novo: `LAYOUT → splitDirection` (AlongLongSide / AlongShortSide / Auto) – podela ekrana prema dužoj ili kraćoj ivici, nezavisno od orijentacije (landscape i portrait).
- Isto podešavanje po profilu (`DisplayProfile → splitDirection`) uz override.
- Novo: `DisplayProfile → overrideRotations` – ručne rotacije zona se primenjuju samo kada je uključeno (inače se računaju automatski).
- Brzi izbor "Igrači" i "Podela ekrana" na vrhu Inspectora.
- Auto ponašanje je isto kao u 1.0.0.

## 1.0.0 — 2026-10-05
- Prva verzija igre "Pronađi pogrešan logo" (1 na 1, touch).
- Inspector-first konfiguracija (PROJECT … DEBUG), Edit Mode preview svih ekrana.
- Display profili: Full HD Landscape 1920×1080, Full HD Portrait 1080×1920, 1920×1200, iPad 2048×1536, 2880×1800, 1200×1920.
- Režimi: horizontalni sto (igrači naspram) i uspravan ekran (igrači jedan pored drugog).
- Auto raspored mreže (npr. 4×4 → 8×2 u širokoj zoni), HUD gore ili sa strane.
- Dodir na pointer-down, anti-spam po igraču, suzbijanje sintetičkog miša, centralni input lock.
- Kazna zamrzavanjem (opciono rastuća), tajmer runde, otkrivanje lažnog logoa i opis greške.
- Lokalna statistika (JSON), CSV izvoz, skriveni admin, kiosk podešavanja.
- Setup / Repair (preserve data), Validator, Build (Windows x64, Android ARM64, iPad), Game View preseti.
- Demo logoi (7 parova) generisani pri Setup-u.
