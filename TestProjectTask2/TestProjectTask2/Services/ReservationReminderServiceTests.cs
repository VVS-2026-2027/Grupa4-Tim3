using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParkirajBa.Services;

namespace ParkirajBa.Tests.Services;

[TestClass]
public class ReservationReminderServiceTests
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

    private static TestableReminderService CreateService() =>
        new(Mock.Of<IServiceScopeFactory>(), NullLogger<ReservationReminderService>.Instance);

    private sealed class TestableReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationReminderService> logger)
        : ReservationReminderService(scopeFactory, logger)
    {
        public Task RunAsync(CancellationToken token) => ExecuteAsync(token);
    }
}
