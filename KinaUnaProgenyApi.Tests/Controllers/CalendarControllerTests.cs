using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.DTOs;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.FamiliesServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class CalendarControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IAzureNotifications> _mockAzureNotifications;
        private readonly Mock<ICalendarService> _mockCalendarService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<IFamiliesService> _mockFamiliesService;
        private readonly Mock<IWebNotificationsService> _mockWebNotificationsService;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly CalendarController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly Family _testFamily;
        private readonly CalendarItem _testCalendarItem;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestCalendarItemId = 100;

        public CalendarControllerTests()
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
                ProfilePicture = "profile.jpg"
            };

            _testProgeny = new Progeny
            {
                Id = TestProgenyId,
                Name = "Test Progeny",
                NickName = "Testy",
                BirthDay = DateTime.UtcNow.AddYears(-2)
            };

            _testFamily = new Family
            {
                FamilyId = TestFamilyId,
                Name = "Test Family"
            };

            _testCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Test Event",
                Notes = "Test notes",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                AllDay = false,
                Author = TestUserId,
                CreatedBy = TestUserId,
                AccessLevel = 0,
                UId = Guid.NewGuid().ToString()
            };

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemId = TestCalendarItemId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ProgenyTime = (DateTime)_testCalendarItem.StartTime,
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow
            };

            // Setup mocks
            _mockAzureNotifications = new Mock<IAzureNotifications>();
            Mock<IUserInfoService> mockUserInfoService = new();
            _mockCalendarService = new Mock<ICalendarService>();
            _mockTimelineService = new Mock<ITimelineService>();
            _mockProgenyService = new Mock<IProgenyService>();
            _mockFamiliesService = new Mock<IFamiliesService>();
            _mockWebNotificationsService = new Mock<IWebNotificationsService>();
            _mockAccessManagementService = new Mock<IAccessManagementService>();

            // Setup default mock behaviors
            mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Initialize controller
            _controller = new CalendarController(
                _mockAzureNotifications.Object,
                mockUserInfoService.Object,
                _mockCalendarService.Object,
                _mockTimelineService.Object,
                _mockProgenyService.Object,
                _mockFamiliesService.Object,
                _mockWebNotificationsService.Object,
                _mockAccessManagementService.Object
            );

            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, TestUserId),
                new Claim(ClaimTypes.Email, TestUserEmail)
            ];
            ClaimsIdentity identity = new(claims, "TestAuthType");
            ClaimsPrincipal claimsPrincipal = new(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region Progenies Tests

        [Fact]
        public async Task Progenies_ReturnsOkWithCalendarItems_WhenValidProgenyIdsProvided()
        {
            // Arrange
            var request = new CalendarItemsRequest
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            var expectedItems = new List<CalendarItem> { _testCalendarItem };
            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, 0, _testUser, request.StartDate, request.EndDate))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _controller.Progenies(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<CalendarItem>>(okResult.Value);
            Assert.Single(returnedItems);
            Assert.Equal(_testCalendarItem.EventId, returnedItems[0].EventId);
        }

        [Fact]
        public async Task Progenies_ReturnsOkWithCalendarItems_WhenValidFamilyIdsProvided()
        {
            // Arrange
            var familyCalendarItem = new CalendarItem
            {
                EventId = 200,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = DateTime.UtcNow.AddDays(5),
                EndTime = DateTime.UtcNow.AddDays(5).AddHours(1),
                UId = Guid.NewGuid().ToString()
            };

            var request = new CalendarItemsRequest
            {
                ProgenyIds = [],
                FamilyIds = [TestFamilyId],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            var expectedItems = new List<CalendarItem> { familyCalendarItem };
            _mockCalendarService.Setup(x => x.GetCalendarList(0, TestFamilyId, _testUser, request.StartDate, request.EndDate))
                .ReturnsAsync(expectedItems);

            // Act
            var result = await _controller.Progenies(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<CalendarItem>>(okResult.Value);
            Assert.Single(returnedItems);
            Assert.Equal(familyCalendarItem.EventId, returnedItems[0].EventId);
        }

        [Fact]
        public async Task Progenies_ReturnsOkWithCombinedItems_WhenBothProgenyAndFamilyIdsProvided()
        {
            // Arrange
            var familyCalendarItem = new CalendarItem
            {
                EventId = 200,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = DateTime.UtcNow.AddDays(5),
                EndTime = DateTime.UtcNow.AddDays(5).AddHours(1),
                UId = Guid.NewGuid().ToString()
            };

            var request = new CalendarItemsRequest
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [TestFamilyId],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            _mockCalendarService.Setup(x => x.GetCalendarList(TestProgenyId, 0, _testUser, request.StartDate, request.EndDate))
                .ReturnsAsync(new List<CalendarItem> { _testCalendarItem });

            _mockCalendarService.Setup(x => x.GetCalendarList(0, TestFamilyId, _testUser, request.StartDate, request.EndDate))
                .ReturnsAsync(new List<CalendarItem> { familyCalendarItem });

            // Act
            var result = await _controller.Progenies(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<CalendarItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        [Fact]
        public async Task Progenies_ReturnsOkWithEmptyList_WhenNoAccessibleProgeniesOrFamilies()
        {
            // Arrange
            var request = new CalendarItemsRequest
            {
                ProgenyIds = [TestProgenyId],
                FamilyIds = [TestFamilyId],
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync((Progeny)null!);

            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync((Family)null!);

            // Act
            var result = await _controller.Progenies(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<CalendarItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region GetCalendarItem Tests

        [Fact]
        public async Task GetCalendarItem_ReturnsOk_WhenCalendarItemExists()
        {
            // Arrange
            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            // Act
            var result = await _controller.GetCalendarItem(TestCalendarItemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<CalendarItem>(okResult.Value);
            Assert.Equal(TestCalendarItemId, returnedItem.EventId);
            Assert.Equal(_testCalendarItem.Title, returnedItem.Title);
        }

        [Fact]
        public async Task GetCalendarItem_ReturnsNotFound_WhenCalendarItemDoesNotExist()
        {
            // Arrange
            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.GetCalendarItem(TestCalendarItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_ReturnsOk_WhenValidProgenyCalendarItemProvided()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                UId = Guid.NewGuid().ToString()
            };

            var addedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = newCalendarItem.StartTime,
                EndTime = newCalendarItem.EndTime,
                Author = TestUserId,
                CreatedBy = TestUserId,
                UId = newCalendarItem.UId
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.AddCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync(addedCalendarItem);

            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            var result = await _controller.Post(newCalendarItem);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<CalendarItem>(okResult.Value);
            Assert.Equal(TestCalendarItemId, returnedItem.EventId);
            Assert.Equal(TestUserId, returnedItem.Author);
            Assert.Equal(TestUserId, returnedItem.CreatedBy);
        }

        [Fact]
        public async Task Post_ReturnsOk_WhenValidFamilyCalendarItemProvided()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                UId = Guid.NewGuid().ToString()
            };

            var addedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = newCalendarItem.StartTime,
                EndTime = newCalendarItem.EndTime,
                Author = TestUserId,
                CreatedBy = TestUserId,
                UId = newCalendarItem.UId
            };

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.AddCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync(addedCalendarItem);

            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            var result = await _controller.Post(newCalendarItem);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<CalendarItem>(okResult.Value);
            Assert.Equal(TestCalendarItemId, returnedItem.EventId);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenBothProgenyIdAndFamilyIdAreSet()
        {
            // Arrange
            var invalidCalendarItem = new CalendarItem
            {
                ProgenyId = TestProgenyId,
                FamilyId = TestFamilyId,
                Title = "Invalid Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            // Act
            var result = await _controller.Post(invalidCalendarItem);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A calendar event must have either a ProgenyId or a FamilyId set, but not both.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenNeitherProgenyIdNorFamilyIdAreSet()
        {
            // Arrange
            var invalidCalendarItem = new CalendarItem
            {
                ProgenyId = 0,
                FamilyId = 0,
                Title = "Invalid Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            // Act
            var result = await _controller.Post(invalidCalendarItem);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("A calendar event must have either a ProgenyId or a FamilyId set.", badRequestResult.Value);
        }

        [Fact]
        public async Task Post_ReturnsUnauthorized_WhenUserLacksProgenyAddPermission()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Post(newCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsUnauthorized_WhenUserLacksFamilyAddPermission()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            _mockAccessManagementService.Setup(x => x.HasFamilyPermission(TestFamilyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Post(newCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsUnauthorized_WhenCalendarServiceReturnsNull()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2)
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.AddCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Post(newCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Post_SendsNotifications_WhenCalendarItemIsAdded()
        {
            // Arrange
            var newCalendarItem = new CalendarItem
            {
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                UId = Guid.NewGuid().ToString()
            };

            var addedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "New Event",
                StartTime = newCalendarItem.StartTime,
                EndTime = newCalendarItem.EndTime,
                Author = TestUserId,
                CreatedBy = TestUserId,
                UId = newCalendarItem.UId
            };

            _mockAccessManagementService.Setup(x => x.HasProgenyPermission(TestProgenyId, _testUser, PermissionLevel.Add))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.AddCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync(addedCalendarItem);

            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Post(newCalendarItem);

            // Assert
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeLineItem>(),
                _testUser.ProfilePicture), Times.Once);

            _mockWebNotificationsService.Verify(x => x.SendCalendarNotification(
                addedCalendarItem,
                _testUser,
                It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_ReturnsOk_WhenValidUpdateProvided()
        {
            // Arrange
            var updatedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Event",
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(2).AddHours(3),
                UId = _testCalendarItem.UId
            };

            var existingCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = _testCalendarItem.Title,
                StartTime = _testCalendarItem.StartTime,
                EndTime = _testCalendarItem.EndTime,
                Author = TestUserId,
                CreatedBy = TestUserId,
                UId = _testCalendarItem.UId,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(existingCalendarItem);

            _mockCalendarService.Setup(x => x.UpdateCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync(updatedCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            var result = await _controller.Put(TestCalendarItemId, updatedCalendarItem);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<CalendarItem>(okResult.Value);
            Assert.Equal(updatedCalendarItem.Title, returnedItem.Title);
            Assert.Equal(TestUserId, updatedCalendarItem.ModifiedBy);
        }

        [Fact]
        public async Task Put_ReturnsUnauthorized_WhenCalendarItemNotFound()
        {
            // Arrange
            var updatedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Event"
            };

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Put(TestCalendarItemId, updatedCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsUnauthorized_WhenUserLacksEditPermission()
        {
            // Arrange
            var updatedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Event"
            };

            var existingCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = _testCalendarItem.Title,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.View
                }
            };

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(existingCalendarItem);

            // Act
            var result = await _controller.Put(TestCalendarItemId, updatedCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsUnauthorized_WhenCalendarServiceUpdateFails()
        {
            // Arrange
            var updatedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Event"
            };

            var existingCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = _testCalendarItem.Title,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(existingCalendarItem);

            _mockCalendarService.Setup(x => x.UpdateCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Put(TestCalendarItemId, updatedCalendarItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Put_UpdatesTimeLineItem_WhenTimeLineItemExists()
        {
            // Arrange
            var updatedCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = "Updated Event",
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(2).AddHours(3),
                UId = _testCalendarItem.UId
            };

            var existingCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                Title = _testCalendarItem.Title,
                ItemPerMission = new TimelineItemPermission
                {
                    PermissionLevel = PermissionLevel.Edit
                }
            };

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(existingCalendarItem);

            _mockCalendarService.Setup(x => x.UpdateCalendarItem(It.IsAny<CalendarItem>(), _testUser))
                .ReturnsAsync(updatedCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            await _controller.Put(TestCalendarItemId, updatedCalendarItem);

            // Assert
            _mockTimelineService.Verify(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser), Times.Once);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenCalendarItemDeletedSuccessfully()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(_testCalendarItem, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            var result = await _controller.Delete(TestCalendarItemId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(TestUserId, _testCalendarItem.ModifiedBy);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenUserLacksAdminPermission()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(TestCalendarItemId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenCalendarItemDoesNotExist()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Delete(TestCalendarItemId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenCalendarServiceDeleteFails()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(_testCalendarItem, _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Delete(TestCalendarItemId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Delete_DeletesTimeLineItem_WhenTimeLineItemExists()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(_testCalendarItem, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestCalendarItemId);

            // Assert
            _mockTimelineService.Verify(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser), Times.Once);
        }

        [Fact]
        public async Task Delete_SendsProgenyNotifications_WhenProgenyCalendarItemDeleted()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(_testCalendarItem, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            // Act
            await _controller.Delete(TestCalendarItemId);

            // Assert
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.Is<string>(s => s.Contains(_testProgeny.NickName)),
                It.Is<string>(s => s.Contains(_testUser.FirstName) && s.Contains(_testCalendarItem.Title)),
                _testTimeLineItem,
                _testUser.ProfilePicture), Times.Once);

            _mockWebNotificationsService.Verify(x => x.SendCalendarNotification(
                _testCalendarItem,
                _testUser,
                It.Is<string>(s => s.Contains(_testProgeny.NickName))), Times.Once);
        }

        [Fact]
        public async Task Delete_SendsFamilyNotifications_WhenFamilyCalendarItemDeleted()
        {
            // Arrange
            var familyCalendarItem = new CalendarItem
            {
                EventId = TestCalendarItemId,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                Title = "Family Event",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Author = TestUserId,
                UId = Guid.NewGuid().ToString()
            };

            var familyTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                ItemId = TestCalendarItemId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Calendar,
                ProgenyTime = (DateTime)familyCalendarItem.StartTime,
                CreatedBy = TestUserId
            };

            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(familyCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(familyCalendarItem, _testUser))
                .ReturnsAsync(familyCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync(familyTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(familyTimeLineItem, _testUser))
                .ReturnsAsync(familyTimeLineItem);

            _mockFamiliesService.Setup(x => x.GetFamilyById(TestFamilyId, _testUser))
                .ReturnsAsync(_testFamily);

            // Act
            await _controller.Delete(TestCalendarItemId);

            // Assert
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.Is<string>(s => s.Contains(_testFamily.Name)),
                It.Is<string>(s => s.Contains(_testUser.FirstName) && s.Contains(familyCalendarItem.Title)),
                familyTimeLineItem,
                _testUser.ProfilePicture), Times.Once);

            _mockWebNotificationsService.Verify(x => x.SendCalendarNotification(
                familyCalendarItem,
                _testUser,
                It.Is<string>(s => s.Contains(_testFamily.Name))), Times.Once);
        }

        [Fact]
        public async Task Delete_DoesNotSendNotifications_WhenTimeLineItemIsNull()
        {
            // Arrange
            _mockAccessManagementService.Setup(x => x.HasItemPermission(
                KinaUnaTypes.TimeLineType.Calendar,
                TestCalendarItemId,
                _testUser,
                PermissionLevel.Admin))
                .ReturnsAsync(true);

            _mockCalendarService.Setup(x => x.GetCalendarItem(TestCalendarItemId, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockCalendarService.Setup(x => x.DeleteCalendarItem(_testCalendarItem, _testUser))
                .ReturnsAsync(_testCalendarItem);

            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId(
                TestCalendarItemId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Calendar,
                _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            await _controller.Delete(TestCalendarItemId);

            // Assert
            _mockAzureNotifications.Verify(x => x.ProgenyUpdateNotification(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeLineItem>(),
                It.IsAny<string>()), Times.Never);

            _mockWebNotificationsService.Verify(x => x.SendCalendarNotification(
                It.IsAny<CalendarItem>(),
                It.IsAny<UserInfo>(),
                It.IsAny<string>()), Times.Never);
        }

        #endregion

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}