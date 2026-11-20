using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using ParkirajBa.Services;
using TestProjectTask2.Pomocne;

namespace TestProjectTask2.Controllers;

[TestClass]
public class PaymentControllerTests
{
    private const string SesijskaRezervacija =
        "{\"ParkingObjectId\":1,\"IssuedAt\":\"2026-01-10T08:00:00\",\"ExpiresAt\":\"2026-01-10T10:00:00\",\"Price\":4.5}";

    private static IConfiguration _konfiguracija = null!;

    private ApplicationDbContext _baza = null!;
    private Mock<IParkingRepository> _repozitorij = null!;
    private Mock<ISmtpSender> _smtp = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private LaznaSesija _sesija = null!;
    private ApplicationUser _korisnik = null!;
    private PaymentController _kontroler = null!;

    // ---------------------------------------------------------------
    // Inicijalizacija i čišćenje
    // ---------------------------------------------------------------

    [ClassInitialize]
    public static void InicijalizujKlasu(TestContext context)
    {
        // Konfiguracija se samo čita, pa je smiju dijeliti svi testovi.
        _konfiguracija = NapraviKonfiguraciju("parkirajba@test.ba", "tajna-lozinka");
    }

    [TestInitialize]
    public void Inicijalizuj()
    {
        _baza = TestBaza.Nova();
        _repozitorij = new Mock<IParkingRepository>();
        _smtp = new Mock<ISmtpSender>();
        _korisnik = new ApplicationUser { Id = "korisnik-1", Email = "tarik@test.ba", FirstName = "Tarik", LastName = "Test" };
        _userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_korisnik);
        _sesija = new LaznaSesija();
        _kontroler = NapraviKontroler(_konfiguracija);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontroler.Dispose();
        _baza.Dispose();
    }

    // ---------------------------------------------------------------
    // Pomoćne metode (bez logike)
    // ---------------------------------------------------------------

    private static IConfiguration NapraviKonfiguraciju(string? email, string? lozinka) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:SenderEmail"] = email,
                ["EmailSettings:AppPassword"] = lozinka
            })
            .Build();

    private PaymentController NapraviKontroler(IConfiguration konfiguracija)
    {
        var httpContext = new DefaultHttpContext
        {
            Session = _sesija,
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "korisnik-1") }, "Test"))
        };

        return new PaymentController(_userManager.Object, _baza, _repozitorij.Object, konfiguracija, _smtp.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private void OdjaviKorisnika() =>
        _userManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

    private void PostaviRezervacijuUSesiju() => _sesija.SetString("PendingReservation", SesijskaRezervacija);

    private Ticket PostaviKartu(int id = 7, decimal dodatnaNaplata = 0, bool dodatnaNaplataPlacena = true)
    {
        var karta = new Ticket
        {
            Id = id,
            ApplicationUserId = _korisnik.Id,
            ParkingObjectId = 1,
            ParkingObject = new ParkingObject { ID = 1, name = "Parking Centar" },
            Price = 6m,
            IssuedAt = DateTime.Now.AddHours(-1),
            ExpiresAt = DateTime.Now.AddHours(1),
            AdditionalCharge = dodatnaNaplata,
            AdditionalChargePaid = dodatnaNaplataPlacena,
            QrCodeActive = false
        };
        _repozitorij.Setup(r => r.GetTicketByIdAsync(id, _korisnik.Id)).ReturnsAsync(karta);
        return karta;
    }

    /// <summary>
    /// Parking se upisuje u InMemory bazu jer i pravi repozitorij vraća entitet iz iste baze (kontekst ga prati).
    /// </summary>
    private ParkingObject PostaviParking(int slobodnaMjesta = 10)
    {
        var parking = new ParkingObject { ID = 1, name = "Parking Centar", address = "Zmaja od Bosne 1", totalSpots = 20, availableSpots = slobodnaMjesta };
        _baza.ParkingObject.Add(parking);
        _baza.SaveChanges();
        _repozitorij.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(parking);
        return parking;
    }

    public static IEnumerable<object[]> CheckoutSlucajeviIzCsv() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "checkout_slucajevi.csv"))
            .Skip(1)
            .Select(linija => linija.Split(';'))
            .Select(kolone => new object[] { int.Parse(kolone[0]), bool.Parse(kolone[1]) });

    // ---------------------------------------------------------------
    // Konstruktor – izuzeci
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Izuzeci")]
    [TestProperty("Autor", "Tarik")]
    public void Konstruktor_NedostajeSenderEmail_BacaInvalidOperationException()
    {
        // Arrange
        var konfiguracija = NapraviKonfiguraciju(null, "tajna-lozinka");

        // Act
        var izuzetak = Assert.ThrowsExactly<InvalidOperationException>(() => NapraviKontroler(konfiguracija));

        // Assert
        Assert.AreEqual("EmailSettings:SenderEmail is not configured.", izuzetak.Message);
    }

    [TestMethod]
    [TestCategory("Izuzeci")]
    [TestProperty("Autor", "Tarik")]
    public void Konstruktor_NedostajeAppPassword_BacaIzuzetakSPorukom()
    {
        // Arrange
        var konfiguracija = NapraviKonfiguraciju("parkirajba@test.ba", null);

        // Act
        var izuzetak = Assert.ThrowsExactly<InvalidOperationException>(() => NapraviKontroler(konfiguracija));

        // Assert
        Assert.AreEqual("EmailSettings:AppPassword is not configured.", izuzetak.Message);
    }

    // ---------------------------------------------------------------
    // Checkout
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Checkout")]
    [TestProperty("Autor", "Tarik")]
    public async Task Checkout_KorisnikNijePrijavljen_PreusmjeravaNaLogin()
    {
        // Arrange
        OdjaviKorisnika();

        // Act
        var rezultat = await _kontroler.Checkout(7) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Login", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Checkout")]
    [TestProperty("Autor", "Tarik")]
    public async Task Checkout_SesijaBezRezervacije_PreusmjeravaNaObjekte()
    {
        // Arrange – sesija je prazna

        // Act
        var rezultat = await _kontroler.Checkout(fromSession: true) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Objekti", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Checkout")]
    [TestProperty("Autor", "Tarik")]
    public async Task Checkout_RezervacijaUSesiji_PrikazujeCijenuIzSesije()
    {
        // Arrange
        PostaviRezervacijuUSesiju();
        PostaviParking();

        // Act
        await _kontroler.Checkout(fromSession: true);

        // Assert
        Assert.AreEqual(4.5m, (_kontroler.ViewData["Ticket"] as Ticket)?.Price);
    }

    [TestMethod]
    [TestCategory("Checkout")]
    [TestProperty("Autor", "Tarik")]
    public async Task Checkout_KartaNePostoji_PreusmjeravaNaRezervacije()
    {
        // Arrange – repozitorij ne vraća kartu

        // Act
        var rezultat = await _kontroler.Checkout(42) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Rezervacije", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Checkout")]
    [TestCategory("DataDriven")]
    [TestProperty("Autor", "Tarik")]
    [DynamicData(nameof(CheckoutSlucajeviIzCsv), DynamicDataSourceType.Method)]
    public async Task Checkout_SlucajeviIzCsv_PostavljaOznakuDodatneNaplate(int ticketId, bool dodatnaNaplata)
    {
        // Arrange
        PostaviKartu(ticketId);

        // Act
        await _kontroler.Checkout(ticketId, dodatnaNaplata);

        // Assert
        Assert.AreEqual(dodatnaNaplata, _kontroler.ViewData["IsAdditionalCharge"]);
    }

    // ---------------------------------------------------------------
    // Confirm
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Confirm")]
    [TestProperty("Autor", "Tarik")]
    public async Task Confirm_KorisnikNijePrijavljen_PreusmjeravaNaLogin()
    {
        // Arrange
        OdjaviKorisnika();

        // Act
        var rezultat = await _kontroler.Confirm("Tarik Test", 7) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Login", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Confirm")]
    [TestProperty("Autor", "Tarik")]
    public async Task Confirm_SesijaBezRezervacije_PreusmjeravaNaObjekte()
    {
        // Arrange – sesija je prazna

        // Act
        var rezultat = await _kontroler.Confirm("Tarik Test", fromSession: true) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Objekti", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Confirm")]
    [TestProperty("Autor", "Tarik")]
    public async Task Confirm_RezervacijaUSesiji_PrikazujeImeSKartice()
    {
        // Arrange
        PostaviRezervacijuUSesiju();
        PostaviParking();

        // Act
        await _kontroler.Confirm("Tarik Test", fromSession: true);

        // Assert
        Assert.AreEqual("Tarik Test", _kontroler.ViewData["CardName"]);
    }

    [TestMethod]
    [TestCategory("Confirm")]
    [TestProperty("Autor", "Tarik")]
    public async Task Confirm_KartaNePostoji_PreusmjeravaNaCheckout()
    {
        // Arrange – repozitorij ne vraća kartu

        // Act
        var rezultat = await _kontroler.Confirm("Tarik Test", 42) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Checkout", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Confirm")]
    [TestProperty("Autor", "Tarik")]
    [DataRow("Tarik Test")]
    [DataRow("")]
    public async Task Confirm_PostojecaKarta_PrikazujeImeSKartice(string imeSKartice)
    {
        // Arrange
        PostaviKartu();

        // Act
        await _kontroler.Confirm(imeSKartice, 7);

        // Assert
        Assert.AreEqual(imeSKartice, _kontroler.ViewData["CardName"]);
    }

    // ---------------------------------------------------------------
    // SendConfirmationEmail
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Placanje")]
    [TestProperty("Autor", "Tarik")]
    public async Task SendConfirmationEmail_KorisnikNijePrijavljen_VracaUnauthorized()
    {
        // Arrange
        OdjaviKorisnika();

        // Act
        var rezultat = await _kontroler.SendConfirmationEmail("Tarik Test", 7);

        // Assert
        Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory("Placanje")]
    [TestProperty("Autor", "Tarik")]
    public async Task SendConfirmationEmail_KartaNePostoji_PreusmjeravaNaCheckout()
    {
        // Arrange – repozitorij ne vraća kartu

        // Act
        var rezultat = await _kontroler.SendConfirmationEmail("Tarik Test", 42) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Checkout", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Placanje")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    public async Task SendConfirmationEmail_RezervacijaIzSesije_SpremaKartuUBazu()
    {
        // Arrange
        PostaviRezervacijuUSesiju();
        PostaviParking();

        // Act
        await _kontroler.SendConfirmationEmail("Tarik Test");

        // Assert
        Assert.AreEqual(1, _baza.Tickets.Count());
    }

    [TestMethod]
    [TestCategory("Placanje")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    public async Task SendConfirmationEmail_RedovnoPlacanje_SmanjujeSlobodnaMjesta()
    {
        // Arrange
        PostaviKartu();
        var parking = PostaviParking(10);

        // Act
        await _kontroler.SendConfirmationEmail("Tarik Test", 7);

        // Assert
        Assert.AreEqual(9, parking.availableSpots);
    }

    [TestMethod]
    [TestCategory("Placanje")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    public async Task SendConfirmationEmail_DodatnaNaplata_DodajeIznosUUkupnoPlaceno()
    {
        // Arrange
        var karta = PostaviKartu(dodatnaNaplata: 9m, dodatnaNaplataPlacena: false);

        // Act
        await _kontroler.SendConfirmationEmail("Tarik Test", 7, additionalCharge: true);

        // Assert
        Assert.AreEqual(9m, karta.TotalAdditionalChargesPaid);
    }

    [TestMethod]
    [TestCategory("Mock")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    [Timeout(2000)]
    public async Task SendConfirmationEmail_UspjesnoPlacanje_SaljeMailJednom()
    {
        // Arrange
        PostaviKartu();

        // Act
        await _kontroler.SendConfirmationEmail("Tarik Test", 7);

        // Assert
        _smtp.Verify(s => s.Send(It.IsAny<MailMessage>(), It.IsAny<NetworkCredential>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Mock")]
    [TestCategory("Izuzeci")]
    [TestProperty("Autor", "Tarik")]
    public async Task SendConfirmationEmail_SmtpBaciIzuzetak_PostavljaPorukuOGresci()
    {
        // Arrange
        PostaviKartu();
        _smtp.Setup(s => s.Send(It.IsAny<MailMessage>(), It.IsAny<NetworkCredential>())).Throws(new SmtpException("Server nedostupan"));

        // Act
        await _kontroler.SendConfirmationEmail("Tarik Test", 7);

        // Assert
        StringAssert.Contains(_kontroler.TempData["EmailError"] as string, "Server nedostupan");
    }

    // ---------------------------------------------------------------
    // Success
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Success")]
    [TestProperty("Autor", "Tarik")]
    public async Task Success_KorisnikNijePrijavljen_PreusmjeravaNaLogin()
    {
        // Arrange
        OdjaviKorisnika();

        // Act
        var rezultat = await _kontroler.Success("PB-1234ABCD", 7) as RedirectToActionResult;

        // Assert
        Assert.AreEqual("Login", rezultat?.ActionName);
    }

    [TestMethod]
    [TestCategory("Success")]
    [TestProperty("Autor", "Tarik")]
    public async Task Success_PrijavljenKorisnik_PrikazujeKodRezervacije()
    {
        // Arrange
        PostaviKartu();

        // Act
        await _kontroler.Success("PB-1234ABCD", 7);

        // Assert
        Assert.AreEqual("PB-1234ABCD", _kontroler.ViewData["ReservationCode"]);
    }

    // ---------------------------------------------------------------
    // GenerateQr
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("QR")]
    [TestProperty("Autor", "Tarik")]
    public async Task GenerateQr_KorisnikNijePrijavljen_VracaUnauthorized()
    {
        // Arrange
        OdjaviKorisnika();

        // Act
        var rezultat = await _kontroler.GenerateQr(7);

        // Assert
        Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
    }

    [TestMethod]
    [TestCategory("QR")]
    [TestProperty("Autor", "Tarik")]
    [DataRow(null)]
    [DataRow("")]
    public async Task GenerateQr_KartaBezKodaRezervacije_VracaNotFound(string? kodRezervacije)
    {
        // Arrange
        var karta = PostaviKartu();
        karta.ReservationCode = kodRezervacije;

        // Act
        var rezultat = await _kontroler.GenerateQr(7);

        // Assert
        Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
    }

    [TestMethod]
    [TestCategory("QR")]
    [TestProperty("Autor", "Tarik")]
    [Timeout(2000)]
    public async Task GenerateQr_IspravnaKarta_VracaPngSliku()
    {
        // Arrange
        var karta = PostaviKartu();
        karta.ReservationCode = "PB-1234ABCD";

        // Act
        var rezultat = await _kontroler.GenerateQr(7) as FileContentResult;

        // Assert
        Assert.AreEqual("image/png", rezultat?.ContentType);
    }
}
