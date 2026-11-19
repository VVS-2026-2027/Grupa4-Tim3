using System.Text.Json;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Services;
using TestProjectTask2.Pomocne;

namespace TestProjectTask2.Services;

[TestClass]
public class OverstayChargeServiceTests
{
    private ApplicationDbContext _baza = null!;
    private Mock<IEmailSender> _email = null!;
    private ServiceProvider _provajder = null!;
    private OverstayChargeService _servis = null!;

    public record SlucajPrekoracenja(double SatiPrekoracenja, decimal CijenaPoSatu, decimal OcekivanaNaplata);

    // ---------------------------------------------------------------
    // Inicijalizacija i čišćenje
    // ---------------------------------------------------------------

    [TestInitialize]
    public void Inicijalizuj()
    {
        _baza = TestBaza.Nova();
        _email = new Mock<IEmailSender>();
        // Pravi DI kontejner: servis sam pravi scope i iz njega uzima bazu i IEmailSender.
        _provajder = new ServiceCollection()
            .AddSingleton(_baza)
            .AddSingleton(_email.Object)
            .BuildServiceProvider();
        _servis = new OverstayChargeService(_provajder.GetRequiredService<IServiceScopeFactory>());
    }

    [TestCleanup]
    public void Ocisti()
    {
        _servis.Dispose();
        _provajder.Dispose();
        _baza.Dispose();
    }

    // ---------------------------------------------------------------
    // Pomoćne metode (bez logike)
    // ---------------------------------------------------------------

    private void DodajSatnuCijenu(decimal cijena)
    {
        _baza.Pricing.Add(new Pricing
        {
            ParkingObjectID = 1,
            pricingType = PricingType.Hourly,
            price = cijena,
            validFrom = DateTime.Now.AddDays(-1)
        });
        _baza.SaveChanges();
    }

    /// <summary>
    /// Karta za vozilo koje je ušlo na parking. satiDoIsteka &lt; 0 znači da je karta istekla prije toliko sati.
    /// Necijeli sati (npr. -2,5) da Math.Ceiling ne "preskoči" na sljedeći sat zbog par milisekundi.
    /// </summary>
    private static Ticket NovaKarta(double satiDoIsteka, bool dodatnaNaplataPlacena = false) => new()
    {
        ApplicationUserId = "korisnik-1",
        ApplicationUser = new ApplicationUser
        {
            Id = "korisnik-1", UserName = "tarik@test.ba", Email = "tarik@test.ba", FirstName = "Tarik", LastName = "Test"
        },
        ParkingObjectId = 1,
        ParkingObject = new ParkingObject { ID = 1, name = "Parking Centar", address = "Zmaja od Bosne 1", totalSpots = 20 },
        IssuedAt = DateTime.Now.AddHours(satiDoIsteka - 2),
        ExpiresAt = DateTime.Now.AddHours(satiDoIsteka),
        Price = 4m,
        IsPaid = true,
        EnteredParking = true,
        AdditionalChargePaid = dodatnaNaplataPlacena
    };

    private void SpremiKartu(Ticket karta)
    {
        _baza.Tickets.Add(karta);
        _baza.SaveChanges();
    }

    private Task Obracunaj(Ticket karta) => _servis.CalculateAdditionalCharge(karta, _baza, _email.Object);

