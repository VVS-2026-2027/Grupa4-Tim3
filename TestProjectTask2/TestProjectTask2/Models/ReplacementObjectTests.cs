using Microsoft.AspNetCore.Mvc;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Tests.Models
{
    [TestClass]
    [TestCategory("Mock")]
    [TestProperty("Autor", "Tim3")]
    public sealed class ReplacementObjectTests
    {
        [TestMethod]
        [Timeout(2000)]
        public async Task Parkings_MockiranParkingRepository_VracaNjegoveParkinge()
        {
            // Arrange
            var repository = new Mock<IParkingRepository>();
            var parkings = new List<ParkingObject> { new() { ID = 1, name = "Centar" } };
            repository.Setup(item => item.GetAllWithOwnerAsync()).ReturnsAsync(parkings);
            var controller = new AdminController(null!, null!, null!, null!, repository.Object);

            // Act
            var result = await controller.Parkings() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(parkings, result.Model);
            repository.Verify(item => item.GetAllWithOwnerAsync(), Times.Once);
        }
    }
}
