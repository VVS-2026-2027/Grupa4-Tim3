using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using ParkirajBa.Services;

namespace TestProjectTask2;

[TestClass]
public sealed class ParkingModelAdditionalTests
{
    [TestMethod]
    [DataRow(-1)]
    [DataRow(10001)]
    public void ParkingObject_AvailableSpotsOutOfRange_ValidationFails(int availableSpots)
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", totalSpots = 10, availableSpots = availableSpots };
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(parking, new ValidationContext(parking), errors, true);

        // Assert
        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void ParkingImage_DefaultValues_AreEmpty()
    {
        // Arrange
        var image = new ParkingImage();

        // Act
        var path = image.ImagePath;

        // Assert
        Assert.IsNull(path);
        Assert.IsNull(image.Position);
        Assert.AreEqual(0, image.ID);
        Assert.AreEqual(0, image.ParkingObjectID);
    }
}

[TestClass]
public sealed class ParkingRepositoryAdditionalTests
{
    private ApplicationDbContext _database = null!;
    private ParkingRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _database = new ApplicationDbContext(options);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());
        _repository = new ParkingRepository(_database, new ImageService(environment.Object));
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    [TestMethod]
    public async Task ModifyParking_ExistingParking_UpdatesValues()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Stari", address = "Stara 1", totalSpots = 5 });
        var changed = new ParkingObject { ID = parking.ID, name = "Novi", address = "Nova 1", totalSpots = 8 };

        // Act
        var result = await _repository.ModifyParkingAsync(changed);

        // Assert
        Assert.AreEqual("Novi", result!.name);
        Assert.AreEqual(8, result.totalSpots);
    }

    [TestMethod]
    public async Task ModifyParking_MissingParking_ReturnsNull()
    {
        // Arrange
        var changed = new ParkingObject { ID = 99, name = "Novi" };

        // Act
        var result = await _repository.ModifyParkingAsync(changed);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetImagePaths_ImagesHavePositions_ReturnsOrderedPaths()
    {
        // Arrange
        _database.ParkingImages.AddRange(
            new ParkingImage { ParkingObjectID = 1, Position = 2, ImagePath = "second" },
            new ParkingImage { ParkingObjectID = 1, Position = 1, ImagePath = "first" });
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetImagePathsByParkingIDAsync(1);

        // Assert
        CollectionAssert.AreEqual(new[] { "first", "second" }, result);
    }

    [TestMethod]
    public async Task GetAllApproved_ApprovedAndPending_ReturnsApprovedOnly()
    {
        // Arrange
        await _repository.AddParking(new ParkingObject { name = "Odobren", address = "Glavna 1", totalSpots = 1, isApproved = true });
        await _repository.AddParking(new ParkingObject { name = "Na cekanju", address = "Sporedna 1", totalSpots = 1, isApproved = false });

        // Act
        var result = await _repository.GetAllApprovedAsync();

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Odobren", result[0].name);
    }

    [TestMethod]
    public async Task AddAllPricings_TwoPrices_ReturnsBothPrices()
    {
        // Arrange
        var prices = new List<Pricing>
        {
            new() { pricingType = PricingType.Hourly, price = 2 },
            new() { pricingType = PricingType.Daily, price = 10 }
        };

        // Act
        var result = await _repository.AddAllPricingsAsync(prices);

        // Assert
        Assert.AreEqual(2, result!.Count);
        Assert.AreEqual(2, (await _repository.GetPricingsByParkingIdAsync(0)).Count);
    }

    [TestMethod]
    public async Task GetTicketsByUser_UserHasTickets_ReturnsNewestFirst()
    {
        // Arrange
        _database.ParkingObject.Add(new ParkingObject { ID = 1, name = "Centar", address = "Glavna 1", totalSpots = 10 });
        _database.Users.Add(new ApplicationUser { Id = "user", UserName = "user", FirstName = "Test", LastName = "User" });
        _database.Tickets.AddRange(
            new Ticket { ApplicationUserId = "user", IssuedAt = DateTime.Today.AddDays(-1), ParkingObjectId = 1 },
            new Ticket { ApplicationUserId = "user", IssuedAt = DateTime.Today, ParkingObjectId = 1 });
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetTicketsByUserIdAsync("user");

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result[0].IssuedAt >= result[1].IssuedAt);
    }

    [TestMethod]
    public async Task GetAll_WithParking_ReturnsParking()
    {
        // Arrange
        await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2 });

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task SaveParkingImage_ValidFile_PersistsImageRecord()
    {
        // Arrange
        var file = new FormFile(new MemoryStream("image"u8.ToArray()), 0, 5, "Image", "one.jpg");

        // Act
        var result = await _repository.SaveParkingImageByIDAsync(file, 1, 4);

        // Assert
        Assert.IsTrue(result.EndsWith(".jpg"));
        Assert.AreEqual(1, await _database.ParkingImages.CountAsync());
    }

    [TestMethod]
    public async Task SaveAllParkingImages_EqualLists_ReturnsAllPaths()
    {
        // Arrange
        var files = new List<IFormFile>
        {
            new FormFile(new MemoryStream("image"u8.ToArray()), 0, 5, "Image", "one.jpg")
        };

        // Act
        var result = await _repository.SaveAllParkingImagesByIDAsync(files, [1], 4);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task GetByOwnerIdWithPricings_OwnerHasParking_ReturnsPricing()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, OwnerId = "owner" });
        await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, price = 2 });

        // Act
        var result = await _repository.GetByOwnerIdWithPricingsAsync("owner");

        // Assert
        Assert.AreEqual(1, result[0].Pricings.Count);
    }

    [TestMethod]
    public async Task GetImagesByParkingId_ImagesExist_ReturnsImages()
    {
        // Arrange
        _database.ParkingImages.Add(new ParkingImage { ParkingObjectID = 3, Position = 1, ImagePath = "one" });
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetImagesByParkingIDAsync(3);

        // Assert
        Assert.AreEqual(1, result!.Count);
    }

    [TestMethod]
    public async Task GetPrimaryImagePath_ImageExists_ReturnsPath()
    {
        // Arrange
        _database.ParkingImages.Add(new ParkingImage { ParkingObjectID = 3, Position = 1, ImagePath = "one" });
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetPrimaryImagePathByParkingIDAsync(3);

        // Assert
        Assert.AreEqual("one", result);
    }

    [TestMethod]
    public async Task GetMaxPricingForRegime_PricingsExist_ReturnsHighestPrice()
    {
        // Arrange
        await _repository.AddPricingAsync(new Pricing { pricingType = PricingType.Hourly, price = 2 });
        await _repository.AddPricingAsync(new Pricing { pricingType = PricingType.Hourly, price = 5 });

        // Act
        var result = await _repository.GetMaxPricingForRegimeAsync(PricingType.Hourly);

        // Assert
        Assert.AreEqual(5m, result);
    }

    [TestMethod]
    public async Task GetTicketsByParkingIds_MatchingTicket_ReturnsTicket()
    {
        // Arrange
        _database.ParkingObject.Add(new ParkingObject { ID = 1, name = "Centar", address = "Glavna 1", totalSpots = 2 });
        _database.Tickets.Add(new Ticket { ParkingObjectId = 1, ApplicationUserId = "user" });
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetTicketsByParkingIdsAsync([1]);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task GetTicketById_CorrectUser_ReturnsTicket()
    {
        // Arrange
        _database.ParkingObject.Add(new ParkingObject { ID = 1, name = "Centar", address = "Glavna 1", totalSpots = 2 });
        var ticket = new Ticket { ParkingObjectId = 1, ApplicationUserId = "user" };
        _database.Tickets.Add(ticket);
        await _database.SaveChangesAsync();

        // Act
        var result = await _repository.GetTicketByIdAsync(ticket.Id, "user");

        // Assert
        Assert.AreEqual(ticket.Id, result!.Id);
    }

    [TestMethod]
    public async Task GetAllWithPricingsAndOwner_ReturnsRelatedData()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        _database.Users.Add(user);
        var parking = new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, OwnerId = user.Id };
        _database.ParkingObject.Add(parking);
        _database.Pricing.Add(new Pricing { ParkingObject = parking, price = 3 });
        await _database.SaveChangesAsync();

        // Act
        var withPrices = await _repository.GetAllWithPricingsAsync();
        var withOwner = await _repository.GetAllWithOwnerAsync();

        // Assert
        Assert.AreEqual(1, withPrices[0].Pricings.Count);
        Assert.AreEqual(user.Id, withOwner[0].OwnerId);
    }

    [TestMethod]
    public async Task GetByIdWithRelations_ReturnsRelatedData()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2 });
        await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, price = 3 });

        // Act
        var withPrices = await _repository.GetByIdWithPricingsAsync(parking.ID);
        var withOwner = await _repository.GetByIdWithOwnerAsync(parking.ID);

        // Assert
        Assert.AreEqual(1, withPrices!.Pricings.Count);
        Assert.IsNotNull(withOwner);
    }

    [TestMethod]
    public async Task GetParkingPricings_ExistingParking_ReturnsPrices()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2 });
        await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, price = 3 });

        // Act
        var result = await _repository.GetParkingPricings(parking.ID);

        // Assert
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public async Task FilterParkings_AllFlags_ReturnsMatchingParking()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, isUnderground = true, hasEVCharger = true, hasCameras = true, isDisabledAccessible = true });
        await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, pricingType = PricingType.Daily, price = 3 });

        // Act
        var result = await _repository.FilterParkings("Centar", true, true, true, true, "Day", 3);

        // Assert
        Assert.AreEqual(1, result.Count);
    }
}

