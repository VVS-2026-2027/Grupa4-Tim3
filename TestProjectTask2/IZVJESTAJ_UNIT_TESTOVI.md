# Izvještaj unit testova

Testni projekat: `TestProjectTask2\TestProjectTask2`  
Framework: MSTest 4.2.3  
Zadnje mjerenje: 20.11.2026.  
Rezultat: **135 prolaznih, 0 neuspješnih i 2 preskočena testa**. Preskočeni su primjeri označeni sa `[Ignore]`.

## Testovi po članu tima

| Član | Testne klase | Šta se provjerava |
|---|---|---|
| Damir | `UserControllerTests` | Prijava, registracija, profil, promjena lozinke, potvrda emaila i odjava. Koriste se Moq zamjene za Identity servise i slanje emaila. |
| Damir | `AdminControllerTests` | Administratorske akcije za korisnike, parkinge, rezervacije, izvještaje i zahtjeve. Koriste se mockovi repozitorija, Identity servisa i InMemory baza. |
| Damir | `ApplicationDbContextTests` | Kreiranje DbSetova, mapiranja, decimalne kolone, kaskadno brisanje i pohrana podataka. InMemory baza je zamjena za aktivnu bazu. |
| Tim3 | `AdminProfileTests`, `OwnerProfileTests` | Postavljanje vrijednosti i veza profila sa korisnikom. |
| Tim3 | `ApplicationUserTests` | Puno ime, vrijeme kreiranja i validacija imena/prezimena, uključujući izuzetak za prazno ime. |
| Tim3 | `ValidationModelsTests` | Validacija registracije, rezervacije i plaćanja karticom. |
| Tim3 | `ReplacementObjectTests` | `Parkings_MockiranParkingRepository_VracaNjegoveParkinge` koristi mock `IParkingRepository`; provjerava vraćene parkinge i da je repozitorij pozvan jednom. |

## Primjeri zamjenskih objekata

- `Parkings_MockiranParkingRepository_VracaNjegoveParkinge` koristi Moq stub za parking repozitorij, bez poziva na stvarni repozitorij.
- `RegisterPost_ValidniPodaci_KreiraKorisnikaDodjeljujeUloguISaljeEmail` koristi mock Identity/email servisa i provjerava pozive dodjele uloge i slanja emaila.
- `ApplicationDbContextTests` koristi EF Core InMemory bazu kao fake, tako da testovi ne zahtijevaju aktivnu bazu podataka.

## Data-driven testiranje

Testovi koriste pohranjene CSV, JSON i XML podatke, učitane iz `TestData`:

- **CSV**: `LoginPost_PraznaPoljaIzCsv_VracaViewSaPorukomGreske` testira ponašanje prijave za nedostajuće podatke; `FirstName_ProvjeraPodatakaIzCsv_VracaOcekivanuValidnost` provjerava validnost imena.
- **JSON**: `ProfilePost_NeispravnaPoljaLozinkeIzJson_VracaViewSaGreskom` provjerava greške pri promjeni lozinke; testovi `ReservationViewModel_RegistracijeIzJson_NormalizujeIValidira` i `CardPaymentViewModel_BrojeviIzJson_ProvjeravaLuhn` pokrivaju validaciju registracije vozila i broja kartice.
- **XML**: `Users_FilterIzXml_VracaOcekivaniBrojKorisnika` provjerava pretragu korisnika po kriterijima.
- Dodatni inline data-driven testovi koriste `[DataRow]` za više scenarija u jednoj metodi.

Testni podaci su u `TestProjectTask2\TestProjectTask2\TestData` i kopiraju se u izlazni direktorij testova.

## Pokrivenost klasa

Procenti su iz posljednjeg Cobertura mjerenja. Prikazane su klase obuhvaćene ovim testnim skupom; pokrivenost linija i grana navedena je odvojeno.

| Produkcijska klasa | Linije | Grane |
|---|---:|---:|
| `AdminProfile` | 100% | 100% |
| `OwnerProfile` | 100% | 100% |
| `ApplicationUser` | 100% | 100% |
| `ExtendedRegisterViewModel` | 100% | 100% |
| `ReservationViewModel` | 100% | 100% |
| `CardPaymentViewModel` | 100% | 100% |
| `ApplicationDbContext` | 100% | 100% |
| `AdminController` | 100% | 100% |
| `UserController` | 100% | 50% |

`UserController` ima potpunu pokrivenost linija, ali dio grananja nije izvršen. Ukupni procenat cijele aplikacije nije prikazan jer ovaj testni skup ne cilja sve klase aplikacije.

Mjerenje je pokrenuto naredbom:

```powershell
dotnet test .\TestProjectTask2\TestProjectTask2\TestProjectTask2.csproj --collect:"Code Coverage;Format=Cobertura"
```
