using Moq;
using ParkirajBa.Services;

namespace TestProjectTask2.Services;

[TestClass]
public class RefundServiceTests
{
    [TestMethod]
    public async Task RefundAsync_IsplataPovrata_ProsljedjujeKodIIznosGatewayu()
    {
        // Arrange
        var gateway = new Mock<IPaymentGateway>();
        var servis = new RefundService(gateway.Object);

        // Act
        await servis.RefundAsync("REZ-001", 50m);

        // Assert
        gateway.Verify(x => x.RefundAsync("REZ-001", 50m), Times.Once);
    }

    [TestMethod]
    public async Task RefundAsync_NePozivaGatewayKadaJeIznosNulaIliNegativan()
    {
        // Arrange
        var gateway = new Mock<IPaymentGateway>();
        var servis = new RefundService(gateway.Object);

        // Act
        await servis.RefundAsync("REZ-001", 0m);
        await servis.RefundAsync("REZ-002", -1m);

        // Assert
        gateway.Verify(x => x.RefundAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }
}
