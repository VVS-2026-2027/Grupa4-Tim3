# Izvještaj o unit testovima i zamjenskim objektima

## 1. Opšti podaci o testiranju

* **Projekat:** `ParkirajBa` (ASP.NET Core MVC)
* **Testni projekat:** `TestProjectTask2`
* **Autor:** Tarik Beganović
* **Testirane klase:** `PaymentController`, `OverstayChargeService`, `Ticket`, `Pricing`, `PricingCreateDto`, `PaymentViewModel`
* **Alati i biblioteke:** MSTest, Moq, Microsoft.EntityFrameworkCore.InMemory, coverlet, ReportGenerator
* **Rezultati izvršavanja:** 3 testne klase, **31 testna metoda** (**50 testnih slučajeva**):
  * **49 uspješnih (Passed)**
  * **0 neuspješnih (Failed)**
  * **1 preskočen (`[Ignore]`)**, koji dokumentuje pronađeni defekt (poglavlje 7)

## 2. Spisak testnih klasa i testova

### 2.1. `PaymentControllerTests` (`TestProjectTask2.Controllers`) — 24 metode

 1. `Konstruktor_NedostajeSenderEmail_BacaInvalidOperationException` *(Izuzetak)*
 2. `Konstruktor_NedostajeAppPassword_BacaIzuzetakSPorukom` *(Izuzetak)*
 3. `Checkout_KorisnikNijePrijavljen_PreusmjeravaNaLogin`
 4. `Checkout_SesijaBezRezervacije_PreusmjeravaNaObjekte`
 5. `Checkout_RezervacijaUSesiji_PrikazujeCijenuIzSesije`
 6. `Checkout_KartaNePostoji_PreusmjeravaNaRezervacije`
 7. `Checkout_SlucajeviIzCsv_PostavljaOznakuDodatneNaplate` *(Data-Driven: CSV)*
 8. `Confirm_KorisnikNijePrijavljen_PreusmjeravaNaLogin`
 9. `Confirm_SesijaBezRezervacije_PreusmjeravaNaObjekte`
10. `Confirm_RezervacijaUSesiji_PrikazujeImeSKartice`
11. `Confirm_KartaNePostoji_PreusmjeravaNaCheckout`
12. `Confirm_PostojecaKarta_PrikazujeImeSKartice` *(DataRow)*
13. `SendConfirmationEmail_KorisnikNijePrijavljen_VracaUnauthorized`
14. `SendConfirmationEmail_KartaNePostoji_PreusmjeravaNaCheckout`
15. `SendConfirmationEmail_RezervacijaIzSesije_SpremaKartuUBazu`
16. `SendConfirmationEmail_RedovnoPlacanje_SmanjujeSlobodnaMjesta`
17. `SendConfirmationEmail_DodatnaNaplata_DodajeIznosUUkupnoPlaceno`
18. `SendConfirmationEmail_UspjesnoPlacanje_SaljeMailJednom` *(Mock)*
19. `SendConfirmationEmail_SmtpBaciIzuzetak_PostavljaPorukuOGresci` *(Mock, Izuzetak)*
20. `Success_KorisnikNijePrijavljen_PreusmjeravaNaLogin`
21. `Success_PrijavljenKorisnik_PrikazujeKodRezervacije`
22. `GenerateQr_KorisnikNijePrijavljen_VracaUnauthorized`
23. `GenerateQr_KartaBezKodaRezervacije_VracaNotFound` *(DataRow)*
24. `GenerateQr_IspravnaKarta_VracaPngSliku`

### 2.2. `OverstayChargeServiceTests` (`TestProjectTask2.Services`) — 6 metoda

