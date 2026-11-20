using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;
using ParkirajBa.Services;

namespace TestProjectTask2;

[TestClass]
public sealed class ParkingObjectTests
{
    public static IEnumerable<object[]> ValidSpotCounts() =>
    [
        [1], [10000]
    ];

    [TestMethod]
    [DynamicData(nameof(ValidSpotCounts), DynamicDataSourceType.Method)]
    public void ParkingObject_TotalSpotsValid_ValidationPasses(int totalSpots)
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", totalSpots = totalSpots };
        var context = new ValidationContext(parking);
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(parking, context, errors, true);

        // Assert
        Assert.IsTrue(valid);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(10001)]
    public void ParkingObject_TotalSpotsOutOfRange_ValidationFails(int totalSpots)
    {
        // Arrange
        var parking = new ParkingObject { name = "Centar", totalSpots = totalSpots };
        var context = new ValidationContext(parking);
        var errors = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(parking, context, errors, true);

        // Assert
        Assert.IsFalse(valid);
    }

    [TestMethod]
    public void ParkingImage_PropertiesAssigned_ReturnsAssignedValues()
    {
        // Arrange
        var image = new ParkingImage { ImagePath = "/images/cover.jpg", Position = 1, ParkingObjectID = 7 };

        // Act
        var path = image.ImagePath;

        // Assert
        Assert.AreEqual("/images/cover.jpg", path);
        Assert.AreEqual(1, image.Position);
        Assert.AreEqual(7, image.ParkingObjectID);
    }
}

[TestClass]
public sealed class ImageServiceTests
{
    private string _root = string.Empty;

    [TestInitialize]
    public void Initialize() => _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [TestMethod]
    public async Task SaveImage_NullFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SaveImageToServerAsync(null!, "images", "cover");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SaveImage_ValidFile_CreatesFileAndReturnsPath()
    {
        // Arrange
        var service = CreateService();
        var file = new FormFile(new MemoryStream("image"u8.ToArray()), 0, 5, "Image", "Cover Image.JPG");

        // Act
        var result = await service.SaveImageToServerAsync(file, "images", "Parking 1");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(File.Exists(Path.Combine(_root, result!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
    }

    private ImageService CreateService()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(_root);
        return new ImageService(environment.Object);
    }
}

[TestClass]
public sealed class ParkingRepositoryTests
{
    private ApplicationDbContext _database = null!;
    private ParkingRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _database = new ApplicationDbContext(options);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(Path.GetTempPath());
        _repository = new ParkingRepository(_database, new ImageService(environment.Object));
    }

    [TestCleanup]
    public void Cleanup() => _database.Dispose();

    [TestMethod]
    [DynamicData(nameof(FilterCasesFromCsv), DynamicDataSourceType.Method)]
    public async Task FilterParkings_CsvCase_ReturnsExpectedCount(string search, bool garage, bool ev, bool cameras, bool accessible, string regime, int maxPrice, int expected)
    {
        // Arrange
        await SeedParkingAsync();

        // Act
        var result = await _repository.FilterParkings(search, garage, ev, cameras, accessible, regime, maxPrice);

        // Assert
        Assert.AreEqual(expected, result.Count);
    }

    [TestMethod]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(FilterCasesFromJson), DynamicDataSourceType.Method)]
    public async Task FilterApprovedParkings_JsonCase_ReturnsExpectedCount(string search, bool garage, bool ev, bool cameras, bool accessible, string regime, int maxPrice, int expected)
    {
        // Arrange
        await SeedParkingAsync();

        // Act
        var result = await _repository.FilterApprovedParkings(search, garage, ev, cameras, accessible, regime, maxPrice);

        // Assert
        Assert.AreEqual(expected, result.Count);
    }

    [TestMethod]
    public async Task ApproveParking_ExistingParking_SetsApproved()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 10, isApproved = false });

        // Act
        var result = await _repository.ApproveParkingAsync(parking.ID);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue((await _repository.GetByIdAsync(parking.ID))!.isApproved);
    }

    [TestMethod]
    public async Task ApproveParking_MissingParking_ReturnsFalse()
    {
        // Arrange
        const int missingId = 999;

        // Act
        var result = await _repository.ApproveParkingAsync(missingId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task IncrementAvailableSpots_BelowCapacity_IncrementsAndReturnsTrue()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, availableSpots = 1 });

        // Act
        var result = await _repository.IncrementAvailableSpotsAsync(parking.ID);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(2, (await _repository.GetByIdAsync(parking.ID))!.availableSpots);
    }

    [TestMethod]
    public async Task IncrementAvailableSpots_AtCapacity_ReturnsFalse()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 2, availableSpots = 2 });

        // Act
        var result = await _repository.IncrementAvailableSpotsAsync(parking.ID);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetActivePricing_WithinValidity_ReturnsPricing()
    {
        // Arrange
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 10 });
        var pricing = await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, pricingType = PricingType.Hourly, price = 3, validFrom = DateTime.Now.AddHours(-1), validTo = DateTime.Now.AddHours(1) });

        // Act
        var result = await _repository.GetActivePricingAsync(parking.ID, PricingType.Hourly);

        // Assert
        Assert.AreEqual(pricing!.ID, result!.ID);
    }

    [TestMethod]
    public async Task SaveAllParkingImages_MismatchedLists_ThrowsException()
    {
        // Arrange
        var images = new List<IFormFile>();
        var positions = new List<int> { 1 };

        // Act and Assert
        await Assert.ThrowsExactlyAsync<Exception>(() => _repository.SaveAllParkingImagesByIDAsync(images, positions, 1));
    }

    public static IEnumerable<object[]> FilterCasesFromCsv() => ReadLines("parking-filter-cases.csv").Select(line =>
    {
        var parts = line.Split(';');
        return new object[] { parts[0], bool.Parse(parts[1]), bool.Parse(parts[2]), bool.Parse(parts[3]), bool.Parse(parts[4]), parts[5], int.Parse(parts[6]), int.Parse(parts[7]) };
    });

    public static IEnumerable<object[]> FilterCasesFromJson() =>
        JsonSerializer.Deserialize<List<FilterCase>>(File.ReadAllText(DataPath("parking-filter-cases.json")))!.Select(c =>
            new object[] { c.SearchText, c.HasGarage, c.HasEVCharger, c.HasCameras, c.IsDisabledAccessible, c.Regime, c.MaxPrice, c.ExpectedCount });

    private async Task SeedParkingAsync()
    {
        var parking = await _repository.AddParking(new ParkingObject { name = "Centar", address = "Glavna 1", totalSpots = 10, isApproved = true, isUnderground = true });
        await _repository.AddPricingAsync(new Pricing { ParkingObjectID = parking.ID, pricingType = PricingType.Hourly, price = 5 });
    }

    private static IEnumerable<string> ReadLines(string file) => File.ReadLines(DataPath(file)).Skip(1);
    private static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);
    private sealed record FilterCase(string SearchText, bool HasGarage, bool HasEVCharger, bool HasCameras, bool IsDisabledAccessible, string Regime, int MaxPrice, int ExpectedCount);
}

