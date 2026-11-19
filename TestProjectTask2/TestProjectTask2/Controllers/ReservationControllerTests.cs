using System.Security.Claims;
using System.Text.Json;
using System.Timers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using ParkirajBa.Tests.Helpers;

namespace ParkirajBa.Tests.Controllers
{
    [TestClass]
    public class ReservationControllerTests
    {
        private ApplicationDbContext _db = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IParkingRepository> _repoMock = null!;
        private Mock<IEmailSender> _emailMock = null!;
        private TestSession _session = null!;
        private ReservationController _controller = null!;
        private ApplicationUser _testUser = null!;

        public record CijenaSlucaj(string pricingType, decimal jedinicnaCijena, int satiTrajanja, decimal ocekivanaCijena);

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _repoMock = new Mock<IParkingRepository>();
            _emailMock = new Mock<IEmailSender>();
            _session = new TestSession();

            _testUser = new ApplicationUser
            {
                Id = "user-1",
                Email = "test@parkiraj.ba",
                FirstName = "Test",
                LastName = "Korisnik"
            };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            var httpContext = new DefaultHttpContext { Session = _session };
            _controller = new ReservationController(_db, _userManagerMock.Object, _repoMock.Object, _emailMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _db.Dispose();
            _controller = null!;
        }

        // ─── POMOĆNE METODE ZA PODATKE I FAJLOVE ─────────────────────────────

        public static IEnumerable<object[]> TrajanjaIzCsv() =>
            File.ReadAllLines(Path.Combine("TestData", "trajanja.csv")).Skip(1)
                .Select(l => l.Split(';'))
                .Select(k => new object[] { k[0], int.Parse(k[1]), k[2] });

        public static IEnumerable<object[]> CijeneIzJson() =>
            JsonSerializer.Deserialize<List<CijenaSlucaj>>(File.ReadAllText(Path.Combine("TestData", "cijene.json")))!
                .Select(s => new object[] { s.pricingType, s.jedinicnaCijena, s.satiTrajanja, s.ocekivanaCijena });

        private void PostaviNeprijavljenogKorisnika() =>
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        private Ticket KreirajTicket(bool isPaid = true, bool isCancelled = false, bool entered = false, int satiDoIsteka = 2) =>
            new Ticket
            {
                ParkingObjectId = 1,
                IsPaid = isPaid,
                IsCancelled = isCancelled,
                EnteredParking = entered,
                ExpiresAt = DateTime.Now.AddHours(satiDoIsteka),
                Price = 10m
            };

