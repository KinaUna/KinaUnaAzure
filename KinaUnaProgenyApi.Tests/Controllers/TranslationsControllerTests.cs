using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class TranslationsControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITextTranslationService> _mockTextTranslationService;
        private readonly TranslationsController _controller;

        private readonly TextTranslation _testTranslation;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserEmail = "admin@example.com";
        private const string AdminUserId = "admin-user-id";
        private const int TestTranslationId = 1;
        private const int TestLanguageId = 1;
        private const string TestWord = "Hello";
        private const string TestPage = "Home";
        private const string TestTranslationText = "Hej";

        public TranslationsControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testTranslation = new TextTranslation
            {
                Id = TestTranslationId,
                Word = TestWord,
                Page = TestPage,
                LanguageId = TestLanguageId,
                Translation = TestTranslationText
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTextTranslationService = new Mock<ITextTranslationService>();

            _controller = new TranslationsController(
                _mockUserInfoService.Object,
                _mockTextTranslationService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext(string userId = TestUserId, string userEmail = TestUserEmail)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, userEmail),
                new(ClaimTypes.NameIdentifier, userId)
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

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region GetAllTranslations Tests

        [Fact]
        public async Task GetAllTranslations_Should_Return_Ok_With_Translations_List()
        {
            // Arrange
            List<TextTranslation> translations =
            [
                _testTranslation,
                new() { Id = 2, Word = "Goodbye", Page = TestPage, LanguageId = TestLanguageId, Translation = "Farvel" }
            ];

            _mockTextTranslationService.Setup(x => x.GetAllTranslations(TestLanguageId))
                .ReturnsAsync(translations);

            // Act
            IActionResult result = await _controller.GetAllTranslations(TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TextTranslation> returnedTranslations = Assert.IsType<List<TextTranslation>>(okResult.Value);
            Assert.Equal(2, returnedTranslations.Count);
            Assert.Equal(TestTranslationId, returnedTranslations[0].Id);

            _mockTextTranslationService.Verify(x => x.GetAllTranslations(TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task GetAllTranslations_Should_Return_Empty_List_When_No_Translations()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetAllTranslations(TestLanguageId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetAllTranslations(TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TextTranslation> returnedTranslations = Assert.IsType<List<TextTranslation>>(okResult.Value);
            Assert.Empty(returnedTranslations);
        }

        #endregion

        #region GetTranslationById Tests

        [Fact]
        public async Task GetTranslationById_Should_Return_Ok_When_Translation_Exists()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetTranslationById(TestTranslationId))
                .ReturnsAsync(_testTranslation);

            // Act
            IActionResult result = await _controller.GetTranslationById(TestTranslationId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestTranslationId, returnedTranslation.Id);
            Assert.Equal(TestWord, returnedTranslation.Word);

            _mockTextTranslationService.Verify(x => x.GetTranslationById(TestTranslationId), Times.Once);
        }

        [Fact]
        public async Task GetTranslationById_Should_Return_Ok_When_Translation_Not_Found()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetTranslationById(999))
                .ReturnsAsync((TextTranslation?)null);

            // Act
            IActionResult result = await _controller.GetTranslationById(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        #endregion

        #region GetTranslationByWord Tests

        [Fact]
        public async Task GetTranslationByWord_Should_Return_Ok_When_Translation_Exists()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetTranslationByWord(TestWord, TestPage, TestLanguageId))
                .ReturnsAsync(_testTranslation);

            // Act
            IActionResult result = await _controller.GetTranslationByWord(TestWord, TestPage, TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestWord, returnedTranslation.Word);
            Assert.Equal(TestPage, returnedTranslation.Page);

            _mockTextTranslationService.Verify(x => x.GetTranslationByWord(TestWord, TestPage, TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task GetTranslationByWord_Should_Create_Translation_When_Not_Found()
        {
            // Arrange
            TextTranslation newTranslation = new()
            {
                Id = TestTranslationId,
                Word = TestWord,
                Page = TestPage,
                LanguageId = TestLanguageId,
                Translation = TestWord // Translation defaults to word when created
            };

            _mockTextTranslationService.Setup(x => x.GetTranslationByWord(TestWord, TestPage, TestLanguageId))
                .ReturnsAsync((TextTranslation?)null);
            _mockTextTranslationService.Setup(x => x.AddTranslation(It.IsAny<TextTranslation>()))
                .ReturnsAsync(newTranslation);

            // Act
            IActionResult result = await _controller.GetTranslationByWord(TestWord, TestPage, TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestWord, returnedTranslation.Word);
            Assert.Equal(TestWord, returnedTranslation.Translation); // Translation equals word when auto-created

            _mockTextTranslationService.Verify(x => x.AddTranslation(It.Is<TextTranslation>(t =>
                t.Word == TestWord &&
                t.Page == TestPage &&
                t.LanguageId == TestLanguageId &&
                t.Translation == TestWord)), Times.Once);
        }

        [Fact]
        public async Task GetTranslationByWord_Should_Use_LanguageId_1_When_Zero()
        {
            // Arrange
            TextTranslation newTranslation = new()
            {
                Id = TestTranslationId,
                Word = TestWord,
                Page = TestPage,
                LanguageId = 1,
                Translation = TestWord
            };

            _mockTextTranslationService.Setup(x => x.GetTranslationByWord(TestWord, TestPage, 1))
                .ReturnsAsync((TextTranslation?)null);
            _mockTextTranslationService.Setup(x => x.AddTranslation(It.IsAny<TextTranslation>()))
                .ReturnsAsync(newTranslation);

            // Act
            IActionResult result = await _controller.GetTranslationByWord(TestWord, TestPage, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(1, returnedTranslation.LanguageId);

            _mockTextTranslationService.Verify(x => x.AddTranslation(It.Is<TextTranslation>(t =>
                t.LanguageId == 0)), Times.Once);
        }

        #endregion

        #region PageTranslations Tests

        [Fact]
        public async Task PageTranslations_Should_Return_Ok_With_Translations_For_Page()
        {
            // Arrange
            List<TextTranslation> pageTranslations =
            [
                _testTranslation,
                new() { Id = 2, Word = "Welcome", Page = TestPage, LanguageId = TestLanguageId, Translation = "Velkommen" }
            ];

            _mockTextTranslationService.Setup(x => x.GetPageTranslations(TestLanguageId, TestPage))
                .ReturnsAsync(pageTranslations);

            // Act
            IActionResult result = await _controller.PageTranslations(TestLanguageId, TestPage);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TextTranslation> returnedTranslations = Assert.IsType<List<TextTranslation>>(okResult.Value);
            Assert.Equal(2, returnedTranslations.Count);
            Assert.All(returnedTranslations, t => Assert.Equal(TestPage, t.Page));

            _mockTextTranslationService.Verify(x => x.GetPageTranslations(TestLanguageId, TestPage), Times.Once);
        }

        [Fact]
        public async Task PageTranslations_Should_Return_Empty_List_When_No_Translations_For_Page()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetPageTranslations(TestLanguageId, "NonExistentPage"))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.PageTranslations(TestLanguageId, "NonExistentPage");

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<TextTranslation> returnedTranslations = Assert.IsType<List<TextTranslation>>(okResult.Value);
            Assert.Empty(returnedTranslations);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Add_Translation_When_Not_Exists()
        {
            // Arrange
            TextTranslation newTranslation = new()
            {
                Word = "NewWord",
                Page = TestPage,
                LanguageId = TestLanguageId,
                Translation = "NytOrd"
            };

            TextTranslation addedTranslation = new()
            {
                Id = 100,
                Word = "NewWord",
                Page = TestPage,
                LanguageId = TestLanguageId,
                Translation = "NytOrd"
            };

            _mockTextTranslationService.Setup(x => x.GetTranslationByWord("NewWord", TestPage, TestLanguageId))
                .ReturnsAsync((TextTranslation?)null);
            _mockTextTranslationService.Setup(x => x.AddTranslation(It.IsAny<TextTranslation>()))
                .ReturnsAsync(addedTranslation);

            // Act
            IActionResult result = await _controller.Post(newTranslation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(100, returnedTranslation.Id);
            Assert.Equal("NewWord", returnedTranslation.Word);

            _mockTextTranslationService.Verify(x => x.AddTranslation(It.IsAny<TextTranslation>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Not_Add_Translation_When_Already_Exists()
        {
            // Arrange
            _mockTextTranslationService.Setup(x => x.GetTranslationByWord(TestWord, TestPage, TestLanguageId))
                .ReturnsAsync(_testTranslation);

            // Act
            IActionResult result = await _controller.Post(_testTranslation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestTranslationId, returnedTranslation.Id);

            _mockTextTranslationService.Verify(x => x.AddTranslation(It.IsAny<TextTranslation>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Use_LanguageId_1_When_Zero()
        {
            // Arrange
            TextTranslation translationWithZeroLanguageId = new()
            {
                Word = "Test",
                Page = TestPage,
                LanguageId = 0,
                Translation = "Test"
            };

            TextTranslation addedTranslation = new()
            {
                Id = 100,
                Word = "Test",
                Page = TestPage,
                LanguageId = 1,
                Translation = "Test"
            };

            _mockTextTranslationService.Setup(x => x.GetTranslationByWord("Test", TestPage, 1))
                .ReturnsAsync((TextTranslation?)null);
            _mockTextTranslationService.Setup(x => x.AddTranslation(It.IsAny<TextTranslation>()))
                .ReturnsAsync(addedTranslation);

            // Act
            IActionResult result = await _controller.Post(translationWithZeroLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(1, returnedTranslation.LanguageId);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Translation_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            TextTranslation updatedTranslation = new()
            {
                Id = TestTranslationId,
                Word = TestWord,
                Page = TestPage,
                LanguageId = TestLanguageId,
                Translation = "UpdatedTranslation"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.UpdateTranslation(TestTranslationId, It.IsAny<TextTranslation>()))
                .ReturnsAsync(updatedTranslation);

            // Act
            IActionResult result = await _controller.Put(TestTranslationId, updatedTranslation);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal("UpdatedTranslation", returnedTranslation.Translation);

            _mockTextTranslationService.Verify(x => x.UpdateTranslation(TestTranslationId, It.IsAny<TextTranslation>()), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestTranslationId, _testTranslation);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTextTranslationService.Verify(x => x.UpdateTranslation(It.IsAny<int>(), It.IsAny<TextTranslation>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Translation_Not_Exists()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.UpdateTranslation(999, It.IsAny<TextTranslation>()))
                .ReturnsAsync((TextTranslation?)null);

            // Act
            IActionResult result = await _controller.Put(999, _testTranslation);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Translation_And_Related_Translations_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.DeleteTranslation(TestTranslationId))
                .ReturnsAsync(_testTranslation);

            // Act
            IActionResult result = await _controller.Delete(TestTranslationId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestTranslationId, returnedTranslation.Id);

            _mockTextTranslationService.Verify(x => x.DeleteTranslation(TestTranslationId), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestTranslationId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTextTranslationService.Verify(x => x.DeleteTranslation(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Translation_Not_Exists()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.DeleteTranslation(999))
                .ReturnsAsync((TextTranslation?)null);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region DeleteSingleItem Tests

        [Fact]
        public async Task DeleteSingleItem_Should_Delete_Only_Single_Translation_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.DeleteSingleTranslation(TestTranslationId))
                .ReturnsAsync(_testTranslation);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(TestTranslationId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            TextTranslation returnedTranslation = Assert.IsType<TextTranslation>(okResult.Value);
            Assert.Equal(TestTranslationId, returnedTranslation.Id);

            _mockTextTranslationService.Verify(x => x.DeleteSingleTranslation(TestTranslationId), Times.Once);
        }

        [Fact]
        public async Task DeleteSingleItem_Should_Return_Unauthorized_When_User_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(TestTranslationId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTextTranslationService.Verify(x => x.DeleteSingleTranslation(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSingleItem_Should_Return_NotFound_When_Translation_Not_Exists()
        {
            // Arrange
            SetupControllerContext(AdminUserId, AdminUserEmail);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockTextTranslationService.Setup(x => x.DeleteSingleTranslation(999))
                .ReturnsAsync((TextTranslation?)null);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion
    }
}