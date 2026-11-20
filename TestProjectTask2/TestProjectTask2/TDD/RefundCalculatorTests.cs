using Microsoft.VisualStudio.TestTools.UnitTesting;
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

    [TestMethod]
    public void IzracunajPovrat_Otkazano23Sata59MinutaPrije_Vraca50Posto()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);
        var otkazivanje = pocetak.AddMinutes(-1439);

        // Act
        var povrat = RefundCalculator.IzracunajPovrat(20m, pocetak, otkazivanje);

        // Assert
        Assert.AreEqual(10m, povrat);
    }

    [TestMethod]
    public void IzracunajPovrat_Otkazano1Sat59MinutaPrije_VracaNula()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);
        var otkazivanje = pocetak.AddMinutes(-119);

        // Act
        var povrat = RefundCalculator.IzracunajPovrat(20m, pocetak, otkazivanje);

        // Assert
        Assert.AreEqual(0m, povrat);
    }

    [TestMethod]
    public void IzracunajPovrat_OtkazanoTacnoNaPocetku_BacaInvalidOperationException()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);

        // Act and Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RefundCalculator.IzracunajPovrat(20m, pocetak, pocetak));
    }

    [TestMethod]
    public void IzracunajPovrat_NegativnaCijena_BacaArgumentOutOfRangeException()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);

        // Act and Assert
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            RefundCalculator.IzracunajPovrat(-1m, pocetak, pocetak.AddHours(-24)));
    }

    [TestMethod]
    public void IzracunajPovrat_OtkazanoTacno2SataPrije_ZaokruzujeNaDvijeDecimale()
    {
        // Arrange
        var pocetak = new DateTime(2026, 11, 20, 12, 0, 0);

        // Act
        var povrat = RefundCalculator.IzracunajPovrat(9.99m, pocetak, pocetak.AddHours(-2));

        // Assert
        Assert.AreEqual(5.00m, povrat);
    public void IzracunajPovrat_WhenExactlyTwoHoursBeforeStart_ReturnsHalfPrice()
    {
        // Arrange
        var cijena = 100m;
        var pocetak = new DateTime(2026, 1, 1, 12, 0, 0);
        var trenutakOtkazivanja = pocetak.AddHours(-2);

        // Act
        var rezultat = RefundCalculator.IzracunajPovrat(cijena, pocetak, trenutakOtkazivanja);

        // Assert
        Assert.AreEqual(50m, rezultat);
    }

    [TestMethod]
    public void IzracunajPovrat_WhenTwentyThreeHoursAndFiftyNineMinutesBeforeStart_ReturnsHalfPrice()
    {
        // Arrange
        var cijena = 100m;
        var pocetak = new DateTime(2026, 1, 1, 12, 0, 0);
        var trenutakOtkazivanja = pocetak.AddHours(-23).AddMinutes(-59);

        // Act
        var rezultat = RefundCalculator.IzracunajPovrat(cijena, pocetak, trenutakOtkazivanja);

        // Assert
        Assert.AreEqual(50m, rezultat);
    }

    [TestMethod]
    public void IzracunajPovrat_WhenOneHourAndFiftyNineMinutesBeforeStart_ReturnsNoRefund()
    {
        // Arrange
        var cijena = 100m;
        var pocetak = new DateTime(2026, 1, 1, 12, 0, 0);
        var trenutakOtkazivanja = pocetak.AddHours(-1).AddMinutes(-59);

        // Act
        var rezultat = RefundCalculator.IzracunajPovrat(cijena, pocetak, trenutakOtkazivanja);

        // Assert
        Assert.AreEqual(0m, rezultat);
    }
}
