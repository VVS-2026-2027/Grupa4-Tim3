using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using System.Net.Sockets;
using System.Text.Json;
using System.Timers;
using System.Xml.Linq;

namespace ParkirajBa.Tests.Controllers
{
    [TestClass]
    [TestCategory("Controllers")]
    [TestProperty("Autor", "Damir")]
    public class AdminControllerTests
    {
        private static string _klasaInicijalizovana = string.Empty;

        private ApplicationDbContext _context = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<RoleManager<IdentityRole>> _roleManagerMock = null!;
        private Mock<IRequestRepository> _requestRepoMock = null!;
        private Mock<IParkingRepository> _parkingRepoMock = null!;
        private AdminController _controller = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _klasaInicijalizovana = "AdminControllerTests";
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

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object, null!, null!, null!, null!);

            _requestRepoMock = new Mock<IRequestRepository>();
            _parkingRepoMock = new Mock<IParkingRepository>();

            _controller = new AdminController(
                _context,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _requestRepoMock.Object,
                _parkingRepoMock.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Dispose();
            _controller.Dispose();
        }

        // ── DATA-DRIVEN IZVORI PODATAKA (CSV, JSON, XML) ──────────────────────

        public static IEnumerable<object[]> UlogeIzCsv() =>
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "uloge.csv"))
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(';'))
                .Select(kolone => new object[] { kolone[0].Trim(), kolone[1].Trim() });

        public record RezervacijaFilterSlucaj(string? status, int ocekivaniBroj);

        public static IEnumerable<object[]> RezervacijeFilterIzJson() =>
            JsonSerializer.Deserialize<List<RezervacijaFilterSlucaj>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "rezervacije_filter.json")))!
                .Select(s => new object[] { s.status!, s.ocekivaniBroj });

        public static IEnumerable<object[]> KorisniciPretragaIzXml() =>
            XDocument.Load(Path.Combine(AppContext.BaseDirectory, "TestData", "korisnici_pretraga.xml"))
                .Descendants("slucaj")
                .Select(s => new object[]
                {
                    (string)s.Attribute("search")!,
                    (string)s.Attribute("role")!,
                    (int)s.Attribute("ocekivaniBroj")!
                });

        // ── POMOĆNE METODE ZA PRIPREMU PODATAKA (BEZ LOGIKE U TESTOVIMA) ───────

        private ApplicationUser NapraviKorisnika(string id, string ime, string prezime, string? email) =>
            new ApplicationUser { Id = id, FirstName = ime, LastName = prezime, Email = email, UserName = email ?? id };

        private ParkingObject NapraviParking(int id, string naziv, int? totalSpots = 20, int availableSpots = 10) =>
            new ParkingObject
            {
                ID = id,
                name = naziv,
                address = "Zmaja od Bosne 1",
                totalSpots = totalSpots,
                availableSpots = availableSpots
            };

        private void PripremiPodatkeZaDashboard()
        {
            var sada = DateTime.Now;
            var korisnici = new List<ApplicationUser>
            {
                NapraviKorisnika("1", "Admin", "Jedan", "admin@test.ba"),
                NapraviKorisnika("2", "Owner", "Jedan", "owner@test.ba"),
                NapraviKorisnika("3", "User", "Jedan", "user@test.ba")
            };

            _userManagerMock.Setup(u => u.Users).Returns(korisnici.AsQueryable());
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Owner")).ReturnsAsync(new List<ApplicationUser> { korisnici[1] });
            _userManagerMock.Setup(u => u.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<ApplicationUser> { korisnici[0] });

            var p1 = NapraviParking(1, "Centar", 50, 20);
            var p2 = NapraviParking(2, "Ilidza", null, 5);
            _context.ParkingObject.Add(p1);

            _context.Tickets.AddRange(
                new Ticket { Id = 1, ApplicationUserId = "3", ParkingObjectId = 1, Price = 10m, TotalAdditionalChargesPaid = 2m, IsPaid = true, IsCancelled = false, IssuedAt = sada.AddHours(-1), ExpiresAt = sada.AddHours(2) },
                new Ticket { Id = 2, ApplicationUserId = "3", ParkingObjectId = 1, Price = 15m, IsPaid = true, IsCancelled = false, IssuedAt = sada.AddMonths(-1), ExpiresAt = sada.AddMonths(-1).AddHours(2) },
                new Ticket { Id = 3, ApplicationUserId = "3", ParkingObjectId = 2, Price = 8m, IsPaid = false, IsCancelled = false, IssuedAt = sada.AddMinutes(-30), ExpiresAt = null },
                new Ticket { Id = 4, ApplicationUserId = "3", ParkingObjectId = 1, Price = 20m, IsPaid = true, IsCancelled = true, IssuedAt = sada }
            );
            _context.SaveChanges();

            _parkingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingObject> { p1, p2 });
        }

        private void PripremiKorisnikeZaPretragu()
        {
            var u1 = NapraviKorisnika("1", "Amir", "Hadzic", "amir@test.ba");
            var u2 = NapraviKorisnika("2", "Selma", "Kovac", "owner@test.ba");
            var u3 = NapraviKorisnika("3", "Tarik", "Begic", null);

            _userManagerMock.Setup(u => u.Users).Returns(new List<ApplicationUser> { u1, u2, u3 }.AsQueryable());
            _userManagerMock.Setup(u => u.GetRolesAsync(u1)).ReturnsAsync(new List<string> { "User" });
            _userManagerMock.Setup(u => u.GetRolesAsync(u2)).ReturnsAsync(new List<string> { "Owner" });
            _userManagerMock.Setup(u => u.GetRolesAsync(u3)).ReturnsAsync(new List<string> { "Admin" });
        }

        private void PripremiRezervacijeZaFiltriranje()
        {
            var sada = DateTime.Now;
            var korisnik = NapraviKorisnika("1", "Amina", "Test", "amina@test.ba");
            var parking = NapraviParking(1, "Centar");
            _context.Users.Add(korisnik);
            _context.ParkingObject.Add(parking);
            _context.Tickets.AddRange(
                new Ticket { Id = 1, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik, ParkingObjectId = parking.ID, ParkingObject = parking, IsPaid = true, IsCancelled = false, IssuedAt = sada.AddHours(-1), ExpiresAt = sada.AddHours(2) },
                new Ticket { Id = 2, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik, ParkingObjectId = parking.ID, ParkingObject = parking, IsPaid = true, IsCancelled = false, IssuedAt = sada.AddHours(-2), ExpiresAt = null },
                new Ticket { Id = 3, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik, ParkingObjectId = parking.ID, ParkingObject = parking, IsPaid = true, IsCancelled = false, IssuedAt = sada.AddHours(-5), ExpiresAt = sada.AddHours(-1) },
                new Ticket { Id = 4, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik, ParkingObjectId = parking.ID, ParkingObject = parking, IsPaid = false, IsCancelled = false, IssuedAt = sada.AddMinutes(-10), ExpiresAt = sada.AddHours(1) },
                new Ticket { Id = 5, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik, ParkingObjectId = parking.ID, ParkingObject = parking, IsPaid = true, IsCancelled = true, IssuedAt = sada.AddHours(-3), ExpiresAt = sada.AddHours(3) }
            );
            _context.SaveChanges();
        }

        // ── 1. DASHBOARD TESTOVI ──────────────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task Dashboard_PostojeciPodaci_VracaViewSaIspravnomStatistikom()
        {
            // Arrange
            PripremiPodatkeZaDashboard();

            // Act
            var rezultat = await _controller.Dashboard() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(3, _controller.ViewBag.UserCount);
            Assert.AreEqual(1, _controller.ViewBag.RegularCount);
            Assert.AreEqual(27m, _controller.ViewBag.TotalRevenue);
            Assert.AreEqual(50, _controller.ViewBag.TotalSpots);
        }

        // ── 2. USERS TESTOVI (UKLJUČUJUĆI XML DATA-DRIVEN) ────────────────────

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(KorisniciPretragaIzXml), DynamicDataSourceType.Method)]
        public async Task Users_FilterIzXml_VracaOcekivaniBrojKorisnika(string search, string role, int ocekivaniBroj)
        {
            // Arrange
            PripremiKorisnikeZaPretragu();

            // Act
            var rezultat = await _controller.Users(search, role) as ViewResult;
            var model = rezultat?.Model as List<AdminUserViewModel>;

            // Assert
            Assert.IsNotNull(model);
            Assert.AreEqual(ocekivaniBroj, model.Count);
        }

        // ── 3. TOGGLELOCK TESTOVI ─────────────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ToggleLock_NepostojeciKorisnik_VracaNotFound()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.ToggleLock("999");

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ToggleLock_ZakljucanKorisnik_OtkljucavaIPreusmjerava()
        {
            // Arrange
            var korisnik = NapraviKorisnika("1", "Amir", "Hadzic", "amir@test.ba");
            korisnik.LockoutEnd = DateTimeOffset.Now.AddDays(5);
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);

            // Act
            var rezultat = await _controller.ToggleLock("1") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Users", rezultat.ActionName);
            _userManagerMock.Verify(u => u.SetLockoutEndDateAsync(korisnik, null), Times.Once);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ToggleLock_OtkljucanKorisnik_ZakljucavaIPreusmjerava()
        {
            // Arrange
            var korisnik = NapraviKorisnika("1", "Amir", "Hadzic", "amir@test.ba");
            korisnik.LockoutEnd = null;
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);

            // Act
            var rezultat = await _controller.ToggleLock("1") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Users", rezultat.ActionName);
            _userManagerMock.Verify(u => u.SetLockoutEndDateAsync(korisnik, It.Is<DateTimeOffset?>(d => d > DateTimeOffset.Now)), Times.Once);
        }

        // ── 4. CHANGEROLE TESTOVI (UKLJUČUJUĆI CSV DATA-DRIVEN) ───────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ChangeRole_NepostojeciKorisnik_VracaNotFound()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.ChangeRole("999", "Admin");

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(UlogeIzCsv), DynamicDataSourceType.Method)]
        public async Task ChangeRole_UlogaIzCsv_MijenjaUloguIPostavljaPoruku(string novaUloga, string ocekivaniPrikaz)
        {
            // Arrange
            var korisnik = NapraviKorisnika("1", "Amir", "Hadzic", "amir@test.ba");
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "User" });

            // Act
            var rezultat = await _controller.ChangeRole("1", novaUloga) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            StringAssert.Contains(_controller.TempData["Success"]?.ToString(), ocekivaniPrikaz);
            _userManagerMock.Verify(u => u.AddToRoleAsync(korisnik, novaUloga), Times.Once);
        }

        // ── 5. DELETEUSER TESTOVI ─────────────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteUser_NepostojeciKorisnik_VracaNotFound()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.DeleteUser("999");

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteUser_PostojeciKorisnik_BriseKarteIKorisnika()
        {
            // Arrange
            var korisnik = NapraviKorisnika("1", "Amir", "Hadzic", "amir@test.ba");
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);
            _context.Tickets.Add(new Ticket { Id = 10, ApplicationUserId = "1", ParkingObjectId = 1, Price = 5m });
            await _context.SaveChangesAsync();

            // Act
            var rezultat = await _controller.DeleteUser("1") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, _context.Tickets.Count());
            _userManagerMock.Verify(u => u.DeleteAsync(korisnik), Times.Once);
        }

        // ── 6. REPORTS TESTOVI ────────────────────────────────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task Reports_BezParametara_KoristiTrenutnuGodinuIMjesec()
        {
            // Arrange
            var sada = DateTime.Now;

            // Act
            var rezultat = await _controller.Reports(null, null) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(sada.Year, _controller.ViewBag.SelYear);
            Assert.AreEqual(sada.Month, _controller.ViewBag.SelMonth);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task Reports_ZadaniMjesecIGodina_RacunaPrihodePoParkinguIDanu()
        {
            // Arrange
            var parking = NapraviParking(1, "Skenderija");
            var user = NapraviKorisnika("1", "Amina", "Test", "amina@test.ba");
            _context.ParkingObject.Add(parking);
            _context.Users.Add(user);
            _context.Tickets.AddRange(
                new Ticket { Id = 1, ApplicationUserId = user.Id, ApplicationUser = user, ParkingObjectId = parking.ID, ParkingObject = parking, Price = 10m, IsPaid = true, IsCancelled = false, IssuedAt = new DateTime(2025, 5, 10) },
                new Ticket { Id = 2, ApplicationUserId = user.Id, ApplicationUser = user, ParkingObjectId = parking.ID, ParkingObject = parking, Price = 5m, IsPaid = true, IsCancelled = false, IssuedAt = new DateTime(2025, 5, 10) },
                new Ticket { Id = 3, ApplicationUserId = user.Id, ApplicationUser = user, ParkingObjectId = parking.ID, ParkingObject = parking, Price = 7m, IsPaid = false, IsCancelled = false, IssuedAt = new DateTime(2025, 5, 12) }
            );
            await _context.SaveChangesAsync();

            // Act
            var rezultat = await _controller.Reports(2025, 5) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(15m, _controller.ViewBag.TotalRevenue);
            Assert.AreEqual(3, _controller.ViewBag.TotalRes);
            Assert.AreEqual(2, _controller.ViewBag.PaidRes);
            Assert.AreEqual(1, _controller.ViewBag.UnpaidRes);
        }

        // ── 7. PARKINGS & PARKINGDETAILS TESTOVI ──────────────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task Parkings_PozivAkcije_VracaViewSaListomParkinga()
        {
            // Arrange
            var lista = new List<ParkingObject> { NapraviParking(1, "BBI"), NapraviParking(2, "SCC") };
            _parkingRepoMock.Setup(r => r.GetAllWithOwnerAsync()).ReturnsAsync(lista);

            // Act
            var rezultat = await _controller.Parkings() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            CollectionAssert.AreEqual(lista, rezultat.Model as List<ParkingObject>);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ParkingDetails_NepostojeciParking_PreusmjeravaNaParkingsSaGreskom()
        {
            // Arrange
            _parkingRepoMock.Setup(r => r.GetByIdWithPricingsAsync(99)).ReturnsAsync((ParkingObject?)null);

            // Act
            var rezultat = await _controller.ParkingDetails(99) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Parkings", rezultat.ActionName);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ParkingDetails_PostojeciParking_VracaViewSaCjenovnikomISlikom()
        {
            // Arrange
            var parking = NapraviParking(1, "Aria");
            _parkingRepoMock.Setup(r => r.GetByIdWithPricingsAsync(1)).ReturnsAsync(parking);
            _parkingRepoMock.Setup(r => r.GetPricingsByParkingIdAsync(1)).ReturnsAsync(new List<Pricing>());
            _parkingRepoMock.Setup(r => r.GetPrimaryImageByParkingIDAsync(1)).ReturnsAsync(new ParkingImage { ID = 1, ImagePath = "/img/1.jpg" });

            // Act
            var rezultat = await _controller.ParkingDetails(1) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreSame(parking, rezultat.Model);
            Assert.IsNotNull(_controller.ViewBag.PrimaryImage);
        }

        // ── 8. UPLOADPARKINGIMAGE TESTOVI ─────────────────────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task UploadParkingImage_PraznaSlika_PostavljaGreskuIPreusmjerava()
        {
            // Arrange
            var praznaSlikaMock = new Mock<IFormFile>();
            praznaSlikaMock.Setup(f => f.Length).Returns(0);

            // Act
            var rezultat = await _controller.UploadParkingImage(1, praznaSlikaMock.Object) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("ParkingDetails", rezultat.ActionName);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task UploadParkingImage_ValidnaSlika_ZamjenjujePrimarnuSliku()
        {
            // Arrange
            _context.ParkingImages.Add(new ParkingImage { ID = 1, ParkingObjectID = 1, Position = 1, ImagePath = "/stara.jpg" });
            await _context.SaveChangesAsync();
            var slikaMock = new Mock<IFormFile>();
            slikaMock.Setup(f => f.Length).Returns(1024);

            // Act
            var rezultat = await _controller.UploadParkingImage(1, slikaMock.Object) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, _context.ParkingImages.Count());
            _parkingRepoMock.Verify(r => r.SaveParkingImageByIDAsync(slikaMock.Object, 1, 1), Times.Once);
        }

        // ── 9. DELETEPARKING TESTOVI ──────────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteParking_NepostojeciParking_VracaNotFound()
        {
            // Arrange
            _parkingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ParkingObject?)null);

            // Act
            var rezultat = await _controller.DeleteParking(99);

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteParking_PostojeciParking_BriseParkingIPripadajuceKarte()
        {
            // Arrange
            var parking = NapraviParking(1, "Vijecnica");
            _context.ParkingObject.Add(parking);
            _context.Tickets.Add(new Ticket { Id = 1, ApplicationUserId = "1", ParkingObjectId = 1, Price = 4m });
            await _context.SaveChangesAsync();
            _parkingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parking);

            // Act
            var rezultat = await _controller.DeleteParking(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, _context.ParkingObject.Count());
            Assert.AreEqual(0, _context.Tickets.Count());
        }

        // ── 10. RESERVATIONS & RESERVATIONDETAILS (JSON DATA-DRIVEN) ──────────

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(RezervacijeFilterIzJson), DynamicDataSourceType.Method)]
        public async Task Reservations_StatusIzJson_VracaFiltriraneRezervacije(string? status, int ocekivaniBroj)
        {
            // Arrange
            PripremiRezervacijeZaFiltriranje();

            // Act
            var rezultat = await _controller.Reservations(status) as ViewResult;
            var model = rezultat?.Model as List<Ticket>;

            // Assert
            Assert.IsNotNull(model);
            Assert.AreEqual(ocekivaniBroj, model.Count);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ReservationDetails_NepostojecaRezervacija_PreusmjeravaSaGreskom()
        {
            // Arrange – prazna baza

            // Act
            var rezultat = await _controller.ReservationDetails(999) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Reservations", rezultat.ActionName);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ReservationDetails_PostojecaRezervacija_VracaDetaljeView()
        {
            // Arrange
            var korisnik = NapraviKorisnika("1", "Amina", "Test", "amina@test.ba");
            var parking = NapraviParking(1, "Centar");
            var tiket = new Ticket
            {
                Id = 5, ApplicationUserId = korisnik.Id, ApplicationUser = korisnik,
                ParkingObjectId = parking.ID, ParkingObject = parking, Price = 6m
            };
            _context.Users.Add(korisnik);
            _context.ParkingObject.Add(parking);
            _context.Tickets.Add(tiket);
            await _context.SaveChangesAsync();

            // Act
            var rezultat = await _controller.ReservationDetails(5) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("~/Views/Reservation/Details.cshtml", rezultat.ViewName);
            Assert.IsTrue((bool)_controller.ViewBag.IsAdminView);
        }

        // ── 11. APPROVEPARKING & DELETERESERVATION TESTOVI ─────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ApproveParking_NepostojeciParking_VracaNotFound()
        {
            // Arrange
            _parkingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ParkingObject?)null);

            // Act
            var rezultat = await _controller.ApproveParking(99);

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow(true, "Success")]
        [DataRow(false, "Error")]
        public async Task ApproveParking_RazlicitIshodOdobravanja_PostavljaOdgovarajuciTempData(bool uspjeh, string ocekivaniKljuc)
        {
            // Arrange
            var parking = NapraviParking(1, "Bascarsija");
            _parkingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parking);
            _parkingRepoMock.Setup(r => r.ApproveParkingAsync(1)).ReturnsAsync(uspjeh);

            // Act
            var rezultat = await _controller.ApproveParking(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsNotNull(_controller.TempData[ocekivaniKljuc]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteReservation_NepostojecaRezervacija_VracaNotFound()
        {
            // Arrange – prazna baza

            // Act
            var rezultat = await _controller.DeleteReservation(999);

            // Assert
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task DeleteReservation_PostojecaRezervacija_BriseKartuIOslobadjaMjesto()
        {
            // Arrange
            var parking = NapraviParking(1, "Grbavica", totalSpots: 10, availableSpots: 4);
            var tiket = new Ticket { Id = 1, ApplicationUserId = "1", ParkingObjectId = 1, ParkingObject = parking };
            _context.ParkingObject.Add(parking);
            _context.Tickets.Add(tiket);
            await _context.SaveChangesAsync();

            // Act
            var rezultat = await _controller.DeleteReservation(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(5, parking.availableSpots);
            Assert.AreEqual(0, _context.Tickets.Count());
        }

        // ── 12. REQUESTS, APPROVEREQUEST & REJECTREQUEST TESTOVI ──────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task Requests_PostojeciZahtjevi_VracaViewSaMapiranimParkinzima()
        {
            // Arrange
            var parking = NapraviParking(10, "Marijin Dvor");
            _context.ParkingObject.Add(parking);
            await _context.SaveChangesAsync();
            var zahtjevi = new List<ParkingRequest> { new ParkingRequest { ID = 1, ParkingID = 10 } };
            _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(zahtjevi);

            // Act
            var rezultat = await _controller.Requests() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            var mapa = _controller.ViewBag.Parkings as Dictionary<int, ParkingObject>;
            Assert.IsNotNull(mapa);
            Assert.IsTrue(mapa.ContainsKey(10));
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ApproveRequest_NepostojeciZahtjev_PreusmjeravaSaGreskom()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ParkingRequest?)null);

            // Act
            var rezultat = await _controller.ApproveRequest(99) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Requests", rezultat.ActionName);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ApproveRequest_NepostojeciParking_PreusmjeravaSaGreskom()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ParkingRequest { ID = 1, ParkingID = 50 });
            _parkingRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync((ParkingObject?)null);

            // Act
            var rezultat = await _controller.ApproveRequest(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ApproveRequest_NeuspjesnoOdobravanje_PreusmjeravaSaGreskom()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ParkingRequest { ID = 1, ParkingID = 50 });
            _parkingRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(NapraviParking(50, "Otoka"));
            _parkingRepoMock.Setup(r => r.ApproveParkingAsync(50)).ReturnsAsync(false);

            // Act
            var rezultat = await _controller.ApproveRequest(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ApproveRequest_ValidanZahtjev_OdobravaParkingIBriseZahtjev()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ParkingRequest { ID = 1, ParkingID = 50 });
            _parkingRepoMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(NapraviParking(50, "Otoka"));
            _parkingRepoMock.Setup(r => r.ApproveParkingAsync(50)).ReturnsAsync(true);

            // Act
            var rezultat = await _controller.ApproveRequest(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            _requestRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            Assert.IsNotNull(_controller.TempData["Success"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RejectRequest_NepostojeciZahtjev_PreusmjeravaSaGreskom()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ParkingRequest?)null);

            // Act
            var rezultat = await _controller.RejectRequest(99) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RejectRequest_NepostojeciParking_PreusmjeravaSaGreskom()
        {
            // Arrange
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ParkingRequest { ID = 1, ParkingID = 77 });

            // Act
            var rezultat = await _controller.RejectRequest(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsNotNull(_controller.TempData["Error"]);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RejectRequest_ValidanZahtjev_BriseSlikeCjenovnikParkingIZahtjev()
        {
            // Arrange
            var parking = NapraviParking(20, "Dobrinja");
            _context.ParkingObject.Add(parking);
            _context.Pricing.Add(new Pricing { ID = 1, ParkingObjectID = 20, price = 2m });
            _context.ParkingImages.Add(new ParkingImage { ID = 1, ParkingObjectID = 20, ImagePath = "/nepostojeca/slika.jpg" });
            await _context.SaveChangesAsync();
            _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new ParkingRequest { ID = 1, ParkingID = 20 });

            // Act
            var rezultat = await _controller.RejectRequest(1) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(0, _context.ParkingObject.Count());
            Assert.AreEqual(0, _context.Pricing.Count());
            Assert.AreEqual(0, _context.ParkingImages.Count());
            _requestRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        // ── 13. ADMINUSERVIEWMODEL TESTOVI ────────────────────────────────────

        [TestMethod]
        [Priority(3)]
        [DataRow(10, true)]
        [DataRow(-10, false)]
        [DataRow(null, false)]
        public void AdminUserViewModel_IsLocked_ProvjeravaStatusZakljucavanja(int? pomakDana, bool ocekivanoZakljucan)
        {
            // Arrange
            var vm = new AdminUserViewModel
            {
                Id = "1",
                FullName = "Test Korisnik",
                Email = "test@test.ba",
                LockoutEnd = pomakDana.HasValue ? DateTimeOffset.Now.AddDays(pomakDana.Value) : null
            };

            // Act
            var zakljucan = vm.IsLocked;

            // Assert
            Assert.AreEqual(ocekivanoZakljucan, zakljucan);
        }

        [TestMethod]
        [Ignore("Primjer demonstracije Ignore stereotipa sa predavanja - metoda Dashboard nema izuzetaka.")]
        public void Dashboard_IgnorisaniPrimjerTesta_DemonstriraStereotip()
        {
            // Arrange – nije potrebno
            // Act
            // Assert
            Assert.IsTrue(true);
        }
    }
}