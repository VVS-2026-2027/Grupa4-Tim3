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

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_ParkingSa5MjestaFilter3_ParkingProlazi()
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", availableSpots = 5 };
        var parkinzi = new List<ParkingObject> { parking };

        // Act
        var rezultat = ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 3);

        // Assert
        CollectionAssert.Contains(rezultat, parking);
    }

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_ParkingSa2MjestaFilter3_ParkingNeProlazi()
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", availableSpots = 2 };
        var parkinzi = new List<ParkingObject> { parking };
        // Act
        var rezultat = ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 3);
        // Assert
        CollectionAssert.DoesNotContain(rezultat, parking);
    }

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_ParkingSaTacno3MjestaFilter3_ParkingProlazi()
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", availableSpots = 3 };
        var parkinzi = new List<ParkingObject> { parking };

        // Act
        var rezultat = ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 3);

        // Assert
        CollectionAssert.Contains(rezultat, parking);
    }

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_ViseParkinga_SortiraOdNajviseMjesta()
    {
        // Arrange
        var parkinzi = new List<ParkingObject>
        {
            new() { availableSpots = 4 }, new() { availableSpots = 8 }
        };
        // Act
        var rezultat = ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 0);
        // Assert
        Assert.AreEqual(8, rezultat[0].availableSpots);
    }

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_NegativanFilter_BacaArgumentException()
    {
        // Arrange
        var parkinzi = new List<ParkingObject>();
        // Act
        // Assert
        Assert.ThrowsExactly<ArgumentException>(() => ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, -1));
    }

    [TestMethod]
    public void FiltrirajPoSlobodnimMjestima_NullLista_BacaArgumentNullException()
    {
        // Arrange
        IEnumerable<ParkingObject> parkinzi = null!;
        // Act
        // Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => ParkingFilter.FiltrirajPoSlobodnimMjestima(parkinzi, 0));
    }
}
