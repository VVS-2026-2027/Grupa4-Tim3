using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Tests.Controllers;

[TestClass]
[TestCategory("HomeController")]
public sealed class HomeControllerCoverageTests
{
    [TestMethod]
    public async Task Index_AdminKorisnik_PreusmjeravaNaDashboard()
    {
        var user = new ApplicationUser { Id = "1", UserName = "admin@test.ba" };
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        fixture.UserManager.Setup(manager => manager.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

        var result = await fixture.Controller.Index();

        var redirect = Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual("Dashboard", redirect.ActionName);
        Assert.AreEqual("Admin", redirect.ControllerName);
    }

    [TestMethod]
    public async Task Index_Gost_PostavljaMapKeyIVracaOdobreneParkinge()
    {
        using var fixture = new Fixture();
        var parkings = new List<ParkingObject> { Parking(1) };
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
        fixture.Repository.Setup(repository => repository.GetAllApprovedAsync()).ReturnsAsync(parkings);

        var result = Assert.IsInstanceOfType<ViewResult>(await fixture.Controller.Index());

        Assert.AreEqual("Guest", fixture.Controller.ViewBag.FullName);
        Assert.AreEqual("maps-key", fixture.Controller.ViewData["GoogleMapsApiKey"]);
        Assert.AreSame(parkings, result.Model);
    }

    [TestMethod]
    public async Task Index_PrijavljeniKorisnik_PrikazujeImeIzBaze()
    {
        using var fixture = new Fixture("member@test.ba");
        fixture.Database.Users.Add(new ApplicationUser { Id = "1", UserName = "member@test.ba", FirstName = "Amina", LastName = "Test" });
        await fixture.Database.SaveChangesAsync();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        await fixture.Controller.Index();

        Assert.AreEqual("Amina Test", fixture.Controller.ViewBag.FullName);
    }

    [TestMethod]
    public async Task Objekti_SaISlikomIViseParkinga_PostavljaPodrazumijevanuSliku()
    {
        using var fixture = new Fixture();
        var parkings = new List<ParkingObject> { Parking(1), Parking(2) };
        fixture.Repository.Setup(repository => repository.GetAllApprovedAsync()).ReturnsAsync(parkings);
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(1))
            .ReturnsAsync(new ParkingImage { ImagePath = "/images/one.jpg" });
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(2)).ReturnsAsync((ParkingImage?)null);

        var result = Assert.IsInstanceOfType<ViewResult>(await fixture.Controller.Objekti());
        var images = (Dictionary<int, string>)fixture.Controller.ViewBag.Slike;

