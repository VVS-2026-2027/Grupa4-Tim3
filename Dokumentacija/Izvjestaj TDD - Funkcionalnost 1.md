# Izvještaj – Funkcionalnost 1: Filtriranje parkinga po slobodnim mjestima

## 1. Specifikacija funkcionalnosti

Korisnik može prikazati samo one parkinge koji imaju **najmanje zadani broj slobodnih mjesta**. Rezultat je sortiran tako da su na vrhu parkinzi s najviše slobodnih mjesta.

Funkcionalnost je implementirana kao statička metoda u servisnom sloju:

```csharp
// ParkingBA/ParkirajBa/ParkirajBa/Services/ParkingFilter.cs
public static List<ParkingObject> FiltrirajPoSlobodnimMjestima(IEnumerable<ParkingObject> parkinzi, int minimalnoMjesta)
```

| | Opis |
| :--- | :--- |
| **Ulaz** | `parkinzi`: kolekcija parkinga; `minimalnoMjesta`: najmanji traženi broj slobodnih mjesta |
| **Izlaz** | Nova lista parkinga kojima je `availableSpots >= minimalnoMjesta`, sortirana opadajuće po `availableSpots` |
| **Korišteno polje** | Postojeće polje `ParkingObject.availableSpots` (model se nije mijenjao) |
| **Greške** | `minimalnoMjesta < 0` → `ArgumentException`; `parkinzi == null` → `ArgumentNullException` |

### 1.1 Pravila

Funkcionalnost je razložena na 7 pravila. Svako pravilo je razvijeno kroz jedan TDD ciklus i pokriveno jednim testom:

| Pravilo | Opis | Test |
| :---: | :--- | :--- |
| **P1** | Prazna lista parkinga vraća praznu listu | `FiltrirajPoSlobodnimMjestima_PraznaLista_VracaPraznuListu` |
| **P2** | Parking sa 5 slobodnih mjesta prolazi za filter 3 | `FiltrirajPoSlobodnimMjestima_ParkingSa5MjestaFilter3_ParkingProlazi` |
| **P3** | Parking sa 2 slobodna mjesta ne prolazi za filter 3 | `FiltrirajPoSlobodnimMjestima_ParkingSa2MjestaFilter3_ParkingNeProlazi` |
| **P4** | Parking sa tačno 3 slobodna mjesta prolazi za filter 3 (granica je uključena) | `FiltrirajPoSlobodnimMjestima_ParkingSaTacno3MjestaFilter3_ParkingProlazi` |
| **P5** | Rezultat je sortiran od parkinga s najviše slobodnih mjesta | `FiltrirajPoSlobodnimMjestima_ViseParkinga_SortiraOdNajviseMjesta` |
| **P6** | Negativan filter baca `ArgumentException` | `FiltrirajPoSlobodnimMjestima_NegativanFilter_BacaArgumentException` |
| **P7** | Lista `null` baca `ArgumentNullException` | `FiltrirajPoSlobodnimMjestima_NullLista_BacaArgumentNullException` |

Testovi se nalaze u `TestProjectTask2/TestProjectTask2/TDD/ParkingFilterTests.cs`.

> **Izmjena specifikacije tokom razvoja:** u prvobitnom planu P5 je glasilo „filter 0 vraća sve parkinge“. Tokom rada je zamijenjeno pravilom o sortiranju rezultata. Slučaj filtera 0 je i dalje obuhvaćen: P5 test koristi filter 0 nad dva parkinga.

## 2. Podjela rada

Na funkcionalnosti su radila tri člana tima. Radili su naizmjenično na zajedničkoj grani `Funkcionalnost-1`, svako sa svog računara: jedan član završi cijeli ciklus za jedno pravilo, pushuje ga, a sljedeći nastavlja od tog stanja.

| Član tima | Pravila |
| :--- | :--- |
| Tarik Beganović | P1, P2, P7 (GREEN) |
| Kerim Mujkanović | P3, P4, P5 |
| Damir Muharem | P6, P7 (RED) |

Grana je spojena u `main` kroz Pull Request **#84** opcijom *Create a merge commit*. Tako je sačuvana kompletna historija RED/GREEN/REFACTOR commitova.

