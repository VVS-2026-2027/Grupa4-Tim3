# Izvještaj – Funkcionalnost 2: Povrat novca pri otkazivanju rezervacije

## 1. Opis funkcionalnosti

U okviru Funkcionalnosti 2 implementirana je mogućnost povrata novca korisniku prilikom otkazivanja rezervacije. Funkcionalnost obuhvata izračunavanje iznosa povrata na osnovu pravila P1–P6, kao i komunikaciju sa payment gateway servisom prema pravilu P7.

Cilj je bio omogućiti ispravan i siguran obračun povrata novca, uz poštivanje svih definisanih poslovnih pravila. Implementacija je rađena korištenjem TDD pristupa, odnosno kroz cikluse:

- **RED** – pisanje testa koji pada,
- **GREEN** – implementacija minimalnog koda potrebnog da test prođe,
- **REFACTOR** – poboljšanje koda bez promjene njegovog ponašanja.

Glavna grana za funkcionalnost bila je:

` tdd-povrat-novca `

## 2. Podjela rada

Rad na funkcionalnosti podijeljen je između dva člana tima kako bi se izbjegli konflikti tokom razvoja.

### Hamza

Hamza je radio na:

- implementaciji klase `RefundCalculator`,
- pravilima P1–P6,
- pisanju testova u klasi `RefundCalculatorTests.cs`,
- provođenju RED, GREEN i REFACTOR ciklusa za svako pravilo.

Njegova radna grana bila je:

`f2-hamza-kalkulator`

### Omer

Omer je radio na:

- definisanju interfejsa `IPaymentGateway`,
- implementaciji servisa `RefundService`,
- pravilu P7,
- pisanju testova u klasi `RefundServiceTests.cs`,
- dopuni dokumentacije u fajlu `Dokumentacija/Task3_TDD.md`.

Njegova radna grana bila je:

`f2-omer-servis`

Obje grane su kreirane iz glavne grane `tdd-povrat-novca`.

## 3. Primijenjena TDD strategija

Za svako pravilo korišten je isti redoslijed rada. Prvo je napisan samo jedan test koji opisuje očekivano ponašanje sistema. Test je pokrenut i očekivano je pao, čime je potvrđeno da funkcionalnost još nije implementirana.

Nakon toga je napisan minimalni kod potreban da test prođe. Ponovnim pokretanjem testova provjereno je da novi test prolazi, kao i svi raniji testovi.

Na kraju je izvršena refaktorizacija koda. U ovoj fazi poboljšana je čitljivost koda, izdvojene su konstante i popravljena imena gdje je bilo potrebno. Nakon refaktorizacije ponovo su pokrenuti svi testovi kako bi se potvrdilo da ponašanje nije promijenjeno.

Za svako pravilo korištena su tri tipa commitova:

1. **RED commit** – dodan test koji pada,
2. **GREEN commit** – implementiran kod za prolazak testa,
3. **REFACTOR commit** – uređivanje koda bez promjene funkcionalnosti.

Na ovaj način sačuvana je kompletna historija razvoja funkcionalnosti.

## 4. Implementacija RefundCalculator klase

Klasa `RefundCalculator` zadužena je za izračunavanje iznosa novca koji korisniku treba biti vraćen nakon otkazivanja rezervacije.

U implementaciji su obuhvaćena pravila P1–P6. Svako pravilo je prvo opisano testom, a zatim implementirano u kodu. Testovi se nalaze u fajlu:

`RefundCalculatorTests.cs`

Testovi su imenovani prema dogovorenoj konvenciji:

`Metoda_Scenarij_OcekivaniRezultat`

Za pisanje testova korišten je AAA obrazac:

- **Arrange** – priprema podataka,
- **Act** – poziv metode koja se testira,
- **Assert** – provjera očekivanog rezultata.

Testovi ne sadrže dodatnu `if` ili `for` logiku, čime su ostali jednostavni i fokusirani na jednu funkcionalnost.

## 5. Implementacija RefundService klase

Klasa `RefundService` predstavlja servisni sloj koji koristi `RefundCalculator` i komunicira sa payment gateway komponentom.

Za potrebe odvajanja stvarnog payment sistema od poslovne logike uveden je interfejs:

`IPaymentGateway`

Na ovaj način payment gateway može biti zamijenjen testnom implementacijom ili mock objektom tokom testiranja. Time je omogućeno testiranje servisa bez stvarnog slanja zahtjeva prema eksternom sistemu.

Omer je implementirao pravilo P7, koje se odnosi na pozivanje payment gateway servisa i obradu povrata novca. Testovi za ovu funkcionalnost nalaze se u fajlu:

`RefundServiceTests.cs`

Korištenjem interfejsa postignuto je lakše testiranje i manja zavisnost `RefundService` klase od konkretne implementacije payment sistema.

## 6. Rezultati testiranja

Nakon svakog RED, GREEN i REFACTOR koraka pokretani su testovi pomoću naredbe:

```bash
dotnet test
```

Tokom RED faze očekivano je padao novo napisani test, dok su raniji testovi prolazili. Tokom GREEN faze implementiran je kod kojim je novi test prolazio. U REFACTOR fazi provjereno je da nakon promjena svi testovi i dalje prolaze.

Na kraju implementacije izvršena je kompletna provjera testova. Testovi za `RefundCalculator` i `RefundService` prolaze, čime je potvrđeno da implementacija zadovoljava definisana pravila P1–P7.

## 7. Organizacija commitova i spajanje grana

Svaki član je commitovao promjene sa svog GitHub naloga i na svojoj radnoj grani. Commitovi su organizovani prema TDD ciklusima, tako da se jasno vidi redoslijed:

- test koji pada,
- implementacija koja omogućava prolazak testa,
- refaktorizacija koda.

Nije korišten squash merge, jer bi se na taj način izgubila historija RED/GREEN/REFACTOR commitova. Pull Requestovi su spojeni opcijom **Create a merge commit**, čime je sačuvana kompletna historija razvoja.

## 8. Zaključak

Funkcionalnost povrata novca pri otkazivanju rezervacije implementirana je kroz saradnju Hamze i Omera. Posao je podijeljen na dva nezavisna dijela, čime je smanjena mogućnost konflikata u kodu.

Korištenjem TDD pristupa svako poslovno pravilo je prvo definisano testom, a zatim implementirano. Na ovaj način je omogućeno kontinuirano provjeravanje ispravnosti koda i lakše otkrivanje grešaka.

Implementirane su sljedeće komponente:

- `RefundCalculator` za izračun povrata novca,
- `IPaymentGateway` za apstrakciju payment sistema,
- `RefundService` za izvršavanje povrata,
- testovi za pravila P1–P7,
- dokumentacija o procesu razvoja.

Na kraju su svi testovi uspješno prošli, a historija razvoja je sačuvana kroz odvojene RED, GREEN i REFACTOR commitove.