[TestClass]
public sealed class ControllerAdditionalTests
{
    [TestMethod]
    public async Task Details_ExistingParking_ReturnsViewWithRelatedData()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        var parking = new ParkingObject { ID = 2, name = "Centar", totalSpots = 10 };
        repository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(parking);
        repository.Setup(r => r.GetPricingsByParkingIdAsync(2)).ReturnsAsync([]);
        repository.Setup(r => r.GetPrimaryImageByParkingIDAsync(2)).ReturnsAsync((ParkingImage?)null);
        var controller = CreateParkingController(repository);

        // Act
        var result = await controller.Details(2);

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreSame(parking, ((ViewResult)result).Model);
        repository.Verify(r => r.GetPricingsByParkingIdAsync(2), Times.Once);
    }

    [TestMethod]
    public async Task GetPrimaryParkingImage_RepositoryReturnsImage_ReturnsJson()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        var image = new ParkingImage { ImagePath = "/images/one.jpg" };
        repository.Setup(r => r.GetPrimaryImageByParkingIDAsync(3)).ReturnsAsync(image);
        var controller = CreateHomeController(repository);

        // Act
        var result = await controller.GetPrimaryParkingImage(3);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreSame(image, ((JsonResult)result).Value);
    }

    [TestMethod]
    [DataRow("Week", PricingType.Weekly)]
    [DataRow("Month", PricingType.Monthly)]
    [DataRow("Year", PricingType.Yearly)]
    public async Task GetMaxPriceForRegime_KnownRegime_UsesExpectedType(string regime, PricingType type)
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetMaxPricingForRegimeAsync(type)).ReturnsAsync(4m);
        var controller = CreateHomeController(repository);

        // Act
        var result = await controller.GetMaxPriceForRegime(regime);

        // Assert
        Assert.AreEqual(4m, result);
    }

    [TestMethod]
    [DataRow("Day", PricingType.Daily)]
    [DataRow("Week", PricingType.Weekly)]
    [DataRow("Month", PricingType.Monthly)]
    [DataRow("Year", PricingType.Yearly)]
    public async Task GetParkingPrice_KnownRegime_ReturnsActivePrice(string regime, PricingType type)
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetActivePricingAsync(1, type)).ReturnsAsync(new Pricing { price = 7 });
        var controller = CreateHomeController(repository);

        // Act
        var result = await controller.GetParkingPrice(1, regime);

        // Assert
        Assert.AreEqual(7m, result);
    }

    [TestMethod]
    public async Task Objekti_ParkingHasNoImage_ReturnsViewWithDefaultImage()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetAllApprovedAsync()).ReturnsAsync([new ParkingObject { ID = 1, name = "Centar", address = "Glavna 1", totalSpots = 2 }]);
        repository.Setup(r => r.GetPrimaryImageByParkingIDAsync(1)).ReturnsAsync((ParkingImage?)null);
        var controller = CreateHomeController(repository);

        // Act
        var result = await controller.Objekti();

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("/images/parking.png", ((Dictionary<int, string>)controller.ViewBag.Slike)[1]);
    }

    [TestMethod]
    public void Potvrda_Name_SetsViewBag()
    {
        // Arrange
        var controller = CreateHomeController(new Mock<IParkingRepository>());

        // Act
        var result = controller.Potvrda("Hamza");

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual("Hamza", controller.ViewBag.ImeKorisnika);
    }

    [TestMethod]
    public async Task Index_Guest_ReturnsApprovedParkings()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetAllApprovedAsync()).ReturnsAsync([]);
        var controller = CreateHomeController(repository);

        // Act
        var result = await controller.Index();

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
        repository.Verify(r => r.GetAllApprovedAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Index_Admin_RedirectsToDashboard()
    {
        // Arrange
        var user = new ApplicationUser { Id = "admin", UserName = "admin", FirstName = "Admin", LastName = "User" };
        var controller = CreateHomeController(new Mock<IParkingRepository>(), user, true);

        // Act
        var result = await controller.Index();

        // Assert
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Dashboard", redirect.ActionName);
    }

    [TestMethod]
    public async Task Objekti_ParkingHasImage_StoresImagePath()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetAllApprovedAsync()).ReturnsAsync([new ParkingObject { ID = 1, name = "Centar", address = "Glavna 1", totalSpots = 2 }]);
        repository.Setup(r => r.GetPrimaryImageByParkingIDAsync(1)).ReturnsAsync(new ParkingImage { ImagePath = "/images/one.jpg" });
        var controller = CreateHomeController(repository);

        // Act
        await controller.Objekti();

        // Assert
        Assert.AreEqual("/images/one.jpg", ((Dictionary<int, string>)controller.ViewBag.Slike)[1]);
    }

    [TestMethod]
    public async Task Rezervacije_NoUser_RedirectsToLogin()
    {
        // Arrange
        var controller = CreateHomeController(new Mock<IParkingRepository>());

        // Act
        var result = await controller.Rezervacije();

        // Assert
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Login", redirect.ActionName);
    }

    [TestMethod]
    public void SimplePages_ReturnViewResults()
    {
        // Arrange
        var controller = CreateHomeController(new Mock<IParkingRepository>());

        // Act
        var results = new[] { controller.Login(), controller.Register(), controller.Placanje(), controller.Uspjeh(), controller.Error() };

        // Assert
        CollectionAssert.AllItemsAreInstancesOfType(results, typeof(ViewResult));
    }

    [TestMethod]
    public async Task ParkingManagement_NoUser_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateParkingController(new Mock<IParkingRepository>());

        // Act
        var result = await controller.ParkingManagement();

        // Assert
        Assert.IsInstanceOfType<UnauthorizedResult>(result);
    }

    [TestMethod]
    public async Task Create_NoUser_ReturnsUserError()
    {
        // Arrange
        var controller = CreateParkingController(new Mock<IParkingRepository>());

        // Act
        var result = await controller.Create(new ParkingObject(), [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Create_InvalidAddress_ReturnsValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        var controller = CreateParkingController(new Mock<IParkingRepository>(), user);
        var parking = new ParkingObject { name = "Centar", totalSpots = 2, latitude = 1, longitude = 1 };

        // Act
        var result = await controller.Create(parking, [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Create_InvalidTotalSpots_ReturnsValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        var controller = CreateParkingController(new Mock<IParkingRepository>(), user);
        var parking = new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 0, latitude = 1, longitude = 1 };

        // Act
        var result = await controller.Create(parking, [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Create_InvalidCoordinates_ReturnsValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        var controller = CreateParkingController(new Mock<IParkingRepository>(), user);
        var parking = new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2 };

        // Act
        var result = await controller.Create(parking, [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Create_InvalidName_ReturnsValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        var controller = CreateParkingController(new Mock<IParkingRepository>(), user);
        var parking = new ParkingObject { address = "Glavna 1", totalSpots = 2, latitude = 1, longitude = 1 };

        // Act
        var result = await controller.Create(parking, [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Create_ValidParking_AddsParkingAndRequest()
    {
        // Arrange
        var user = new ApplicationUser { Id = "owner", FirstName = "Test", LastName = "Owner" };
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.AddParking(It.IsAny<ParkingObject>())).ReturnsAsync((ParkingObject p) => { p.ID = 8; return p; });
        var requests = new Mock<IRequestRepository>();
        requests.Setup(r => r.AddParkingRequestAsync(8)).ReturnsAsync(new ParkingRequest());
        var controller = CreateParkingController(repository, user, requests);
        var parking = new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, latitude = 1, longitude = 1 };

        // Act
        var result = await controller.Create(parking, [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
        repository.Verify(r => r.AddParking(It.IsAny<ParkingObject>()), Times.Once);
        requests.Verify(r => r.AddParkingRequestAsync(8), Times.Once);
    }

    [TestMethod]
    public async Task Edit_MissingParking_ReturnsError()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((ParkingObject?)null);
        var controller = CreateParkingController(repository);

        // Act
        var result = await controller.Edit(9, "Centar", "Glavna 1", "1", "1", 2, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Edit_ValidParking_ModifiesParking()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new ParkingObject { ID = 9, name = "Stari", address = "Stara 1", totalSpots = 2, availableSpots = 1 });
        repository.Setup(r => r.ModifyParkingAsync(It.IsAny<ParkingObject>())).ReturnsAsync((ParkingObject p) => p);
        var controller = CreateParkingController(repository);

        // Act
        var result = await controller.Edit(9, "Novi", "Nova 1", "1", "1", 3, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
        repository.Verify(r => r.ModifyParkingAsync(It.IsAny<ParkingObject>()), Times.Once);
    }

    [TestMethod]
    public async Task Edit_InvalidLatitude_ReturnsError()
    {
        // Arrange
        var repository = ExistingParkingRepository();
        var controller = CreateParkingController(repository);

        // Act
        var result = await controller.Edit(9, "Novi", "Nova 1", "bad", "1", 3, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Edit_InvalidLongitude_ReturnsError()
    {
        // Arrange
        var controller = CreateParkingController(ExistingParkingRepository());

        // Act
        var result = await controller.Edit(9, "Novi", "Nova 1", "1", "bad", 3, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Edit_EmptyName_ReturnsError()
    {
        // Arrange
        var controller = CreateParkingController(ExistingParkingRepository());

        // Act
        var result = await controller.Edit(9, "", "Nova 1", "1", "1", 3, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task Edit_InvalidTotalSpots_ReturnsError()
    {
        // Arrange
        var controller = CreateParkingController(ExistingParkingRepository());

        // Act
        var result = await controller.Edit(9, "Novi", "Nova 1", "1", "1", 0, null, false, false, false, false, null, null, null, [], [], [], []);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    private static Mock<IParkingRepository> ExistingParkingRepository()
    {
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new ParkingObject { ID = 9, name = "Stari", address = "Stara 1", totalSpots = 2, availableSpots = 1 });
        return repository;
    }

    [TestMethod]
    public async Task UploadCoverImage_EmptyFile_ReturnsError()
    {
        // Arrange
        var controller = CreateParkingController(new Mock<IParkingRepository>());

        // Act
        var result = await controller.UploadCoverImage(1, null!);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
    }

    [TestMethod]
    public async Task UploadCoverImage_ValidFile_SavesImage()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.SaveParkingImageByIDAsync(It.IsAny<IFormFile>(), 1, 1)).ReturnsAsync("/images/new.jpg");
        var controller = CreateParkingController(repository);
        var file = new FormFile(new MemoryStream("image"u8.ToArray()), 0, 5, "Images", "cover.jpg");

        // Act
        var result = await controller.UploadCoverImage(1, file);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
        repository.Verify(r => r.SaveParkingImageByIDAsync(It.IsAny<IFormFile>(), 1, 1), Times.Once);
    }

    private static ParkingController CreateParkingController(Mock<IParkingRepository> repository, ApplicationUser? user = null, Mock<IRequestRepository>? requests = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var controller = new ParkingController(repository.Object, userManager.Object, new ApplicationDbContext(options), (requests ?? new Mock<IRequestRepository>()).Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };
        return controller;
    }

    private static HomeController CreateHomeController(Mock<IParkingRepository> repository, ApplicationUser? user = null, bool admin = false)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin")).ReturnsAsync(admin);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var controller = new HomeController(Mock.Of<Microsoft.Extensions.Logging.ILogger<HomeController>>(), new ApplicationDbContext(options), Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(), userManager.Object, repository.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        return controller;
    }
}
