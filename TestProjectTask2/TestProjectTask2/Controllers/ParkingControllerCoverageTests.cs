using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace ParkirajBa.Tests.Controllers;

[TestClass]
[TestCategory("ParkingController")]
public sealed class ParkingControllerCoverageTests
{
    [TestMethod]
    public async Task Details_ParkingPostoji_VracaDetaljeICjenovnik()
    {
        using var fixture = new Fixture();
        var parking = Parking(7);
        var prices = new List<Pricing> { new() { ID = 1, ParkingObjectID = 7, price = 3m } };
        var image = new ParkingImage { ImagePath = "/image.jpg" };
        fixture.Repository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(parking);
        fixture.Repository.Setup(repository => repository.GetPricingsByParkingIdAsync(7)).ReturnsAsync(prices);
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(7)).ReturnsAsync(image);

        var result = Assert.IsInstanceOfType<ViewResult>(await fixture.Controller.Details(7));

        Assert.AreSame(parking, result.Model);
        Assert.AreSame(prices, fixture.Controller.ViewBag.Pricings);
        Assert.AreSame(image, fixture.Controller.ViewBag.PrimaryImage);
    }

    [TestMethod]
    public async Task ParkingManagement_NemaKorisnika_VracaUnauthorized()
    {
        using var fixture = new Fixture();

        Assert.IsInstanceOfType<UnauthorizedResult>(await fixture.Controller.ParkingManagement());
    }

    [TestMethod]
    public async Task ParkingManagement_ParkinziBezSlika_PostavljaPodrazumijevanuSliku()
    {
        using var fixture = new Fixture("Owner");
        var owner = new ApplicationUser { Id = "owner-1" };
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(owner);
        fixture.Repository.Setup(repository => repository.GetByOwnerIdAsync(owner.Id)).ReturnsAsync(new List<ParkingObject> { Parking(1), Parking(2) });
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(1)).ReturnsAsync((ParkingImage?)null);
        fixture.Repository.Setup(repository => repository.GetPrimaryImageByParkingIDAsync(2)).ReturnsAsync(new ParkingImage { ImagePath = "/two.jpg" });

        var result = Assert.IsInstanceOfType<ViewResult>(await fixture.Controller.ParkingManagement());
        var images = (Dictionary<int, string>)fixture.Controller.ViewBag.Slike;

        Assert.AreEqual(2, ((List<ParkingObject>)result.Model!).Count);
        Assert.AreEqual("/images/parking.jpg", images[1]);
        Assert.AreEqual("/two.jpg", images[2]);
    }

    [TestMethod]
    public void GetCreateForm_Request_ReturnsCreatePartial()
    {
        using var fixture = new Fixture();

        var result = Assert.IsInstanceOfType<PartialViewResult>(fixture.Controller.GetCreateForm());

        Assert.AreEqual("_ParkingCreate", result.ViewName);
    }

    [TestMethod]
    public async Task GetEditForm_ParkingPostoji_ReturnsEditPartial()
    {
        using var fixture = new Fixture();
        var parking = Parking(4);
        fixture.Repository.Setup(repository => repository.GetByIdAsync(4)).ReturnsAsync(parking);

        var result = Assert.IsInstanceOfType<PartialViewResult>(await fixture.Controller.GetEditForm(4));

        Assert.AreEqual("_ParkingEdit", result.ViewName);
        Assert.AreSame(parking, result.Model);
    }

    [TestMethod]
    public async Task Create_KorisnikNedostaje_VracaGresku()
    {
        using var fixture = new Fixture();

        var result = ResultValue(await fixture.Controller.Create(Parking(1), [], [], []));

        AssertJson(result, false, "Korisnik nije pronađen.");
    }

    [TestMethod]
    [DataRow("", "Adresa", 5, 1, 1, "Naziv parkinga je obavezan.")]
    [DataRow("Centar", "", 5, 1, 1, "Adresa je obavezna.")]
    [DataRow("Centar", "Adresa", 0, 1, 1, "Ukupan broj mjesta mora biti veći od 0.")]
    [DataRow("Centar", "Adresa", 5, 0, 1, "Koordinate su obavezne.")]
    [DataRow("Centar", "Adresa", 5, 1, 0, "Koordinate su obavezne.")]
    public async Task Create_NeispravniObavezniPodaci_VracaValidacijskuGresku(string name, string address, int spots, double latitude, double longitude, string error)
    {
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "owner-1" });

        var result = ResultValue(await fixture.Controller.Create(new ParkingObject
        {
            name = name, address = address, totalSpots = spots, latitude = latitude, longitude = longitude
        }, [], [], []));

        AssertJson(result, false, error);
        fixture.Repository.Verify(repository => repository.AddParking(It.IsAny<ParkingObject>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_PozitivnaCijena_KreiraParkingCijenuIZahtjev()
    {
        using var fixture = new Fixture();
        var owner = new ApplicationUser { Id = "owner-1" };
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(owner);
        fixture.Repository.Setup(repository => repository.AddParking(It.IsAny<ParkingObject>()))
            .ReturnsAsync((ParkingObject parking) => { parking.ID = 12; return parking; });
        var parking = Parking(0);

        var result = ResultValue(await fixture.Controller.Create(parking, [], [], [new PricingCreateDto { pricingType = 1, price = 5m }]));

        AssertJson(result, true, "Parking uspješno kreiran!");
        Assert.AreEqual(owner.Id, parking.OwnerId);
        Assert.AreEqual(10, parking.availableSpots);
        Assert.IsFalse(parking.isApproved);
        fixture.Repository.Verify(repository => repository.AddPricingAsync(It.Is<Pricing>(price => price.ParkingObjectID == 12 && price.price == 5m)), Times.Once);
        fixture.RequestRepository.Verify(repository => repository.AddParkingRequestAsync(12), Times.Once);
    }

    [TestMethod]
    public async Task Create_NegativnaCijena_VracaGreskuNakonKreiranjaParkinga()
    {
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "owner-1" });
        fixture.Repository.Setup(repository => repository.AddParking(It.IsAny<ParkingObject>())).ReturnsAsync((ParkingObject parking) => { parking.ID = 8; return parking; });

        var result = ResultValue(await fixture.Controller.Create(Parking(8), [], [], [new PricingCreateDto { price = -1m }]));

        AssertJson(result, false, "Cijena ne može biti negativna.");
        fixture.RequestRepository.Verify(repository => repository.AddParkingRequestAsync(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_GreskaPriDodavanjuParkinga_VracaOpisGreske()
    {
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "owner-1" });
        fixture.Repository.Setup(repository => repository.AddParking(It.IsAny<ParkingObject>())).ThrowsAsync(new InvalidOperationException("repo error"));

        var result = ResultValue(await fixture.Controller.Create(Parking(1), [], [], []));

        AssertJson(result, false, "Greška pri kreiranju parkinga: repo error");
    }

    [TestMethod]
    public async Task Create_GreskaPriSnimanjuSlike_NastavljaKreiranjeZahtjeva()
    {
        using var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "owner-1" });
        fixture.Repository.Setup(repository => repository.AddParking(It.IsAny<ParkingObject>())).ReturnsAsync((ParkingObject parking) => { parking.ID = 9; return parking; });
        fixture.Repository.Setup(repository => repository.SaveAllParkingImagesByIDAsync(It.IsAny<List<IFormFile>>(), It.IsAny<List<int>>(), 9))
            .ThrowsAsync(new IOException("image error"));

        var result = ResultValue(await fixture.Controller.Create(Parking(9), [Mock.Of<IFormFile>()], [1], []));

        AssertJson(result, true, "Parking uspješno kreiran!");
        fixture.RequestRepository.Verify(repository => repository.AddParkingRequestAsync(9), Times.Once);
    }

    [TestMethod]
    public async Task UploadCoverImage_OwnerNemaParking_VracaGresku()
    {
        using var fixture = new Fixture("Owner");
        fixture.Repository.Setup(repository => repository.GetByIdAsync(6)).ReturnsAsync((ParkingObject?)null);

        var result = ResultValue(await fixture.Controller.UploadCoverImage(6, Mock.Of<IFormFile>()));

        AssertJson(result, false, "Parking nije pronađen.");
    }

    [TestMethod]
    public async Task UploadCoverImage_OwnerUredjujeTudjiParking_VracaZabranu()
    {
        using var fixture = new Fixture("Owner");
        fixture.UserManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new ApplicationUser { Id = "owner-1" });
        fixture.Repository.Setup(repository => repository.GetByIdAsync(6)).ReturnsAsync(Parking(6, "owner-2"));

        var result = ResultValue(await fixture.Controller.UploadCoverImage(6, Mock.Of<IFormFile>()));

        AssertJson(result, false, "Nemate pravo mijenjati ovu sliku.");
    }

    [TestMethod]
    public async Task UploadCoverImage_NemaOdabraneSlike_VracaGresku()
    {
        using var fixture = new Fixture();

        var result = ResultValue(await fixture.Controller.UploadCoverImage(6, null!));

        AssertJson(result, false, "Molimo odaberite sliku.");
    }

    [TestMethod]
    public async Task UploadCoverImage_SlikaPostoji_BriseStariZapisISpremaNovu()
    {
        using var fixture = new Fixture();
        fixture.Database.ParkingImages.Add(new ParkingImage { ID = 1, ParkingObjectID = 6, Position = 1, ImagePath = string.Empty });
        await fixture.Database.SaveChangesAsync();
        var image = new Mock<IFormFile>();
        image.Setup(file => file.Length).Returns(10);

        var result = ResultValue(await fixture.Controller.UploadCoverImage(6, image.Object));

        AssertJson(result, true, "Slika uspješno ažurirana.");
        Assert.AreEqual(0, await fixture.Database.ParkingImages.CountAsync());
        fixture.Repository.Verify(repository => repository.SaveParkingImageByIDAsync(image.Object, 1, 6), Times.Once);
    }

    [TestMethod]
    public async Task Edit_ParkingNedostaje_VracaGresku()
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync((ParkingObject?)null);

        AssertJson(ResultValue(await Edit(fixture, 3)), false, "Parking nije pronađen.");
    }

    [TestMethod]
    [DataRow("bad", "2", "Neispravan format geografske širine.")]
    [DataRow("1", "bad", "Neispravan format geografske dužine.")]
    [DataRow("1", "2", "Naziv parkinga je obavezan.", " ")]
    public async Task Edit_NeispravniPodaci_VracaValidacijskuGresku(string latitude, string longitude, string error, string name = "Centar")
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(Parking(3));

        AssertJson(ResultValue(await Edit(fixture, 3, name, latitude, longitude)), false, error);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(0)]
    public async Task Edit_NeispravanBrojMjesta_VracaValidacijskuGresku(int? spots)
    {
        using var fixture = new Fixture();
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(Parking(3));

        AssertJson(ResultValue(await Edit(fixture, 3, totalSpots: spots)), false, "Ukupan broj mjesta mora biti veći od 0.");
    }

    [TestMethod]
    public async Task Edit_AdminMijenjaParkingIVracaRedirect()
    {
        using var fixture = new Fixture("Admin");
        var parking = Parking(3);
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(parking);

        var result = Assert.IsInstanceOfType<RedirectToActionResult>(await Edit(fixture, 3, "Novo", "44.1", "18.2", 12));

        Assert.AreEqual("ParkingDetails", result.ActionName);
        Assert.AreEqual(12, parking.totalSpots);
        Assert.AreEqual(7, parking.availableSpots);
        fixture.Repository.Verify(repository => repository.ModifyParkingAsync(parking), Times.Once);
    }

    [TestMethod]
    public async Task Edit_OwnerMijenjaParking_VracaJson()
    {
        using var fixture = new Fixture("Owner");
        var parking = Parking(3, "owner-1");
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(parking);
        fixture.Repository.Setup(repository => repository.ModifyParkingAsync(parking)).ReturnsAsync(parking);

        var result = ResultValue(await Edit(fixture, 3, "Novo", "44.1", "18.2", 12));

        AssertJson(result, true, "Parking uspješno ažuriran!");
    }

    [TestMethod]
    public async Task Edit_CijeneIDiSlikeObradjujePostojeceINoveCijene()
    {
        using var fixture = new Fixture("Admin");
        var parking = Parking(3);
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(parking);
        var existingPrice = new Pricing { ID = 10, ParkingObjectID = 3, pricingType = PricingType.Hourly, price = 1m };
        var existingPrices = new List<Pricing> { existingPrice };
        fixture.Repository.Setup(repository => repository.GetPricingsByParkingIdAsync(3)).ReturnsAsync(existingPrices);
        fixture.Repository.Setup(repository => repository.ModifyParkingAsync(parking)).ReturnsAsync(parking);
        fixture.Repository.Setup(repository => repository.AddPricingAsync(It.IsAny<Pricing>())).ReturnsAsync((Pricing price) => price);
        var upload = new Mock<IFormFile>();
        upload.Setup(file => file.Length).Returns(10);
        fixture.Repository.Setup(repository => repository.SaveAllParkingImagesByIDAsync(It.IsAny<List<IFormFile>>(), It.IsAny<List<int>>(), 3))
            .ThrowsAsync(new IOException("image error"));
        fixture.Database.Pricing.Add(existingPrice);
        await fixture.Database.SaveChangesAsync();

        var result = Assert.IsInstanceOfType<RedirectToActionResult>(await Edit(fixture, 3, "Novo", "44.1", "18.2", 12,
            prices: [new PricingCreateDto { pricingType = 0, price = 2m, validFrom = "2026-01-01" }, new PricingCreateDto { pricingType = 1, price = 3m }, new PricingCreateDto { pricingType = 2, price = -5m }],
            images: [upload.Object], imagePositions: null));

        Assert.AreEqual("ParkingDetails", result.ActionName);
        Assert.AreEqual(2m, existingPrice.price);
        fixture.Repository.Verify(repository => repository.SaveAllParkingImagesByIDAsync(It.IsAny<List<IFormFile>>(), It.IsAny<List<int>>(), 3), Times.Once);
        fixture.Repository.Verify(repository => repository.AddPricingAsync(It.Is<Pricing>(price => price.pricingType == PricingType.Daily && price.price == 3m)), Times.Once);
    }

    [TestMethod]
    public async Task Edit_CijenePoIduAzuriraSamoZapiseKojiPripadajuParkingu()
    {
        using var fixture = new Fixture("Owner");
        var parking = Parking(3, "owner-1");
        fixture.Repository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(parking);
        var current = new Pricing { ID = 10, ParkingObjectID = 3, price = 1m };
        var other = new Pricing { ID = 11, ParkingObjectID = 4, price = 1m };
        fixture.Database.Pricing.AddRange(current, other);
        await fixture.Database.SaveChangesAsync();

        var result = ResultValue(await Edit(fixture, 3, pricingIds: [10, 11, 999], pricingValues: [4m, 5m]));

        AssertJson(result, true, "Parking uspješno ažuriran!");
        Assert.AreEqual(4m, current.price);
        Assert.AreEqual(1m, other.price);
    }

    private static async Task<IActionResult> Edit(Fixture fixture, int id, string name = "Centar", string latitude = "44.1", string longitude = "18.2", int? totalSpots = 10,
        List<PricingCreateDto>? prices = null, List<IFormFile>? images = null, List<int>? imagePositions = null,
        List<int>? pricingIds = null, List<decimal>? pricingValues = null) =>
        await fixture.Controller.Edit(id, name, "Adresa", latitude, longitude, totalSpots, 2.2, true, true, true, false,
            "08:00", "22:00", prices, images, imagePositions, pricingIds, pricingValues);

    private static object ResultValue(IActionResult actionResult) => Assert.IsInstanceOfType<JsonResult>(actionResult).Value!;

    private static void AssertJson(object result, bool success, string message)
    {
        Assert.AreEqual(success, (bool)result.GetType().GetProperty("success")!.GetValue(result)!);
        Assert.AreEqual(message, result.GetType().GetProperty("message")!.GetValue(result));
    }

    private static ParkingObject Parking(int id, string? ownerId = null) => new()
    {
        ID = id, name = $"Parking {id}", address = "Test", OwnerId = ownerId,
        totalSpots = 10, availableSpots = 5, latitude = 44, longitude = 18
    };

    private sealed class Fixture : IDisposable
    {
        public ApplicationDbContext Database { get; }
        public Mock<IParkingRepository> Repository { get; } = new();
        public Mock<IRequestRepository> RequestRepository { get; } = new();
        public Mock<UserManager<ApplicationUser>> UserManager { get; }
        public ParkingController Controller { get; }

        public Fixture(string? role = null)
        {
            Database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var store = new Mock<IUserStore<ApplicationUser>>();
            UserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            Controller = new ParkingController(Repository.Object, UserManager.Object, Database, RequestRepository.Object);
                var httpContext = new DefaultHttpContext { User = role == null
                    ? new ClaimsPrincipal(new ClaimsIdentity())
                    : new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "test")) };
                Controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
                Controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        }

        public void Dispose()
        {
            Controller.Dispose();
            Database.Dispose();
        }
    }
}
