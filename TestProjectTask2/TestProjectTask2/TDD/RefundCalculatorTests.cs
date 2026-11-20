using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParkirajBa.Services;

namespace TestProjectTask2.TDD;

[TestClass]
public class RefundCalculatorTests
{
    [TestMethod]
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
