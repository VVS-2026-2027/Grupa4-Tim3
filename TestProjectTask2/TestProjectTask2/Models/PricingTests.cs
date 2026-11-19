using ParkirajBa.Models;

namespace TestProjectTask2.Models;

/// <summary>
/// Zadana vrijednost nove cijene parkinga. OverstayChargeService za obračun koristi satnu cijenu.
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