1. `CalculateAdditionalCharge_NemaSatneCijene_NeObracunavaNaplatu`
2. `CalculateAdditionalCharge_KartaNijeIstekla_NeObracunavaNaplatu`
3. `CalculateAdditionalCharge_VoziloIzasloSParkinga_NeSaljeMail` *(Mock)*
4. `CalculateAdditionalCharge_PrekoracenjeIzJson_ObracunavaOcekivanuNaplatu` *(Data-Driven: JSON)*
5. `CalculateAdditionalCharge_NeproduzenaKartaPrekoracena2_5h_ObracunavaNaplatu` *(Preskočeno – `[Ignore]`, defekt)*
6. `ExecuteAsync_PokretanjeIZaustavljanje_ObradiIstekleKarte` *(Mock)*

### 2.3. `TicketTests` (`TestProjectTask2.Models`) — 1 metoda

1. `NovaKarta_LogickoSvojstvo_ImaZadanuVrijednost` *(DataRow, 9 slučajeva)*

## 3. Korišteni MSTest stereotipi i organizacija testova

Testovi su strukturirani po AAA obrascu (*Arrange – Act – Assert*), bez programske logike u testnim metodama i uz potpunu izolaciju svakog testa. Svaki test dobija novu bazu i nove mockove. Korišteni su sljedeći MSTest atributi i metode:

| Stereotip | Gdje se koristi | Svrha |
| --- | --- | --- |
| `[TestClass]` | Sve 3 testne klase | Označava klasu sa unit testovima. |
| `[TestMethod]` | Svih 31 testnih metoda | Označava pojedinačnu testnu metodu. |
| `[TestInitialize]` / `[TestCleanup]` | Sve 3 testne klase | Prije svakog testa pravi novu InMemory bazu, mockove i kontroler/servis, a nakon testa ih oslobađa (`Dispose`). |
| `[ClassInitialize]` | `PaymentControllerTests` | Jednokratno pravljenje konfiguracije koju svi testovi samo čitaju. |
| `[TestCategory("...")]` | Sve testne metode | Grupisanje testova (`Checkout`, `Confirm`, `Placanje`, `Success`, `QR`, `Obracun`, `Obrada`, `Modeli`, `DataDriven`, `Mock`, `Izuzeci`, `Bug`). |
| `[TestProperty("Autor", "Tarik")]` | Sve testne metode | Dodavanje metapodataka o autoru testova. |
| `[Priority(1)]` | Testovi plaćanja i obračuna naplate | Označavanje najvažnijih testova. |
| `[Timeout(2000)]` / `[Timeout(5000)]` | Testovi slanja maila, generisanja QR koda i pozadinskog servisa | Ograničavanje trajanja testa. |
| `[DataRow(...)]` | 3 parametrizovane metode | Inline data-driven testiranje više ulaznih vrijednosti. |
| `[DynamicData(...)]` | 2 data-driven metode | Učitavanje testnih podataka iz eksternih CSV i JSON fajlova. |
| `[Ignore("...")]` | `OverstayChargeServiceTests` | Isključivanje testa koji dokumentuje pronađeni defekt. |
| `Assert.ThrowsExactly<T>` | `PaymentControllerTests` | Provjera da nedostajuća konfiguracija baca tačno `InvalidOperationException` s očekivanom porukom. |

## 4. Data-driven testiranje (CSV, JSON i DataRow)

Za data-driven testiranje korišteni su podaci iz foldera `TestData` u CSV i JSON formatu, te inline podaci kroz `[DataRow]`:

