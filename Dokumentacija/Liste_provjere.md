# Liste provjere – inspekcija koda eVrtić

Svaki nalaz mora imati:
- jednu stavku liste provjere (npr. LP-S3)
- ocjenu ozbiljnosti od 1 do 5
- kategoriju uzroka (U1–U7)


## Opća lista provjere (LP-O)

LP-O1 – Struktura
- Da li kod radi ono što aplikacija treba da radi?
- Koriste li se konstante ili enum umjesto "magičnih" brojeva i stringova?
- Ima li mrtvog, nedostižnog ili zakomentarisanog koda?
- Ima li dupliciranog (copy-paste) koda?
- Ima li predugih metoda ili kontrolera koji rade previše stvari?

LP-O2 – Varijable i aritmetika
- Jesu li imena varijabli, metoda i klasa smislena i dosljedna?
- Ima li nekorištenih varijabli, parametara ili using direktiva?
- Provjerava li se null prije upotrebe (npr. nakon FirstOrDefault ili Find)?
- Koristi li se decimal za novac?
- Koristi li se datum/vrijeme dosljedno (DateTime.Now vs. DateTime.UtcNow)?

LP-O3 – Petlje i grananja
- Ima li svaki switch granu default?
- Jesu li uslovi ispravni (< ili <=, negacije, prazne liste)?
- Vraćaju li svi putevi kroz metodu ispravan rezultat?
- Je li kod previše ugniježđen (previše if-ova jedan u drugom)?

LP-O4 – Defanzivno programiranje
- Provjeravaju li se granice listi i indeksa?
- Zatvaraju li se resursi (using)?
- Ima li praznih catch blokova?
- Koristi li se await ispravno (bez .Result i .Wait())?
- Provjerava li se ModelState.IsValid prije spremanja?


## Sigurnosna lista provjere (LP-S)

LP-S1 – Validacija unosa (CWE-20, CWE-89; OWASP A05)
- Provjeravaju li se svi podaci koje korisnik šalje (forme, URL)?
- Može li korisnik poslati polja koja ne bi smio mijenjati (nedostaje [Bind] ili ViewModel)?
- Ima li SQL upita sastavljenih spajanjem stringova?

LP-S2 – Tajne u kodu (CWE-798)
- Ima li lozinki, ključeva ili connection stringova upisanih u kod, appsettings.json ili README?

LP-S3 – Kontrola pristupa (CWE-862, CWE-639; OWASP A01)
- Ima li svaka osjetljiva akcija [Authorize] s ispravnom ulogom?
- Provjerava li server da traženi podatak (dijete, izvještaj, poruka) pripada prijavljenom korisniku?

LP-S4 – Lozinke i zaštita podataka (CWE-521, CWE-311; OWASP A04)
- Jesu li pravila za lozinke dovoljno jaka?
- Jesu li osjetljivi podaci (zdravlje djece, kontakti) zaštićeni?

LP-S5 – Greške i logiranje (CWE-209, CWE-778; OWASP A09, A10)
- Vidi li korisnik tehničke detalje greške (stack trace)?
- Upisuju li se osjetljivi podaci u logove?

LP-S6 – CSRF (CWE-352)
- Imaju li sve POST akcije [ValidateAntiForgeryToken]?

LP-S7 – XSS (CWE-79)
- Prikazuje li se tekst koji je unio korisnik bez enkodiranja (Html.Raw, innerHTML)?

LP-S8 – Zastarjele biblioteke (CWE-1395; OWASP A03)
- Koriste li se stari NuGet ili JavaScript paketi s poznatim ranjivostima?


## Skala ozbiljnosti

- 5 – Kritično: aplikacija ne radi ili je ugrožena sigurnost (npr. neovlašten pristup podacima djece)
- 4 – Visoko: ozbiljan problem u ključnoj funkciji, nema zaobilaznog rješenja
- 3 – Srednje: funkcija ne radi kako treba, ali postoji zaobilazno rješenje
- 2 – Nisko: smeta korisniku ili programeru, ali ne utiče na glavnu namjenu
- 1 – Minorno: kozmetika (ime, stil, tipfeler)


## Kategorije uzroka (za Pareto analizu)

- U1 – Generisani (scaffold) kod koji niko nije doradio
- U2 – Nepoznavanje sigurnosnih praksi
- U3 – Copy-paste programiranje
- U4 – Hardkodirane vrijednosti i tajne
- U5 – Nepoštovanje konvencija pisanja koda
- U6 – Nedostatak provjera (null, izuzeci, granice)
- U7 – Loša podjela odgovornosti (kontroleri rade sve)


## Format komentara u PR-u

[LP-S3 – Kontrola pristupa] Ozbiljnost: 5 – Kritično
Defekt: šta je pogrešno i zašto je to problem (1–3 rečenice).
CWE/OWASP: CWE-862, A01 (samo za sigurnosne nalaze)
Uzrok: U2
Inspektor: Ime Prezime


## Format issue-a

- Naslov: [S5][LP-S3] NazivKontrolera – kratak opis
- Opis: link na liniju koda, tekst iz komentara, link na komentar u PR-u
- Labele: ozbiljnost (npr. ozbiljnost-5-kriticno), stavka liste (npr. LP-S3), paket (npr. paket-3)
