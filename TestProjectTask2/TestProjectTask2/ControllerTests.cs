using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Hubs;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace TestProjectTask2;

[TestClass]
public class QrControllerTests
{
    [TestMethod]
    public async Task Scan_NepostojeciKod_VracaBadRequest()
    {
        // Arrange
        await using var database = CreateDatabase();
        var controller = new QrController(database, Mock.Of<IHubContext<ParkingHub>>(), Mock.Of<IParkingRepository>());
        // Act
        var result = await controller.Scan(new QrRequest { Code = "NEPOSTOJECI" });
        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Status_NepostojeciKod_VracaNotFound()
    {
        // Arrange
        await using var database = CreateDatabase();
        var controller = new QrController(database, Mock.Of<IHubContext<ParkingHub>>(), Mock.Of<IParkingRepository>());
        // Act
        var result = await controller.Status("NEPOSTOJECI");
        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task Status_PostojeciTiket_VracaStanjeTiketa()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(new Ticket { ReservationCode = "ABC", ApplicationUserId = "user-1", ParkingObjectId = 1, EnteredParking = true, ExitedParking = false });
        await database.SaveChangesAsync();
        var controller = new QrController(database, Mock.Of<IHubContext<ParkingHub>>(), Mock.Of<IParkingRepository>());
        // Act
        var result = await controller.Status("ABC");
        // Assert
        var response = (OkObjectResult)result;
        Assert.IsNotNull(response.Value);
    }

    [TestMethod]
    public async Task Scan_ParkingJosNijeAktivan_VracaBadRequest()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("EARLY", issuedAt: DateTime.Now.AddHours(1)));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "EARLY" });
        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Scan_TiketNijePlacen_VracaBadRequest()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("UNPAID", isPaid: false));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "UNPAID" });
        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Scan_PlacenTiketNaUlazu_DoZvoljavaUlaz()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("ENTRY", isPaid: true));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "ENTRY" });
        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.IsTrue((await database.Tickets.FirstAsync()).EnteredParking);
    }

    [TestMethod]
    public async Task Scan_IsteklaRezervacijaNaIzlazu_VracaBadRequest()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("EXPIRED", entered: true, expiresAt: DateTime.Now.AddMinutes(-1)));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "EXPIRED" });
        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Scan_DodatnaNaknadaNijePlacena_VracaBadRequest()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("CHARGE", entered: true, additionalChargePaid: false));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "CHARGE" });
        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Scan_KratkaRezervacijaNaIzlazu_PovecavaSlobodnaMjesta()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("EXIT", entered: true, expiresAt: DateTime.Now.AddHours(2)));
        await database.SaveChangesAsync();
        var parking = new Mock<IParkingRepository>();
        var controller = CreateController(database, parking.Object);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "EXIT" });
        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result);
        parking.Verify(p => p.IncrementAvailableSpotsAsync(1), Times.Once);
    }

    [TestMethod]
    public async Task Scan_DugaRezervacijaNaIzlazu_OstavljaKodAktivnim()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.Tickets.Add(CreateTicket("LONG", entered: true, expiresAt: DateTime.Now.AddHours(25)));
        await database.SaveChangesAsync();
        var controller = CreateController(database);
        // Act
        var result = await controller.Scan(new QrRequest { Code = "LONG" });
        // Assert
        Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.IsTrue((await database.Tickets.FirstAsync()).QrCodeActive);
    }

    private static QrController CreateController(ApplicationDbContext database, IParkingRepository? parking = null)
    {
        var clients = new Mock<IHubClients>();
        clients.SetupGet(c => c.All).Returns(Mock.Of<IClientProxy>());
        var hub = new Mock<IHubContext<ParkingHub>>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        return new QrController(database, hub.Object, parking ?? Mock.Of<IParkingRepository>());
    }

    private static Ticket CreateTicket(string code, bool isPaid = true, bool entered = false,
        bool additionalChargePaid = true, DateTime? issuedAt = null, DateTime? expiresAt = null, bool exited = false)
    {
        return new Ticket
        {
            ReservationCode = code,
            ApplicationUserId = "user-1",
            ParkingObjectId = 1,
            IsPaid = isPaid,
            EnteredParking = entered,
            ExitedParking = exited,
            AdditionalChargePaid = additionalChargePaid,
            IssuedAt = issuedAt ?? DateTime.Now.AddHours(-1),
            ExpiresAt = expiresAt,
            QrCodeActive = true
        };
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }
}

[TestClass]
public class ReportControllerTests
{
    [TestMethod]
    public async Task Index_NeprijavljenKorisnik_PreusmjeravaNaLogin()
    {
        // Arrange
        var controller = CreateController(null, []);
        // Act
        var result = await controller.Index();
        // Assert
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Login", redirect.ActionName);
    }