        // ─── GET: Create ─────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        [Timeout(2000)]
        public async Task CreateGet_NepostojeciParking_PreusmjeravaNaObjekte()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(99)).ReturnsAsync((ParkingObject?)null);
            // Act
            var rez = await _controller.Create(99) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Objekti", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        [Timeout(2000)]
        public async Task CreateGet_PostojeciParking_VracaViewSaPodacima()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            _repoMock.Setup(r => r.GetPricingsByParkingIdAsync(1)).ReturnsAsync((List<Pricing>?)null);
            // Act
            var rez = await _controller.Create(1) as ViewResult;
            // Assert
            Assert.IsNotNull(rez);
        }

        // ─── POST: Create ────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            PostaviNeprijavljenogKorisnika();
            // Act
            var rez = await _controller.Create(1, DateTime.Now, DateTime.Now.AddHours(1), "Hourly") as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Login", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_NepostojeciParking_PreusmjeravaNaObjekte()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(99)).ReturnsAsync((ParkingObject?)null);
            // Act
            var rez = await _controller.Create(99, DateTime.Now, DateTime.Now.AddHours(1), "Hourly") as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Objekti", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_NullStartsAt_VracaViewSaGreskom()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            // Act
            var rez = await _controller.Create(1, null, DateTime.Now.AddHours(1), "Hourly") as ViewResult;
            // Assert
            Assert.AreEqual("Molimo unesite datum i vrijeme početka rezervacije.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_NullExpiresAt_VracaViewSaGreskom()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            // Act
            var rez = await _controller.Create(1, DateTime.Now, null, "Hourly") as ViewResult;
            // Assert
            Assert.AreEqual("Molimo unesite datum i vrijeme isteka rezervacije.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_ExpiresAtPrijeStartsAt_VracaViewSaGreskom()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            var start = DateTime.Now.AddHours(2);
            // Act
            var rez = await _controller.Create(1, start, start.AddHours(-1), "Hourly") as ViewResult;
            // Assert
            Assert.AreEqual("Datum isteka mora biti nakon početka rezervacije.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_StartsAtUProslosti_KorigujeVrijemeIPreusmjeravaNaCheckout()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            // Act
            var rez = await _controller.Create(1, DateTime.Now.AddMinutes(-10), DateTime.Now.AddHours(2), null) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Checkout", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [TestProperty("Autor", "Omer Masleša")]
        [Priority(1)]
        [DynamicData(nameof(TrajanjaIzCsv), DynamicDataSourceType.Method)]
        public async Task CreatePost_TrajanjaIzCsv_ProvjeravaMaksimalnoTrajanje(string tip, int sati, string ocekivanaAkcija)
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            var start = DateTime.Now;
            // Act
            var rez = await _controller.Create(1, start, start.AddHours(sati), tip) as RedirectToActionResult;
            // Assert
            Assert.AreEqual(ocekivanaAkcija, rez?.ActionName ?? string.Empty);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [TestProperty("Autor", "Omer Masleša")]
        [Priority(1)]
        [DynamicData(nameof(CijeneIzJson), DynamicDataSourceType.Method)]
        public async Task CreatePost_CijeneIzJson_IzracunavaIspravnuCijenuUSesiji(string tip, decimal cijenaPoJedinici, int sati, decimal ocekivano)
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            _repoMock.Setup(r => r.GetActivePricingAsync(1, It.IsAny<PricingType>())).ReturnsAsync(new Pricing { price = cijenaPoJedinici });
            var start = DateTime.Now;
            // Act
            await _controller.Create(1, start, start.AddHours(sati), tip);
            // Assert
            StringAssert.Contains(_session.GetString("PendingReservation"), $"\"Price\":{ocekivano.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        [TestMethod]
        [TestCategory("Create")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task CreatePost_NemaCijeneZaDaily_KoristiHourlyFallback()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(new ParkingObject());
            _repoMock.Setup(r => r.GetActivePricingAsync(1, PricingType.Hourly)).ReturnsAsync(new Pricing { price = 2m });
            // Act
            await _controller.Create(1, DateTime.Now, DateTime.Now.AddDays(2), "Daily");
            // Assert
            _repoMock.Verify(r => r.GetActivePricingAsync(1, PricingType.Hourly), Times.Once);
        }

        // ─── GET: Details ────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Details")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Details_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            PostaviNeprijavljenogKorisnika();
            // Act
            var rez = await _controller.Details(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Login", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Details")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Details_NepostojecaRezervacija_PreusmjeravaNaRezervacije()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync((Ticket?)null);
            // Act
            var rez = await _controller.Details(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Rezervacije", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Details")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Details_PostojecaRezervacija_VracaViewSaTicketom()
        {
            // Arrange
            var ticket = KreirajTicket();
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(ticket);
            // Act
            var rez = await _controller.Details(1) as ViewResult;
            // Assert
            Assert.AreSame(ticket, rez?.Model);
        }

        // ─── GET: Extend ─────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendGet_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            PostaviNeprijavljenogKorisnika();
            // Act
            var rez = await _controller.Extend(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Login", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendGet_NeplacenaRezervacija_PreusmjeravaNaRezervacije()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(isPaid: false));
            // Act
            var rez = await _controller.Extend(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Rezervacije", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendGet_IsteklaRezervacija_PreusmjeravaNaDetails()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(satiDoIsteka: -1));
            // Act
            var rez = await _controller.Extend(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Details", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendGet_AktivnaPlacenaRezervacija_VracaView()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket());
            // Act
            var rez = await _controller.Extend(1) as ViewResult;
            // Assert
            Assert.IsNotNull(rez);
        }

        // ─── POST: Extend ────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendPost_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            PostaviNeprijavljenogKorisnika();
            // Act
            var rez = await _controller.Extend(1, 2) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Login", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendPost_OtkazanaRezervacija_PreusmjeravaNaRezervacije()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(isCancelled: true));
            // Act
            var rez = await _controller.Extend(1, 2) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Rezervacije", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendPost_IsteklaRezervacija_PreusmjeravaNaDetails()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(satiDoIsteka: -2));
            // Act
            var rez = await _controller.Extend(1, 2) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Details", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Granice")]
        [TestProperty("Autor", "Omer Masleša")]
        [DataRow(0, "Details")]
        [DataRow(1, "Checkout")]
        [DataRow(24, "Checkout")]
        [DataRow(25, "Details")]
        public async Task ExtendPost_GranicniBrojSati_VracaOcekivanuAkciju(int extraHours, string ocekivanaAkcija)
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket());
            // Act
            var rez = await _controller.Extend(1, extraHours) as RedirectToActionResult;
            // Assert
            Assert.AreEqual(ocekivanaAkcija, rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Extend")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task ExtendPost_ValidnoProduzenje_AzuriraCijenuIDeaktiviraQrKod()
        {
            // Arrange
            var ticket = KreirajTicket();
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(ticket);
            _repoMock.Setup(r => r.GetActivePricingAsync(1, PricingType.Hourly)).ReturnsAsync(new Pricing { price = 3m });
            // Act
            await _controller.Extend(1, 4);
            // Assert
            Assert.AreEqual(12m, ticket.AdditionalCharge);
            Assert.IsFalse(ticket.QrCodeActive);
        }

        // ─── POST: Cancel ────────────────────────────────────────────────────

        [TestMethod]
        [TestCategory("Cancel")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Cancel_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            PostaviNeprijavljenogKorisnika();
            // Act
            var rez = await _controller.Cancel(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Login", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Cancel")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Cancel_NepostojecaRezervacija_PreusmjeravaNaRezervacije()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync((Ticket?)null);
            // Act
            var rez = await _controller.Cancel(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Rezervacije", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Cancel")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Cancel_IsteklaRezervacija_PreusmjeravaNaDetails()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(satiDoIsteka: -1));
            // Act
            var rez = await _controller.Cancel(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Details", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Cancel")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Cancel_EvidentiranUlazak_PreusmjeravaNaDetails()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(entered: true));
            // Act
            var rez = await _controller.Cancel(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Details", rez?.ActionName);
        }

        [TestMethod]
        [TestCategory("Mock")]
        [TestProperty("Autor", "Omer Masleša")]
        [Priority(1)]
        public async Task Cancel_PlacenaRezervacija_OslobadjaMjestoISaljeEmail()
        {
            // Arrange
            var ticket = KreirajTicket(isPaid: true);
            var parking = new ParkingObject { availableSpots = 4, totalSpots = 10 };
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(ticket);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parking);
            // Act
            await _controller.Cancel(1);
            // Assert
            Assert.IsTrue(ticket.IsCancelled);
            Assert.AreEqual(5, parking.availableSpots);
            _emailMock.Verify(e => e.SendEmailAsync(_testUser.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        [TestProperty("Autor", "Omer Masleša")]
        public async Task Cancel_SlanjeEmailaBacaIzuzetak_IpakOtkazujeRezervaciju()
        {
            // Arrange
            _repoMock.Setup(r => r.GetTicketByIdAsync(1, _testUser.Id)).ReturnsAsync(KreirajTicket(isPaid: false));
            _emailMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP greška"));
            // Act
            var rez = await _controller.Cancel(1) as RedirectToActionResult;
            // Assert
            Assert.AreEqual("Rezervacije", rez?.ActionName);
            Assert.AreEqual("Rezervacija je uspješno otkazana.", _controller.TempData["Success"]);
        }
    }
}