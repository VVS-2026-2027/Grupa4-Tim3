using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParkirajBa.Controllers;

namespace ParkirajBa.Tests.Controllers
{
    [TestClass]
    public class NotificationControllerTests
    {
        [TestMethod]
        [TestCategory("Notification")]
        public void Index_PostojeciKontroler_VracaView()
        {
            // Arrange – nije potrebno
            var controller = new NotificationController();
            // Act
            var rezultat = controller.Index();
            // Assert
            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }
    }
}
