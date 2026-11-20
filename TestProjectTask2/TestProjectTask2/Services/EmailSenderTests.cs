using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ParkirajBa.Services;

namespace ParkirajBa.Tests.Services;

[TestClass]
public class EmailSenderTests
{
    [TestMethod]
    [TestCategory("Services")]
    public async Task SendEmailAsync_NeispravnaAdresa_NeBacaIzuzetak()
    {
        // Arrange
        var sender = CreateSender();
        // Act
        var slanje = sender.SendEmailAsync("neispravna-adresa", "Naslov", "Poruka");
        // Assert
        Assert.IsNotNull(slanje);
        await slanje;
    }

    private static EmailSender CreateSender()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Address"] = "sender@parkiraj.ba",
            ["Email:Password"] = "password"
        }).Build();
        return new EmailSender(configuration, NullLogger<EmailSender>.Instance);
    }
}
