# Task 2 – Unit testiranje (Tarik)

**Testirane klase:** `PaymentController`, `OverstayChargeService` i modeli `PaymentViewModel`, `Pricing`, `PricingCreateDto`, `Ticket`.
**Okruženje:** MSTest 3.11.1, Moq 4.20.72, EF Core InMemory 8.0.25, coverlet.collector 6.0.4.
**Testni projekat:** `TestProjectTask2/TestProjectTask2`
- `Controllers/PaymentControllerTests.cs`
- `Services/OverstayChargeServiceTests.cs`

**Rezultat:** 57 testnih metoda, odnosno 73 izvršavanja zajedno s data-driven slučajevima. Prolaze 72, a 1 je preskočen s `[Ignore]` jer otkriva bug (vidi poglavlje 6).

---

## 1. Refaktor radi testabilnosti

| Izmjena | Zašto |
|---|---|
| `Services/ISmtpSender.cs` + `Services/SmtpSender.cs`. `PaymentController` mail šalje preko `ISmtpSender`, koji dobija kroz konstruktor. Registracija u `Program.cs`: `AddTransient<ISmtpSender, SmtpSender>()` | Kontroler je ranije sam pravio `new SmtpClient(...)`, koji se ne može zamijeniti mockom, pa bi svaki test slao pravi mail. Sada se u testu `ISmtpSender` zamijeni mockom. `SmtpSender` sadrži isti kod kao ranije, pa se ponašanje aplikacije nije promijenilo. |
| `OverstayChargeService.ProcessExpiredTickets` i `CalculateAdditionalCharge`: `private` je promijenjeno u `internal` | Privatne metode se ne testiraju direktno, a `PrivateObject` ne postoji na .NET 8. S `internal` metodama i `InternalsVisibleTo` testovi ih pozivaju direktno, bez čekanja pozadinske petlje. |
| `ParkirajBa.csproj`: `<InternalsVisibleTo Include="TestProjectTask2" />` | Samo testni assembly vidi `internal` članove. Za ostatak koda metode su i dalje skrivene. |

---

## 2. Spisak testova

Zajednički stereotipi:
- svaki test ima `[TestMethod]`, `[TestProperty("Autor", "Tarik")]` i barem jedan `[TestCategory]`
- klasa ima `[TestClass]`, `[TestInitialize]` (novi objekti i nova baza za svaki test) i `[TestCleanup]` (dispose)
- `PaymentControllerTests` ima i `[ClassInitialize]` (konfiguracija koja se samo čita)

U tabelama su navedeni samo dodatni stereotipi.

### 2.1 PaymentControllerTests (39 metoda)

