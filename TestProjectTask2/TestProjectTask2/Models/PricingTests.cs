using ParkirajBa.Models;

namespace TestProjectTask2.Models;

/// <summary>
/// Zadane vrijednosti nove cijene parkinga. OverstayChargeService koristi satnu cijenu
/// i sortira cijene po datumu važenja (validFrom).
/// </summary>
[TestClass]
public class PricingTests
{
    private Pricing _cijena = null!;

    [TestInitialize]
    public void Inicijalizuj()
    {
        _cijena = new Pricing();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _cijena = null!;
    }

    private static object? Vrijednost(object objekat, string svojstvo) =>
        objekat.GetType().GetProperty(svojstvo)!.GetValue(objekat);

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    [DataRow(nameof(Pricing.validFrom))]
    [DataRow(nameof(Pricing.validTo))]
    public void NovaCijena_DatumVazenja_NijePostavljen(string svojstvo)
    {
        // Arrange – nova cijena iz [TestInitialize]

        // Act
        var vrijednost = Vrijednost(_cijena, svojstvo);

        // Assert
        Assert.IsNull(vrijednost);
    }

    [TestMethod]
    [TestCategory("Modeli")]
    [TestProperty("Autor", "Tarik")]
    public void NovaCijena_VrstaCijene_JeSatna()
    {
        // Arrange – nova cijena iz [TestInitialize]

        // Act
        var vrsta = _cijena.pricingType;

        // Assert
        Assert.AreEqual(PricingType.Hourly, vrsta);
    }
}
