using ParkirajBa.Models;

namespace TestProjectTask2.Models;

/// <summary>
/// Zadane vrijednosti nove karte. Na njima se zasniva logika plaćanja i naplate prekoračenja
/// (PaymentController, OverstayChargeService, QrController).
/// </summary>
[TestClass]
public class TicketTests
{
    private Ticket _karta = null!;

    [TestInitialize]
    public void Inicijalizuj()
    {
        _karta = new Ticket();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _karta = null!;
    }

    private static object? Vrijednost(object objekat, string svojstvo) =>
        objekat.GetType().GetProperty(svojstvo)!.GetValue(objekat);

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    [DataRow(nameof(Ticket.IsPaid), false)]
    [DataRow(nameof(Ticket.IsCancelled), false)]
    [DataRow(nameof(Ticket.ExpirationReminderSent), false)]
    [DataRow(nameof(Ticket.EnteredParking), false)]
    [DataRow(nameof(Ticket.ExitedParking), false)]
    [DataRow(nameof(Ticket.QrCodeActive), true)]
    [DataRow(nameof(Ticket.AdditionalChargePaid), true)]
    [DataRow(nameof(Ticket.OverstayEmailSent), false)]
    [DataRow(nameof(Ticket.SpotCountApplied), false)]
    public void NovaKarta_LogickoSvojstvo_ImaZadanuVrijednost(string svojstvo, bool ocekivano)
    {
        // Arrange – nova karta iz [TestInitialize]

        // Act
        var vrijednost = Vrijednost(_karta, svojstvo);

        // Assert
        Assert.AreEqual(ocekivano, vrijednost);
    }

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    [DataRow(nameof(Ticket.ExpiresAt))]
    [DataRow(nameof(Ticket.PaidAt))]
    [DataRow(nameof(Ticket.EnteredAt))]
    [DataRow(nameof(Ticket.ExitedAt))]
    [DataRow(nameof(Ticket.ReservationCode))]
    public void NovaKarta_OpcionalnoSvojstvo_NijePostavljeno(string svojstvo)
    {
        // Arrange – nova karta iz [TestInitialize]

        // Act
        var vrijednost = Vrijednost(_karta, svojstvo);

        // Assert
        Assert.IsNull(vrijednost);
    }

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    public void NovaKarta_DodatnaNaplata_JeNula()
    {
        // Arrange – nova karta iz [TestInitialize]

        // Act
        var dodatnaNaplata = _karta.AdditionalCharge;

        // Assert
        Assert.AreEqual(0m, dodatnaNaplata);
    }

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    public void NovaKarta_UkupnoPlaceneDodatneNaplate_JeNula()
    {
        // Arrange – nova karta iz [TestInitialize]

        // Act
        var ukupnoPlaceno = _karta.TotalAdditionalChargesPaid;

        // Assert
        Assert.AreEqual(0m, ukupnoPlaceno);
    }

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    public void NovaKarta_VrijemeIzdavanja_JeTrenutnoVrijeme()
    {
        // Arrange
        var prije = DateTime.Now;

        // Act
        var karta = new Ticket();

        // Assert
        Assert.IsTrue(karta.IssuedAt >= prije);
    }
}