| Test | Šta provjerava | Dodatni stereotipi |
|---|---|---|
| Konstruktor_NedostajeSenderEmail_BacaInvalidOperationException | bez `EmailSettings:SenderEmail` konstruktor baca izuzetak | `[ExpectedException]` |
| Konstruktor_NedostajeAppPassword_BacaIzuzetakSPorukom | bez `AppPassword` baca izuzetak s tačnom porukom | `Assert.ThrowsException` |
| Checkout_KorisnikNijePrijavljen_PreusmjeravaNaLogin | neprijavljen korisnik ide na Login | |
| Checkout_SesijaBezRezervacije_PreusmjeravaNaObjekte | `fromSession` bez rezervacije u sesiji vodi na Objekti | |
| Checkout_RezervacijaUSesiji_PrikazujeCijenuIzSesije | privremena karta ima cijenu iz sesije (4,5 KM) | |
| Checkout_RezervacijaUSesiji_OznacavaDaJeIzSesije | `ViewData["FromSession"] == true` | |
| Checkout_KartaNePostoji_PreusmjeravaNaRezervacije | nepostojeća karta vodi na Rezervacije | |
| Checkout_SlucajeviIzCsv_PostavljaOznakuDodatneNaplate | `IsAdditionalCharge` odgovara parametru (5 slučajeva iz CSV-a) | `[DynamicData]`, DataDriven |
| Confirm_KorisnikNijePrijavljen_PreusmjeravaNaLogin | neprijavljen korisnik ide na Login | |
| Confirm_SesijaBezRezervacije_PreusmjeravaNaObjekte | prazna sesija vodi na Objekti | |
| Confirm_RezervacijaUSesiji_PrikazujeImeSKartice | `CardName` se prikazuje i za rezervaciju iz sesije | |
| Confirm_KartaNePostoji_PreusmjeravaNaCheckout | nepostojeća karta vodi na Checkout | |
| Confirm_PostojecaKarta_PrikazujeImeSKartice | `CardName` za postojeću kartu (ime i prazan string) | `[DataRow]` ×2 |
| SendConfirmationEmail_KorisnikNijePrijavljen_VracaUnauthorized | vraća `UnauthorizedResult` | |
| SendConfirmationEmail_KartaNePostoji_PreusmjeravaNaCheckout | nepostojeća karta vodi na Checkout | |
| SendConfirmationEmail_RezervacijaIzSesije_SpremaKartuUBazu | nova karta je upisana u bazu | `[Priority(1)]` |
| SendConfirmationEmail_RezervacijaIzSesije_BriseRezervacijuIzSesije | `PendingReservation` se briše iz sesije | |
| SendConfirmationEmail_SesijaIPostojecaKarta_NeSpremaNovuKartu | uz `ticketId ≠ 0` koristi se postojeća karta, ne sesija | |
| SendConfirmationEmail_RedovnoPlacanje_OznacavaKartuPlacenom | `IsPaid == true` | `[Priority(1)]` |
| SendConfirmationEmail_RedovnoPlacanje_GeneriseKodRezervacije | kod je u formatu `PB-XXXXXXXX` | `StringAssert.Matches` |
| SendConfirmationEmail_RedovnoPlacanje_SmanjujeSlobodnaMjesta | broj slobodnih mjesta pada s 10 na 9 | `[Priority(1)]` |
| SendConfirmationEmail_UslovZaMjestoNijeIspunjen_NeMijenjaSlobodnaMjesta | nema smanjenja kad nema mjesta, kad rezervacija još nije počela ili kad je mjesto već uračunato | `[DataRow]` ×3 |
| SendConfirmationEmail_ParkingNePostoji_PreusmjeravaNaSuccess | plaćanje prolazi i kad parking ne postoji | |
| SendConfirmationEmail_UspjesnoPlacanje_ProsljedjujeIdKarte | redirect na Success nosi `ticketId` | |
| SendConfirmationEmail_DodatnaNaplata_DodajeIznosUUkupnoPlaceno | `TotalAdditionalChargesPaid` raste za iznos dodatne naplate | `[Priority(1)]` |
| SendConfirmationEmail_DodatnaNaplata_PonovoAktiviraQrKod | `QrCodeActive == true` nakon plaćanja dodatne naplate | |
| SendConfirmationEmail_UspjesnoPlacanje_SaljeMailJednom | `ISmtpSender.Send` je pozvan tačno jednom | Mock, `Verify(Times.Once)`, `[Priority(1)]`, `[Timeout(2000)]` |
| SendConfirmationEmail_UspjesnoPlacanje_SaljeMailNaAdresuKorisnika | mail ide na email prijavljenog korisnika | Mock, `[Timeout(2000)]` |
| SendConfirmationEmail_UspjesnoPlacanje_KoristiKredencijaleIzKonfiguracije | SMTP kredencijali su iz `EmailSettings` | Mock, `[Timeout(2000)]` |
| SendConfirmationEmail_KartaNePostoji_NeSaljeMail | mail se ne šalje | Mock, `Verify(Times.Never)` |
| SendConfirmationEmail_KartaBezRokaVazenja_IpakSaljeMail | karta bez `ExpiresAt` ne ruši slanje | Mock, `[Timeout(2000)]` |
| SendConfirmationEmail_SmtpBaciIzuzetak_PostavljaPorukuOGresci | kad SMTP baci izuzetak, `TempData["EmailError"]` sadrži poruku | Mock (`Setup().Throws()`), Izuzeci |
| SendConfirmationEmail_SmtpBaciIzuzetak_IpakPreusmjeravaNaSuccess | greška u slanju maila ne poništava plaćanje | Mock, Izuzeci |
| Success_KorisnikNijePrijavljen_PreusmjeravaNaLogin | neprijavljen korisnik ide na Login | |
| Success_PrijavljenKorisnik_PrikazujeKodRezervacije | `ViewData["ReservationCode"]` | |
| GenerateQr_KorisnikNijePrijavljen_VracaUnauthorized | vraća `UnauthorizedResult` | |
| GenerateQr_KartaNePostoji_VracaNotFound | vraća `NotFoundResult` | |
| GenerateQr_KartaBezKodaRezervacije_VracaNotFound | `null` i prazan kod daju NotFound | `[DataRow]` ×2 |
| GenerateQr_IspravnaKarta_VracaPngSliku | vraća `FileContentResult` tipa `image/png` | `[Timeout(2000)]` |