[TestClass]
public sealed class HomeControllerTests
{
    [TestMethod]
    public async Task Search_RepositoryReturnsResults_ReturnsJsonResult()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.FilterApprovedParkings("Centar", false, false, false, false, "Hour", 10)).ReturnsAsync([]);
        var controller = CreateController(repository);

        // Act
        var result = await controller.Search("Centar", false, false, false, false, "Hour", 10);

        // Assert
        Assert.IsInstanceOfType<JsonResult>(result);
        repository.Verify(r => r.FilterApprovedParkings("Centar", false, false, false, false, "Hour", 10), Times.Once);
    }

    [TestMethod]
    [DataRow("Day", PricingType.Daily)]
    [DataRow("Unknown", PricingType.Hourly)]
    public async Task GetMaxPriceForRegime_RegimeMapsToPricingType_ReturnsRepositoryValue(string regime, PricingType expectedType)
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetMaxPricingForRegimeAsync(expectedType)).ReturnsAsync(12.5m);
        var controller = CreateController(repository);

        // Act
        var result = await controller.GetMaxPriceForRegime(regime);

        // Assert
        Assert.AreEqual(12.5m, result);
        repository.Verify(r => r.GetMaxPricingForRegimeAsync(expectedType), Times.Once);
    }

    [TestMethod]
    public async Task GetParkingPrice_MissingPricing_ReturnsZero()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetActivePricingAsync(1, PricingType.Hourly)).ReturnsAsync((Pricing?)null);
        var controller = CreateController(repository);

        // Act
        var result = await controller.GetParkingPrice(1, "Unknown");

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Privacy_NoDependencies_ReturnsViewResult()
    {
        // Arrange
        var controller = CreateController(new Mock<IParkingRepository>());

        // Act
        var result = controller.Privacy();

        // Assert
        Assert.IsInstanceOfType<ViewResult>(result);
    }

    private static HomeController CreateController(Mock<IParkingRepository> repository)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var database = new ApplicationDbContext(options);
        return new HomeController(Mock.Of<ILogger<HomeController>>(), database, Mock.Of<IConfiguration>(), userManager.Object, repository.Object);
    }
}

[TestClass]
public sealed class ParkingControllerTests
{
    [TestMethod]
    public async Task Details_MissingParking_RedirectsToObjects()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((ParkingObject?)null);
        var controller = CreateController(repository);

        // Act
        var result = await controller.Details(7);

        // Assert
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Objekti", redirect.ActionName);
        repository.Verify(r => r.GetPricingsByParkingIdAsync(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void GetCreateForm_Request_ReturnsCreatePartial()
    {
        // Arrange
        var controller = CreateController(new Mock<IParkingRepository>());

        // Act
        var result = controller.GetCreateForm();

        // Assert
        var partial = (PartialViewResult)result;
        Assert.AreEqual("_ParkingCreate", partial.ViewName);
    }

    [TestMethod]
    public async Task GetEditForm_MissingParking_ReturnsNotFound()
    {
        // Arrange
        var repository = new Mock<IParkingRepository>();
        repository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync((ParkingObject?)null);
        var controller = CreateController(repository);

        // Act
        var result = await controller.GetEditForm(4);

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    private static ParkingController CreateController(Mock<IParkingRepository> repository)
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new ApplicationDbContext(options);
        var requests = new Mock<IRequestRepository>();
        var controller = new ParkingController(repository.Object, userManager.Object, context, requests.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };
        return controller;
    }
}
