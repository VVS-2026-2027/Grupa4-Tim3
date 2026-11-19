using ParkirajBa.Models;

namespace ParkirajBa.Tests.Models
{
    [TestClass]
    [TestCategory("Models")]
    [TestProperty("Autor", "Tim3")]
    public sealed class AdminProfileTests
    {
        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public void Properties_PostavljanjeVrijednosti_VracajuPostavljeneVrijednosti()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-1" };
            var profile = new AdminProfile();

            // Act
            profile.Id = 7;
            profile.Department = "Support";
            profile.UserId = user.Id;
            profile.User = user;

            // Assert
            Assert.AreEqual(7, profile.Id);
            Assert.AreEqual("Support", profile.Department);
            Assert.AreEqual("user-1", profile.UserId);
            Assert.AreSame(user, profile.User);
        }
    }
}
