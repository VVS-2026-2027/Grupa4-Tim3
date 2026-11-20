using ParkirajBa.Models;

namespace ParkirajBa.Tests.Models;

[TestClass]
public class ErrorViewModelTests
{
    [TestMethod]
    [TestCategory("Models")]
    public void ShowRequestId_EmptyRequestId_ReturnsFalse()
    {
        // Arrange
        var model = new ErrorViewModel();
        // Act
        var result = model.ShowRequestId;
        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [TestCategory("Models")]
    public void ShowRequestId_RequestIdExists_ReturnsTrue()
    {
        // Arrange
        var model = new ErrorViewModel { RequestId = "request-1" };
        // Act
        var result = model.ShowRequestId;
        // Assert
        Assert.IsTrue(result);
    }
}
