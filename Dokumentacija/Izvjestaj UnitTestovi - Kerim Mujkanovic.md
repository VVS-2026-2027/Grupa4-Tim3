# Izvještaj o unit testovima i zamjenskim objektima

## 1. Opšti podaci o testiranju

- **Projekat:** `ParkirajBa`
- **Testni projekat:** `TestProjectTask2`
- **Autor:** Kerim Mujkanovic
- **Testne klase:**
  - `QrControllerTests`
  - `ReportControllerTests`
  - `RequestRepositoryTests`
  - `RequestModelTests`
  - `DataDrivenTests`
- **Obuhvaćene produkcijske komponente:**
  - `QrController`
  - `ReportController`
  - `RequestRepository`
  - `ParkingRequest`
	- `QrRequest`

Ukupno je obuhvaćeno **34 testne metode**, odnosno **37 izvršenih testnih slučajeva**. 
Razlika postoji zato što se metoda u `DataDrivenTests` izvršava četiri puta, jednom za svaki zapis iz JSON datoteke.

- **10** testova za `QrController`
- **14** testova za `ReportController`
- **7** testova za `RequestRepository`
- **1** test za `ParkingRequest`
- **1** test za `QrRequest`
- **1** data-driven testna metoda sa **4** ulazna slučaja

## 2. Spisak testnih klasa i testova

### 2.1. `QrControllerTests` — 10 metoda

Testovi se nalaze u fajlu `TestProjectTask2/ControllerTests.cs`.

1. `Scan_NepostojeciKod_VracaBadRequest`
2. `Status_NepostojeciKod_VracaNotFound`
3. `Status_PostojeciTiket_VracaStanjeTiketa`
4. `Scan_ParkingJosNijeAktivan_VracaBadRequest`
5. `Scan_TiketNijePlacen_VracaBadRequest`
6. `Scan_PlacenTiketNaUlazu_DoZvoljavaUlaz`
7. `Scan_IsteklaRezervacijaNaIzlazu_VracaBadRequest`
8. `Scan_DodatnaNaknadaNijePlacena_VracaBadRequest`
9. `Scan_KratkaRezervacijaNaIzlazu_PovecavaSlobodnaMjesta`
10. `Scan_DugaRezervacijaNaIzlazu_OstavljaKodAktivnim`

### 2.2. `ReportControllerTests` — 14 metoda

Testovi se nalaze u fajlu `TestProjectTask2/ControllerTests.cs`.

1. `Index_NeprijavljenKorisnik_PreusmjeravaNaLogin`
2. `Index_PrijavljenKorisnik_PopunjavaStatistiku`
3. `ExportPdf_PrijavljenKorisnik_VracaView`
4. `ExportExcel_PrijavljenKorisnik_VracaExcelFajl`
5. `TicketPdf_KorisnikNemaTiket_PreusmjeravaNaPocetnu`
6. `TicketPdf_KorisnikImaTiket_VracaView`
7. `OwnerReport_VlasnikSaParkingom_VracaView`
8. `OwnerReportPdf_VlasnikBezParkinga_VracaView`
9. `OwnerReportExcel_VlasnikBezParkinga_VracaExcelFajl`
10. `OwnerReportExcel_VlasnikSaParkingom_VracaExcelFajl`
11. `AdminReport_AdminBezParkinga_VracaView`
12. `AdminReportPdf_AdminBezParkinga_VracaView`
13. `AdminReportExcel_AdminBezParkinga_VracaExcelFajl`
14. `AdminReportExcel_AdminSaParkingom_VracaExcelFajl`

### 2.3. `RequestRepositoryTests` — 7 metoda

Testovi se nalaze u fajlu `TestProjectTask2/RequestRepositoryTests.cs`.

1. `AddParkingRequest_ObjekatZahtjeva_SpremaZahtjev`
2. `AddParkingRequest_IdParkinga_KreiraZahtjevSaVremenom`
3. `GetAll_PostojeciZahtjevi_VracaSveZahtjeve`
4. `GetById_NepostojeciId_VracaNull`
5. `GetByParkingId_PostojiParking_VracaZahtjev`
6. `Delete_PostojiZahtjev_UklanjaZahtjev`
7. `Delete_NepostojeciId_NeMijenjaBazu`

### 2.4. `RequestModelTests` — 2 metode

Ista klasa se nalazi u fajlu `TestProjectTask2/RequestRepositoryTests.cs`.

1. `ParkingRequest_PocetneVrijednosti_SuIspravne`
2. `QrRequest_DodijeljenKod_VracaIstiKod`

### 2.5. `DataDrivenTests` — 1 metoda, 4 izvršenja

Test se nalazi u fajlu `TestProjectTask2/DataDrivenTests.cs`.

1. `Validate_BrojSatiIzJson_RazlikujeDozvoljeneVrijednosti`

Test koristi `[DynamicData]` i podatke učitava iz fajla `TestData/sati.json`. U datoteci se nalaze sljedeći slučajevi:

| Broj sati | Očekivani rezultat | Objašnjenje |
|---:|:---:|---|
| 0 | `false` | Vrijednost je manja od minimalno dozvoljenog broja sati. |
| 1 | `true` | Donja granica dozvoljenog intervala. |
| 24 | `true` | Gornja granica dozvoljenog intervala. |
| 25 | `false` | Vrijednost je veća od maksimalno dozvoljenog broja sati. |

U testu se provjerava pravilo `sati >= 1 && sati <= 24`. Na ovaj način se jednim testom provjeravaju obje granice intervala, kao i vrijednosti izvan intervala. Očekivani rezultati se ne upisuju direktno u testnu metodu, nego se učitavaju zajedno sa ulaznim vrijednostima iz JSON datoteke.

