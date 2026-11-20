using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParkirajBa.Data;
using ParkirajBa.Hubs;
using ParkirajBa.Models;
using ParkirajBa.Services;

namespace ParkirajBa.Tests.Services;

[TestClass]
[TestCategory("ParkingReservationBackgroundService")]
public sealed class ParkingReservationBackgroundServiceCoverageTests
{
    [TestMethod]
    public async Task ExecuteAsync_EligibleTickets_DecrementsAndPublishesAvailableSpots()
    {
        using var database = CreateDatabase();
        var parking = new ParkingObject { ID = 1, name = "Centar", address = "Test", totalSpots = 10, availableSpots = 3 };
        var user = User();
        database.Users.Add(user);
        database.ParkingObject.Add(parking);
        var now = DateTime.Now;
        database.Tickets.AddRange(
            Ticket(1, user, parking, now.AddMinutes(-1)),
            Ticket(2, user, parking, now.AddMinutes(-1), isPaid: false),
            Ticket(3, user, parking, now.AddMinutes(-1), isCancelled: true),
            Ticket(4, user, parking, now.AddMinutes(-1), spotCountApplied: true),
            Ticket(5, user, parking, now.AddMinutes(1)));
        await database.SaveChangesAsync();
        var signal = SetupSignalR();
        using var cancellation = new CancellationTokenSource();
        var service = CreateService(database, signal.Hub.Object, cancellation);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));

        Assert.AreEqual(2, parking.availableSpots);
        Assert.IsTrue((await database.Tickets.FindAsync(1))!.SpotCountApplied);
        Assert.IsFalse((await database.Tickets.FindAsync(2))!.SpotCountApplied);
        signal.Client.Verify(proxy => proxy.SendCoreAsync("ParkingSpotsChanged",
            It.Is<object?[]>(arguments => (int)arguments[0]! == parking.ID && (int)arguments[1]! == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ParkingAtCapacity_DoesNotDecrementOrPublish()
    {
        using var database = CreateDatabase();
        var parking = new ParkingObject { ID = 1, name = "Centar", address = "Test", totalSpots = 10, availableSpots = 0 };
        var user = User();
        database.Users.Add(user);
        database.ParkingObject.Add(parking);
        database.Tickets.Add(Ticket(1, user, parking, DateTime.Now.AddMinutes(-1)));
        await database.SaveChangesAsync();
        var signal = SetupSignalR();
        using var cancellation = new CancellationTokenSource();
        var service = CreateService(database, signal.Hub.Object, cancellation);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));

        Assert.AreEqual(0, parking.availableSpots);
        Assert.IsFalse((await database.Tickets.FindAsync(1))!.SpotCountApplied);
        signal.Client.Verify(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_SignalRSendsException_HandlesIterationError()
    {
        using var database = CreateDatabase();
        var parking = new ParkingObject { ID = 1, name = "Centar", address = "Test", totalSpots = 10, availableSpots = 1 };
        var user = User();
        database.Users.Add(user);
        database.ParkingObject.Add(parking);
        database.Tickets.Add(Ticket(1, user, parking, DateTime.Now.AddMinutes(-1)));
        await database.SaveChangesAsync();
        var signal = SetupSignalR();
        signal.Client.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("signal error"));
        using var cancellation = new CancellationTokenSource();
        var service = CreateService(database, signal.Hub.Object, cancellation);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));

        Assert.AreEqual(0, parking.availableSpots);
        Assert.IsFalse((await database.Tickets.FindAsync(1))!.SpotCountApplied);
    }

    [TestMethod]
    public async Task ExecuteAsync_ScopeCreationFails_HandlesIterationError()
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(item => item.CreateScope()).Callback(cancellation.Cancel).Throws(new InvalidOperationException("scope error"));
        var service = new TestableParkingService(factory.Object, NullLogger<ParkingReservationBackgroundService>.Instance,
            Mock.Of<IHubContext<ParkingHub>>());

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));
    }

    private static ApplicationDbContext CreateDatabase() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ApplicationUser User() => new()
    {
        Id = "user-1", UserName = "user@test.ba", Email = "user@test.ba", FirstName = "Amina", LastName = "Test"
    };

    private static Ticket Ticket(int id, ApplicationUser user, ParkingObject parking, DateTime issuedAt, bool isPaid = true,
        bool isCancelled = false, bool spotCountApplied = false) => new()
    {
        Id = id,
        ApplicationUserId = user.Id,
        ApplicationUser = user,
        ParkingObjectId = parking.ID,
        ParkingObject = parking,
        IsPaid = isPaid,
        IsCancelled = isCancelled,
        IssuedAt = issuedAt,
        SpotCountApplied = spotCountApplied
    };

    private static (Mock<IHubContext<ParkingHub>> Hub, Mock<IClientProxy> Client) SetupSignalR()
    {
        var client = new Mock<IClientProxy>();
        client.Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.SetupGet(item => item.All).Returns(client.Object);
        var hub = new Mock<IHubContext<ParkingHub>>();
        hub.SetupGet(item => item.Clients).Returns(clients.Object);
        return (hub, client);
    }

    private static TestableParkingService CreateService(ApplicationDbContext database, IHubContext<ParkingHub> hub,
        CancellationTokenSource cancellation)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(item => item.GetService(typeof(ApplicationDbContext))).Returns(database);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(item => item.ServiceProvider).Returns(provider.Object);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(item => item.CreateScope()).Callback(cancellation.Cancel).Returns(scope.Object);
        return new TestableParkingService(factory.Object, NullLogger<ParkingReservationBackgroundService>.Instance, hub);
    }

    private sealed class TestableParkingService(IServiceScopeFactory scopeFactory,
        Microsoft.Extensions.Logging.ILogger<ParkingReservationBackgroundService> logger,
        IHubContext<ParkingHub> hubContext)
        : ParkingReservationBackgroundService(scopeFactory, logger, hubContext)
    {
        public Task RunAsync(CancellationToken token) => ExecuteAsync(token);
    }
}
