using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;
using ParkirajBa.Models;
using ParkirajBa.Repositories;

namespace TestProjectTask2;

[TestClass]
public class RequestRepositoryTests
{
    private ApplicationDbContext _database = null!;
    private IRequestRepository _repository = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _database = new ApplicationDbContext(options);
        _repository = new RequestRepository(_database);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _database.Dispose();
    }

    [TestMethod]
    public async Task AddParkingRequest_ObjekatZahtjeva_SpremaZahtjev()
    {
        // Arrange
        var request = new ParkingRequest { ParkingID = 7, TimeSent = DateTime.UtcNow };
        // Act
        await _repository.AddParkingRequestAsync(request);
        // Assert
        Assert.AreSame(request, await _repository.GetByIdAsync(request.ID));
    }

    [TestMethod]
    public async Task AddParkingRequest_IdParkinga_KreiraZahtjevSaVremenom()
    {
        // Arrange – nije potrebno
        // Act
        var request = await _repository.AddParkingRequestAsync(8);
        // Assert
        Assert.AreEqual(8, request.ParkingID);
        Assert.AreNotEqual(default, request.TimeSent);
    }

    [TestMethod]
    public async Task GetAll_PostojeciZahtjevi_VracaSveZahtjeve()
    {
        // Arrange
        await _repository.AddParkingRequestAsync(new ParkingRequest { ParkingID = 1 });
        await _repository.AddParkingRequestAsync(new ParkingRequest { ParkingID = 2 });
        // Act
        var requests = await _repository.GetAllAsync();
        // Assert
        Assert.AreEqual(2, requests.Count);
    }

    [TestMethod]
    public async Task GetById_NepostojeciId_VracaNull()
    {
        // Arrange – nije potrebno
        // Act
        var request = await _repository.GetByIdAsync(999);
        // Assert
        Assert.IsNull(request);
    }

    [TestMethod]
    public async Task GetByParkingId_PostojiParking_VracaZahtjev()
    {
        // Arrange
        await _repository.AddParkingRequestAsync(new ParkingRequest { ParkingID = 12 });
        // Act
        var request = await _repository.GetByParkingIdAsync(12);
        // Assert
        Assert.IsNotNull(request);
    }

    [TestMethod]
    public async Task Delete_PostojiZahtjev_UklanjaZahtjev()
    {
        // Arrange
        var request = new ParkingRequest { ParkingID = 3 };
        await _repository.AddParkingRequestAsync(request);
        // Act
        await _repository.DeleteAsync(request.ID);
        // Assert
        Assert.IsNull(await _repository.GetByIdAsync(request.ID));
    }

    [TestMethod]
    public async Task Delete_NepostojeciId_NeMijenjaBazu()
    {
        // Arrange – nije potrebno
        // Act
        await _repository.DeleteAsync(404);
        // Assert
        Assert.AreEqual(0, (await _repository.GetAllAsync()).Count);
    }
}

[TestClass]
public class RequestModelTests
{
    [TestMethod]
    public void ParkingRequest_PocetneVrijednosti_SuIspravne()
    {
        // Arrange – nije potrebno
        // Act
        var request = new ParkingRequest();
        // Assert
        Assert.AreEqual(0, request.ID);
        Assert.AreEqual(0, request.ParkingID);
    }

    [TestMethod]
    public void QrRequest_DodijeljenKod_VracaIstiKod()
    {
        // Arrange
        var request = new QrRequest { Code = "ABC123" };
        // Act
        var code = request.Code;
        // Assert
        Assert.AreEqual("ABC123", code);
    }
}
