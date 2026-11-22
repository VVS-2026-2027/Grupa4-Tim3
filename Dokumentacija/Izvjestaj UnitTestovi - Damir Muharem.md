# Izvještaj o unit testovima i zamjenskim objektima

## 1. Opšti podaci o testiranju

* **Projekat:** `ParkirajBa` (ASP.NET Core MVC)
* **Testni projekat:** `TestProjectTask2`
* **Autor:** Damir Muharem
* **Alati i biblioteke:** MSTest, Moq, Microsoft.EntityFrameworkCore.InMemory
* **Rezultati izvršavanja:** 8 testnih klasa, **89 testnih metoda** (**132 testna slučaja**):
  * **130 uspješnih (Passed)**
  * **0 neuspješnih (Failed)**
  * **2 preskočena (`[Ignore]`)**

## 2. Spisak testnih klasa i testova

### 2.1. `AdminControllerTests` (`ParkirajBa.Tests.Controllers`) — 35 metoda

 1. `Dashboard_PostojeciPodaci_VracaViewSaIspravnomStatistikom`
 2. `Users_FilterIzXml_VracaOcekivaniBrojKorisnika` *(Data-Driven: XML)*
 3. `ToggleLock_NepostojeciKorisnik_VracaNotFound`
 4. `ToggleLock_ZakljucanKorisnik_OtkljucavaIPreusmjerava`
 5. `ToggleLock_OtkljucanKorisnik_ZakljucavaIPreusmjerava`
 6. `ChangeRole_NepostojeciKorisnik_VracaNotFound`
 7. `ChangeRole_UlogaIzCsv_MijenjaUloguIPostavljaPoruku` *(Data-Driven: CSV)*
 8. `DeleteUser_NepostojeciKorisnik_VracaNotFound`
 9. `DeleteUser_PostojeciKorisnik_BriseKarteIKorisnika`
10. `Reports_BezParametara_KoristiTrenutnuGodinuIMjesec`
11. `Reports_ZadaniMjesecIGodina_RacunaPrihodePoParkinguIDanu`
12. `Parkings_PozivAkcije_VracaViewSaListomParkinga`
13. `ParkingDetails_NepostojeciParking_PreusmjeravaNaParkingsSaGreskom`
14. `ParkingDetails_PostojeciParking_VracaViewSaCjenovnikomISlikom`
15. `UploadParkingImage_PraznaSlika_PostavljaGreskuIPreusmjerava`
16. `UploadParkingImage_ValidnaSlika_ZamjenjujePrimarnuSliku`
17. `DeleteParking_NepostojeciParking_VracaNotFound`
18. `DeleteParking_PostojeciParking_BriseParkingIPripadajuceKarte`
19. `Reservations_StatusIzJson_VracaFiltriraneRezervacije` *(Data-Driven: JSON)*
20. `ReservationDetails_NepostojecaRezervacija_PreusmjeravaSaGreskom`
21. `ReservationDetails_PostojecaRezervacija_VracaDetaljeView`
22. `ApproveParking_NepostojeciParking_VracaNotFound`
23. `ApproveParking_RazlicitIshodOdobravanja_PostavljaOdgovarajuciTempData` *(DataRow)*
24. `DeleteReservation_NepostojecaRezervacija_VracaNotFound`
25. `DeleteReservation_PostojecaRezervacija_BriseKartuIOslobadjaMjesto`
26. `Requests_PostojeciZahtjevi_VracaViewSaMapiranimParkinzima`
27. `ApproveRequest_NepostojeciZahtjev_PreusmjeravaSaGreskom`
28. `ApproveRequest_NepostojeciParking_PreusmjeravaSaGreskom`
29. `ApproveRequest_NeuspjesnoOdobravanje_PreusmjeravaSaGreskom`
30. `ApproveRequest_ValidanZahtjev_OdobravaParkingIBriseZahtjev`
31. `RejectRequest_NepostojeciZahtjev_PreusmjeravaSaGreskom`
32. `RejectRequest_NepostojeciParking_PreusmjeravaSaGreskom`
33. `RejectRequest_ValidanZahtjev_BriseSlikeCjenovnikParkingIZahtjev`
34. `AdminUserViewModel_IsLocked_ProvjeravaStatusZakljucavanja` *(DataRow)*
35. `Dashboard_IgnorisaniPrimjerTesta_DemonstriraStereotip` *(Preskočeno – `[Ignore]`)*

