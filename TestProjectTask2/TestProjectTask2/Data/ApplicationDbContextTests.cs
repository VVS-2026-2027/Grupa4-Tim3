using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;

namespace ParkirajBa.Tests.Data
{
    [TestClass]
    [TestCategory("Data")]
    [TestProperty("Autor", "Damir")]
    public class ApplicationDbContextTests
    {
        private static string _klasaInicijalizovana = string.Empty;
        private ApplicationDbContext _context = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _klasaInicijalizovana = "ApplicationDbContextTests";
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _klasaInicijalizovana = string.Empty;
        }

        [TestInitialize]
        public void TestInit()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Dispose();
        }

        // ── 1. PROVJERA INICIJALIZACIJE DBSET KOLEKCIJA ───────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void DbSets_InicijalizacijaKonteksta_SviDbSetoviSuKreirani()
        {
            // Arrange – kontekst kreiran u TestInitialize

            // Act
            var sviKreirani = _context.ParkingObject != null &&
                              _context.Pricing != null &&
                              _context.Owners != null &&
                              _context.Administrators != null &&
                              _context.Tickets != null &&
                              _context.ParkingImages != null &&
                              _context.ParkingRequest != null;

            // Assert
            Assert.IsTrue(sviKreirani);
        }

        // ── 2. PROVJERA FLUENT API MAPIRANJA TABELA (ONMODELCREATING) ──────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow(typeof(ParkingObject), "ParkingObject")]
        [DataRow(typeof(Pricing), "Pricing")]
        [DataRow(typeof(OwnerProfile), "Owner")]
        [DataRow(typeof(AdminProfile), "Administrator")]
        [DataRow(typeof(ParkingImage), "ParkingImage")]
        [DataRow(typeof(ParkingRequest), "ParkingRequest")]
        public void OnModelCreating_MapiranjeEntiteta_PostavljaIspravanNazivTabele(Type tipEntiteta, string ocekivaniNazivTabele)
        {
            // Arrange
            var entityType = _context.Model.FindEntityType(tipEntiteta);

            // Act
            var stvarniNaziv = entityType?.GetTableName();

            // Assert
            Assert.AreEqual(ocekivaniNazivTabele, stvarniNaziv);
        }

        // ── 3. PROVJERA DECIMAL(18,2) KONFIGURACIJE KOLONA NA TICKET-U ────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        [DataRow("Price")]
        [DataRow("AdditionalCharge")]
        [DataRow("TotalAdditionalChargesPaid")]
        public void OnModelCreating_TicketDecimalPolja_PostavljaTipDecimal18_2(string nazivPolja)
        {
            // Arrange
            var property = _context.Model.FindEntityType(typeof(Ticket))?.FindProperty(nazivPolja);

            // Act
            var tipKolone = property?.FindAnnotation("Relational:ColumnType")?.Value?.ToString();

            // Assert
            Assert.AreEqual("decimal(18,2)", tipKolone);
        }

        // ── 4. PROVJERA RELACIJE I KASKADNOG BRISANJA (PRICING -> PARKING) ────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void OnModelCreating_RelacijaPricingParkingObject_PostavljaCascadeBrisanje()
        {
            // Arrange
            var pricingEntity = _context.Model.FindEntityType(typeof(Pricing));

            // Act
            var straniKljuc = pricingEntity?.GetForeignKeys()
                .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "ParkingObjectID"));

            // Assert
            Assert.IsNotNull(straniKljuc);
            Assert.AreEqual(DeleteBehavior.Cascade, straniKljuc.DeleteBehavior);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task SaveChangesAsync_BrisanjeParkinga_KaskadnoBrisePovezaneCjenovnike()
        {
            // Arrange
            var parking = new ParkingObject { ID = 1, name = "Centar", address = "Titova 1", totalSpots = 30, availableSpots = 30 };
            parking.Pricings.Add(new Pricing { ID = 1, pricingType = PricingType.Hourly, price = 2.50m });
            _context.ParkingObject.Add(parking);
            await _context.SaveChangesAsync();

            // Act
            _context.ParkingObject.Remove(parking);
            await _context.SaveChangesAsync();

            // Assert
            Assert.AreEqual(0, _context.Pricing.Count());
        }

        // ── 5. PROVJERA UPISA I ČITANJA ENTITETA U BAZI ───────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task SaveChangesAsync_DodavanjeZahtjevaIParkingSlike_UspjesnoPohranjujePodatke()
        {
            // Arrange
            _context.ParkingRequest.Add(new ParkingRequest { ID = 1, ParkingID = 10, TimeSent = DateTime.UtcNow });
            _context.ParkingImages.Add(new ParkingImage { ID = 1, ParkingObjectID = 10, ImagePath = "/slike/1.jpg", Position = 1 });

            // Act
            var brojUpisanih = await _context.SaveChangesAsync();

            // Assert
            Assert.AreEqual(2, brojUpisanih);
            Assert.AreEqual(1, _context.ParkingRequest.Count());
        }
    }
}