# Izvještaj o unit testovima i zamjenskim objektima

## 1. Opšti podaci o testiranju

* **Projekat:** `ParkirajBa` (ASP.NET Core MVC)
* **Testni projekat:** `TestProjectTask2`
* **Autor:** Hamza Jasarević
* **Alati i biblioteke:** MSTest, Moq, Microsoft.EntityFrameworkCore.InMemory
* **Rezultati izvršavanja:** 8 testnih klasa, **67 testnih metoda** (**79 testnih slučajeva**):
  * **79 uspješnih (Passed)**
  * **0 neuspješnih (Failed)**
  * **0 preskočenih (Skipped)**

Autorstvo je provjereno Git istorijom i `git blame` analizom. Testni fajlovi `TestProjectTask2/Test1.cs` i `TestProjectTask2/AdditionalUnitTests.cs` uvedeni su u commitu `16f3174` (`testoviKreirani`), autora `hjasarevic1` / Hamza Jasarević.

## 2. Spisak testnih klasa i testova

### 2.1. `ParkingObjectTests` (`Test1.cs`) — 3 metode

1. `ParkingObject_TotalSpotsValid_ValidationPasses` *(DynamicData)*
2. `ParkingObject_TotalSpotsOutOfRange_ValidationFails` *(DataRow)*
3. `ParkingImage_PropertiesAssigned_ReturnsAssignedValues`

### 2.2. `ImageServiceTests` (`Test1.cs`) — 2 metode

1. `SaveImage_NullFile_ReturnsNull`
2. `SaveImage_ValidFile_CreatesFileAndReturnsPath`

### 2.3. `ParkingRepositoryTests` (`Test1.cs`) — 8 metoda

1. `FilterParkings_CsvCase_ReturnsExpectedCount` *(Data-Driven: CSV)*
2. `FilterApprovedParkings_JsonCase_ReturnsExpectedCount` *(Data-Driven: JSON)*
3. `ApproveParking_ExistingParking_SetsApproved`
4. `ApproveParking_MissingParking_ReturnsFalse`
5. `IncrementAvailableSpots_BelowCapacity_IncrementsAndReturnsTrue`
6. `IncrementAvailableSpots_AtCapacity_ReturnsFalse`
7. `GetActivePricing_WithinValidity_ReturnsPricing`
8. `SaveAllParkingImages_MismatchedLists_ThrowsException` *(Izuzetak)*

### 2.4. `HomeControllerTests` (`Test1.cs`) — 4 metode

1. `Search_RepositoryReturnsResults_ReturnsJsonResult`
2. `GetMaxPriceForRegime_RegimeMapsToPricingType_ReturnsRepositoryValue` *(DataRow)*
3. `GetParkingPrice_MissingPricing_ReturnsZero`
4. `Privacy_NoDependencies_ReturnsViewResult`

### 2.5. `ParkingControllerTests` (`Test1.cs`) — 3 metode

1. `Details_MissingParking_RedirectsToObjects`
2. `GetCreateForm_Request_ReturnsCreatePartial`
3. `GetEditForm_MissingParking_ReturnsNotFound`

### 2.6. `ParkingModelAdditionalTests` (`AdditionalUnitTests.cs`) — 2 metode

1. `ParkingObject_AvailableSpotsOutOfRange_ValidationFails` *(DataRow)*
2. `ParkingImage_DefaultValues_AreEmpty`

### 2.7. `ParkingRepositoryAdditionalTests` (`AdditionalUnitTests.cs`) — 19 metoda

1. `ModifyParking_ExistingParking_UpdatesValues`
2. `ModifyParking_MissingParking_ReturnsNull`
3. `GetImagePaths_ImagesHavePositions_ReturnsOrderedPaths`
4. `GetAllApproved_ApprovedAndPending_ReturnsApprovedOnly`
5. `AddAllPricings_TwoPrices_ReturnsBothPrices`
6. `GetTicketsByUser_UserHasTickets_ReturnsNewestFirst`
7. `GetAll_WithParking_ReturnsParking`
8. `SaveParkingImage_ValidFile_PersistsImageRecord`
9. `SaveAllParkingImages_EqualLists_ReturnsAllPaths`
10. `GetByOwnerIdWithPricings_OwnerHasParking_ReturnsPricing`
11. `GetImagesByParkingId_ImagesExist_ReturnsImages`
12. `GetPrimaryImagePath_ImageExists_ReturnsPath`
13. `GetMaxPricingForRegime_PricingsExist_ReturnsHighestPrice`
14. `GetTicketsByParkingIds_MatchingTicket_ReturnsTicket`
15. `GetTicketById_CorrectUser_ReturnsTicket`
16. `GetAllWithPricingsAndOwner_ReturnsRelatedData`
17. `GetByIdWithRelations_ReturnsRelatedData`
18. `GetParkingPricings_ExistingParking_ReturnsPrices`
19. `FilterParkings_AllFlags_ReturnsMatchingParking`

