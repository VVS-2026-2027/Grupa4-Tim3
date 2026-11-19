using System.ComponentModel.DataAnnotations;
using ParkirajBa.Models;

namespace ParkirajBa.Tests.Models
{
    [TestClass]
    [TestCategory("Models")]
    [TestProperty("Autor", "Tim3")]
    public sealed class ApplicationUserTests
    {
        public static IEnumerable<object[]> ImenaIzCsv() =>
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "application_user_names.csv"))
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(';'))
                .Select(columns => new object[] { columns[0], bool.Parse(columns[1]) });

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void FullName_PostavljenoImeIPrezime_VracaPunoIme()
        {
            // Arrange
            var user = new ApplicationUser { FirstName = "Amina", LastName = "Hadžić" };

            // Act
            var fullName = user.FullName;

            // Assert
            Assert.AreEqual("Amina Hadžić", fullName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void CreatedAt_KreiranjeKorisnika_PostavljaUtcVrijeme()
        {
            // Arrange
            var prijeKreiranja = DateTime.UtcNow;

            // Act
            var user = new ApplicationUser();
            var nakonKreiranja = DateTime.UtcNow;

            // Assert
            Assert.IsTrue(user.CreatedAt >= prijeKreiranja && user.CreatedAt <= nakonKreiranja);
            Assert.AreEqual(DateTimeKind.Utc, user.CreatedAt.Kind);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(ImenaIzCsv))]
        public void FirstName_ProvjeraPodatakaIzCsv_VracaOcekivanuValidnost(string ime, bool ocekivano)
        {
            // Arrange
            var user = new ApplicationUser { FirstName = ime };
            var context = new ValidationContext(user) { MemberName = nameof(user.FirstName) };
            var errors = new List<ValidationResult>();

            // Act
            var validno = Validator.TryValidateProperty(user.FirstName, context, errors);

            // Assert
            Assert.AreEqual(ocekivano, validno);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public void FirstName_DuzeOdMaksimalneDuzine_NeProlaziValidaciju()
        {
            // Arrange
            var user = new ApplicationUser { FirstName = new string('A', 51) };
            var context = new ValidationContext(user) { MemberName = nameof(user.FirstName) };

            // Act
            var validno = Validator.TryValidateProperty(user.FirstName, context, new List<ValidationResult>());

            // Assert
            Assert.IsFalse(validno);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        [DataRow("Hadžić", true)]
        [DataRow("Hadžić1", false)]
        public void LastName_ProvjeraFormata_VracaOcekivanuValidnost(string prezime, bool ocekivano)
        {
            // Arrange
            var user = new ApplicationUser { LastName = prezime };
            var context = new ValidationContext(user) { MemberName = nameof(user.LastName) };
            var errors = new List<ValidationResult>();

            // Act
            var validno = Validator.TryValidateProperty(user.LastName, context, errors);

            // Assert
            Assert.AreEqual(ocekivano, validno);
        }

        [TestMethod]
        [TestCategory("Izuzeci")]
        [Priority(2)]
        [Timeout(2000)]
        public void FirstName_PraznoIme_BacaValidationException()
        {
            // Arrange
            var user = new ApplicationUser { FirstName = string.Empty };
            var context = new ValidationContext(user) { MemberName = nameof(user.FirstName) };

            // Act
            var exception = Assert.ThrowsExactly<ValidationException>(
                () => Validator.ValidateProperty(user.FirstName, context));

            // Assert
            Assert.AreEqual("Ime je obavezno.", exception.ValidationResult?.ErrorMessage);
        }
    }
}
