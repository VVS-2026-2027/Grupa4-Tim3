# Izvještaj o unit testovima i zamjenskim objektima

## 1. Opšti podaci o testiranju

* **Projekat:** `ParkirajBa` (ASP.NET Core MVC)

* **Testni projekat:** `TestProjectTask2`

* **Autor:** Omer Masleša

* **Alati i biblioteke:** MSTest, Moq, Microsoft.EntityFrameworkCore.InMemory, Fine Code Coverage

* **Rezultati izvršavanja (cijeli testni projekat `TestProjectTask2`):** **209 testnih slučajeva** (trajanje izvršavanja: **23.6 s**):

  * **207 uspješnih (Passed)**

  * **0 neuspješnih (Failed)**

  * **2 preskočena (`[Ignore]`)**

* **Moj doprinos (`Omer Masleša`):** 6 testnih klasa, **35 testnih metoda** (**48 testnih slučajeva** — svi uspješni / **Passed**)

## 2. Spisak mojih testnih klasa i testova

### 2.1. `ReservationControllerTests` (`ParkirajBa.Tests.Controllers`) — 29 metoda (42 testna slučaja)

 1. `CreateGet_NepostojeciParking_PreusmjeravaNaObjekte`

 2. `CreateGet_PostojeciParking_VracaViewSaPodacima`

 3. `CreatePost_NeprijavljenKorisnik_PreusmjeravaNaLogin`

 4. `CreatePost_NepostojeciParking_PreusmjeravaNaObjekte`

 5. `CreatePost_NullStartsAt_VracaViewSaGreskom`

 6. `CreatePost_NullExpiresAt_VracaViewSaGreskom`

 7. `CreatePost_ExpiresAtPrijeStartsAt_VracaViewSaGreskom`

 8. `CreatePost_StartsAtUProslosti_KorigujeVrijemeIPreusmjeravaNaCheckout`

 9. `CreatePost_TrajanjaIzCsv_ProvjeravaMaksimalnoTrajanje` *(Data-Driven: CSV — 8 slučajeva)*

10. `CreatePost_CijeneIzJson_IzracunavaIspravnuCijenuUSesiji` *(Data-Driven: JSON — 4 slučaja)*

11. `CreatePost_NemaCijeneZaDaily_KoristiHourlyFallback`

12. `Details_NeprijavljenKorisnik_PreusmjeravaNaLogin`

13. `Details_NepostojecaRezervacija_PreusmjeravaNaRezervacije`

14. `Details_PostojecaRezervacija_VracaViewSaTicketom`

15. `ExtendGet_NeprijavljenKorisnik_PreusmjeravaNaLogin`

16. `ExtendGet_NeplacenaRezervacija_PreusmjeravaNaRezervacije`

17. `ExtendGet_IsteklaRezervacija_PreusmjeravaNaDetails`

18. `ExtendGet_AktivnaPlacenaRezervacija_VracaView`

19. `ExtendPost_NeprijavljenKorisnik_PreusmjeravaNaLogin`

20. `ExtendPost_OtkazanaRezervacija_PreusmjeravaNaRezervacije`

21. `ExtendPost_IsteklaRezervacija_PreusmjeravaNaDetails`

22. `ExtendPost_GranicniBrojSati_VracaOcekivanuAkciju` *(DataRow — 4 slučaja)*

23. `ExtendPost_ValidnoProduzenje_AzuriraCijenuIDeaktiviraQrKod`

24. `Cancel_NeprijavljenKorisnik_PreusmjeravaNaLogin`

25. `Cancel_NepostojecaRezervacija_PreusmjeravaNaRezervacije`

26. `Cancel_IsteklaRezervacija_PreusmjeravaNaDetails`

27. `Cancel_EvidentiranUlazak_PreusmjeravaNaDetails`

28. `Cancel_PlacenaRezervacija_OslobadjaMjestoISaljeEmail`

29. `Cancel_SlanjeEmailaBacaIzuzetak_IpakOtkazujeRezervaciju`

### 2.2. `NotificationControllerTests` (`ParkirajBa.Tests.Controllers`) — 1 metoda

1. `Index_PostojeciKontroler_VracaView`