## 3. Primijenjena TDD strategija

Za svako pravilo korišten je isti postupak:

1. **RED** – napisan je samo jedan test za to pravilo, bez izmjene produkcijskog koda. Test je pokrenut i morao je pasti, a svi raniji testovi su morali prolaziti.
2. **GREEN** – napisano je **najmanje koda** potrebnog da novi test prođe, bez implementiranja pravila koja još nisu bila testirana. Pokrenuti su svi testovi.
3. **REFACTOR** – kod je uređen bez promjene ponašanja: imena, poruke, struktura. Ako nije bilo šta smisleno urediti, faza je preskočena.

Svaka faza je zaseban commit s porukom oblika `F1 [RED|GREEN|REFACTOR] P#: kratak opis`.

Pravila za testove:

* ime oblika `Metoda_Scenarij_OcekivaniRezultat`;
* AAA struktura (*Arrange – Act – Assert*), bez petlji i `if` naredbi u testu;
* za izuzetke se koristi `Assert.ThrowsExactly<T>`.

## 4. Razvoj funkcionalnosti kroz testove

Ovo poglavlje prikazuje kako je svaki test „natjerao“ kod da se razvije: zašto je test u RED fazi pao i koja je minimalna izmjena u GREEN fazi bila dovoljna.

### P1 – prazna lista vraća praznu listu (Tarik)

* **RED** (`141242b`):
  * napravljen je test i kostur klase `ParkingFilter`, čija metoda baca `NotImplementedException`, samo da bi se kod kompajlirao;
  * test pada zbog izuzetka.
* **GREEN** (`69e7627`): minimalno rješenje je vratiti praznu listu.
  ```csharp
  return new List<ParkingObject>();
  ```
* **REFACTOR:** preskočen, jer je metoda imala samo jednu liniju.

### P2 – parking sa 5 mjesta prolazi za filter 3 (Tarik)

* **RED** (`869851b`): metoda uvijek vraća praznu listu, pa `CollectionAssert.Contains` pada.
* **GREEN** (`773afe4`): najmanja izmjena koja zadovoljava i P1 i P2 je vratiti sve primljene parkinge.
  ```csharp
  return parkinzi.ToList();
  ```
  Metoda još ne gleda `availableSpots`. Filtriranje nijedan test još ne zahtijeva.
* **REFACTOR:** preskočen.

### P3 – parking sa 2 mjesta ne prolazi za filter 3 (Kerim)

* **RED** (`4f007af`): metoda vraća sve parkinge, pa se parking sa 2 mjesta nalazi u rezultatu i `CollectionAssert.DoesNotContain` pada.
* **GREEN** (`a8ac7c4`): uvedeno je filtriranje. Za P2 i P3 je bio dovoljan strogi uslov `>`.
  ```csharp
  return parkinzi
      .Where(parking => parking.availableSpots > minimalnoMjesta)
      .ToList();
  ```
* **REFACTOR:** preskočen.

### P4 – parking sa tačno 3 mjesta prolazi za filter 3 (Kerim)

* **RED** (`a28e45a`): sa uslovom `>` parking sa tačno 3 mjesta ne prolazi (3 > 3 nije tačno), pa test pada. Ovaj test je otkrio da granična vrijednost mora biti uključena.
* **GREEN** (`def5032`): uslov je promijenjen u `>=`.
  ```csharp
  .Where(parking => parking.availableSpots >= minimalnoMjesta)
  ```
* **REFACTOR:** preskočen.

### P5 – sortiranje od najviše slobodnih mjesta (Kerim)

* **RED** (`f81a914`): za parkinge s 4 i 8 mjesta (filter 0) metoda vraća redoslijed iz ulaza, pa je prvi parking onaj s 4 mjesta i test pada.
* **GREEN** (`be89e25`): dodano je sortiranje.
  ```csharp
  .OrderByDescending(parking => parking.availableSpots)
  ```
* **REFACTOR:** preskočen.

### P6 – negativan filter baca `ArgumentException` (Damir)