### 2.2 OverstayChargeServiceTests (18 metoda)

| Test | Šta provjerava | Dodatni stereotipi |
|---|---|---|
| CalculateAdditionalCharge_NemaSatneCijene_NeObracunavaNaplatu | bez satne cijene naplata ostaje 0 | |
| CalculateAdditionalCharge_KartaNijeIstekla_NeObracunavaNaplatu | karta koja nije istekla nema naplatu | |
| CalculateAdditionalCharge_VoziloIzasloSParkinga_NeSaljeMail | vozilo koje je izašlo ne dobija mail | Mock, `Verify(Times.Never)` |
| CalculateAdditionalCharge_PrekoracenjeIzJson_ObracunavaOcekivanuNaplatu | prekoračenje zaokruženo naviše na sate, pomnoženo cijenom (6 slučajeva iz JSON-a) | `[DynamicData]`, DataDriven, `[Priority(1)]` |
| CalculateAdditionalCharge_ViseSatnihCijena_KoristiNajnovijuCijenu | koristi se najnovija satna cijena (`validFrom`) | |
| CalculateAdditionalCharge_Prekoracenje_DeaktiviraQrKod | `QrCodeActive == false` | |
| CalculateAdditionalCharge_NaplataVecObracunata_NeDeaktiviraQrKod | ista naplata se ne obračunava ponovo | |
| CalculateAdditionalCharge_NeproduzenaKartaPrekoracena2_5h_ObracunavaNaplatu | očekivano ponašanje za neproduženu kartu (3 h × 2 KM = 6 KM) | `[Ignore]` (bug), Bug |
| CalculateAdditionalCharge_MailNijePoslan_SaljeMailKorisnikuJednom | `IEmailSender.SendEmailAsync` je pozvan jednom, na email korisnika | Mock, `Verify(Times.Once)`, `[Priority(1)]`, `[Timeout(2000)]` |
| CalculateAdditionalCharge_MailNijePoslan_OznacavaDaJeMailPoslan | `OverstayEmailSent == true` | |
| CalculateAdditionalCharge_MailVecPoslan_NeSaljeNoviMail | drugi mail se ne šalje | Mock, `Verify(Times.Never)` |
| CalculateAdditionalCharge_KartaBezParkinga_MailSadrziZadaniNazivParkinga | mail sadrži zadani naziv „ParkirajBa Parking“ | Mock (`It.Is<string>`) |
| CalculateAdditionalCharge_KorisnikBezImena_MailOslovljavaKorisnika | mail sadrži zadano obraćanje „korisnik“ | Mock (`It.Is<string>`) |
| CalculateAdditionalCharge_KartaBezRokaVazenja_BacaInvalidOperationException | karta bez `ExpiresAt` baca izuzetak | `Assert.ThrowsExceptionAsync` |
| ProcessExpiredTickets_IsteklaKartaNaParkingu_SaljeMail | istekla karta vozila na parkingu dobija mail | Mock, `[Priority(1)]` |
| ProcessExpiredTickets_IsteklaKartaNaParkingu_SpremaNaplatuUBazu | naplata od 6 KM je upisana u bazu | |
| ProcessExpiredTickets_KartaNijeZaObradu_NeSaljeMail | ne obrađuju se karte vozila koja nisu ušla ili su izašla, otkazane karte ni karte koje nisu istekle | `[DataRow]` ×4, Mock |
| ExecuteAsync_PokretanjeIZaustavljanje_ObradiIstekleKarte | `StartAsync`/`StopAsync`: prva iteracija pozadinske petlje upiše naplatu | `[Timeout(5000)]` |

### 2.3 Modeli

`PaymentViewModel`, `PricingCreateDto` i `Pricing` imaju samo propertije, a `Ticket` samo propertije sa zadanim vrijednostima. Nemaju logiku, pa nisu pisani posebni testovi za getere i setere. Alat za obuhvat ne broji auto-propertije (`SkipAutoProps`). Konstruktori `Ticket` i `Pricing` se izvršavaju u ostalim testovima. `PaymentViewModel` i `PricingCreateDto` nemaju izvršnih linija, pa se ne pojavljuju u izvještaju o obuhvatu. `PaymentViewModel` se nigdje u aplikaciji ne koristi.

