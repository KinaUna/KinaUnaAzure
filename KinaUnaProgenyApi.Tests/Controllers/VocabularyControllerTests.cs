using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class VocabularyControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IVocabularyService> _mockVocabularyService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly VocabularyController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly VocabularyItem _testVocabularyItem;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestWordId = 100;

        public VocabularyControllerTests()
        {
            // Setup in-memory DbContext
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            // Setup test data
            _testUser = new UserInfo
            {
                UserId = TestUserId,
                UserEmail = TestUserEmail,
                ViewChild = TestProgenyId,
                IsKinaUnaAdmin = false,
                FirstName = "Test",
                MiddleName = "M",
                LastName = "User"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testVocabularyItem = new VocabularyItem
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Test Word",
                Description = "Test Description",
                Language = "English",
                SoundsLike = "test werd",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary,
                ItemId = TestWordId.ToString(),
                ProgenyTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockVocabularyService = new Mock<IVocabularyService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            _controller = new VocabularyController(
                _mockUserInfoService.Object,
                _mockTimelineService.Object,
                _mockVocabularyService.Object,
                _mockProgenyService.Object,
                _mockWebNotificationsService.Object);

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new(ClaimTypes.Email, TestUserEmail),
                new("sub", TestUserId)
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

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_VocabularyList()
        {
            // Arrange
            List<VocabularyItem> wordList =
            [
                _testVocabularyItem,
                new() { WordId = TestWordId + 1, ProgenyId = TestProgenyId, Word = "Another Word" }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(wordList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<VocabularyItem> returnedList = Assert.IsType<List<VocabularyItem>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVocabularyService.Verify(x => x.GetVocabularyList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_Empty_List_When_No_Words()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<VocabularyItem> returnedList = Assert.IsType<List<VocabularyItem>>(okResult.Value);
            Assert.Empty(returnedList);
        }

        #endregion

        #region GetVocabularyItem Tests

        [Fact]
        public async Task GetVocabularyItem_Should_Return_Ok_When_Item_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);

            // Act
            IActionResult result = await _controller.GetVocabularyItem(TestWordId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyItem returnedItem = Assert.IsType<VocabularyItem>(okResult.Value);
            Assert.Equal(TestWordId, returnedItem.WordId);
            Assert.Equal("Test Word", returnedItem.Word);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVocabularyService.Verify(x => x.GetVocabularyItem(TestWordId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVocabularyItem_Should_Return_NotFound_When_Item_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(999, _testUser))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.GetVocabularyItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVocabularyService.Verify(x => x.GetVocabularyItem(999, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Add_VocabularyItem_And_Return_Ok()
        {
            // Arrange
            VocabularyItem newVocabularyItem = new()
            {
                ProgenyId = TestProgenyId,
                Word = "New Word",
                Description = "New Description",
                Language = "English",
                Date = DateTime.UtcNow
            };

            VocabularyItem addedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "New Word",
                Description = "New Description",
                Language = "English",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.AddVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(addedVocabularyItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(addedVocabularyItem);

            // Act
            IActionResult result = await _controller.Post(newVocabularyItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyItem returnedItem = Assert.IsType<VocabularyItem>(okResult.Value);
            Assert.Equal(TestWordId, returnedItem.WordId);
            Assert.Equal(TestUserId, returnedItem.CreatedBy);
            Assert.Equal(TestUserId, returnedItem.ModifiedBy);

            _mockVocabularyService.Verify(x => x.AddVocabularyItem(It.Is<VocabularyItem>(v =>
                v.CreatedBy == TestUserId && v.ModifiedBy == TestUserId)), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendVocabularyNotification(
                It.IsAny<VocabularyItem>(), _testUser, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_VocabularyService_Returns_Null()
        {
            // Arrange
            VocabularyItem newVocabularyItem = new()
            {
                ProgenyId = TestProgenyId,
                Word = "New Word"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.AddVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.Post(newVocabularyItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Set_CreatedBy_And_ModifiedBy()
        {
            // Arrange
            VocabularyItem newVocabularyItem = new()
            {
                ProgenyId = TestProgenyId,
                Word = "New Word"
            };

            VocabularyItem addedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "New Word",
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.AddVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(addedVocabularyItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Post(newVocabularyItem);

            // Assert
            _mockVocabularyService.Verify(x => x.AddVocabularyItem(It.Is<VocabularyItem>(v =>
                v.CreatedBy == TestUserId && v.ModifiedBy == TestUserId)), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Progeny_Name()
        {
            // Arrange
            VocabularyItem newVocabularyItem = new()
            {
                ProgenyId = TestProgenyId,
                Word = "New Word"
            };

            VocabularyItem addedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "New Word",
                CreatedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.AddVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(addedVocabularyItem);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Post(newVocabularyItem);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendVocabularyNotification(
                It.IsAny<VocabularyItem>(), _testUser, $"Word added for {_testProgeny.NickName}"), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_VocabularyItem_And_Return_Ok()
        {
            // Arrange
            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word",
                Description = "Updated Description"
            };

            VocabularyItem returnedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word",
                Description = "Updated Description",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockVocabularyService.Setup(x => x.UpdateVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(returnedVocabularyItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestWordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestWordId, updatedVocabularyItem);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyItem returnedItem = Assert.IsType<VocabularyItem>(okResult.Value);
            Assert.Equal(TestUserId, returnedItem.ModifiedBy);

            _mockVocabularyService.Verify(x => x.GetVocabularyItem(TestWordId, _testUser), Times.Exactly(2));
            _mockVocabularyService.Verify(x => x.UpdateVocabularyItem(It.Is<VocabularyItem>(v =>
                v.ModifiedBy == TestUserId)), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Item_Does_Not_Exist()
        {
            // Arrange
            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = 999,
                ProgenyId = TestProgenyId,
                Word = "Non-existent Word"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(999, _testUser))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.Put(999, updatedVocabularyItem);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVocabularyService.Verify(x => x.UpdateVocabularyItem(It.IsAny<VocabularyItem>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateVocabularyItem_Returns_Null()
        {
            // Arrange
            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockVocabularyService.Setup(x => x.UpdateVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestWordId, updatedVocabularyItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word"
            };

            VocabularyItem returnedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockVocabularyService.Setup(x => x.UpdateVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(returnedVocabularyItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestWordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary, _testUser))
                .ReturnsAsync((TimeLineItem?)null);

            // Act
            IActionResult result = await _controller.Put(TestWordId, updatedVocabularyItem);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Set_ModifiedBy()
        {
            // Arrange
            VocabularyItem updatedVocabularyItem = new()
            {
                WordId = TestWordId,
                ProgenyId = TestProgenyId,
                Word = "Updated Word"
            };

            VocabularyItem returnedVocabularyItem = new()
            {
                WordId = TestWordId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockVocabularyService.Setup(x => x.UpdateVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(returnedVocabularyItem);

            // Act
            await _controller.Put(TestWordId, updatedVocabularyItem);

            // Assert
            _mockVocabularyService.Verify(x => x.UpdateVocabularyItem(It.Is<VocabularyItem>(v =>
                v.ModifiedBy == TestUserId)), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_VocabularyItem_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(_testVocabularyItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestWordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestWordId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockVocabularyService.Verify(x => x.GetVocabularyItem(TestWordId, _testUser), Times.Once);
            _mockVocabularyService.Verify(x => x.DeleteVocabularyItem(It.Is<VocabularyItem>(v =>
                v.ModifiedBy == TestUserId)), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendVocabularyNotification(
                It.IsAny<VocabularyItem>(), _testUser, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Item_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(999, _testUser))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockVocabularyService.Verify(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteVocabularyItem_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync((VocabularyItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestWordId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(_testVocabularyItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestWordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary, _testUser))
                .ReturnsAsync((TimeLineItem?)null);

            // Act
            IActionResult result = await _controller.Delete(TestWordId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Set_ModifiedBy()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(_testVocabularyItem);

            // Act
            await _controller.Delete(TestWordId);

            // Assert
            _mockVocabularyService.Verify(x => x.DeleteVocabularyItem(It.Is<VocabularyItem>(v =>
                v.ModifiedBy == TestUserId)), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Send_Notification_With_Progeny_Name()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyItem(TestWordId, _testUser))
                .ReturnsAsync(_testVocabularyItem);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockVocabularyService.Setup(x => x.DeleteVocabularyItem(It.IsAny<VocabularyItem>()))
                .ReturnsAsync(_testVocabularyItem);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestWordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Delete(TestWordId);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendVocabularyNotification(
                It.IsAny<VocabularyItem>(), _testUser, $"Word deleted for {_testProgeny.NickName}"), Times.Once);
        }

        #endregion

        #region GetVocabularyListPage Tests

        [Fact]
        public async Task GetVocabularyListPage_Should_Return_Ok_With_VocabularyListPage()
        {
            // Arrange
            List<VocabularyItem> allItems =
            [
                _testVocabularyItem,
                new() { WordId = TestWordId + 1, ProgenyId = TestProgenyId, Word = "Word 2", Date = DateTime.UtcNow.AddDays(-1) },
                new() { WordId = TestWordId + 2, ProgenyId = TestProgenyId, Word = "Word 3", Date = DateTime.UtcNow.AddDays(-2) }
            ];

            VocabularyListPage expectedModel = new();
            expectedModel.ProcessVocabularyList(allItems, 1, 1, 8);

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(allItems);

            // Act
            IActionResult result = await _controller.GetVocabularyListPage(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyListPage returnedModel = Assert.IsType<VocabularyListPage>(okResult.Value);
            Assert.NotNull(returnedModel.VocabularyList);
            
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockVocabularyService.Verify(x => x.GetVocabularyList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVocabularyListPage_Should_Use_Default_Values()
        {
            // Arrange
            List<VocabularyItem> allItems = [_testVocabularyItem];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(Constants.DefaultChildId, _testUser))
                .ReturnsAsync(allItems);

            // Act
            IActionResult result = await _controller.GetVocabularyListPage();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyListPage returnedModel = Assert.IsType<VocabularyListPage>(okResult.Value);
            Assert.NotNull(returnedModel);

            _mockVocabularyService.Verify(x => x.GetVocabularyList(Constants.DefaultChildId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetVocabularyListPage_Should_Handle_Pagination()
        {
            // Arrange
            List<VocabularyItem> allItems = new();
            for (int i = 0; i < 20; i++)
            {
                allItems.Add(new VocabularyItem
                {
                    WordId = TestWordId + i,
                    ProgenyId = TestProgenyId,
                    Word = $"Word {i}",
                    Date = DateTime.UtcNow.AddDays(-i)
                });
            }

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(allItems);

            // Act - Get page 2 with 8 items per page
            IActionResult result = await _controller.GetVocabularyListPage(8, 2, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyListPage returnedModel = Assert.IsType<VocabularyListPage>(okResult.Value);
            Assert.Equal(8, returnedModel.VocabularyList?.Count);
            Assert.Equal(2, returnedModel.PageNumber);
        }

        [Fact]
        public async Task GetVocabularyListPage_Should_Sort_By_Newest_First_When_SortBy_Is_1()
        {
            // Arrange
            List<VocabularyItem> allItems =
            [
                new() { WordId = TestWordId, ProgenyId = TestProgenyId, Word = "Oldest", Date = DateTime.UtcNow.AddDays(-10) },
                new() { WordId = TestWordId + 1, ProgenyId = TestProgenyId, Word = "Middle", Date = DateTime.UtcNow.AddDays(-5) },
                new() { WordId = TestWordId + 2, ProgenyId = TestProgenyId, Word = "Newest", Date = DateTime.UtcNow }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(allItems);

            // Act
            IActionResult result = await _controller.GetVocabularyListPage(8, 1, TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyListPage returnedModel = Assert.IsType<VocabularyListPage>(okResult.Value);
            Assert.NotNull(returnedModel.VocabularyList);
            Assert.Equal("Newest", returnedModel.VocabularyList[0].Word);
            Assert.Equal("Oldest", returnedModel.VocabularyList[^1].Word);
        }

        [Fact]
        public async Task GetVocabularyListPage_Should_Sort_By_Oldest_First_When_SortBy_Is_0()
        {
            // Arrange
            List<VocabularyItem> allItems =
            [
                new() { WordId = TestWordId, ProgenyId = TestProgenyId, Word = "Oldest", Date = DateTime.UtcNow.AddDays(-10) },
                new() { WordId = TestWordId + 1, ProgenyId = TestProgenyId, Word = "Middle", Date = DateTime.UtcNow.AddDays(-5) },
                new() { WordId = TestWordId + 2, ProgenyId = TestProgenyId, Word = "Newest", Date = DateTime.UtcNow }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockVocabularyService.Setup(x => x.GetVocabularyList(TestProgenyId, _testUser))
                .ReturnsAsync(allItems);

            // Act
            IActionResult result = await _controller.GetVocabularyListPage(8, 1, TestProgenyId, 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            VocabularyListPage returnedModel = Assert.IsType<VocabularyListPage>(okResult.Value);
            Assert.NotNull(returnedModel.VocabularyList);
            Assert.Equal("Oldest", returnedModel.VocabularyList[0].Word);
            Assert.Equal("Newest", returnedModel.VocabularyList[^1].Word);
        }

        #endregion
    }
}