* **RED** (`113a1c9`): za filter −1 metoda bez greške vraća rezultat, pa `Assert.ThrowsExactly<ArgumentException>` pada.
* **GREEN** (`84e3af7`): dodana je provjera ulaza.
  ```csharp
  if (minimalnoMjesta < 0)
  {
      throw new ArgumentException();
  }
  ```
* **REFACTOR** (`f2c60c2`): izuzetak je dobio poruku i ime parametra, a provjera je oblikovana kao guard klauzula na početku metode.
  ```csharp
  throw new ArgumentException("Minimalni broj mjesta ne može biti negativan.", nameof(minimalnoMjesta));
  ```

### P7 – lista `null` baca `ArgumentNullException` (Damir, Tarik)

* **RED** (`c055346`, Damir):
  * napisan je test, ali on **nije pao**;
  * razlog: LINQ metoda `Where` sama baca `ArgumentNullException` kada je izvor `null`, pa je ponašanje „slučajno“ već bilo ispunjeno;
  * izuzetak je ipak nosio ime LINQ-ovog parametra (`source`), a ne parametra naše metode.
* **GREEN** (`21002cc`, Tarik):
  * dodana je eksplicitna provjera na početku metode;
  * izuzetak se sada baca prije bilo kakve obrade, s ispravnim imenom parametra (`parkinzi`);
  * pravilo više ne zavisi od interne implementacije LINQ-a.
  ```csharp
  ArgumentNullException.ThrowIfNull(parkinzi);
  ```
* **REFACTOR:** preskočen, jer je izmjena jedna linija.

## 5. Konačna implementacija

```csharp
public static class ParkingFilter
{
    public static List<ParkingObject> FiltrirajPoSlobodnimMjestima(IEnumerable<ParkingObject> parkinzi, int minimalnoMjesta)
    {
        ArgumentNullException.ThrowIfNull(parkinzi);                         // P7

        if (minimalnoMjesta < 0)                                             // P6
        {
            throw new ArgumentException("Minimalni broj mjesta ne može biti negativan.", nameof(minimalnoMjesta));
        }

        return parkinzi
            .Where(parking => parking.availableSpots >= minimalnoMjesta)     // P2, P3, P4
            .OrderByDescending(parking => parking.availableSpots)            // P5
            .ToList();                                                       // P1
    }
}
```

Svaka linija konačnog koda nastala je kao odgovor na konkretan test. Nijedan dio nije napisan „unaprijed“.

## 6. Rezultati testiranja

Nakon svake faze pokretani su svi testovi testnog projekta:

```
dotnet test TestProjectTask2/TestProjectTask2/TestProjectTask2.csproj
```

Rezultati:

* u RED fazi padao je samo novi test, osim kod P7 (poglavlje 4);
* u GREEN i REFACTOR fazama prolazili su svi testovi;
* na kraju razvoja prolazi svih **7 TDD testova** za `ParkingFilter`;
* cijela testna suita: **409 testova prolazi, 0 pada**, a 3 su namjerno preskočena (`[Ignore]`, ranije postojeći testovi iz Taska 2).

## 7. Zapažanja

* **P5:** specifikacija je izmijenjena tokom razvoja (poglavlje 1.1).
* **P7:** RED faza nije dala test koji pada, jer je ponašanje već osiguravao LINQ. Ovo je dobar primjer zašto se u RED fazi test **mora pokrenuti**: bez toga se ne bi primijetilo da je pravilo ispunjeno slučajno, a ne namjerno. GREEN commit je pravilo učinio eksplicitnim.
* **Preskočeni REFACTOR koraci:** preskočeni su kod pravila gdje je izmjena bila jedna linija i nije bilo ponavljanja, loših imena ni magičnih vrijednosti. Smislen REFACTOR je bio kod P6 (poruka i ime parametra u izuzetku).
* **Integracija u aplikaciju:** funkcionalnost je implementirana i testirana na nivou servisne klase. Metoda `FiltrirajPoSlobodnimMjestima` još nije povezana s kontrolerom ni s korisničkim interfejsom.
* **Autor commitova P1 i P2:** u historiji je prikazan kao `unknown`, jer u tom trenutku na računaru nije bilo postavljeno git korisničko ime. Te commitove je napravio Tarik Beganović.