---

## 3. Data-driven testovi

Podaci su u fajlovima u `TestData/` (kopiraju se uz testove). Pomoćna metoda ih čita i preko `[DynamicData]` prosljeđuje testu, pa sam test nema logike. `[DataSource]` ne radi na .NET 8.

| Fajl | Format | Slučajeva | Test | Funkcionalnost |
|---|---|---|---|---|
| `TestData/checkout_slucajevi.csv` | CSV (`;`, zaglavlje `ticketId;additionalCharge`) | 5 | Checkout_SlucajeviIzCsv_PostavljaOznakuDodatneNaplate | Checkout za različite karte i oznake dodatne naplate. Pogled mora dobiti tačnu vrijednost `IsAdditionalCharge`. |
| `TestData/prekoracenja.json` | JSON (niz `{ satiPrekoracenja, cijenaPoSatu, ocekivanaNaplata }`) | 6 | CalculateAdditionalCharge_PrekoracenjeIzJson_ObracunavaOcekivanuNaplatu | Obračun dodatne naplate: sati prekoračenja se zaokružuju naviše (`Math.Ceiling`) i množe satnom cijenom, npr. 0,25 h → 1 h, 5,75 h × 3 KM → 18 KM. |

Inline data-driven testovi (`[DataRow]`): Confirm (2), SendConfirmationEmail za uslov smanjenja mjesta (3), GenerateQr za prazan kod (2), ProcessExpiredTickets za karte koje se ne obrađuju (4).

Vremena prekoračenja su namjerno necijeli sati (0,25; 2,5; 5,75…). Između postavljanja `ExpiresAt` i poziva `DateTime.Now` u servisu prođe par milisekundi, pa bi kod cijelog broja (npr. 3 h) `Math.Ceiling` dao 4.

---

## 4. Zamjenski objekti

| Šta je zamijenjeno | Čime | Vrsta | Zašto |
|---|---|---|---|
| SQL Server baza (`ApplicationDbContext`) | EF Core InMemory, `Pomocne/TestBaza.Nova()`, za svaki test nova baza s imenom iz `Guid` | Fake | Testovi ne zavise od prave baze, brzi su i neovisni jedan o drugom. |
| `ISmtpSender` (slanje maila preko Gmail SMTP-a) | `Mock<ISmtpSender>` | Mock (`Verify`) i stub (`Setup().Throws()`) | Ne šalje se pravi mail. Provjerava se da je mail poslan jednom ili nijednom, s ispravnom adresom i kredencijalima, te ponašanje kad SMTP baci izuzetak. |
| `IEmailSender` (mail o prekoračenju) | `Mock<IEmailSender>` | Mock (`Verify`, `It.Is<string>` nad HTML sadržajem) | Provjerava se da li se mail šalje i šta sadrži, bez slanja maila. |
| `IParkingRepository` | `Mock<IParkingRepository>` | Stub (`Setup().ReturnsAsync()`) | Kontroler se testira izolovano od repozitorija, koji je kod drugog člana tima. Repozitorij vraća unaprijed zadanu kartu ili parking. |
| `UserManager<ApplicationUser>` | `Mock<UserManager>` napravljen preko `Mock<IUserStore<ApplicationUser>>` | Stub | `GetUserAsync` vraća prijavljenog korisnika ili `null` (neprijavljen korisnik) bez Identity baze. |
| HTTP sesija (`ISession`) | `Pomocne/LaznaSesija` (Dictionary u memoriji) | Fake | Kontroler čita i briše `PendingReservation` iz sesije. Nema pravog HTTP zahtjeva. |
| `TempData` | `TempDataDictionary` + `Mock.Of<ITempDataProvider>()` | Stub | Kontroler upisuje `EmailError` u TempData. |
| DI kontejner (`IServiceScopeFactory`) | pravi `ServiceCollection` s InMemory bazom i mock `IEmailSender` | Fake okruženje | `OverstayChargeService` sam pravi scope i iz njega uzima bazu i mail servis. |

---

## 5. Obuhvat koda

Komande:
```
dotnet test TestProjectTask2/TestProjectTask2 --collect:"XPlat Code Coverage"
dotnet tool restore
dotnet reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:Html
```
`coverage.runsettings` se automatski koristi preko `<RunSettingsFilePath>` u csproj-u. Isključuje migracije, poglede (`AspNetCoreGeneratedDocument`), klasu `Program` i auto-propertije.