### 2.3. `ParkingReservationBackgroundServiceTests` (`ParkirajBa.Tests.Services`) — 1 metoda

1. `ExecuteAsync_OtkazanToken_ZavrsavaIzvrsavanje`

### 2.4. `ReservationReminderServiceTests` (`ParkirajBa.Tests.Services`) — 1 metoda

1. `ExecuteAsync_OtkazanToken_ZavrsavaIzvrsavanje`

### 2.5. `EmailSenderTests` (`ParkirajBa.Tests.Services`) — 1 metoda

1. `SendEmailAsync_NeispravnaAdresa_NeBacaIzuzetak`

### 2.6. `ErrorViewModelTests` (`ParkirajBa.Tests.Models`) — 2 metoda

1. `ShowRequestId_EmptyRequestId_ReturnsFalse`

2. `ShowRequestId_RequestIdExists_ReturnsTrue`

## 3. Korišteni MSTest stereotipi i organizacija testova

Testovi su pisani po AAA obrascu (*Arrange – Act – Assert*), bez programske logike u testnim metodama i uz potpunu izolaciju svakog testa. Korišteni su sljedeći MSTest atributi i metode provjere:

| Stereotip | Gdje se koristi | Svrha | 
| ----- | ----- | ----- | 
| `[TestClass]` | Svih 6 testnih klasa | Označava klasu koja sadrži unit testove. | 
| `[TestMethod]` | Svih 35 testnih metoda | Označava pojedinačnu testnu metodu. | 
| `[TestInitialize]` / `[TestCleanup]` | `ReservationControllerTests` | Priprema izoliranu bazu, sesiju i mock objekte prije svakog testa te oslobađa resurse nakon testa. | 
| `[TestCategory("...")]` | Svih 6 testnih klasa | Grupisanje testova po funkcionalnosti (`Create`, `Details`, `Extend`, `Cancel`, `Notification`, `Services`, `Models`, `DataDriven`, `Granice`, `Mock`). | 
| `[TestProperty("Autor", "Omer Masleša")]` | `ReservationControllerTests` | Evidentiranje autora testova u Test Exploreru. | 
| `[Priority(1)]` | `ReservationControllerTests` | Označavanje prioritetnih data-driven i mock testova. | 
| `[Timeout(2000)]` | `ReservationControllerTests` | Ograničavanje vremena izvršavanja testa na najviše 2000 ms. | 
| `[DataRow(...)]` | `ReservationControllerTests` | Inline data-driven provjera graničnih vrijednosti (`0, 1, 24, 25` sati). | 
| `[DynamicData(...)]` | `ReservationControllerTests` | Učitavanje testnih slučajeva iz eksternih CSV i JSON fajlova. | 
| `Assert.IsInstanceOfType<T>` | `NotificationControllerTests` | Provjera da akcija `Index` vraća instancu tipa `ViewResult`. | 
| `StringAssert.Contains` | `ReservationControllerTests` | Provjera da serijalizovani JSON podatak u sesiji sadrži očekivani iznos cijene. | 

## 4. Data-driven testiranje (CSV i JSON)

Za data-driven testiranje korišteni su eksterni fajlovi iz direktorija `TestData` u CSV i JSON formatu:

| Format | Fajl (`TestData`) | Testna metoda | Testirana funkcionalnost | 
| ----- | ----- | ----- | ----- | 
| **CSV** | `trajanja.csv` | `ReservationControllerTests.CreatePost_TrajanjaIzCsv_ProvjeravaMaksimalnoTrajanje` | Validacija maksimalnog trajanja rezervacije za pakete `Hourly` (24/25h), `Daily` (360/361h), `Monthly` (2160/2250h) i `Yearly` (8760/8800h). | 
| **JSON** | `cijene.json` | `ReservationControllerTests.CreatePost_CijeneIzJson_IzracunavaIspravnuCijenuUSesiji` | Obračun ukupne cijene rezervacije za različite tipove cjenovnika i provjera pohrane u sesiju. | 

## 5. Zamjenski objekti (Test Doubles)