    [TestMethod]
    public async Task Index_PrijavljenKorisnik_PopunjavaStatistiku()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-1", FirstName = "Ana", LastName = "Test", Email = "ana@test.ba" };
        var tickets = new List<Ticket> { new() { Price = 12.50m, IssuedAt = DateTime.Now } };
        var controller = CreateController(user, tickets);
        // Act
        var result = await controller.Index();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(12.50m, controller.ViewBag.TotalSpent);
    }

    [TestMethod]
    public async Task ExportPdf_PrijavljenKorisnik_VracaView()
    {
        // Arrange
        var controller = CreateController(CreateUser(), []);
        // Act
        var result = await controller.ExportPdf();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task ExportExcel_PrijavljenKorisnik_VracaExcelFajl()
    {
        // Arrange
        var parking = CreateParking(1);
        var ticket = CreateReportTicket(1, parking);
        var controller = CreateController(CreateUser(), [ticket]);
        // Act
        var result = await controller.ExportExcel();
        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task TicketPdf_KorisnikNemaTiket_PreusmjeravaNaPocetnu()
    {
        // Arrange
        var controller = CreateController(CreateUser(), []);
        // Act
        var result = await controller.TicketPdf(99);
        // Assert
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Index", redirect.ActionName);
    }

    [TestMethod]
    public async Task TicketPdf_KorisnikImaTiket_VracaView()
    {
        // Arrange
        var ticket = CreateReportTicket(4, CreateParking(4));
        var controller = CreateController(CreateUser(), [ticket]);
        // Act
        var result = await controller.TicketPdf(4);
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task OwnerReport_VlasnikSaParkingom_VracaView()
    {
        // Arrange
        var parking = CreateParking(1);
        var controller = CreateController(CreateUser(), [CreateReportTicket(1, parking)], [parking]);
        // Act
        var result = await controller.OwnerReport();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task OwnerReportPdf_VlasnikBezParkinga_VracaView()
    {
        // Arrange
        var controller = CreateController(CreateUser(), [], []);
        // Act
        var result = await controller.OwnerReportPdf();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task OwnerReportExcel_VlasnikBezParkinga_VracaExcelFajl()
    {
        // Arrange
        var controller = CreateController(CreateUser(), [], []);
        // Act
        var result = await controller.OwnerReportExcel();
        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task OwnerReportExcel_VlasnikSaParkingom_VracaExcelFajl()
    {
        // Arrange
        var parking = CreateParking(2);
        var controller = CreateController(CreateUser(), [CreateReportTicket(2, parking)], [parking]);
        // Act
        var result = await controller.OwnerReportExcel();
        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task AdminReport_AdminBezParkinga_VracaView()
    {
        // Arrange
        var controller = CreateController(CreateUser(), [], []);
        // Act
        var result = await controller.AdminReport();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task AdminReportPdf_AdminBezParkinga_VracaView()
    {
        // Arrange
        var controller = CreateController(CreateUser(), [], []);
        // Act
        var result = await controller.AdminReportPdf();
        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task AdminReportExcel_AdminBezParkinga_VracaExcelFajl()
    {
        // Arrange
        var controller = CreateController(CreateUser(), [], []);
        // Act
        var result = await controller.AdminReportExcel();
        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task AdminReportExcel_AdminSaParkingom_VracaExcelFajl()
    {
        // Arrange
        var parking = CreateParking(3);
        var controller = CreateController(CreateUser(), [CreateReportTicket(3, parking)], [parking]);
        // Act
        var result = await controller.AdminReportExcel();
        // Assert
        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser { Id = "user-1", FirstName = "Ana", LastName = "Test", Email = "ana@test.ba" };
    }

    private static ParkingObject CreateParking(int id)
    {
        return new ParkingObject { ID = id, name = "Parking", address = "Adresa", totalSpots = 10 };
    }

    private static Ticket CreateReportTicket(int parkingId, ParkingObject parking)
    {
        return new Ticket
        {
            Id = parkingId,
            ParkingObjectId = parkingId,
            ParkingObject = parking,
            IssuedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddHours(2),
            Price = 10m,
            ApplicationUserId = "user-1"
        };
    }

    private static ReportController CreateController(ApplicationUser? user, List<Ticket> tickets, List<ParkingObject>? parkings = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin")).ReturnsAsync(false);
        var parking = new Mock<IParkingRepository>();
        parking.Setup(repository => repository.GetTicketsByUserIdAsync(It.IsAny<string>())).ReturnsAsync(tickets);
        parking.Setup(repository => repository.GetByOwnerIdAsync(It.IsAny<string>())).ReturnsAsync(parkings ?? []);
        parking.Setup(repository => repository.GetAllWithOwnerAsync()).ReturnsAsync(parkings ?? []);
        parking.Setup(repository => repository.GetTicketsByParkingIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(tickets);
        parking.Setup(repository => repository.GetTicketByIdAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((int id, string _) => tickets.FirstOrDefault(ticket => ticket.Id == id));
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ReportController(new ApplicationDbContext(options), userManager.Object, parking.Object);
    }
}
