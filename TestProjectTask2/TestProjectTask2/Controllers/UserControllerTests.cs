using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ParkirajBa.Controllers;
using ParkirajBa.Models;

namespace ParkirajBa.Tests.Controllers
{
    [TestClass]
    [TestCategory("Controllers")]
    [TestProperty("Autor", "Damir")]
    public class UserControllerTests
    {
        private static string _klasaInicijalizovana = string.Empty;

        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<SignInManager<ApplicationUser>> _signInManagerMock = null!;
        private Mock<IEmailSender> _emailSenderMock = null!;
        private UserController _controller = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _klasaInicijalizovana = "UserControllerTests";
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _klasaInicijalizovana = string.Empty;
        }

        [TestInitialize]
        public void TestInit()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
            var loggerMock = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemesMock = new Mock<IAuthenticationSchemeProvider>();
            var confirmationMock = new Mock<IUserConfirmation<ApplicationUser>>();

            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                optionsMock.Object,
                loggerMock.Object,
                schemesMock.Object,
                confirmationMock.Object);

            _emailSenderMock = new Mock<IEmailSender>();

            _controller = new UserController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _emailSenderMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("https://localhost/User/ConfirmEmail?userId=1&token=token123");
            _controller.Url = urlHelperMock.Object;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _controller.Dispose();
        }

        // ── DATA-DRIVEN IZVORI PODATAKA (CSV I JSON) ──────────────────────────

        public static IEnumerable<object[]> LoginPraznaPoljaIzCsv() =>
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "login_prazna_polja.csv"))
                .Skip(1)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(';'))
                .Select(k => new object[] { k[0], k[1], k[2].Trim() });

        public record ProfilLozinkaSlucaj(string trenutna, string nova, string potvrda, string ocekivanaGreska);

        public static IEnumerable<object[]> ProfilLozinkeIzJson() =>
            JsonSerializer.Deserialize<List<ProfilLozinkaSlucaj>>(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "profil_neispravne_lozinke.json")))!
                .Select(s => new object[] { s.trenutna, s.nova, s.potvrda, s.ocekivanaGreska });

        // ── POMOĆNE METODE ZA PRIPREMU OBJEKATA ───────────────────────────────

        private ApplicationUser NapraviKorisnika(string id = "1", string email = "damir@test.ba") =>
            new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                FirstName = "Damir",
                LastName = "Muharem"
            };

        private ExtendedRegisterViewModel NapraviRegisterModel(string email = "novi@test.ba") =>
            new ExtendedRegisterViewModel
            {
                Email = email,
                Ime = "Damir",
                Prezime = "Muharem",
                Password = "Lozinka123"
            };

        private void PripremiValidnogKorisnikaZaLogin(ApplicationUser korisnik, bool jeAdmin = false)
        {
            _userManagerMock.Setup(u => u.FindByEmailAsync(korisnik.Email!)).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(korisnik)).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(korisnik)).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.IsInRoleAsync(korisnik, "Admin")).ReturnsAsync(jeAdmin);
            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(korisnik.UserName!, "Lozinka123", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        }

        // ── 1. LOGOUT I LOGIN GET TESTOVI ─────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task Logout_PozivAkcije_OdjavljujeKorisnikaIPreusmjeravaNaPocetnu()
        {
            // Arrange – nije potrebno

            // Act
            var rezultat = await _controller.Logout() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual("Home", rezultat.ControllerName);
            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
        }

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public void LoginGet_PozivAkcije_VracaViewISakrivaHeader()
        {
            // Arrange – nije potrebno

            // Act
            var rezultat = _controller.Login() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsTrue((bool)_controller.ViewBag.HideHeader);
        }

        // ── 2. LOGIN POST TESTOVI (UKLJUČUJUĆI CSV DATA-DRIVEN) ───────────────

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(LoginPraznaPoljaIzCsv), DynamicDataSourceType.Method)]
        public async Task LoginPost_PraznaPoljaIzCsv_VracaViewSaPorukomGreske(string email, string password, string ocekivanaGreska)
        {
            // Arrange – nije potrebno

            // Act
            var rezultat = await _controller.Login(email, password) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_NepostojeciKorisnik_VracaViewSaGreskom()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByEmailAsync("nema@test.ba")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.Login("nema@test.ba", "Lozinka123") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Korisnik ne postoji.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_NepotvrdjenEmail_OmogucavaPonovnoSlanjePotvrde()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByEmailAsync(korisnik.Email!)).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(korisnik)).ReturnsAsync(false);

            // Act
            var rezultat = await _controller.Login(korisnik.Email!, "Lozinka123") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsTrue((bool)_controller.ViewBag.ShowResend);
            Assert.AreEqual(korisnik.Id, _controller.ViewBag.UserId);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_ZakljucanKorisnik_VracaViewSaPorukomOZakljucavanju()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByEmailAsync(korisnik.Email!)).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(korisnik)).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(korisnik)).ReturnsAsync(true);

            // Act
            var rezultat = await _controller.Login(korisnik.Email!, "Lozinka123") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Vaš profil je zaključan. Kontaktirajte administratora.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_ValidanObicniKorisnik_PreusmjeravaNaHomeIndex()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            PripremiValidnogKorisnikaZaLogin(korisnik, jeAdmin: false);

            // Act
            var rezultat = await _controller.Login(korisnik.Email!, "Lozinka123") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Index", rezultat.ActionName);
            Assert.AreEqual("Home", rezultat.ControllerName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_ValidanAdminKorisnik_PreusmjeravaNaAdminDashboard()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            PripremiValidnogKorisnikaZaLogin(korisnik, jeAdmin: true);

            // Act
            var rezultat = await _controller.Login(korisnik.Email!, "Lozinka123") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Dashboard", rezultat.ActionName);
            Assert.AreEqual("Admin", rezultat.ControllerName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task LoginPost_PogresnaLozinka_VracaViewSaGreskom()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByEmailAsync(korisnik.Email!)).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.IsEmailConfirmedAsync(korisnik)).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(korisnik)).ReturnsAsync(false);
            _signInManagerMock
                .Setup(s => s.PasswordSignInAsync(korisnik.UserName!, "KrivaLozinka", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var rezultat = await _controller.Login(korisnik.Email!, "KrivaLozinka") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Pogrešan password.", _controller.ViewBag.Error);
        }

        // ── 3. RESENDCONFIRMATION TESTOVI ─────────────────────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public async Task ResendConfirmation_NepostojeciKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.ResendConfirmation("999") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ActionName);
        }

        [TestMethod]
        [TestCategory("Mock")]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ResendConfirmation_PostojeciKorisnik_SaljeEmailIVracaViewPotvrde()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(korisnik)).ReturnsAsync("token123");

            // Act
            var rezultat = await _controller.ResendConfirmation("1") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("EmailConfirmationSent", rezultat.ViewName);
            _emailSenderMock.Verify(e => e.SendEmailAsync(korisnik.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ── 4. REGISTER GET I POST TESTOVI ────────────────────────────────────

        [TestMethod]
        [Priority(2)]
        [Timeout(2000)]
        public void RegisterGet_PozivAkcije_VracaViewISakrivaHeader()
        {
            // Arrange – nije potrebno

            // Act
            var rezultat = _controller.Register() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.IsTrue((bool)_controller.ViewBag.HideHeader);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RegisterPost_NeispravanModelState_VracaViewSaPrvomGreskom()
        {
            // Arrange
            var model = NapraviRegisterModel();
            _controller.ModelState.AddModelError("Password", "Lozinka je preslaba.");

            // Act
            var rezultat = await _controller.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Lozinka je preslaba.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RegisterPost_EmailVecPostoji_VracaViewSaGreskom()
        {
            // Arrange
            var model = NapraviRegisterModel();
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(NapraviKorisnika());

            // Act
            var rezultat = await _controller.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Korisnik već postoji.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RegisterPost_NeuspjesnoKreiranjeKorisnika_VracaViewSaOpisomGreske()
        {
            // Arrange
            var model = NapraviRegisterModel();
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock
                .Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Greška baze." }));

            // Act
            var rezultat = await _controller.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Greška baze.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [TestCategory("Mock")]
        [Priority(1)]
        [Timeout(2000)]
        public async Task RegisterPost_ValidniPodaci_KreiraKorisnikaDodjeljujeUloguISaljeEmail()
        {
            // Arrange
            var model = NapraviRegisterModel();
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), model.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("tok");

            // Act
            var rezultat = await _controller.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("EmailConfirmationSent", rezultat.ViewName);
            _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
            _emailSenderMock.Verify(e => e.SendEmailAsync(model.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ── 5. CONFIRMEMAIL TESTOVI ───────────────────────────────────────────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow("", "token")]
        [DataRow("1", "")]
        [DataRow(null, "token")]
        public async Task ConfirmEmail_NedostajeParametar_PreusmjeravaNaLogin(string? userId, string? token)
        {
            // Arrange – nije potrebno

            // Act
            var rezultat = await _controller.ConfirmEmail(userId!, token!) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ActionName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ConfirmEmail_NepostojeciKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            _userManagerMock.Setup(u => u.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.ConfirmEmail("999", "tok") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ActionName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ConfirmEmail_ValidanToken_PrijavljujeKorisnikaIVracaEmailConfirmed()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.ConfirmEmailAsync(korisnik, "tok")).ReturnsAsync(IdentityResult.Success);

            // Act
            var rezultat = await _controller.ConfirmEmail("1", "tok") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("EmailConfirmed", rezultat.ViewName);
            _signInManagerMock.Verify(s => s.SignInAsync(korisnik, false, null), Times.Once);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ConfirmEmail_NevalidanToken_VracaLoginViewSaGreskom()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.ConfirmEmailAsync(korisnik, "los")).ReturnsAsync(IdentityResult.Failed());

            // Act
            var rezultat = await _controller.ConfirmEmail("1", "los") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ViewName);
            Assert.AreEqual("Link za potvrdu nije validan ili je istekao.", _controller.ViewBag.Error);
        }

        // ── 6. PROFILE GET I POST TESTOVI (UKLJUČUJUĆI JSON DATA-DRIVEN) ──────

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfileGet_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.Profile() as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ActionName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfileGet_PrijavljenKorisnik_VracaViewSaKorisnikom()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);

            // Act
            var rezultat = await _controller.Profile() as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreSame(korisnik, rezultat.Model);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfilePost_NeprijavljenKorisnik_PreusmjeravaNaLogin()
        {
            // Arrange
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var rezultat = await _controller.Profile("Damir", "Muharem", "", "", "") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Login", rezultat.ActionName);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow("", "Muharem", "Ime je obavezno.")]
        [DataRow("Damir", "", "Prezime je obavezno.")]
        [DataRow("Damir123", "Muharem", "Ime može sadržavati samo slova, razmak i crticu.")]
        [DataRow("Damir", "Muharem!", "Prezime može sadržavati samo slova, razmak i crticu.")]
        public async Task ProfilePost_NeispravnoImeIliPrezime_VracaViewSaPorukomGreske(string ime, string prezime, string ocekivanaGreska)
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);

            // Act
            var rezultat = await _controller.Profile(ime, prezime, "", "", "") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfilePost_NeuspjesnoAzuriranjeBaze_VracaViewSaGreskom()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Failed());

            // Act
            var rezultat = await _controller.Profile("Damir", "Muharem", "", "", "") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Greška pri ažuriranju profila.", _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfilePost_ValidnoImeBezPromjeneLozinke_AzuriraProfilIPreusmjerava()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);

            // Act
            var rezultat = await _controller.Profile("Novi", "Naziv", "", "", "") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Profile", rezultat.ActionName);
            Assert.AreEqual("Profil uspješno ažuriran.", _controller.TempData["Success"]);
        }

        [TestMethod]
        [TestCategory("DataDriven")]
        [Priority(1)]
        [Timeout(2000)]
        [DynamicData(nameof(ProfilLozinkeIzJson), DynamicDataSourceType.Method)]
        public async Task ProfilePost_NeispravnaPoljaLozinkeIzJson_VracaViewSaGreskom(
            string trenutna, string nova, string potvrda, string ocekivanaGreska)
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);

            // Act
            var rezultat = await _controller.Profile("Damir", "Muharem", trenutna, nova, potvrda) as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        [DataRow("PasswordMismatch", "Trenutna lozinka nije ispravna.")]
        [DataRow("PasswordRequiresDigit", "Lozinka ne ispunjava sigurnosne uslove (mora imati velika, mala slova i brojeve).")]
        public async Task ProfilePost_ChangePasswordNeuspjesan_VracaOdgovarajucuGresku(string kodGreske, string ocekivanaGreska)
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(u => u.ChangePasswordAsync(korisnik, "Stara123", "NovaLozinka1"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = kodGreske }));

            // Act
            var rezultat = await _controller.Profile("Damir", "Muharem", "Stara123", "NovaLozinka1", "NovaLozinka1") as ViewResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual(ocekivanaGreska, _controller.ViewBag.Error);
        }

        [TestMethod]
        [Priority(1)]
        [Timeout(2000)]
        public async Task ProfilePost_ValidnaPromjenaLozinke_MijenjaLozinkuIPreusmjerava()
        {
            // Arrange
            var korisnik = NapraviKorisnika();
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
            _userManagerMock.Setup(u => u.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.ChangePasswordAsync(korisnik, "Stara123", "NovaLozinka1")).ReturnsAsync(IdentityResult.Success);

            // Act
            var rezultat = await _controller.Profile("Damir", "Muharem", "Stara123", "NovaLozinka1", "NovaLozinka1") as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Profile", rezultat.ActionName);
            _userManagerMock.Verify(u => u.ChangePasswordAsync(korisnik, "Stara123", "NovaLozinka1"), Times.Once);
        }

        [TestMethod]
        [Ignore("Primjer Ignore stereotipa - sve aktivne grane u UserController su pokrivene.")]
        public void UserController_IgnorisaniTest_DemonstriraStereotip()
        {
            // Arrange – nije potrebno
            // Act
            // Assert
            Assert.IsTrue(true);
        }
    }
}