Kako bi se testovi izvršavali brzo, ponovljivo i bez oslanjanja na vanjske resurse (SQL Server, SMTP server, aktivnu HTTP sesiju ili SignalR konekcije), korišteni su sljedeći zamjenski objekti:

* **Fake baza podataka (`Microsoft.EntityFrameworkCore.InMemory`):** U `ReservationControllerTests` svaki test dobija zasebnu bazu u memoriji (`UseInMemoryDatabase(Guid.NewGuid().ToString())`) radi izolacije stanja.

* **Fake sesija (`TestSession : ISession`):** Korištena u `ReservationControllerTests` za simulaciju `HttpContext.Session` i čitanje pohranjenog ključa `PendingReservation`.

* **Mock repozitorija (`Mock<IParkingRepository>`):** Korišten u `ReservationControllerTests` za simulaciju dohvata parking objekata, cjenovnika i karata te provjeru poziva fallback metode (`Verify(..., Times.Once)`).

* **Mock email servisa (`Mock<IEmailSender>`):** Korišten u `ReservationControllerTests` za verifikaciju slanja emaila pri otkazivanju plaćene rezervacije i simulaciju greške prilikom slanja (`ThrowsAsync`).

* **Mock Identity servisa (`Mock<UserManager<ApplicationUser>>`):** Korišten u `ReservationControllerTests` za simulaciju prijavljenog i neprijavljenog korisnika.

* **Mock zavisnosti pozadinskih servisa (`Mock.Of<IServiceScopeFactory>()`, `Mock.Of<IHubContext<ParkingHub>>()`, `NullLogger<T>`):** Korišteni u `ParkingReservationBackgroundServiceTests`, `ReservationReminderServiceTests` i `EmailSenderTests` uz izvedene testne klase (`TestableParkingService`, `TestableReminderService`) za pozivanje zaštićene metode `ExecuteAsync`.

* **In-memory konfiguracija (`ConfigurationBuilder().AddInMemoryCollection(...)`):** Korištena u `EmailSenderTests` za simulaciju SMTP postavki bez čitanja `appsettings.json` fajla.

## 6. Pokrivenost koda (Fine Code Coverage)

### 6.1. Pokrivenost testnog projekta (`TestProjectTask2`)

Prema izvještaju alata **Fine Code Coverage** u Visual Studiju za izvršenih **207/209** testnih slučajeva:

| Cjelina / Namespace u `TestProjectTask2` | Pokrivenost linija | 
| ----- | ----- | 
| `TestProjectTask2` (ukupno testni projekat) | **95.0%** | 
| `ParkirajBa.Tests.Services` | **100.0%** | 
| `ParkirajBa.Tests.Services.ParkingReservationBackgroundServiceTests` | **100.0%** | 
| `ParkirajBa.Tests.Services.ReservationReminderServiceTests` | **100.0%** | 
| `ParkirajBa.Tests.Data` | **100.0%** | 
| `ParkirajBa.Tests.Controllers` | **96.2%** | 
| `ParkirajBa.Tests.Models` | **93.3%** | 

### 6.2. Pokrivenost mojih testiranih produkcijskih klasa (`ParkirajBa`)

| Produkcijska klasa | Izvršive linije | Pokrivenost linija | Pokrivenost grananja | 
| ----- | ----- | ----- | ----- | 
| `ParkirajBa.Controllers.ReservationController` | 126 / 128 | **98.4%** | 91.9% (102 / 111) | 
| `ParkirajBa.Controllers.NotificationController` | 3 / 3 | **100.0%** | — | 
| `ParkirajBa.Services.EmailSender` | 18 / 19 | **94.7%** | 100.0% (4 / 4) | 
| `ParkirajBa.Models.ErrorViewModel` | 2 / 2 | **100.0%** | 100.0% (2 / 2) | 
| `ParkirajBa.Services.ParkingReservationBackgroundService` | 35 / 38 | **92.1%** | 91.7% (11 / 12) | 
| `ParkirajBa.Services.ReservationReminderService` | 33 / 36 | **91.7%** | 91.7% (11 / 12) | 
| **Ukupno (testirane klase)** | **217 / 226** | **96.0%** | **92.2% (130 / 141)** | 