using Microsoft.EntityFrameworkCore;
using ParkirajBa.Data;

namespace TestProjectTask2.Pomocne;

/// <summary>
/// Fake baza: EF Core InMemory umjesto SQL Servera.
/// Svaki poziv pravi novu, praznu bazu (novo ime = Guid), pa su testovi neovisni.
/// </summary>
public static class TestBaza
{
    public static ApplicationDbContext Nova()
    {
        var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(opcije);
    }
}
