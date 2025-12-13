using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class LanguagesControllerTests
    {
        private readonly Mock<ILanguageService> _mockLanguageService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly LanguagesController _controller;

        private readonly KinaUnaLanguage _testLanguage;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const int TestLanguageId = 1;

        public LanguagesControllerTests()
        {
            // Setup test data
            _testLanguage = new KinaUnaLanguage
            {
                Id = TestLanguageId,
                Name = "English",
                Code = "en-US",
                Icon = "en"
            };

            // Setup mocks
            _mockLanguageService = new Mock<ILanguageService>();
            _mockUserInfoService = new Mock<IUserInfoService>();

            // Initialize controller
            _controller = new LanguagesController(
                _mockLanguageService.Object,
                _mockUserInfoService.Object);

            SetupControllerContext(TestUserId, TestUserEmail);
        }

        private void SetupControllerContext(string userId, string userEmail)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, userEmail),
                new("sub", userId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        #region GetAllLanguages Tests

        [Fact]
        public async Task GetAllLanguages_Should_Return_Ok_With_Languages_List()
        {
            // Arrange
            List<KinaUnaLanguage> languages =
            [
                _testLanguage,
                new() { Id = 2, Name = "Danish", Code = "da-DK", Icon = "dk" },
                new() { Id = 3, Name = "German", Code = "de-DE", Icon = "de" }
            ];

            _mockLanguageService.Setup(x => x.GetAllLanguages())
                .ReturnsAsync(languages);

            // Act
            IActionResult result = await _controller.GetAllLanguages();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaLanguage> returnedLanguages = Assert.IsType<List<KinaUnaLanguage>>(okResult.Value);
            Assert.Equal(3, returnedLanguages.Count);
            Assert.Contains(returnedLanguages, l => l.Name == "English");
            Assert.Contains(returnedLanguages, l => l.Name == "Danish");
            Assert.Contains(returnedLanguages, l => l.Name == "German");

            _mockLanguageService.Verify(x => x.GetAllLanguages(), Times.Once);
        }

        [Fact]
        public async Task GetAllLanguages_Should_Return_Empty_List_When_No_Languages()
        {
            // Arrange
            _mockLanguageService.Setup(x => x.GetAllLanguages())
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetAllLanguages();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaLanguage> returnedLanguages = Assert.IsType<List<KinaUnaLanguage>>(okResult.Value);
            Assert.Empty(returnedLanguages);
        }

        #endregion

        #region GetLanguage Tests

        [Fact]
        public async Task GetLanguage_Should_Return_Ok_When_Language_Exists()
        {
            // Arrange
            _mockLanguageService.Setup(x => x.GetLanguage(TestLanguageId))
                .ReturnsAsync(_testLanguage);

            // Act
            IActionResult result = await _controller.GetLanguage(TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaLanguage returnedLanguage = Assert.IsType<KinaUnaLanguage>(okResult.Value);
            Assert.Equal(TestLanguageId, returnedLanguage.Id);
            Assert.Equal("English", returnedLanguage.Name);
            Assert.Equal("en-US", returnedLanguage.Code);

            _mockLanguageService.Verify(x => x.GetLanguage(TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task GetLanguage_Should_Return_Ok_With_Null_When_Language_Not_Found()
        {
            // Arrange
            _mockLanguageService.Setup(x => x.GetLanguage(999))
                .ReturnsAsync((KinaUnaLanguage)null!);

            // Act
            IActionResult result = await _controller.GetLanguage(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        #endregion

        #region AddLanguage Tests

        [Fact]
        public async Task AddLanguage_Should_Return_Ok_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage newLanguage = new()
            {
                Name = "  Spanish  ", // With whitespace to test trimming
                Code = "es-ES",
                Icon = "es"
            };

            KinaUnaLanguage addedLanguage = new()
            {
                Id = 4,
                Name = "Spanish",
                Code = "es-ES",
                Icon = "es"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.AddLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(addedLanguage);

            // Act
            IActionResult result = await _controller.AddLanguage(newLanguage);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaLanguage returnedLanguage = Assert.IsType<KinaUnaLanguage>(okResult.Value);
            Assert.Equal(4, returnedLanguage.Id);
            Assert.Equal("Spanish", returnedLanguage.Name);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockLanguageService.Verify(x => x.AddLanguage(It.Is<KinaUnaLanguage>(l =>
                l.Name == "Spanish")), Times.Once);
        }

        [Fact]
        public async Task AddLanguage_Should_Trim_Language_Name()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage newLanguage = new()
            {
                Name = "  French  ",
                Code = "fr-FR",
                Icon = "fr"
            };

            KinaUnaLanguage addedLanguage = new()
            {
                Id = 5,
                Name = "French",
                Code = "fr-FR",
                Icon = "fr"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.AddLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(addedLanguage);

            // Act
            await _controller.AddLanguage(newLanguage);

            // Assert
            _mockLanguageService.Verify(x => x.AddLanguage(It.Is<KinaUnaLanguage>(l =>
                l.Name == "French")), Times.Once);
        }

        [Fact]
        public async Task AddLanguage_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            KinaUnaLanguage newLanguage = new()
            {
                Name = "Italian",
                Code = "it-IT",
                Icon = "it"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.AddLanguage(newLanguage);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLanguageService.Verify(x => x.AddLanguage(It.IsAny<KinaUnaLanguage>()), Times.Never);
        }

        [Fact]
        public async Task AddLanguage_Should_Handle_Null_Language_Name()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage newLanguage = new()
            {
                Name = null,
                Code = "pt-PT",
                Icon = "pt"
            };

            KinaUnaLanguage addedLanguage = new()
            {
                Id = 6,
                Name = null,
                Code = "pt-PT",
                Icon = "pt"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.AddLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(addedLanguage);

            // Act
            IActionResult result = await _controller.AddLanguage(newLanguage);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<KinaUnaLanguage>(okResult.Value);
        }

        #endregion

        #region UpdateLanguage Tests

        [Fact]
        public async Task UpdateLanguage_Should_Return_Ok_When_User_Is_Admin_And_Language_Exists()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage updateValues = new()
            {
                Id = TestLanguageId,
                Name = "British English",
                Code = "en-GB",
                Icon = "gb"
            };

            KinaUnaLanguage updatedLanguage = new()
            {
                Id = TestLanguageId,
                Name = "British English",
                Code = "en-GB",
                Icon = "gb"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.GetLanguage(TestLanguageId))
                .ReturnsAsync(_testLanguage);
            _mockLanguageService.Setup(x => x.UpdateLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(updatedLanguage);

            // Act
            IActionResult result = await _controller.UpdateLanguage(TestLanguageId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaLanguage returnedLanguage = Assert.IsType<KinaUnaLanguage>(okResult.Value);
            Assert.Equal("British English", returnedLanguage.Name);
            Assert.Equal("en-GB", returnedLanguage.Code);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockLanguageService.Verify(x => x.GetLanguage(TestLanguageId), Times.Once);
            _mockLanguageService.Verify(x => x.UpdateLanguage(updateValues), Times.Once);
        }

        [Fact]
        public async Task UpdateLanguage_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            KinaUnaLanguage updateValues = new()
            {
                Id = TestLanguageId,
                Name = "Updated Language"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.UpdateLanguage(TestLanguageId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLanguageService.Verify(x => x.GetLanguage(It.IsAny<int>()), Times.Never);
            _mockLanguageService.Verify(x => x.UpdateLanguage(It.IsAny<KinaUnaLanguage>()), Times.Never);
        }

        [Fact]
        public async Task UpdateLanguage_Should_Return_NotFound_When_Language_Does_Not_Exist()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage updateValues = new()
            {
                Id = 999,
                Name = "Non-existent Language"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.GetLanguage(999))
                .ReturnsAsync((KinaUnaLanguage)null!);

            // Act
            IActionResult result = await _controller.UpdateLanguage(999, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockLanguageService.Verify(x => x.UpdateLanguage(It.IsAny<KinaUnaLanguage>()), Times.Never);
        }

        [Fact]
        public async Task UpdateLanguage_Should_Check_Admin_Before_Checking_Language_Existence()
        {
            // Arrange
            KinaUnaLanguage updateValues = new()
            {
                Id = TestLanguageId,
                Name = "Updated Language"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.UpdateLanguage(TestLanguageId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLanguageService.Verify(x => x.GetLanguage(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region DeleteLanguage Tests

        [Fact]
        public async Task DeleteLanguage_Should_Return_Ok_When_User_Is_Admin_And_Language_Exists()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage deletedLanguage = new()
            {
                Id = TestLanguageId,
                Name = "English",
                Code = "en-US",
                Icon = "en"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.DeleteLanguage(TestLanguageId))
                .ReturnsAsync(deletedLanguage);

            // Act
            IActionResult result = await _controller.DeleteLanguage(TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaLanguage returnedLanguage = Assert.IsType<KinaUnaLanguage>(okResult.Value);
            Assert.Equal(TestLanguageId, returnedLanguage.Id);
            Assert.Equal("English", returnedLanguage.Name);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockLanguageService.Verify(x => x.DeleteLanguage(TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task DeleteLanguage_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteLanguage(TestLanguageId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLanguageService.Verify(x => x.DeleteLanguage(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteLanguage_Should_Return_NotFound_When_Language_Does_Not_Exist()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.DeleteLanguage(999))
                .ReturnsAsync((KinaUnaLanguage)null!);

            // Act
            IActionResult result = await _controller.DeleteLanguage(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteLanguage_Should_Check_Admin_Before_Attempting_Delete()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteLanguage(TestLanguageId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockLanguageService.Verify(x => x.DeleteLanguage(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Authorization Edge Cases

        [Fact]
        public async Task AddLanguage_Should_Use_GetUserId_Extension_Method()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage newLanguage = new()
            {
                Name = "Test",
                Code = "test",
                Icon = "test"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.AddLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(newLanguage);

            // Act
            await _controller.AddLanguage(newLanguage);

            // Assert
            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
        }

        [Fact]
        public async Task UpdateLanguage_Should_Use_GetUserId_Extension_Method()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            KinaUnaLanguage updateValues = new()
            {
                Id = TestLanguageId,
                Name = "Updated"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.GetLanguage(TestLanguageId))
                .ReturnsAsync(_testLanguage);
            _mockLanguageService.Setup(x => x.UpdateLanguage(It.IsAny<KinaUnaLanguage>()))
                .ReturnsAsync(_testLanguage);

            // Act
            await _controller.UpdateLanguage(TestLanguageId, updateValues);

            // Assert
            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteLanguage_Should_Use_GetUserId_Extension_Method()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockLanguageService.Setup(x => x.DeleteLanguage(TestLanguageId))
                .ReturnsAsync(_testLanguage);

            // Act
            await _controller.DeleteLanguage(TestLanguageId);

            // Assert
            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
        }

        #endregion
    }
}