### 2.2. `UserControllerTests` (`ParkirajBa.Tests.Controllers`) — 30 metoda

 1. `Logout_PozivAkcije_OdjavljujeKorisnikaIPreusmjeravaNaPocetnu`
 2. `LoginGet_PozivAkcije_VracaViewISakrivaHeader`
 3. `LoginPost_PraznaPoljaIzCsv_VracaViewSaPorukomGreske` *(Data-Driven: CSV)*
 4. `LoginPost_NepostojeciKorisnik_VracaViewSaGreskom`
 5. `LoginPost_NepotvrdjenEmail_OmogucavaPonovnoSlanjePotvrde`
 6. `LoginPost_ZakljucanKorisnik_VracaViewSaPorukomOZakljucavanju`
 7. `LoginPost_ValidanObicniKorisnik_PreusmjeravaNaHomeIndex`
 8. `LoginPost_ValidanAdminKorisnik_PreusmjeravaNaAdminDashboard`
 9. `LoginPost_PogresnaLozinka_VracaViewSaGreskom`
10. `ResendConfirmation_NepostojeciKorisnik_PreusmjeravaNaLogin`
11. `ResendConfirmation_PostojeciKorisnik_SaljeEmailIVracaViewPotvrde`
12. `RegisterGet_PozivAkcije_VracaViewISakrivaHeader`
13. `RegisterPost_NeispravanModelState_VracaViewSaPrvomGreskom`
14. `RegisterPost_EmailVecPostoji_VracaViewSaGreskom`
15. `RegisterPost_NeuspjesnoKreiranjeKorisnika_VracaViewSaOpisomGreske`
16. `RegisterPost_ValidniPodaci_KreiraKorisnikaDodjeljujeUloguISaljeEmail`
17. `ConfirmEmail_NedostajeParametar_PreusmjeravaNaLogin` *(DataRow)*
18. `ConfirmEmail_NepostojeciKorisnik_PreusmjeravaNaLogin`
19. `ConfirmEmail_ValidanToken_PrijavljujeKorisnikaIVracaEmailConfirmed`
20. `ConfirmEmail_NevalidanToken_VracaLoginViewSaGreskom`
21. `ProfileGet_NeprijavljenKorisnik_PreusmjeravaNaLogin`
22. `ProfileGet_PrijavljenKorisnik_VracaViewSaKorisnikom`
23. `ProfilePost_NeprijavljenKorisnik_PreusmjeravaNaLogin`
24. `ProfilePost_NeispravnoImeIliPrezime_VracaViewSaPorukomGreske` *(DataRow)*
25. `ProfilePost_NeuspjesnoAzuriranjeBaze_VracaViewSaGreskom`
26. `ProfilePost_ValidnoImeBezPromjeneLozinke_AzuriraProfilIPreusmjerava`
27. `ProfilePost_NeispravnaPoljaLozinkeIzJson_VracaViewSaGreskom` *(Data-Driven: JSON)*
28. `ProfilePost_ChangePasswordNeuspjesan_VracaOdgovarajucuGresku` *(DataRow)*
29. `ProfilePost_ValidnaPromjenaLozinke_MijenjaLozinkuIPreusmjerava`
30. `UserController_IgnorisaniTest_DemonstriraStereotip` *(Preskočeno – `[Ignore]`)*

### 2.3. `ApplicationDbContextTests` (`ParkirajBa.Tests.Data`) — 6 metoda

1. `DbSets_InicijalizacijaKonteksta_SviDbSetoviSuKreirani`
2. `OnModelCreating_MapiranjeEntiteta_PostavljaIspravanNazivTabele` *(DataRow)*
3. `OnModelCreating_TicketDecimalPolja_PostavljaTipDecimal18_2` *(DataRow)*
4. `OnModelCreating_RelacijaPricingParkingObject_PostavljaCascadeBrisanje`
5. `SaveChangesAsync_BrisanjeParkinga_KaskadnoBrisePovezaneCjenovnike`
6. `SaveChangesAsync_DodavanjeZahtjevaIParkingSlike_UspjesnoPohranjujePodatke`

### 2.4. Testovi modela i validacije (`ParkirajBa.Tests.Models`) — 18 metoda

* **`ApplicationUserTests` (6 metoda):**
  1. `FullName_PostavljenoImeIPrezime_VracaPunoIme`
  2. `CreatedAt_KreiranjeKorisnika_PostavljaUtcVrijeme`
  3. `FirstName_ProvjeraPodatakaIzCsv_VracaOcekivanuValidnost` *(Data-Driven: CSV)*
  4. `FirstName_DuzeOdMaksimalneDuzine_NeProlaziValidaciju`
  5. `LastName_ProvjeraFormata_VracaOcekivanuValidnost` *(DataRow)*
  6. `FirstName_PraznoIme_BacaValidationException` *(Izuzetak)*

