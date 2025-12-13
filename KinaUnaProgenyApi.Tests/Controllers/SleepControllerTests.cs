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
    public class SleepControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<ISleepService> _mockSleepService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly SleepController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Sleep _testSleep;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestSleepId = 100;

        public SleepControllerTests()
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
                LastName = "User",
                Timezone = "Central Standard Time"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "TestNick",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testSleep = new Sleep
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(8),
                SleepRating = 5,
                SleepNotes = "Good night sleep",
                Author = TestUserId,
                CreatedBy = TestUserId,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1),
                ModifiedBy = TestUserId,
                ModifiedTime = DateTime.UtcNow.AddDays(-1),
                ItemPermissionsDtoList = []
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                ItemType = (int)KinaUnaTypes.TimeLineType.Sleep,
                ItemId = TestSleepId.ToString(),
                ProgenyTime = DateTime.UtcNow.AddDays(-1)
            };

            // Setup mocks
            _mockTimelineService = new Mock<ITimelineService>();
            _mockSleepService = new Mock<ISleepService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();

            // Initialize controller
            _controller = new SleepController(
                _mockTimelineService.Object,
                _mockSleepService.Object,
                _mockProgenyService.Object,
                _mockUserInfoService.Object,
                _mockWebNotificationsService.Object
            );

            // Setup controller context with claims
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
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Progeny Tests

        [Fact]
        public async Task Progeny_Should_Return_Ok_With_SleepList_When_Items_Exist()
        {
            // Arrange
            List<Sleep> sleepList =
            [
                _testSleep,
                new()
                {
                    SleepId = TestSleepId + 1,
                    ProgenyId = TestProgenyId,
                    SleepStart = DateTime.UtcNow.AddDays(-2),
                    SleepEnd = DateTime.UtcNow.AddDays(-2).AddHours(7)
                }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(sleepList);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Sleep> returnedList = Assert.IsAssignableFrom<List<Sleep>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockSleepService.Verify(x => x.GetSleepList(TestProgenyId, _testUser), Times.Once);
        }

        [Fact]
        public async Task Progeny_Should_Return_NotFound_When_No_Items_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync([]);

            // Act
            IActionResult result = await _controller.Progeny(TestProgenyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region GetSleepItem Tests

        [Fact]
        public async Task GetSleepItem_Should_Return_Ok_When_Sleep_Exists()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);

            // Act
            IActionResult result = await _controller.GetSleepItem(TestSleepId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Sleep returnedSleep = Assert.IsType<Sleep>(okResult.Value);
            Assert.Equal(TestSleepId, returnedSleep.SleepId);
            Assert.Equal(_testSleep.SleepNotes, returnedSleep.SleepNotes);

            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
            _mockSleepService.Verify(x => x.GetSleep(TestSleepId, _testUser), Times.Once);
        }

        [Fact]
        public async Task GetSleepItem_Should_Return_NotFound_When_Sleep_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.GetSleepItem(TestSleepId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSleepService.Verify(x => x.GetSleep(TestSleepId, _testUser), Times.Once);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_Should_Create_Sleep_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = TestProgenyId,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(8),
                SleepRating = 5,
                SleepNotes = "New sleep entry"
            };

            Sleep createdSleep = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepStart = newSleep.SleepStart,
                SleepEnd = newSleep.SleepEnd,
                SleepRating = newSleep.SleepRating,
                SleepNotes = newSleep.SleepNotes,
                Author = TestUserId,
                CreatedBy = TestUserId,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.AddSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(createdSleep);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(createdSleep);

            // Act
            IActionResult result = await _controller.Post(newSleep);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Sleep returnedSleep = Assert.IsType<Sleep>(okResult.Value);
            Assert.Equal(TestSleepId, returnedSleep.SleepId);
            Assert.Equal(TestUserId, returnedSleep.Author);
            Assert.Equal(TestUserId, returnedSleep.CreatedBy);
            Assert.Equal(TestUserId, returnedSleep.ModifiedBy);

            _mockSleepService.Verify(x => x.AddSleep(It.Is<Sleep>(s =>
                s.Author == TestUserId &&
                s.CreatedBy == TestUserId &&
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendSleepNotification(
                It.IsAny<Sleep>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Post_Should_Return_Unauthorized_When_AddSleep_Returns_Null()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = TestProgenyId,
                SleepStart = DateTime.UtcNow,
                SleepEnd = DateTime.UtcNow.AddHours(8)
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.AddSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.Post(newSleep);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
            _mockTimelineService.Verify(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Post_Should_Send_Notification_With_Progeny_NickName()
        {
            // Arrange
            Sleep newSleep = new()
            {
                ProgenyId = TestProgenyId,
                SleepStart = DateTime.UtcNow,
                SleepEnd = DateTime.UtcNow.AddHours(8)
            };

            Sleep createdSleep = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                Author = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.AddSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(createdSleep);
            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(createdSleep);

            // Act
            await _controller.Post(newSleep);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendSleepNotification(
                It.IsAny<Sleep>(), 
                It.IsAny<UserInfo>(), 
                It.Is<string>(s => s.Contains(_testProgeny.NickName))), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_Should_Update_Sleep_And_Timeline_And_Return_Ok()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepStart = DateTime.UtcNow.AddDays(-1),
                SleepEnd = DateTime.UtcNow.AddDays(-1).AddHours(10),
                SleepRating = 4,
                SleepNotes = "Updated notes"
            };

            Sleep updatedSleep = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepStart = updateValues.SleepStart,
                SleepEnd = updateValues.SleepEnd,
                SleepRating = updateValues.SleepRating,
                SleepNotes = updateValues.SleepNotes,
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockSleepService.Setup(x => x.UpdateSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(updatedSleep);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestSleepId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Sleep returnedSleep = Assert.IsType<Sleep>(okResult.Value);
            Assert.Equal(TestUserId, returnedSleep.ModifiedBy);

            _mockSleepService.Verify(x => x.GetSleep(TestSleepId, _testUser), Times.Exactly(2));
            _mockSleepService.Verify(x => x.UpdateSleep(It.Is<Sleep>(s =>
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        [Fact]
        public async Task Put_Should_Return_NotFound_When_Sleep_Does_Not_Exist()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = TestSleepId,
                SleepNotes = "Updated notes"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.Put(TestSleepId, updateValues);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSleepService.Verify(x => x.UpdateSleep(It.IsAny<Sleep>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Put_Should_Return_Unauthorized_When_UpdateSleep_Returns_Null()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = TestSleepId,
                SleepNotes = "Updated notes"
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockSleepService.Setup(x => x.UpdateSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.Put(TestSleepId, updateValues);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_Should_Return_Ok_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            Sleep updateValues = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepNotes = "Updated notes"
            };

            Sleep updatedSleep = new()
            {
                SleepId = TestSleepId,
                ProgenyId = TestProgenyId,
                SleepNotes = "Updated notes",
                ModifiedBy = TestUserId
            };

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockSleepService.Setup(x => x.UpdateSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(updatedSleep);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Put(TestSleepId, updateValues);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Sleep returnedSleep = Assert.IsType<Sleep>(okResult.Value);
            Assert.Equal("Updated notes", returnedSleep.SleepNotes);

            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Should_Delete_Sleep_And_Timeline_And_Return_NoContent()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.DeleteSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(_testSleep);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestSleepId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockSleepService.Verify(x => x.GetSleep(TestSleepId, _testUser), Times.Once);
            _mockSleepService.Verify(x => x.DeleteSleep(It.Is<Sleep>(s =>
                s.ModifiedBy == TestUserId), _testUser), Times.Once);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
            _mockWebNotificationsService.Verify(x => x.SendSleepNotification(
                It.IsAny<Sleep>(), It.IsAny<UserInfo>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Return_NotFound_When_Sleep_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.Delete(TestSleepId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSleepService.Verify(x => x.DeleteSleep(It.IsAny<Sleep>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Return_Unauthorized_When_DeleteSleep_Returns_Null()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.DeleteSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(null as Sleep);

            // Act
            IActionResult result = await _controller.Delete(TestSleepId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_Should_Return_NoContent_When_TimelineItem_Does_Not_Exist()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.DeleteSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(_testSleep);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep, _testUser))
                .ReturnsAsync(null as TimeLineItem);

            // Act
            IActionResult result = await _controller.Delete(TestSleepId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Delete_Should_Send_Notification_With_Deleted_Message()
        {
            // Arrange
            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);
            _mockSleepService.Setup(x => x.DeleteSleep(It.IsAny<Sleep>(), _testUser))
                .ReturnsAsync(_testSleep);
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestSleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep, _testUser))
                .ReturnsAsync(_testTimeLineItem);
            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Delete(TestSleepId);

            // Assert
            _mockWebNotificationsService.Verify(x => x.SendSleepNotification(
                It.IsAny<Sleep>(), 
                It.IsAny<UserInfo>(), 
                It.Is<string>(s => s.Contains("deleted") && s.Contains(_testProgeny.NickName))), Times.Once);
        }

        #endregion

        #region GetSleepListPage Tests

        [Fact]
        public async Task GetSleepListPage_Should_Return_Ok_With_Paged_List()
        {
            // Arrange
            List<Sleep> allSleeps =
            [
                new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-10) },
                new() { SleepId = 2, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-9) },
                new() { SleepId = 3, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-8) },
                new() { SleepId = 4, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-7) },
                new() { SleepId = 5, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-6) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepListPage(pageSize: 2, pageIndex: 1, progenyId: TestProgenyId, sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SleepListPage model = Assert.IsType<SleepListPage>(okResult.Value);
            Assert.Equal(2, model.SleepList.Count);
            Assert.Equal(3, model.TotalPages); // 5 items / 2 per page = 3 pages
            Assert.Equal(1, model.PageNumber);
            Assert.Equal(1, model.SortBy);
        }

        [Fact]
        public async Task GetSleepListPage_Should_Sort_By_Oldest_First_When_SortBy_Is_Zero()
        {
            // Arrange
            List<Sleep> allSleeps =
            [
                new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-10) },
                new() { SleepId = 2, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-5) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepListPage(pageSize: 10, pageIndex: 1, progenyId: TestProgenyId, sortBy: 0);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SleepListPage model = Assert.IsType<SleepListPage>(okResult.Value);
            Assert.Equal(1, model.SleepList[0].SleepNumber);
            Assert.Equal(2, model.SleepList[1].SleepNumber);
        }

        [Fact]
        public async Task GetSleepListPage_Should_Sort_By_Newest_First_When_SortBy_Is_One()
        {
            // Arrange
            List<Sleep> allSleeps =
            [
                new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-10) },
                new() { SleepId = 2, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-5) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepListPage(pageSize: 10, pageIndex: 1, progenyId: TestProgenyId, sortBy: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SleepListPage model = Assert.IsType<SleepListPage>(okResult.Value);
            Assert.Equal(2, model.SleepList[0].SleepNumber);
            Assert.Equal(1, model.SleepList[1].SleepNumber);
        }

        [Fact]
        public async Task GetSleepListPage_Should_Set_PageIndex_To_One_When_Less_Than_One()
        {
            // Arrange
            List<Sleep> allSleeps = [new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepListPage(pageSize: 8, pageIndex: 0, progenyId: TestProgenyId);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SleepListPage model = Assert.IsType<SleepListPage>(okResult.Value);
            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public async Task GetSleepListPage_Should_Use_Default_Values_When_Not_Provided()
        {
            // Arrange
            List<Sleep> allSleeps = [new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow }];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleepList(Constants.DefaultChildId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepListPage();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            SleepListPage model = Assert.IsType<SleepListPage>(okResult.Value);
            Assert.Single(model.SleepList);
        }

        #endregion

        #region GetSleepDetails Tests

        [Fact]
        public async Task GetSleepDetails_Should_Return_Ok_With_Sleep_List()
        {
            // Arrange
            List<Sleep> allSleeps =
            [
                new() { SleepId = 1, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-10) },
                _testSleep,
                new() { SleepId = 3, ProgenyId = TestProgenyId, SleepStart = DateTime.UtcNow.AddDays(-5) }
            ];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepDetails(TestSleepId, sortOrder: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            List<Sleep> returnedList = Assert.IsAssignableFrom<List<Sleep>>(okResult.Value);
            Assert.NotEmpty(returnedList);
        }

        [Fact]
        public async Task GetSleepDetails_Should_Use_User_Timezone()
        {
            // Arrange
            List<Sleep> allSleeps = [_testSleep];

            _mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);
            _mockSleepService.Setup(x => x.GetSleep(TestSleepId, _testUser))
                .ReturnsAsync(_testSleep);
            _mockSleepService.Setup(x => x.GetSleepList(TestProgenyId, _testUser))
                .ReturnsAsync(allSleeps);

            // Act
            IActionResult result = await _controller.GetSleepDetails(TestSleepId, sortOrder: 1);

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<List<Sleep>>(okResult.Value);
            _mockUserInfoService.Verify(x => x.GetUserInfoByUserId(TestUserId), Times.Once);
        }

        #endregion
    }
}