## 3. Testirana funkcionalnost

### `QrController`

Testirano je:

- nepostojeći QR kod;
- status nepostojećeg tiketa;
- status postojećeg tiketa;
- pokušaj ulaska prije početka parkinga;
- ulazak sa neplaćenim tiketom;
- uspješan ulazak sa plaćenim tiketom;
- izlazak nakon isteka rezervacije;
- izlazak bez plaćene dodatne naknade;
- uspješan izlazak kod kratke rezervacije;
- izlazak kod duge rezervacije i ponovno aktiviranje QR koda.

### `ReportController`

Testirano je:

- preusmjeravanje neprijavljenog korisnika na login;
- prikaz statistike prijavljenog korisnika;
- korisnički PDF izvještaj;
- korisnički Excel izvještaj;
- `TicketPdf` kada tiket ne postoji;
- `TicketPdf` kada tiket postoji;
- owner izvještaj sa parkingom;
- owner PDF izvještaj bez parkinga;
- owner Excel izvještaj sa i bez parkinga;
- admin izvještaj sa i bez parkinga;
- admin PDF izvještaj;
- admin Excel izvještaj sa i bez parkinga.

### `RequestRepository`

Testirano je:

- dodavanje zahtjeva preko objekta `ParkingRequest`;
- dodavanje zahtjeva preko ID-a parkinga;
- dohvat svih zahtjeva;
- dohvat po ID-u za nepostojeći zahtjev;
- dohvat po ID-u parkinga;
- brisanje postojećeg zahtjeva;
- brisanje nepostojećeg zahtjeva.

### Modeli

Za `ParkingRequest` provjerene su početne vrijednosti identifikatora i ID-a parkinga.

Za `QrRequest` provjereno je pravilno čuvanje vrijednosti QR koda u svojstvu `Code`.

## 4. AAA struktura i MSTest stereotipi

Testovi koriste AAA obrazac:

- **Arrange** – priprema baze, objekata i zamjenskih zavisnosti;
- **Act** – poziv metode koja se testira;
- **Assert** – provjera rezultata, tipa odgovora, promjene stanja ili poziva mocka.

Korišteni MSTest elementi:

- `[TestClass]` za testne klase;
- `[TestMethod]` za testne metode;
- `[TestInitialize]` za kreiranje nove InMemory baze prije svakog repository testa;
- `[TestCleanup]` za oslobađanje baze nakon svakog testa;
- `Assert.IsInstanceOfType` za provjeru MVC rezultata;
- `Assert.AreEqual`, `Assert.IsNull`, `Assert.IsNotNull` za provjeru vrijednosti;
- `Assert.IsTrue` za provjeru promjene stanja;
- `CollectionAssert` nije korišten u ovim ranijim testovima;
- `Verify(..., Times.Once)` za provjeru poziva parking repozitorija.

Testovi su izolovani i ne koriste stvarni SQL Server, stvarni SignalR hub, vanjski servis ili stvarno slanje emaila.

## 5. Zamjenski objekti

### Fake baza podataka

`Microsoft.EntityFrameworkCore.InMemory` koristi se u `RequestRepositoryTests` i `QrControllerTests`.

Svaki test dobija novu bazu sa jedinstvenim imenom:

```csharp
.UseInMemoryDatabase(Guid.NewGuid().ToString())
```

Na taj način testovi ne zavise od podataka iz drugih testova niti od stvarne baze.

### Mock parking repozitorija

Za `QrController` koristi se `Mock<IParkingRepository>`. U testu uspješnog izlaska provjerava se da je metoda za povećanje slobodnih mjesta pozvana tačno jednom:

```csharp
parking.Verify(
	p => p.IncrementAvailableSpotsAsync(1),
	Times.Once);
```

Za `ReportController` mock parking repozitorija vraća pripremljene tikete i parkinge, bez pristupa stvarnom repozitoriju.

### Mock SignalR hub-a

`IHubContext<ParkingHub>`, `IHubClients` i `IClientProxy` zamijenjeni su Moq objektima. Time se omogućava testiranje QR ulaza i izlaza bez stvarne SignalR komunikacije.

### Mock UserManager-a

`UserManager<ApplicationUser>` je mockovan da bi se simulirali prijavljen i neprijavljen korisnik, kao i provjera administratorske uloge u report testovima.

## 6. Obuhvat koda

Prema ranije generisanom Coverlet izvještaju za ove komponente, line coverage je iznosio:

| Produkcijska komponenta | Pokrivenost linija |
|---|---:|
| `QrController` | **98.31%** |
| `ReportController` | **97.31%** |
| `RequestRepository` | **100%** |
| `ParkingRequest` | **100%** |
| `QrRequest` | **100%** |
| `IRequestRepository` | Nema izvršivih linija za mjerenje |

Ovi procenti odnose se na ranije mjerenje obuhvata za navedene klase. Ukupni coverage cijelog projekta nije naveden jer ovaj izvještaj obuhvata samo testove i produkcijske komponente koje su ranije ciljano testirane.

## 7. Zaključak

Ranije napisani testovi pokrivaju ključne normalne, granične i neispravne slučajeve za QR kontroler, report kontroler, repository i pripadajuće modele.

Za izolaciju su korišteni EF Core InMemory i Moq. Testovi su brzi, ponovljivi i ne zavise od stvarne baze podataka, SignalR servera ili vanjskih servisa.

`IRequestRepository` nije testiran direktno jer je interfejs bez izvršivog koda. Njegov ugovor se provjerava kroz testove konkretne implementacije `RequestRepository` i kroz mockovanje u zavisnim klasama.