* **`ValidationModelsTests` (9 metoda):**
  1. `ExtendedRegisterViewModel_IspravniPodaci_ProlazeValidaciju`
  2. `ExtendedRegisterViewModel_PraznaObaveznaPolja_NeProlazeValidaciju`
  3. `ReservationViewModel_RegistracijeIzJson_NormalizujeIValidira` *(Data-Driven: JSON)*
  4. `ReservationViewModel_IdIKomentar_PostavljajuSeIVracaju`
  5. `ReservationViewModel_PocetakUProslosti_VracaGreskuZaPocetak`
  6. `ReservationViewModel_KrajNijeNakonPocetka_VracaGreskuZaKraj` *(DataRow)*
  7. `CardPaymentViewModel_BrojeviIzJson_ProvjeravaLuhn` *(Data-Driven: JSON)*
  8. `CardPaymentViewModel_IsteklaKartica_VracaGreskuZaIstek`
  9. `CardPaymentViewModel_NeispravanFormatIsteka_PadaValidacijaAtributa`

* **`AdminProfileTests` (1 metoda):**
  1. `Properties_PostavljanjeVrijednosti_VracajuPostavljeneVrijednosti`

* **`OwnerProfileTests` (1 metoda):**
  1. `Properties_PostavljanjeVrijednosti_VracajuPostavljeneVrijednosti`

* **`ReplacementObjectTests` (1 metoda):**
  1. `Parkings_MockiranParkingRepository_VracaNjegoveParkinge`

## 3. Korišteni MSTest stereotipi i organizacija testova

Testovi su strukturirani po AAA obrascu (*Arrange – Act – Assert*), bez programske logike u testnim metodama i uz potpunu izolaciju svakog testa. Korišteni su sljedeći MSTest atributi i metode:

| Stereotip | Gdje se koristi | Svrha |
| --- | --- | --- |
| `[TestClass]` | Svih 8 testnih klasa | Označava klasu sa unit testovima. |
| `[TestMethod]` | Svih 89 testnih metoda | Označava pojedinačnu testnu metodu. |
| `[TestInitialize]` / `[TestCleanup]` | `AdminControllerTests`, `UserControllerTests`, `ApplicationDbContextTests` | Priprema izoliranu okolinu prije svakog testa i oslobađa resurse nakon izvršavanja. |
| `[ClassInitialize]` / `[ClassCleanup]` | `AdminControllerTests`, `UserControllerTests`, `ApplicationDbContextTests` | Jednokratna inicijalizacija i čišćenje na nivou klase. |
| `[TestCategory("...")]` | Sve klase i odabrane metode | Grupisanje testova (`Controllers`, `Data`, `Models`, `DataDriven`, `Mock`, `Izuzeci`). |
| `[TestProperty("Autor", "...")]` | Svih 8 testnih klasa | Dodavanje metapodataka o autoru testova. |
| `[Priority(1..3)]` | Aktivne testne metode | Definisanje prioriteta izvršavanja testova. |
| `[Timeout(2000)]` | Aktivne testne metode | Ograničavanje trajanja pojedinačnog testa na 2000 ms. |
| `[DataRow(...)]` | 7 parametrizovanih metoda | Inline data-driven testiranje više ulaznih vrijednosti. |
| `[DynamicData(...)]` | 7 data-driven metoda | Učitavanje testnih podataka iz eksternih CSV, JSON i XML fajlova. |
| `[Ignore("...")]` | `AdminControllerTests`, `UserControllerTests` | Privremeno isključivanje testa iz izvršavanja. |
| `Assert.ThrowsExactly<T>` | `ApplicationUserTests` | Provjera da neispravan unos baca tačno očekivani izuzetak (`ValidationException`). |

## 4. Data-driven testiranje (CSV, JSON i XML)

Za data-driven testiranje korišteni su podaci iz foldera `TestData` pohranjeni u CSV, JSON i XML formatima:

