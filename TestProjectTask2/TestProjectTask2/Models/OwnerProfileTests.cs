using ParkirajBa.Models;

namespace ParkirajBa.Tests.Models
{
    [TestClass]
    [TestCategory("Models")]
    [TestProperty("Autor", "Tim3")]
    public sealed class OwnerProfileTests
    {
        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void Properties_PostavljanjeVrijednosti_VracajuPostavljeneVrijednosti()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-2" };
            var profile = new OwnerProfile();

            // Act
            profile.Id = 9;
            profile.CompanyName = "Parking d.o.o.";
            profile.UserId = user.Id;
            profile.User = user;

            // Assert
            Assert.AreEqual(9, profile.Id);
            Assert.AreEqual("Parking d.o.o.", profile.CompanyName);
            Assert.AreEqual("user-2", profile.UserId);
            Assert.AreSame(user, profile.User);
        }
    }
}