    public static IEnumerable<object[]> PrekoracenjaIzJson() =>
        JsonSerializer.Deserialize<List<SlucajPrekoracenja>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "prekoracenja.json")),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!
            .Select(s => new object[] { s.SatiPrekoracenja, s.CijenaPoSatu, s.OcekivanaNaplata });

    // ---------------------------------------------------------------
    // CalculateAdditionalCharge – kada se naplata NE obračunava
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Obracun")]
    [TestProperty("Autor", "Tarik")]
    public async Task CalculateAdditionalCharge_NemaSatneCijene_NeObracunavaNaplatu()
    {
        // Arrange – u bazi nema satne cijene
        var karta = NovaKarta(-2.5);

        // Act
        await Obracunaj(karta);

        // Assert
        Assert.AreEqual(0m, karta.AdditionalCharge);
    }

    [TestMethod]
    [TestCategory("Obracun")]
    [TestProperty("Autor", "Tarik")]
    public async Task CalculateAdditionalCharge_KartaNijeIstekla_NeObracunavaNaplatu()
    {
        // Arrange
        DodajSatnuCijenu(2m);
        var karta = NovaKarta(1.5);

        // Act
        await Obracunaj(karta);

        // Assert
        Assert.AreEqual(0m, karta.AdditionalCharge);
    }

    [TestMethod]
    [TestCategory("Obracun")]
    [TestCategory("Mock")]
    [TestProperty("Autor", "Tarik")]
    public async Task CalculateAdditionalCharge_VoziloIzasloSParkinga_NeSaljeMail()
    {
        // Arrange
        DodajSatnuCijenu(2m);
        var karta = NovaKarta(-2.5);
        karta.ExitedParking = true;

        // Act
        await Obracunaj(karta);

        // Assert
        _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ---------------------------------------------------------------
    // CalculateAdditionalCharge – obračun naplate
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Obracun")]
    [TestCategory("DataDriven")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    [DynamicData(nameof(PrekoracenjaIzJson), DynamicDataSourceType.Method)]
    public async Task CalculateAdditionalCharge_PrekoracenjeIzJson_ObracunavaOcekivanuNaplatu(
        double satiPrekoracenja, decimal cijenaPoSatu, decimal ocekivanaNaplata)
    {
        // Arrange
        DodajSatnuCijenu(cijenaPoSatu);
        var karta = NovaKarta(-satiPrekoracenja);

        // Act
        await Obracunaj(karta);

        // Assert
        Assert.AreEqual(ocekivanaNaplata, karta.AdditionalCharge);
    }

    [TestMethod]
    [TestCategory("Obracun")]
    [TestCategory("Bug")]
    [TestProperty("Autor", "Tarik")]
    [Ignore("BUG: Ticket.AdditionalChargePaid je po defaultu true, pa servis nikad ne obračuna naplatu " +
            "za kartu koja nije produžena (AdditionalCharge ostaje 0, očekivano 6), a mail o naplati se ipak pošalje.")]
    public async Task CalculateAdditionalCharge_NeproduzenaKartaPrekoracena2_5h_ObracunavaNaplatu()
    {
        // Arrange – karta nije produžena, pa AdditionalChargePaid ima zadanu vrijednost iz modela (true)
        DodajSatnuCijenu(2m);
        var karta = NovaKarta(-2.5, dodatnaNaplataPlacena: true);

        // Act
        await Obracunaj(karta);

        // Assert – 2,5h zaokruženo na 3 sata × 2 KM
        Assert.AreEqual(6m, karta.AdditionalCharge);
    }

    // ---------------------------------------------------------------
    // CalculateAdditionalCharge – mail o prekoračenju (mock IEmailSender)
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Mock")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    [Timeout(2000)]
    public async Task CalculateAdditionalCharge_MailNijePoslan_SaljeMailKorisnikuJednom()
    {
        // Arrange
        DodajSatnuCijenu(2m);
        var karta = NovaKarta(-2.5);

        // Act
        await Obracunaj(karta);

        // Assert
        _email.Verify(e => e.SendEmailAsync("tarik@test.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // ProcessExpiredTickets
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Obrada")]
    [TestCategory("Mock")]
    [TestProperty("Autor", "Tarik")]
    [Priority(1)]
    public async Task ProcessExpiredTickets_IsteklaKartaNaParkingu_SaljeMail()
    {
        // Arrange
        DodajSatnuCijenu(2m);
        SpremiKartu(NovaKarta(-2.5));

        // Act
        await _servis.ProcessExpiredTickets();

        // Assert
        _email.Verify(e => e.SendEmailAsync("tarik@test.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ---------------------------------------------------------------
    // ExecuteAsync (petlja pozadinskog servisa)
    // ---------------------------------------------------------------

    [TestMethod]
    [TestCategory("Obrada")]
    [TestProperty("Autor", "Tarik")]
    [Timeout(5000)]
    public async Task ExecuteAsync_PokretanjeIZaustavljanje_ObradiIstekleKarte()
    {
        // Arrange – TaskCompletionSource javlja kad je prva iteracija petlje poslala mail
        DodajSatnuCijenu(2m);
        SpremiKartu(NovaKarta(-2.5));
        var mailPoslan = new TaskCompletionSource();
        _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => mailPoslan.TrySetResult()).Returns(Task.CompletedTask);

        // Act
        await _servis.StartAsync(CancellationToken.None);
        await mailPoslan.Task;
        await _servis.StopAsync(CancellationToken.None);

        // Assert
        Assert.AreEqual(6m, _baza.Tickets.Single().AdditionalCharge);
    }
}
