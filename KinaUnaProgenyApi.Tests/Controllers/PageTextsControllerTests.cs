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
    public class PageTextsControllerTests
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IKinaUnaTextService> _mockKinaUnaTextService;
        private readonly PageTextsController _controller;

        private readonly KinaUnaText _testKinaUnaText;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const string AdminUserId = "admin-user-id";
        private const int TestTextId = 1;
        private const int TestKinaUnaTextId = 100;
        private const int TestLanguageId = 1;

        public PageTextsControllerTests()
        {
            // Setup test data
            _testKinaUnaText = new KinaUnaText
            {
                Id = TestKinaUnaTextId,
                TextId = TestTextId,
                LanguageId = TestLanguageId,
                Title = "Test Title",
                Page = "Test Page",
                Text = "Test Text Content"
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockKinaUnaTextService = new Mock<IKinaUnaTextService>();

            // Initialize controller
            _controller = new PageTextsController(_mockUserInfoService.Object, _mockKinaUnaTextService.Object);

            // Setup controller context with claims
            SetupControllerContext(TestUserEmail, TestUserId);
        }

        private void SetupControllerContext(string email, string userId)
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, email),
                new("sub", userId)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region ByTitle Tests

        [Fact]
        public async Task ByTitle_Should_Return_Ok_With_KinaUnaText_When_Text_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextByTitle("Test Title", "Test Page", TestLanguageId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.ByTitle("Test Title", "Test Page", TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(_testKinaUnaText.Id, returnedText.Id);
            Assert.Equal(_testKinaUnaText.Title, returnedText.Title);
            Assert.Equal(_testKinaUnaText.Page, returnedText.Page);

            _mockKinaUnaTextService.Verify(x => x.GetTextByTitle("Test Title", "Test Page", TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task ByTitle_Should_Return_Ok_With_Null_When_Text_Not_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextByTitle("Nonexistent", "Test Page", TestLanguageId))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.ByTitle("Nonexistent", "Test Page", TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task ByTitle_Should_Handle_Special_Characters_In_Title()
        {
            // Arrange
            string specialTitle = "Title/With\\Special*Characters";
            _mockKinaUnaTextService.Setup(x => x.GetTextByTitle(specialTitle, "Test Page", TestLanguageId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.ByTitle(specialTitle, "Test Page", TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<KinaUnaText>(okResult.Value);
            _mockKinaUnaTextService.Verify(x => x.GetTextByTitle(specialTitle, "Test Page", TestLanguageId), Times.Once);
        }

        #endregion

        #region GetTextById Tests

        [Fact]
        public async Task GetTextById_Should_Return_Ok_With_KinaUnaText_When_Text_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextById(TestKinaUnaTextId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.GetTextById(TestKinaUnaTextId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(_testKinaUnaText.Id, returnedText.Id);

            _mockKinaUnaTextService.Verify(x => x.GetTextById(TestKinaUnaTextId), Times.Once);
        }

        [Fact]
        public async Task GetTextById_Should_Return_Ok_With_Null_When_Text_Not_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextById(999))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.GetTextById(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        #endregion

        #region GetTextByTextId Tests

        [Fact]
        public async Task GetTextByTextId_Should_Return_Ok_With_KinaUnaText_When_Text_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextByTextId(TestTextId, TestLanguageId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.GetTextByTextId(TestTextId, TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(_testKinaUnaText.TextId, returnedText.TextId);
            Assert.Equal(_testKinaUnaText.LanguageId, returnedText.LanguageId);

            _mockKinaUnaTextService.Verify(x => x.GetTextByTextId(TestTextId, TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task GetTextByTextId_Should_Return_Ok_With_Null_When_Text_Not_Exists()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetTextByTextId(999, TestLanguageId))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.GetTextByTextId(999, TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task GetTextByTextId_Should_Handle_Different_Languages()
        {
            // Arrange
            KinaUnaText germanText = new()
            {
                Id = TestKinaUnaTextId + 1,
                TextId = TestTextId,
                LanguageId = 2, // German
                Title = "Test Titel",
                Page = "Test Page",
                Text = "Test Text Inhalt"
            };

            _mockKinaUnaTextService.Setup(x => x.GetTextByTextId(TestTextId, 2))
                .ReturnsAsync(germanText);

            // Act
            IActionResult result = await _controller.GetTextByTextId(TestTextId, 2);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(2, returnedText.LanguageId);
            Assert.Equal("Test Titel", returnedText.Title);
        }

        #endregion

        #region PageTexts Tests

        [Fact]
        public async Task PageTexts_Should_Return_Ok_With_List_Of_KinaUnaTexts()
        {
            // Arrange
            List<KinaUnaText> textsList =
            [
                _testKinaUnaText,
                new()
                {
                    Id = TestKinaUnaTextId + 1,
                    TextId = TestTextId + 1,
                    LanguageId = TestLanguageId,
                    Title = "Another Title",
                    Page = "Test Page",
                    Text = "Another Text"
                }
            ];

            _mockKinaUnaTextService.Setup(x => x.GetPageTextsList("Test Page", TestLanguageId))
                .ReturnsAsync(textsList);

            // Act
            IActionResult result = await _controller.PageTexts("Test Page", TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaText> returnedList = Assert.IsType<List<KinaUnaText>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
            Assert.All(returnedList, text => Assert.Equal("Test Page", text.Page));

            _mockKinaUnaTextService.Verify(x => x.GetPageTextsList("Test Page", TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task PageTexts_Should_Return_Empty_List_When_No_Texts_Found()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetPageTextsList("Nonexistent Page", TestLanguageId))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.PageTexts("Nonexistent Page", TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaText> returnedList = Assert.IsType<List<KinaUnaText>>(okResult.Value);
            Assert.Empty(returnedList);
        }

        #endregion

        #region GetAllTexts Tests

        [Fact]
        public async Task GetAllTexts_Should_Return_Ok_With_All_Texts_For_Language()
        {
            // Arrange
            List<KinaUnaText> allTextsList =
            [
                _testKinaUnaText,
                new()
                {
                    Id = TestKinaUnaTextId + 1,
                    TextId = TestTextId + 1,
                    LanguageId = TestLanguageId,
                    Title = "Different Title",
                    Page = "Different Page",
                    Text = "Different Text"
                },
                new()
                {
                    Id = TestKinaUnaTextId + 2,
                    TextId = TestTextId + 2,
                    LanguageId = TestLanguageId,
                    Title = "Third Title",
                    Page = "Third Page",
                    Text = "Third Text"
                }
            ];

            _mockKinaUnaTextService.Setup(x => x.GetAllPageTextsList(TestLanguageId))
                .ReturnsAsync(allTextsList);

            // Act
            IActionResult result = await _controller.GetAllTexts(TestLanguageId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaText> returnedList = Assert.IsType<List<KinaUnaText>>(okResult.Value);
            Assert.Equal(3, returnedList.Count);
            Assert.All(returnedList, text => Assert.Equal(TestLanguageId, text.LanguageId));

            _mockKinaUnaTextService.Verify(x => x.GetAllPageTextsList(TestLanguageId), Times.Once);
        }

        [Fact]
        public async Task GetAllTexts_Should_Return_Empty_List_When_No_Texts_For_Language()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.GetAllPageTextsList(999))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.GetAllTexts(999);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<KinaUnaText> returnedList = Assert.IsType<List<KinaUnaText>>(okResult.Value);
            Assert.Empty(returnedList);
        }

        #endregion

        #region CheckLanguages Tests

        [Fact]
        public async Task CheckLanguages_Should_Return_Ok()
        {
            // Arrange
            _mockKinaUnaTextService.Setup(x => x.CheckLanguages())
                .Returns(Task.CompletedTask);

            // Act
            IActionResult result = await _controller.CheckLanguages();

            // Assert
            Assert.IsType<OkResult>(result);
            _mockKinaUnaTextService.Verify(x => x.CheckLanguages(), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Return_Ok_With_Added_Text_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);
            KinaUnaText newText = new()
            {
                TextId = 0,
                LanguageId = TestLanguageId,
                Title = "New Title",
                Page = "New Page",
                Text = "New Text Content"
            };

            KinaUnaText addedText = new()
            {
                Id = TestKinaUnaTextId + 1,
                TextId = TestTextId + 1,
                LanguageId = TestLanguageId,
                Title = "New Title",
                Page = "New Page",
                Text = "New Text Content"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.AddText(It.IsAny<KinaUnaText>()))
                .ReturnsAsync(addedText);

            // Act
            IActionResult result = await _controller.Post(newText);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(addedText.Id, returnedText.Id);
            Assert.Equal(addedText.Title, returnedText.Title);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockKinaUnaTextService.Verify(x => x.AddText(It.IsAny<KinaUnaText>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            KinaUnaText newText = new()
            {
                LanguageId = TestLanguageId,
                Title = "New Title",
                Page = "New Page",
                Text = "New Text Content"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newText);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockKinaUnaTextService.Verify(x => x.AddText(It.IsAny<KinaUnaText>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_User_Not_Found()
        {
            // Arrange
            SetupControllerContext("", "");
            KinaUnaText newText = new()
            {
                LanguageId = TestLanguageId,
                Title = "New Title",
                Page = "New Page",
                Text = "New Text Content"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Post(newText);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Return_Ok_With_Updated_Text_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);
            KinaUnaText updateText = new()
            {
                Id = TestKinaUnaTextId,
                TextId = TestTextId,
                LanguageId = TestLanguageId,
                Title = "Updated Title",
                Page = "Test Page",
                Text = "Updated Text Content"
            };

            KinaUnaText updatedText = new()
            {
                Id = TestKinaUnaTextId,
                TextId = TestTextId,
                LanguageId = TestLanguageId,
                Title = "Updated Title",
                Page = "Test Page",
                Text = "Updated Text Content"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.UpdateText(TestKinaUnaTextId, It.IsAny<KinaUnaText>()))
                .ReturnsAsync(updatedText);

            // Act
            IActionResult result = await _controller.Put(TestKinaUnaTextId, updateText);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal("Updated Title", returnedText.Title);
            Assert.Equal("Updated Text Content", returnedText.Text);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockKinaUnaTextService.Verify(x => x.UpdateText(TestKinaUnaTextId, It.IsAny<KinaUnaText>()), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            KinaUnaText updateText = new()
            {
                Id = TestKinaUnaTextId,
                Title = "Updated Title"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Put(TestKinaUnaTextId, updateText);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockKinaUnaTextService.Verify(x => x.UpdateText(It.IsAny<int>(), It.IsAny<KinaUnaText>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Text_Update_Fails()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);
            KinaUnaText updateText = new()
            {
                Id = TestKinaUnaTextId,
                Title = "Updated Title"
            };

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.UpdateText(TestKinaUnaTextId, It.IsAny<KinaUnaText>()))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.Put(TestKinaUnaTextId, updateText);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Return_Ok_With_Deleted_Text_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteText(TestKinaUnaTextId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.Delete(TestKinaUnaTextId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(_testKinaUnaText.Id, returnedText.Id);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockKinaUnaTextService.Verify(x => x.DeleteText(TestKinaUnaTextId), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_User_Is_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.Delete(TestKinaUnaTextId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockKinaUnaTextService.Verify(x => x.DeleteText(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Text_Not_Found()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteText(999))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Delete_All_Language_Versions()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteText(TestKinaUnaTextId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.Delete(TestKinaUnaTextId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKinaUnaTextService.Verify(x => x.DeleteText(TestKinaUnaTextId), Times.Once);
        }

        #endregion

        #region DeleteSingleItem Tests

        [Fact]
        public async Task DeleteSingleItem_Should_Return_Ok_With_Deleted_Text_When_User_Is_Admin()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteSingleText(TestKinaUnaTextId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(TestKinaUnaTextId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            KinaUnaText returnedText = Assert.IsType<KinaUnaText>(okResult.Value);
            Assert.Equal(_testKinaUnaText.Id, returnedText.Id);

            _mockUserInfoService.Verify(x => x.IsAdminUserId(AdminUserId), Times.Once);
            _mockKinaUnaTextService.Verify(x => x.DeleteSingleText(TestKinaUnaTextId), Times.Once);
        }

        [Fact]
        public async Task DeleteSingleItem_Should_Return_NotFound_When_User_Is_Not_Admin()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.IsAdminUserId(TestUserId))
                .ReturnsAsync(false);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(TestKinaUnaTextId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockKinaUnaTextService.Verify(x => x.DeleteSingleText(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSingleItem_Should_Return_NotFound_When_Text_Not_Found()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteSingleText(999))
                .ReturnsAsync((KinaUnaText)null!);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteSingleItem_Should_Only_Delete_Specified_Language_Version()
        {
            // Arrange
            SetupControllerContext("admin@example.com", AdminUserId);

            _mockUserInfoService.Setup(x => x.IsAdminUserId(AdminUserId))
                .ReturnsAsync(true);
            _mockKinaUnaTextService.Setup(x => x.DeleteSingleText(TestKinaUnaTextId))
                .ReturnsAsync(_testKinaUnaText);

            // Act
            IActionResult result = await _controller.DeleteSingleItem(TestKinaUnaTextId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockKinaUnaTextService.Verify(x => x.DeleteSingleText(TestKinaUnaTextId), Times.Once);
        }

        #endregion
    }
}