| Klasa | Linije | Grane |
|---|---|---|
| `ParkirajBa.Controllers.PaymentController` | **100%** (181/181) | **98,9%** (91/92) |
| `ParkirajBa.Services.OverstayChargeService` | **98,3%** (121/123) | **88,4%** (23/26) |
| `ParkirajBa.Models.Ticket` | **100%** (2/2) | – |
| `ParkirajBa.Models.Pricing` | **100%** (2/2) | – |
| `ParkirajBa.Models.PaymentViewModel`, `PricingCreateDto` | nema izvršnih linija (samo auto-propertiji) | – |

Nepokriveni dijelovi i razlozi:
- `PaymentController`, grana `user.FullName ?? cardName`: `FullName` je izračunati property koji nikad nije `null`, pa je desna strana mrtav kod.
- `OverstayChargeService.ExecuteAsync`, povratak na početak `while` petlje i izlaz iz nje (2 linije, 1 grana): do toga se dolazi tek kad `Task.Delay(1 minuta)` regularno istekne. Kad se servis zaustavi, `Task.Delay` baci `OperationCanceledException` i petlja nikad ne dođe do kraja tijela. Test bi morao trajati više od minute.
- `OverstayChargeService`, grana `ExpiresAt.HasValue ? … : "—"`: kad je `ExpiresAt == null`, metoda već ranije baci izuzetak na `ExpiresAt!.Value`, pa je `"—"` mrtav kod.
- `OverstayChargeService`, grana `ApplicationUser?.FirstName` kad je `ApplicationUser == null`: odmah nakon nje `ticket.ApplicationUser.Email!` baca `NullReferenceException` (bug 2).

`SmtpSender` (0%) nije u spisku mojih klasa za testiranje. To je tanak omotač oko `SmtpClient`, koji se ne može unit testirati bez mreže, pa se zamjenjuje mockom.

---

## 6. Pronađeni bugovi

1. **Neproduzena karta nikad ne dobije dodatnu naplatu** (`OverstayChargeService.CalculateAdditionalCharge`, model `Ticket`). `Ticket.AdditionalChargePaid` je po defaultu `true`, a naplata se upisuje samo ako je `!AdditionalChargePaid`. Za kartu koja nije produžena `AdditionalCharge` ostaje 0, a mail s iznosom naplate se ipak pošalje. Korisnik zatim ne može platiti dodatnu naplatu (`SendConfirmationEmail` je tretira kao redovno plaćanje) ni izaći s parkinga (QR kontroler javlja „Rezervacija istekla“). Test `CalculateAdditionalCharge_NeproduzenaKartaPrekoracena2_5h_ObracunavaNaplatu` opisuje očekivano ponašanje i pada (očekivano 6, dobijeno 0). Označen je s `[Ignore]` dok se bug ne popravi.
2. **NullReferenceException kad korisnik nije učitan**: u `CalculateAdditionalCharge` se poziva `ticket.ApplicationUser.Email!` bez provjere na `null`, iako se dvije linije iznad koristi `?.`.
3. **Ponovni mail nakon plaćene naknade**: `SendConfirmationEmail` postavlja `OverstayEmailSent = false` i `AdditionalChargePaid = true`. Ako vozilo i dalje ostane na parkingu, servis ponovo šalje mail, ali naplatu ne upisuje (`AdditionalChargePaid == true`). Osim toga, prekoračenje se uvijek računa od prvobitnog `ExpiresAt`, pa uključuje i već plaćene sate.
4. **Pozadinski servis nema obradu izuzetaka**: `ExecuteAsync` nema `try/catch`. Izuzetak u `ProcessExpiredTickets` (npr. bug 2) u .NET 8 po defaultu zaustavlja cijelu aplikaciju.
5. **Mrtav kod**: `user.FullName ?? cardName` (PaymentController), `ExpiresAt.HasValue ? … : "—"` i provjera `if (ticket.ExitedParking) return;` (OverstayChargeService), koju upit u `ProcessExpiredTickets` već filtrira.
6. **Sigurnost**: Gmail App Password je commitan u `appsettings.json`. Trebalo bi ga prebaciti u User Secrets ili varijable okruženja i promijeniti lozinku.
