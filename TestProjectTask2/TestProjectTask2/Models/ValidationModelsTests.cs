using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using ParkirajBa.Models;

namespace ParkirajBa.Tests.Models
{
    [TestClass]
    [TestCategory("Models")]
    [TestProperty("Autor", "Tim3")]
    public sealed class ValidationModelsTests
    {
        public record CardNumberCase(string broj, bool validan);

        public static IEnumerable<object[]> BrojeviKarticaIzJson() =>
            JsonSerializer.Deserialize<List<CardNumberCase>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "validation_card_numbers.json")))!
                .Select(testCase => new object[] { testCase.broj, testCase.validan });

        public record ReservationPlateCase(string tablica, string normalizovana, bool validna);

        public static IEnumerable<object[]> RegistracijeIzJson() =>
            JsonSerializer.Deserialize<List<ReservationPlateCase>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "validation_plates.json")))!
                .Select(testCase => new object[] { testCase.tablica, testCase.normalizovana, testCase.validna });

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void ExtendedRegisterViewModel_IspravniPodaci_ProlazeValidaciju()
        {
            // Arrange
            var model = new ExtendedRegisterViewModel
            {
                Ime = "Amina", Prezime = "Hadžić", Email = "amina@example.com",
                Password = "Lozinka1!", ConfirmPassword = "Lozinka1!"
            };
            var errors = new List<ValidationResult>();

            // Act
            var validno = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

            // Assert
            Assert.IsTrue(validno);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void ExtendedRegisterViewModel_PraznaObaveznaPolja_NeProlazeValidaciju()
        {
            // Arrange
            var model = new ExtendedRegisterViewModel();
            var errors = new List<ValidationResult>();

            // Act
            var validno = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

            // Assert
            Assert.IsFalse(validno);
            Assert.AreEqual(5, errors.Count);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(RegistracijeIzJson))]
        public void ReservationViewModel_RegistracijeIzJson_NormalizujeIValidira(string tablica, string normalizovana, bool validna)
        {
            // Arrange
            var pocetak = DateTime.Now.AddHours(1);
            var model = new ReservationViewModel
            {
                PlateNumber = tablica, StartTime = pocetak, EndTime = pocetak.AddHours(1), DurationHours = 1
            };

            // Act
            var errors = model.Validate(new ValidationContext(model)).ToList();
            var validacijaTablice = ValidirajSvojstvo(model, nameof(model.PlateNumber), model.PlateNumber);

            // Assert
            Assert.AreEqual(normalizovana, model.PlateNumber);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(validna, validacijaTablice);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public void ReservationViewModel_IdIKomentar_PostavljajuSeIVracaju()
        {
            // Arrange
            var model = NapraviRezervaciju(DateTime.Now.AddHours(1), DateTime.Now.AddHours(2));

            // Act
            model.ParkingObjectId = 42;
            model.Comment = "Test komentar";

            // Assert
            Assert.AreEqual(42, model.ParkingObjectId);
            Assert.AreEqual("Test komentar", model.Comment);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void ReservationViewModel_PocetakUProslosti_VracaGreskuZaPocetak()
        {
            // Arrange
            var model = NapraviRezervaciju(DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1));

            // Act
            var errors = model.Validate(new ValidationContext(model)).ToList();

            // Assert
            Assert.AreEqual(nameof(model.StartTime), errors.Single().MemberNames.Single());
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow(0)]
        [DataRow(-1)]
        public void ReservationViewModel_KrajNijeNakonPocetka_VracaGreskuZaKraj(int pomakMinuta)
        {
            // Arrange
            var pocetak = DateTime.Now.AddHours(1);
            var model = NapraviRezervaciju(pocetak, pocetak.AddMinutes(pomakMinuta));

            // Act
            var errors = model.Validate(new ValidationContext(model)).ToList();

            // Assert
            Assert.AreEqual(nameof(model.EndTime), errors.Single().MemberNames.Single());
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(BrojeviKarticaIzJson))]
        public void CardPaymentViewModel_BrojeviIzJson_ProvjeravaLuhn(string broj, bool validan)
        {
            // Arrange
            var model = NapraviPlacanje(broj, DateTime.UtcNow.AddYears(2).ToString("MM/yy", CultureInfo.InvariantCulture));

            // Act
            var errors = model.Validate(new ValidationContext(model)).ToList();

            // Assert
            Assert.AreEqual(validan, errors.All(error => !error.MemberNames.Contains(nameof(model.CardNumber))));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void CardPaymentViewModel_IsteklaKartica_VracaGreskuZaIstek()
        {
            // Arrange
            var datumIsteka = DateTime.UtcNow.AddMonths(-2).ToString("MM/yy", CultureInfo.InvariantCulture);
            var model = NapraviPlacanje("4111111111111111", datumIsteka);

            // Act
            var errors = model.Validate(new ValidationContext(model)).ToList();

            // Assert
            Assert.AreEqual(nameof(model.ExpiryDate), errors.Single().MemberNames.Single());
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public void CardPaymentViewModel_NeispravanFormatIsteka_PadaValidacijaAtributa()
        {
            // Arrange
            var model = NapraviPlacanje("4111111111111111", "13/29");
            var errors = new List<ValidationResult>();

            // Act
            var validno = Validator.TryValidateObject(model, new ValidationContext(model), errors, true);

            // Assert
            Assert.IsFalse(validno);
            CollectionAssert.Contains(errors.SelectMany(error => error.MemberNames).ToList(), nameof(model.ExpiryDate));
        }

        private static bool ValidirajSvojstvo(object model, string nazivSvojstva, object vrijednost)
        {
            var context = new ValidationContext(model) { MemberName = nazivSvojstva };
            return Validator.TryValidateProperty(vrijednost, context, new List<ValidationResult>());
        }

        private static ReservationViewModel NapraviRezervaciju(DateTime pocetak, DateTime kraj) => new()
        {
            PlateNumber = "AB-123", StartTime = pocetak, EndTime = kraj, DurationHours = 1
        };

        private static CardPaymentViewModel NapraviPlacanje(string brojKartice, string datumIsteka) => new()
        {
            TicketId = 1, CardName = "Amina Hadzic", CardNumber = brojKartice, ExpiryDate = datumIsteka, CVV = "123"
        };
    }
}