        Assert.AreSame(parkings, result.Model);
        Assert.AreEqual("/images/one.jpg", images[1]);
        Assert.AreEqual("/images/parking.png", images[2]);
    }

    [TestMethod]
    public async Task Rezervacije_NemaKorisnika_PreusmjeravaNaLogin()
    {
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

        var result = Assert.IsInstanceOfType<RedirectToActionResult>(await fixture.Controller.Rezervacije());

        Assert.AreEqual("Login", result.ActionName);
        Assert.AreEqual("User", result.ControllerName);
    }

    [TestMethod]
    public async Task Rezervacije_PrijavljenKorisnik_VracaSamoNjegoveKarte()
    {
        using var fixture = new Fixture("member@test.ba");
        var user = new ApplicationUser { Id = "1", UserName = "member@test.ba", FirstName = "Amina", LastName = "Test" };
        var parking = Parking(1);
        fixture.Database.Users.Add(user);
        fixture.Database.ParkingObject.Add(parking);
        fixture.Database.Tickets.Add(new Ticket { Id = 1, ApplicationUserId = user.Id, ApplicationUser = user, ParkingObjectId = parking.ID, ParkingObject = parking });
        await fixture.Database.SaveChangesAsync();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var result = Assert.IsInstanceOfType<ViewResult>(await fixture.Controller.Rezervacije());
        var tickets = Assert.IsInstanceOfType<List<Ticket>>(result.Model);

        Assert.AreEqual(1, tickets.Count);
        Assert.AreEqual(1, tickets[0].Id);
    }

    [TestMethod]
    public async Task Logout_PozivAkcije_OdjavljujeIPreusmjerava()
    {
        using var fixture = new Fixture();
        var auth = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        fixture.HttpContext.RequestServices = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider();
        fixture.Controller.Url = Mock.Of<Microsoft.AspNetCore.Mvc.IUrlHelper>(helper =>
            helper.Action(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlActionContext>()) == "/");

        var result = Assert.IsInstanceOfType<RedirectToActionResult>(await fixture.Controller.Logout());

        Assert.AreEqual("Index", result.ActionName);
        auth.Verify(service => service.SignOutAsync(fixture.HttpContext, null, null), Times.Once);
    }

    [TestMethod]
    public void StaticViews_PozivAkcija_VracajuOcekivanePoglede()
    {
        using var fixture = new Fixture();

        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Privacy());
        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Login());
        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Register());
        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Placanje());
        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Uspjeh());
        var confirmation = Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Potvrda("Amina"));
        Assert.AreEqual("Amina", fixture.Controller.ViewBag.ImeKorisnika);
        Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Error());
        Assert.AreEqual(fixture.HttpContext.TraceIdentifier, ((ErrorViewModel)Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Error()).Model!).RequestId);
    }

    [TestMethod]
    public void Error_AktivnaActivity_KoristiActivityIdKaoRequestId()
    {
        using var fixture = new Fixture();
        using var activity = new Activity("coverage-test").Start();

        var result = Assert.IsInstanceOfType<ViewResult>(fixture.Controller.Error());

        Assert.AreEqual(activity.Id, ((ErrorViewModel)result.Model!).RequestId);
    }

    [TestMethod]
    public void DatabaseTest_PozivAkcije_PohranjujeParkingICjenovnik()
    {
        using var fixture = new Fixture();

        var result = Assert.IsInstanceOfType<ContentResult>(fixture.Controller.databaseTest());

        Assert.AreEqual("Database test completed successfully!", result.Content);
        Assert.AreEqual(1, fixture.Database.ParkingObject.Count());
        Assert.AreEqual(1, fixture.Database.Pricing.Count());
    }

    [TestMethod]
    public async Task PosaljiEmail_NeprijavljenKorisnik_PreusmjeravaNaLogin()
    {
        using var fixture = new Fixture();

        var result = Assert.IsInstanceOfType<RedirectToActionResult>(await fixture.Controller.PosaljiEmail("Amina"));

        Assert.AreEqual("Login", result.ActionName);
    }

    [TestMethod]
    public async Task PosaljiEmail_NeuspravnaAdresaKonfiguracije_VracaSadrzajGreske()
    {
        using var fixture = new Fixture("member@test.ba", new Dictionary<string, string?>
        {
            ["EmailSettings:SenderEmail"] = "not-an-email",
            ["EmailSettings:AppPassword"] = "password"
        });

        var result = Assert.IsInstanceOfType<ContentResult>(await fixture.Controller.PosaljiEmail("Amina"));

        StringAssert.StartsWith(result.Content!, "Greška:");
    }

    [TestMethod]
    public async Task Search_FilterParameters_ProsljedjujeRezultatRepozitorija()
    {
        using var fixture = new Fixture();
        var expected = new List<ParkingObject> { Parking(3) };
        fixture.Repository.Setup(repository => repository.FilterApprovedParkings("Centar", true, false, true, false, "Day", 15))
            .ReturnsAsync(expected);

        var result = Assert.IsInstanceOfType<JsonResult>(await fixture.Controller.Search("Centar", true, false, true, false, "Day", 15));

        Assert.AreSame(expected, result.Value);
        fixture.Repository.Verify(repository => repository.FilterApprovedParkings("Centar", true, false, true, false, "Day", 15), Times.Once);
    }

    [TestMethod]
    [DataRow("Day", PricingType.Daily)]
    [DataRow("Week", PricingType.Weekly)]
    [DataRow("Month", PricingType.Monthly)]
    [DataRow("Year", PricingType.Yearly)]
    [DataRow("other", PricingType.Hourly)]
    public async Task GetMaxPriceForRegime_MapiraSveRezimove(string regime, PricingType expectedType)
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetMaxPricingForRegimeAsync(expectedType)).ReturnsAsync(99m);

        var result = await fixture.Controller.GetMaxPriceForRegime(regime);

        Assert.AreEqual(99m, result);
        fixture.Repository.Verify(repository => repository.GetMaxPricingForRegimeAsync(expectedType), Times.Once);
    }

    [TestMethod]
    [DataRow("Day", PricingType.Daily)]
    [DataRow("Week", PricingType.Weekly)]
    [DataRow("Month", PricingType.Monthly)]
    [DataRow("Year", PricingType.Yearly)]
    [DataRow("other", PricingType.Hourly)]
    public async Task GetParkingPrice_MapiraRezimIVracaCijenu(string regime, PricingType expectedType)
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetActivePricingAsync(7, expectedType))
            .ReturnsAsync(new Pricing { price = 3.5m });

        var result = await fixture.Controller.GetParkingPrice(7, regime);

        Assert.AreEqual(3.5m, result);
        fixture.Repository.Verify(repository => repository.GetActivePricingAsync(7, expectedType), Times.Once);
    }

    [TestMethod]
    public async Task GetParkingPrice_CijenaNePostoji_VracaNulu()
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetActivePricingAsync(7, PricingType.Hourly)).ReturnsAsync((Pricing?)null);

        Assert.AreEqual(0m, await fixture.Controller.GetParkingPrice(7, "unknown"));
    }

    [TestMethod]
    public async Task GetPrimaryParkingImage_PozivAkcije_VracaJson()
    {
        using var fixture = new Fixture();
        var image = new ParkingImage { ImagePath = "/images/primary.jpg" };
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(8)).ReturnsAsync(image);

        var result = Assert.IsInstanceOfType<JsonResult>(await fixture.Controller.GetPrimaryParkingImage(8));

        Assert.AreSame(image, result.Value);
    }

    private static ParkingObject Parking(int id) => new() { ID = id, name = $"Parking {id}", address = "Test", totalSpots = 10, availableSpots = 10 };

    private sealed class Fixture : IDisposable
    {
        public ApplicationDbContext Database { get; }
        public Mock<UserManager<ApplicationUser>> UserManager { get; }
        public Mock<IParkingRepository> Repository { get; } = new();
        public HomeController Controller { get; }
        public DefaultHttpContext HttpContext { get; }

        public Fixture(string? username = null, IDictionary<string, string?>? settings = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            Database = new ApplicationDbContext(options);
            var store = new Mock<IUserStore<ApplicationUser>>();
            UserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["GoogleMaps:ApiKey"] = "maps-key"
            }).Build();
            Controller = new HomeController(Mock.Of<ILogger<HomeController>>(), Database, configuration, UserManager.Object, Repository.Object);
            HttpContext = new DefaultHttpContext();
            var identity = username == null
                ? new ClaimsIdentity()
                : new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "test");
            HttpContext.User = new ClaimsPrincipal(identity);
            Controller.ControllerContext = new ControllerContext { HttpContext = HttpContext };
        }

        public void Dispose()
        {
            Controller.Dispose();
            Database.Dispose();
        }
    }
}
