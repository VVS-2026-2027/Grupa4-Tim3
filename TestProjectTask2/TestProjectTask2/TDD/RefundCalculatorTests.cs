using ParkirajBa.Services;

namespace TestProjectTask2.TDD;

[TestClass]
public class RefundCalculatorTests
{
    [TestMethod]
    public void IzracunajPovrat_Otkazano24SataPrije_Vraca100Posto()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);
        var otkazivanje = pocetak.AddHours(-24);

        // Act
        var povrat = RefundCalculator.IzracunajPovrat(20m, pocetak, otkazivanje);

        // Assert
        Assert.AreEqual(20m, povrat);
    }
}