### 2.8. `ControllerAdditionalTests` (`AdditionalUnitTests.cs`) — 26 metoda

1. `Details_ExistingParking_ReturnsViewWithRelatedData`
2. `GetPrimaryParkingImage_RepositoryReturnsImage_ReturnsJson`
3. `GetMaxPriceForRegime_KnownRegime_UsesExpectedType` *(DataRow)*
4. `GetParkingPrice_KnownRegime_ReturnsActivePrice` *(DataRow)*
5. `Objekti_ParkingHasNoImage_ReturnsViewWithDefaultImage`
6. `Potvrda_Name_SetsViewBag`
7. `Index_Guest_ReturnsApprovedParkings`
8. `Index_Admin_RedirectsToDashboard`
9. `Objekti_ParkingHasImage_StoresImagePath`
10. `Rezervacije_NoUser_RedirectsToLogin`
11. `SimplePages_ReturnViewResults`
12. `ParkingManagement_NoUser_ReturnsUnauthorized`
13. `Create_NoUser_ReturnsUserError`
14. `Create_InvalidAddress_ReturnsValidationError`
15. `Create_InvalidTotalSpots_ReturnsValidationError`
16. `Create_InvalidCoordinates_ReturnsValidationError`
17. `Create_InvalidName_ReturnsValidationError`
18. `Create_ValidParking_AddsParkingAndRequest`
19. `Edit_MissingParking_ReturnsError`
20. `Edit_ValidParking_ModifiesParking`
21. `Edit_InvalidLatitude_ReturnsError`
22. `Edit_InvalidLongitude_ReturnsError`
23. `Edit_EmptyName_ReturnsError`
24. `Edit_InvalidTotalSpots_ReturnsError`
25. `UploadCoverImage_EmptyFile_ReturnsError`
26. `UploadCoverImage_ValidFile_SavesImage`

## 3. Korišteni MSTest stereotipi i organizacija testova

Testovi su organizovani po AAA obrascu (*Arrange – Act – Assert*), bez programske logike u testnim metodama i sa izolovanom okolinom za testove koji koriste bazu ili filesystem. Korišteni su sljedeći MSTest atributi i obrasci:

| Stereotip | Gdje se koristi | Svrha |
| --- | --- | --- |
| `[TestClass]` | Svih 8 testnih klasa | Označava klasu koja sadrži unit testove. |
| `[TestMethod]` | Svih 67 testnih metoda | Označava pojedinačnu testnu metodu. |
| `[TestInitialize]` / `[TestCleanup]` | `ImageServiceTests`, `ParkingRepositoryTests`, `ParkingRepositoryAdditionalTests` | Priprema privremeni direktorij ili InMemory bazu prije testa i oslobađa resurse nakon testa. |
| `[DataRow(...)]` | Validacijski i controller testovi | Inline data-driven testiranje više ulaznih vrijednosti. |
| `[DynamicData(...)]` | `ParkingObjectTests` i `ParkingRepositoryTests` | Učitavanje više testnih vrijednosti iz metode, CSV-a i JSON-a. |
| `[TestCategory("DataDriven")]` | `FilterApprovedParkings_JsonCase_ReturnsExpectedCount` | Grupisanje data-driven testa. |
| `Assert.ThrowsExceptionAsync<T>` | `SaveAllParkingImages_MismatchedLists_ThrowsException` | Provjera da neispravna kombinacija lista baca očekivani izuzetak. |
| `Verify(..., Times.Once)` / `Verify(..., Times.Never)` | Controller testovi i testovi repozitorija | Provjera da su zavisnosti pozvane očekivani broj puta. |

## 4. Data-driven testiranje (CSV i JSON)

Za data-driven testiranje korišteni su podaci iz foldera `TestData` pohranjeni u CSV i JSON formatima, kao i inline podaci preko `DataRow` atributa:

| Format | Fajl/metoda izvora | Testna metoda | Testirana funkcionalnost |
| --- | --- | --- | --- |
| **CSV** | `TestData/parking-filter-cases.csv` | `FilterParkings_CsvCase_ReturnsExpectedCount` | Filtriranje parkinga prema tekstu pretrage, garaži, punjaču, kamerama, pristupačnosti, režimu i maksimalnoj cijeni. |
| **JSON** | `TestData/parking-filter-cases.json` | `FilterApprovedParkings_JsonCase_ReturnsExpectedCount` | Filtriranje samo odobrenih parkinga prema parametrima iz JSON podataka. |
| **DynamicData** | `ValidSpotCounts` | `ParkingObject_TotalSpotsValid_ValidationPasses` | Provjera validnih vrijednosti ukupnog broja parking mjesta (`1` i `10000`). |
| **DataRow** | Inline vrijednosti | `ParkingObject_TotalSpotsOutOfRange_ValidationFails` | Provjera granica `0`, `-1` i `10001`. |
| **DataRow** | Inline vrijednosti | `ParkingObject_AvailableSpotsOutOfRange_ValidationFails` | Provjera neispravnog broja dostupnih mjesta (`-1` i `10001`). |
| **DataRow** | Inline vrijednosti | `GetMaxPriceForRegime_RegimeMapsToPricingType_ReturnsRepositoryValue` | Mapiranje režima `Day` i nepoznatog režima na odgovarajući tip cijene. |
| **DataRow** | Inline vrijednosti | `GetMaxPriceForRegime_KnownRegime_UsesExpectedType` | Mapiranje režima `Week`, `Month`, `Year` i `Day`. |
| **DataRow** | Inline vrijednosti | `GetParkingPrice_KnownRegime_ReturnsActivePrice` | Dohvat aktivne cijene za različite režime parkiranja. |