| Format | Izvor | Testna metoda | Testirana funkcionalnost |
| --- | --- | --- | --- |
| **CSV** | `checkout_slucajevi.csv` (5 slučajeva) | `PaymentControllerTests.Checkout_SlucajeviIzCsv_PostavljaOznakuDodatneNaplate` | Prikaz stranice plaćanja: kontroler mora view-u proslijediti da li korisnik plaća rezervaciju ili dodatnu naplatu za prekoračenje. |
| **JSON** | `prekoracenja.json` (6 slučajeva) | `OverstayChargeServiceTests.CalculateAdditionalCharge_PrekoracenjeIzJson_ObracunavaOcekivanuNaplatu` | Obračun dodatne naplate za prekoračenje: `⌈sati prekoračenja⌉ × satna cijena`, započeti sat se naplaćuje cijeli (npr. 0.25 h × 2 KM = 2 KM, 2.5 h × 1.5 KM = 4.5 KM). |
| **DataRow** | 2 slučaja | `PaymentControllerTests.Confirm_PostojecaKarta_PrikazujeImeSKartice` | Prosljeđivanje imena s kartice na stranicu potvrde, uključujući prazno ime. |
| **DataRow** | 2 slučaja | `PaymentControllerTests.GenerateQr_KartaBezKodaRezervacije_VracaNotFound` | QR kod se ne generiše za kartu bez koda rezervacije (`null` i `""`). |
| **DataRow** | 9 slučajeva | `TicketTests.NovaKarta_LogickoSvojstvo_ImaZadanuVrijednost` | Zadane vrijednosti logičkih svojstava nove karte (`IsPaid`, `QrCodeActive`, `AdditionalChargePaid`…), na kojima se zasniva logika plaćanja i naplate. |

U JSON slučajevima namjerno su korišteni necijeli sati. Između pravljenja karte i obračuna prođe nekoliko milisekundi, pa bi kod cijelog broja `Math.Ceiling` mogao preskočiti na sljedeći sat i test bi bio nestabilan.

## 5. Zamjenski objekti (Test Doubles)

Kako bi se testovi izvršavali brzo i bez zavisnosti od aktivne baze podataka, SMTP servera i ostalih modula aplikacije, korišteni su sljedeći zamjenski objekti:

* **Fake baza podataka (`TestBaza`, EF Core InMemory):** U `PaymentControllerTests` i `OverstayChargeServiceTests` svaki test dobija novu, praznu bazu u memoriji (ime = `Guid`). Tako se provjerava upis karte i smanjenje slobodnih mjesta bez SQL Servera.
* **Fake sesija (`LaznaSesija`):** Ručno napisana implementacija `ISession` koja podatke čuva u `Dictionary`. `DefaultHttpContext` nema sesiju, a `PaymentController` iz nje čita rezervaciju koja čeka plaćanje.
* **Mock repozitorija (`Mock<IParkingRepository>`):** Test sam određuje da li karta ili parking postoje (`Setup(GetTicketByIdAsync)`, `Setup(GetByIdAsync)`). Kad nema `Setup`-a, vraća se `null`, što simulira nepostojeću kartu.
* **Mock SMTP servisa (`Mock<ISmtpSender>`):**
  * sprječava slanje stvarnih emailova preko Gmail SMTP-a;
  * provjerava da se potvrda plaćanja šalje tačno jednom (`Verify(..., Times.Once)`);
  * simulira pad servera (`Throws(new SmtpException(...))`), nakon čega plaćanje ostaje potvrđeno, a korisnik dobija poruku o grešci.
* **Mock email servisa (`Mock<IEmailSender>`):**
  * u `OverstayChargeServiceTests` provjerava da se mail o prekoračenju ne šalje vozilu koje je izašlo (`Times.Never`);
  * kroz `Callback` i `TaskCompletionSource` javlja testu da je pozadinski servis završio prvu obradu, pa se ne čeka fiksno vrijeme.
* **Mock Identity servisa (`Mock<UserManager<ApplicationUser>>`):** `GetUserAsync` vraća prijavljenog korisnika ili `null`, čime se simulira neprijavljen korisnik.
* **Stub konfiguracije i HTTP objekata (`ConfigurationBuilder.AddInMemoryCollection`, `ITempDataProvider`):** Omogućavaju testiranje konfiguracije kojoj nedostaje vrijednost i rad `TempData` bez pravog providera.
* **Pravi DI kontejner (`ServiceCollection`):** `OverstayChargeService` sam pravi scope preko `IServiceScopeFactory`, pa mu se daje kontejner u kojem su registrovani fake baza i mock `IEmailSender`.

