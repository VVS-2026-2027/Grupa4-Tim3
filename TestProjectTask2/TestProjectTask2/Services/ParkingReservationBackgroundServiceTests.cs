using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParkirajBa.Hubs;
using ParkirajBa.Services;

namespace ParkirajBa.Tests.Services;

[TestClass]
public class ParkingReservationBackgroundServiceTests
{
    [TestMethod]
    [TestCategory("Services")]
    public async Task ExecuteAsync_OtkazanToken_ZavrsavaIzvrsavanje()
    {
        // Arrange
        var service = CreateService();
        // Act
        var izvrsavanje = service.RunAsync(new CancellationToken(true));
        // Assert
        Assert.IsTrue(izvrsavanje.IsCompletedSuccessfully);
        await izvrsavanje;
    }

    private static TestableParkingService CreateService() =>
        new(Mock.Of<IServiceScopeFactory>(), NullLogger<ParkingReservationBackgroundService>.Instance, Mock.Of<IHubContext<ParkingHub>>());

    private sealed class TestableParkingService(
        IServiceScopeFactory scopeFactory,
        ILogger<ParkingReservationBackgroundService> logger,
        IHubContext<ParkingHub> hubContext)
        : ParkingReservationBackgroundService(scopeFactory, logger, hubContext)
    {
        public Task RunAsync(CancellationToken token) => ExecuteAsync(token);
    }
}