| Format | Fajl (`TestData`) | Testna metoda | Testirana funkcionalnost |
| --- | --- | --- | --- |
| **CSV** | `uloge.csv` | `AdminControllerTests.ChangeRole_UlogaIzCsv_MijenjaUloguIPostavljaPoruku` | Dodjela uloga (`Admin`, `Owner`, `User`) korisniku i provjera poruke u `TempData`. |
| **CSV** | `login_prazna_polja.csv` | `UserControllerTests.LoginPost_PraznaPoljaIzCsv_VracaViewSaPorukomGreske` | Validacija prijave kada nedostaje email, lozinka ili oba polja. |
| **CSV** | `application_user_names.csv` | `ApplicationUserTests.FirstName_ProvjeraPodatakaIzCsv_VracaOcekivanuValidnost` | Validacija formata i dozvoljenih znakova za polje `FirstName`. |
| **JSON** | `rezervacije_filter.json` | `AdminControllerTests.Reservations_StatusIzJson_VracaFiltriraneRezervacije` | Filtriranje rezervacija prema statusima (`active`, `expired`, `unpaid`, `cancelled`, `all`). |
| **JSON** | `profil_neispravne_lozinke.json` | `UserControllerTests.ProfilePost_NeispravnaPoljaLozinkeIzJson_VracaViewSaGreskom` | Provjera grešaka pri promjeni lozinke na korisničkom profilu. |
| **JSON** | `validation_plates.json` | `ValidationModelsTests.ReservationViewModel_RegistracijeIzJson_NormalizujeIValidira` | Normalizacija i validacija formata registarskih oznaka vozila. |
| **JSON** | `validation_card_numbers.json` | `ValidationModelsTests.CardPaymentViewModel_BrojeviIzJson_ProvjeravaLuhn` | Validacija broja platne kartice primjenom Luhn algoritma. |
| **XML** | `korisnici_pretraga.xml` | `AdminControllerTests.Users_FilterIzXml_VracaOcekivaniBrojKorisnika` | Pretraga korisnika po imenu/emailu i filtriranje po ulozi. |

## 5. Zamjenski objekti (Test Doubles)

Kako bi se testovi izvršavali brzo i bez zavisnosti od aktivne baze podataka, SMTP servera i ostalih modula aplikacije, korišteni su sljedeći zamjenski objekti:

* **Fake baza podataka (`Microsoft.EntityFrameworkCore.InMemory`):** U klasama `AdminControllerTests` i `ApplicationDbContextTests` svaki test dobija izoliranu bazu u memoriji (`UseInMemoryDatabase`), čime se testiraju upiti i relacije bez SQL Servera.
* **Mock repozitorija (`Mock<IParkingRepository>`, `Mock<IRequestRepository>`):** Korišteni u `AdminControllerTests` i `ReplacementObjectTests` za simulaciju dohvata podataka i verifikaciju poziva metoda repozitorija (`Verify(..., Times.Once)`).
* **Mock email servisa (`Mock<IEmailSender>`):** Korišten u `UserControllerTests` za sprječavanje slanja stvarnih emailova i provjeru da se potvrda registracije šalje ispravnom primaocu.
* **Mock Identity servisa (`UserManager`, `SignInManager`, `RoleManager`):** Korišteni za simulaciju različitih stanja korisnika (nepotvrđen email, zaključan nalog, pogrešna lozinka, provjera uloga).
* **Mock HTTP objekata (`Mock<IFormFile>`, `Mock<IUrlHelper>`, `ITempDataProvider`):** Korišteni za testiranje uploada slika parkinga i generisanja linkova za potvrdu email adrese.

## 6. Pokrivenost koda (Fine Code Coverage)

Mjerenje pokrivenosti koda izvršeno je alatom **Fine Code Coverage** u Visual Studiju:

| Produkcijska klasa | Izvršive linije | Pokrivenost linija | Pokrivenost grananja |
| --- | --- | --- | --- |
| `ParkirajBa.Controllers.AdminController` | 301 / 304 | **99%** | 85.2% (196 / 230) |
| `ParkirajBa.Controllers.AdminUserViewModel` | 8 / 8 | **100%** | 75% (3 / 4) |
| `ParkirajBa.Controllers.UserController` | 204 / 204 | **100%** | 78.4% (102 / 130) |
| `ParkirajBa.Data.ApplicationDbContext` | 31 / 31 | **100%** | — |
| `ParkirajBa.Models.AdminProfile` | 4 / 4 | **100%** | — |
| `ParkirajBa.Models.OwnerProfile` | 4 / 4 | **100%** | — |
| `ParkirajBa.Models.ApplicationUser` | 7 / 7 | **100%** | — |
| `ParkirajBa.Models.ExtendedRegisterViewModel` | 5 / 5 | **100%** | — |
| `ParkirajBa.Models.ReservationViewModel` | 18 / 18 | **100%** | 100% (6 / 6) |
| `ParkirajBa.Models.CardPaymentViewModel` | 33 / 33 | **100%** | 100% (16 / 16) |
| **Ukupno (testirane klase)** | **615 / 618** | **99.5%** | **83.7% (323 / 386)** |