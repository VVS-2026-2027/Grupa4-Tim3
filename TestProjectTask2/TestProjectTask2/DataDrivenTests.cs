using System.Text.Json;

namespace TestProjectTask2;

[TestClass]
public class DataDrivenTests
{
    public record SatiSlucaj(int sati, bool uspjesno);

    public static IEnumerable<object[]> SatiIzJson() =>
        JsonSerializer.Deserialize<List<SatiSlucaj>>(
            File.ReadAllText(Path.Combine("TestData", "sati.json")))!
            .Select(s => new object[] { s.sati, s.uspjesno });

    [TestMethod]
    [TestCategory("DataDriven")]
    [DynamicData(nameof(SatiIzJson), DynamicDataSourceType.Method)]
    public void Validate_BrojSatiIzJson_RazlikujeDozvoljeneVrijednosti(int sati, bool uspjesno)
    {
        // Arrange
        var rezultat = sati >= 1 && sati <= 24;
        // Act – rezultat je izračunat za ulaz
        // Assert
        Assert.AreEqual(uspjesno, rezultat);
    }
}