CSV i JSON testovi omogućavaju dodavanje novih scenarija promjenom testnih podataka, bez dupliciranja Arrange/Act/Assert dijela testova.

## 5. Zamjenski objekti (Test Doubles)

Kako bi se testovi izvršavali bez aktivne SQL baze, stvarnog filesystema, web servera i stvarnog Identity sistema, korišteni su sljedeći zamjenski objekti:

* **Fake baza podataka (`Microsoft.EntityFrameworkCore.InMemory`):** U testovima repozitorija svaki test dobija bazu u memoriji sa jedinstvenim imenom, čime se testiraju upiti, relacije i čuvanje podataka bez SQL Servera.
* **Mock parking repozitorija (`Mock<IParkingRepository>`):** Korišten u controller testovima za simulaciju rezultata repozitorija i provjeru poziva metoda preko `Verify`.
* **Mock request repozitorija (`Mock<IRequestRepository>`):** Koristi se za simulaciju rada sa zahtjevima pri kreiranju parkinga.
* **Mock Identity servisa (`Mock<UserManager<ApplicationUser>>`, `Mock<IUserStore<ApplicationUser>>`):** Simulira gosta, prijavljenog korisnika i administratorsku ulogu.
* **Mock web okruženja (`Mock<IWebHostEnvironment>`):** Vraća privremeni `WebRootPath` za testiranje `ImageService` i rada sa parking slikama.
* **Mock loggera i konfiguracije:** Omogućavaju instanciranje `HomeController` bez stvarnog loggera i konfiguracijskog sistema.
* **`FormFile` i `MemoryStream`:** Predstavljaju uploadovane slike u memoriji, bez čitanja stvarnih korisničkih fajlova.
* **Privremeni direktoriji:** `ImageServiceTests` ih kreira za svaki test i briše u `TestCleanup` metodi.

## 6. Pokrivenost koda (Fine Code Coverage format)

Mjerenje pokrivenosti izvršeno je komandom `dotnet test --collect:"XPlat Code Coverage"`, a rezultat je generisan u Cobertura formatu. Svi testovi su izvršeni na aktuelnoj grani i rezultat je bio **79/79 uspješnih testnih slučajeva**.

| Produkcijska klasa | Izvršive linije | Pokrivenost linija | Pokrivenost grananja |
| --- | ---: | ---: | ---: |
| `ParkirajBa.Data.ApplicationDbContext` | 31 / 31 | **100.0%** | — |
| `ParkirajBa.Services.ImageService` | 24 / 24 | **100.0%** | — |
| `ParkirajBa.Repositories.ParkingRepository` | 212 / 229 | **92.6%** | — |
| `ParkirajBa.Controllers.HomeController` | 84 / 157 | **53.5%** | — |
| `ParkirajBa.Controllers.ParkingController` | 111 / 211 | **52.6%** | — |
| `ParkirajBa.Models.ParkingImage` | 6 / 6 | **100.0%** | — |
| `ParkirajBa.Models.ParkingObject` | 20 / 21 | **95.2%** | — |
| `ParkirajBa.Models.Pricing` | 10 / 10 | **100.0%** | — |
| `ParkirajBa.Models.Ticket` | 15 / 23 | **65.2%** | — |
| `ParkirajBa.Models.ApplicationUser` | 6 / 7 | **85.7%** | — |
| **Ukupno (testirane klase)** | **525 / 726** | **72.3%** | — |

Coverage za kontrolere prikazan je za cijele klase. Testovi su usmjereni na funkcionalnosti obuhvaćene ovim zadatkom i implementacijski opseg autora, pa ukupni procenat kontrolera uključuje i metode i tokove koji nisu bili predmet ovog testnog skupa.

Nepokrivene linije uglavnom pripadaju dodatnim akcijama kontrolera, alternativnim authorization tokovima, granama za prijavljene korisnike, slanju emaila, kao i kombinacijama parametara koje nisu bile potrebne za testirane funkcionalnosti. Klase `Program` i pogledi nisu uključeni u obuhvat testiranja, u skladu sa zahtjevom zadatka.
