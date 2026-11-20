using ParkirajBa.Models;
using ParkirajBa.Services;

namespace TestProjectTask2.TDD;

[TestClass]
public sealed class ParkingFilterTests
{
    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_PraznaLista_VracaPraznuListu()
    {
        // Arrange
        var parkinzi = new List<ParkingObject>();

        // Act
        var rezultat = ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 3);

        // Assert
        Assert.AreEqual(0, rezultat.Count);
    }
}
