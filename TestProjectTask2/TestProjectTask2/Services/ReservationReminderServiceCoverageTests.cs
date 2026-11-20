using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Services;

namespace ParkirajBa.Tests.Services;

[TestClass]
[TestCategory("ReservationReminderService")]
public sealed class ReservationReminderServiceCoverageTests
{
    [TestMethod]
    public async Task ExecuteAsync_MatchingAndNonMatchingReservations_SendsOnlyEligibleReminders()
    {
        using var database = CreateDatabase();
        var user = new ApplicationUser { Id = "user-1", UserName = "user@test.ba", Email = "user@test.ba", FirstName = "Amina", LastName = "Test" };
        var parking = new ParkingObject { ID = 1, name = "Centar", address = "Test", totalSpots = 10, availableSpots = 10 };
        database.Users.Add(user);
        database.ParkingObject.Add(parking);
        var now = DateTime.Now;
        database.Tickets.AddRange(
            Ticket(1, user, parking, now.AddMinutes(5)),
            Ticket(2, user, parking, now.AddMinutes(9)),
            Ticket(3, user, parking, now.AddMinutes(5), isPaid: false),
            Ticket(4, user, parking, now.AddMinutes(5), reminderSent: true),
            Ticket(5, user, parking, null),
            Ticket(6, user, parking, now.AddMinutes(-1)),
            Ticket(7, user, parking, now.AddMinutes(11)),
            Ticket(8, user, parking, now.AddMinutes(5), exited: true));
        await database.SaveChangesAsync();
        var email = new Mock<IEmailSender>();
        var sentTo = new List<string>();
        email.Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((address, _, _) => sentTo.Add(address))
            .Returns(Task.CompletedTask);
        using var cancellation = new CancellationTokenSource();
        var service = CreateService(database, email.Object, cancellation, out _);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));

        Assert.AreEqual(2, sentTo.Count);
        Assert.IsTrue(sentTo.All(address => address == user.Email));
        Assert.IsTrue((await database.Tickets.FindAsync(1))!.ExpirationReminderSent);
        Assert.IsTrue((await database.Tickets.FindAsync(2))!.ExpirationReminderSent);
        Assert.IsFalse((await database.Tickets.FindAsync(3))!.ExpirationReminderSent);
        Assert.IsFalse((await database.Tickets.FindAsync(8))!.ExpirationReminderSent);
    }

    [TestMethod]
    public async Task ExecuteAsync_EmailSenderFails_LeavesReminderPendingAndContinues()
    {
        using var database = CreateDatabase();
        var user = new ApplicationUser { Id = "user-1", UserName = "user@test.ba", Email = "user@test.ba", FirstName = "Amina", LastName = "Test" };
        var parking = new ParkingObject { ID = 1, name = "Centar", address = "Test", totalSpots = 10, availableSpots = 10 };
        database.Users.Add(user);
        database.ParkingObject.Add(parking);
        database.Tickets.Add(Ticket(1, user, parking, DateTime.Now.AddMinutes(5)));
        await database.SaveChangesAsync();
        var email = new Mock<IEmailSender>();
        email.Setup(sender => sender.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp unavailable"));
        using var cancellation = new CancellationTokenSource();
        var service = CreateService(database, email.Object, cancellation, out _);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));

        Assert.IsFalse((await database.Tickets.FindAsync(1))!.ExpirationReminderSent);
        email.Verify(sender => sender.SendEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ScopeCreationFails_HandlesIterationError()
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(item => item.CreateScope()).Callback(cancellation.Cancel).Throws(new InvalidOperationException("scope error"));
        var service = new TestableReminderService(factory.Object, NullLogger<ReservationReminderService>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync(cancellation.Token));
    }

    private static ApplicationDbContext CreateDatabase() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Ticket Ticket(int id, ApplicationUser user, ParkingObject parking, DateTime? expiresAt,
        bool isPaid = true, bool reminderSent = false, bool exited = false) => new()
    {
        Id = id,
        ApplicationUserId = user.Id,
        ApplicationUser = user,
        ParkingObjectId = parking.ID,
        ParkingObject = parking,
        IsPaid = isPaid,
        ExpiresAt = expiresAt,
        ExpirationReminderSent = reminderSent,
        ExitedParking = exited
    };

    private static TestableReminderService CreateService(ApplicationDbContext database, IEmailSender email,
        CancellationTokenSource cancellation, out Mock<IServiceScopeFactory> factory)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(item => item.GetService(typeof(ApplicationDbContext))).Returns(database);
        provider.Setup(item => item.GetService(typeof(IEmailSender))).Returns(email);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(item => item.ServiceProvider).Returns(provider.Object);
        factory = new Mock<IServiceScopeFactory>();
        factory.Setup(item => item.CreateScope()).Callback(cancellation.Cancel).Returns(scope.Object);
        return new TestableReminderService(factory.Object, NullLogger<ReservationReminderService>.Instance);
    }

    private sealed class TestableReminderService(IServiceScopeFactory scopeFactory, Microsoft.Extensions.Logging.ILogger<ReservationReminderService> logger)
        : ReservationReminderService(scopeFactory, logger)
    {
        public Task RunAsync(CancellationToken token) => ExecuteAsync(token);
    }
}