Da bi se ovi zamjenski objekti mogli koristiti, produkcijski kod je malo izmijenjen, bez promjene ponašanja:

* `PaymentController` je direktno pravio `new SmtpClient(...)`, pa su uvedeni `ISmtpSender` i `SmtpSender`, registrovani u `Program.cs`.
* Metode `ProcessExpiredTickets` i `CalculateAdditionalCharge` u `OverstayChargeService` su promijenjene iz `private` u `internal`, uz `InternalsVisibleTo("TestProjectTask2")`.

## 6. Pokrivenost koda (coverlet + ReportGenerator)

Mjerenje pokrivenosti koda izvršeno je alatom **coverlet** (`XPlat Code Coverage`), a rezultati su obrađeni alatom **ReportGenerator**. Mjereno je samo izvršavanjem mojih testova. Postavke su u `coverage.runsettings`: izuzete su migracije, pogledi i `Program`, a auto-propertiji se ne broje.

| Produkcijska klasa | Izvršive linije | Pokrivenost linija | Pokrivenost grananja |
| --- | --- | --- | --- |
| `ParkirajBa.Controllers.PaymentController` | 181 / 181 | **100%** | 94.5% (87 / 92) |
| `ParkirajBa.Services.OverstayChargeService` | 121 / 123 | **98.3%** | 73% (19 / 26) |
| `ParkirajBa.Models.Ticket` | 2 / 2 | **100%** | — |
| `ParkirajBa.Models.Pricing` | 2 / 2 | **100%** | — |
| `ParkirajBa.Models.PricingCreateDto` | 0 / 0 | N/A | — |
| `ParkirajBa.Models.PaymentViewModel` | 0 / 0 | N/A | — |
| **Ukupno (testirane klase)** | **306 / 308** | **99.3%** | **89.8% (106 / 118)** |

Napomene:

* **`PricingCreateDto` i `PaymentViewModel`** sadrže samo auto-propertije bez logike, pa nemaju nijednu izvršivu liniju.
* **`Pricing`** ima samo prazan konstruktor i pokriven je indirektno kroz `OverstayChargeServiceTests`, koji u bazu upisuju satne cijene.
* **`PaymentController`:** nepokrivene grane su rezervne vrijednosti za `null` podatke u tekstu maila (parking bez imena, korisnik bez punog imena, karta bez datuma isteka) i slučaj nepostojeće karte u `GenerateQr`.
* **`OverstayChargeService`:**
  * nepokrivene su rezervne vrijednosti za `null` podatke u mailu i ponovni obračun istog iznosa;
  * nepokriven je i normalan izlaz iz `while` petlje, koji je nedostižan jer se servis zaustavlja otkazivanjem unutar `Task.Delay`;
  * provjera `ExpiresAt.HasValue` u liniji 102 je mrtav kod, jer linija 76 već koristi `ExpiresAt!.Value`.

## 7. Pronađeni defekt

Test `CalculateAdditionalCharge_NeproduzenaKartaPrekoracena2_5h_ObracunavaNaplatu` je označen sa `[Ignore]` i `[TestCategory("Bug")]` jer otkriva defekt:

* `Ticket.AdditionalChargePaid` ima zadanu vrijednost `true`, a `OverstayChargeService` obračunava naplatu samo kad je ta vrijednost `false`.
* Zato se karti koja nikad nije produžena **nikad ne obračuna dodatna naplata**: `AdditionalCharge` ostaje 0, a QR kod ostaje aktivan. Korisniku se ipak pošalje mail da mora platiti naknadu.
* **Primjer:** za prekoračenje od 2.5 h uz cijenu od 2 KM/h očekivana naplata je 6 KM, a stvarna je 0 KM.

**Prijedlog ispravke:** zadana vrijednost `AdditionalChargePaid = false`. Nakon ispravke treba ukloniti `[Ignore]`.
