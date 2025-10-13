using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Controllers;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace KinaUnaProgenyApi.Tests.Controllers
{
    public class TimeLineControllerTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly Mock<IProgenyService> _mockProgenyService;
        private readonly Mock<ITimelineService> _mockTimelineService;
        private readonly Mock<ICalendarService> _mockCalendarService;
        private readonly TimeLineController _controller;

        private readonly UserInfo _testUser;
        private readonly Progeny _testProgeny;
        private readonly TimeLineItem _testTimeLineItem;

        private const string TestUserEmail = Constants.DefaultUserEmail;
        private const string TestUserId = Constants.DefaultUserId;
        private const int TestProgenyId = 1;
        private const int TestFamilyId = 1;
        private const int TestTimeLineId = 100;

        public TimeLineControllerTests()
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

            _testTimeLineItem = new TimeLineItem
            {
                TimeLineId = TestTimeLineId,
                ProgenyId = TestProgenyId,
                FamilyId = 0,
                ItemId = "1",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(-1),
                CreatedBy = TestUserId,
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };
            
            // Setup mocks
            _mockProgenyService = new Mock<IProgenyService>();
            _mockTimelineService = new Mock<ITimelineService>();
            Mock<IUserInfoService> mockUserInfoService = new();
            _mockCalendarService = new Mock<ICalendarService>();

            // Setup default mock behaviors
            mockUserInfoService.Setup(x => x.GetUserInfoByUserId(TestUserId))
                .ReturnsAsync(_testUser);

            // Initialize controller
            _controller = new TimeLineController(
                _mockProgenyService.Object,
                _mockTimelineService.Object,
                mockUserInfoService.Object,
                _mockCalendarService.Object
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

        #region GetTimeLineRequestData Tests

        [Fact]
        public async Task GetTimeLineRequestData_ReturnsOk_WhenValidRequestProvided()
        {
            // Arrange
            var request = new TimelineRequest
            {
                ProgenyId = TestProgenyId,
                Progenies = [TestProgenyId],
                Families = [],
                TimelineStartDateTime = DateTime.UtcNow.AddDays(-30),
                SortOrder = 0
            };

            var expectedResponse = new TimelineResponse
            {
                TimeLineItems = [_testTimeLineItem],
                Request = request
            };

            _mockTimelineService.Setup(x => x.GetTimelineData(It.IsAny<TimelineRequest>(), _testUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetTimeLineRequestData(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponse = Assert.IsType<TimelineResponse>(okResult.Value);
            Assert.Single(returnedResponse.TimeLineItems);
        }

        [Fact]
        public async Task GetTimeLineRequestData_AdjustsStartDateTime_WhenSortOrderIsDescending()
        {
            // Arrange
            var startDate = new DateTime(2023, 5, 15, 10, 30, 0);
            var request = new TimelineRequest
            {
                ProgenyId = TestProgenyId,
                Progenies = [TestProgenyId],
                Families = [],
                TimelineStartDateTime = startDate,
                SortOrder = 1
            };

            TimelineRequest? capturedRequest = null;
            _mockTimelineService.Setup(x => x.GetTimelineData(It.IsAny<TimelineRequest>(), _testUser))
                .Callback<TimelineRequest, UserInfo>((req, _) => capturedRequest = req)
                .ReturnsAsync(new TimelineResponse { TimeLineItems = [], Request = request });

            // Act
            await _controller.GetTimeLineRequestData(request);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal(new DateTime(2023, 5, 15, 23, 59, 59), capturedRequest.TimelineStartDateTime);
        }
        
        #endregion

        #region Progeny Tests

        [Fact]
        public async Task Progeny_ReturnsOk_WithTimeLineItems()
        {
            // Arrange
            var timeLineItems = new List<TimeLineItem> { _testTimeLineItem };
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timeLineItems);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Progeny(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Single(returnedItems);
        }

        [Fact]
        public async Task Progeny_FiltersOutFutureItems()
        {
            // Arrange
            var futureItem = new TimeLineItem
            {
                TimeLineId = 2,
                ProgenyId = TestProgenyId,
                ItemId = "2",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(10)
            };

            var timeLineItems = new List<TimeLineItem> { _testTimeLineItem, futureItem };
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timeLineItems);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Progeny(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Single(returnedItems);
            Assert.DoesNotContain(returnedItems, item => item.TimeLineId == 2);
        }

        [Fact]
        public async Task Progeny_IncludesRecurringCalendarItems()
        {
            // Arrange
            var timeLineItems = new List<TimeLineItem> { _testTimeLineItem };
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timeLineItems);

            var recurringItem = new CalendarItem
            {
                EventId = 10,
                ProgenyId = TestProgenyId,
                StartTime = DateTime.UtcNow.AddDays(-5),
                EndTime = DateTime.UtcNow.AddDays(-5).AddHours(1),
                Title = "Recurring Event"
            };

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem> { recurringItem });

            _mockCalendarService.Setup(x => x.GetCalendarItem(10, _testUser))
                .ReturnsAsync(recurringItem);

            // Act
            var result = await _controller.Progeny(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        [Fact]
        public async Task Progeny_SkipsRecurringItemsWithoutOriginal()
        {
            // Arrange
            var timeLineItems = new List<TimeLineItem> { _testTimeLineItem };
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(timeLineItems);

            var recurringItem = new CalendarItem
            {
                EventId = 10,
                ProgenyId = TestProgenyId,
                StartTime = DateTime.UtcNow.AddDays(-5)
            };

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem> { recurringItem });

            _mockCalendarService.Setup(x => x.GetCalendarItem(10, _testUser))
                .ReturnsAsync((CalendarItem)null!);

            // Act
            var result = await _controller.Progeny(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Single(returnedItems);
        }

        [Fact]
        public async Task Progeny_ReturnsEmptyList_WhenNoItems()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<TimeLineItem>());

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Progeny(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region Family Tests

        [Fact]
        public async Task Family_ReturnsOk_WithTimeLineItems()
        {
            // Arrange
            var familyTimeLineItem = new TimeLineItem
            {
                TimeLineId = 1,
                ProgenyId = 0,
                FamilyId = TestFamilyId,
                ItemId = "1",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(-1)
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId, _testUser))
                .ReturnsAsync(new List<TimeLineItem> { familyTimeLineItem });

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(0, TestFamilyId, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Family(TestFamilyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Single(returnedItems);
            Assert.Equal(TestFamilyId, returnedItems[0].FamilyId);
        }

        [Fact]
        public async Task Family_FiltersOutFutureItems()
        {
            // Arrange
            var pastItem = new TimeLineItem
            {
                TimeLineId = 1,
                FamilyId = TestFamilyId,
                ProgenyTime = DateTime.UtcNow.AddDays(-1)
            };

            var futureItem = new TimeLineItem
            {
                TimeLineId = 2,
                FamilyId = TestFamilyId,
                ProgenyTime = DateTime.UtcNow.AddDays(10)
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId, _testUser))
                .ReturnsAsync(new List<TimeLineItem> { pastItem, futureItem });

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(0, TestFamilyId, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Family(TestFamilyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Single(returnedItems);
            Assert.Equal(1, returnedItems[0].TimeLineId);
        }

        #endregion

        #region Progenies Tests

        [Fact]
        public async Task Progenies_ReturnsOk_WithCombinedTimeLineItems()
        {
            // Arrange
            var progenies = new List<int> { TestProgenyId, TestProgenyId + 1 };
            
            var progeny1Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = DateTime.UtcNow.AddDays(-1) }
            };

            var progeny2Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 2, ProgenyId = TestProgenyId + 1, ProgenyTime = DateTime.UtcNow.AddDays(-2) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(progeny1Items);

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId + 1, 0, _testUser))
                .ReturnsAsync(progeny2Items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(It.IsAny<int>(), 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Progenies(progenies);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        [Fact]
        public async Task Progenies_ReturnsEmptyList_WhenNoProgeniesProvided()
        {
            // Arrange
            var progenies = new List<int>();

            // Act
            var result = await _controller.Progenies(progenies);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region Families Tests

        [Fact]
        public async Task Families_ReturnsOk_WithCombinedTimeLineItems()
        {
            // Arrange
            var families = new List<int> { TestFamilyId, TestFamilyId + 1 };
            
            var family1Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, FamilyId = TestFamilyId, ProgenyTime = DateTime.UtcNow.AddDays(-1) }
            };

            var family2Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 2, FamilyId = TestFamilyId + 1, ProgenyTime = DateTime.UtcNow.AddDays(-2) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId, _testUser))
                .ReturnsAsync(family1Items);

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId + 1, _testUser))
                .ReturnsAsync(family2Items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(0, It.IsAny<int>(), _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.Families(families);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        #endregion

        #region ProgenyLatest Tests

        [Fact]
        public async Task ProgenyLatest_ReturnsOk_WithLimitedItems()
        {
            // Arrange
            var items = new List<TimeLineItem>();
            for (int i = 0; i < 10; i++)
            {
                items.Add(new TimeLineItem
                {
                    TimeLineId = i + 1,
                    ProgenyId = TestProgenyId,
                    ProgenyTime = DateTime.UtcNow.AddDays(-i - 1)
                });
            }

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgenyLatest(TestProgenyId, count: 5, start: 0);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<TimeLineItem>>(okResult.Value);
            Assert.Equal(5, returnedItems.Count());
        }

        [Fact]
        public async Task ProgenyLatest_RespectsSkipParameter()
        {
            // Arrange
            var items = new List<TimeLineItem>();
            for (int i = 0; i < 10; i++)
            {
                items.Add(new TimeLineItem
                {
                    TimeLineId = i + 1,
                    ProgenyId = TestProgenyId,
                    ProgenyTime = DateTime.UtcNow.AddDays(-i - 1)
                });
            }

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgenyLatest(TestProgenyId, count: 3, start: 2);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<TimeLineItem>>(okResult.Value).ToList();
            Assert.Equal(3, returnedItems.Count);
        }

        [Fact]
        public async Task ProgenyLatest_ReturnsReversedOrder()
        {
            // Arrange
            var items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = DateTime.UtcNow.AddDays(-10) },
                new() { TimeLineId = 2, ProgenyId = TestProgenyId, ProgenyTime = DateTime.UtcNow.AddDays(-5) },
                new() { TimeLineId = 3, ProgenyId = TestProgenyId, ProgenyTime = DateTime.UtcNow.AddDays(-1) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgenyLatest(TestProgenyId, count: 5, start: 0);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<TimeLineItem>>(okResult.Value).ToList();
            Assert.Equal(3, returnedItems[0].TimeLineId); // Most recent first
        }

        [Fact]
        public async Task ProgenyLatest_ReturnsEmptyList_WhenNoItems()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<TimeLineItem>());

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsLatestPosts(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgenyLatest(TestProgenyId, count: 5, start: 0);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<IEnumerable<TimeLineItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        #endregion

        #region ProgenyYearAgo Tests

        [Fact]
        public async Task ProgenyYearAgo_ReturnsOk_WithMatchingDayAndMonthItems()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 10, 0, 0) },
                new() { TimeLineId = 2, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 2, today.Month, today.Day, 14, 0, 0) },
                new() { TimeLineId = 3, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day + 1, 10, 0, 0) } // Different day
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            // Act
            var result = await _controller.ProgenyYearAgo(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
            Assert.DoesNotContain(returnedItems, item => item.TimeLineId == 3);
        }

        [Fact]
        public async Task ProgenyYearAgo_ReturnsEmptyList_WhenNoMatchingItems()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<TimeLineItem>());

            // Act
            var result = await _controller.ProgenyYearAgo(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Empty(returnedItems);
        }

        [Fact]
        public async Task ProgenyYearAgo_ReturnsReversedOrder()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 3, today.Month, today.Day, 10, 0, 0) },
                new() { TimeLineId = 2, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 14, 0, 0) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            // Act
            var result = await _controller.ProgenyYearAgo(TestProgenyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems[0].TimeLineId); // Most recent first
        }

        #endregion

        #region ProgeniesYearAgo Tests

        [Fact]
        public async Task ProgeniesYearAgo_ReturnsOk_WithCombinedItems()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var progenies = new List<int> { TestProgenyId, TestProgenyId + 1 };
            
            var progeny1Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 10, 0, 0) }
            };

            var progeny2Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 2, ProgenyId = TestProgenyId + 1, ProgenyTime = new DateTime(today.Year - 2, today.Month, today.Day, 14, 0, 0) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(progeny1Items);

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId + 1, 0, _testUser))
                .ReturnsAsync(progeny2Items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsOnThisDay(It.IsAny<int>(), 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgeniesYearAgo(progenies);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        [Fact]
        public async Task ProgeniesYearAgo_IncludesRecurringCalendarItems()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var progenies = new List<int> { TestProgenyId };
            
            var progenyItems = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 10, 0, 0) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(progenyItems);

            var recurringItem = new CalendarItem
            {
                EventId = 10,
                ProgenyId = TestProgenyId,
                StartTime = new DateTime(today.Year - 1, today.Month, today.Day, 12, 0, 0),
                Title = "Recurring Event"
            };

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsOnThisDay(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem> { recurringItem });

            _mockCalendarService.Setup(x => x.GetCalendarItem(10, _testUser))
                .ReturnsAsync(recurringItem);

            // Act
            var result = await _controller.ProgeniesYearAgo(progenies);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        [Fact]
        public async Task ProgeniesYearAgo_ReturnsSortedDescending()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var progenies = new List<int> { TestProgenyId };
            
            var items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 3, today.Month, today.Day, 10, 0, 0) },
                new() { TimeLineId = 2, ProgenyId = TestProgenyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 14, 0, 0) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(TestProgenyId, 0, _testUser))
                .ReturnsAsync(items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsOnThisDay(TestProgenyId, 0, _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.ProgeniesYearAgo(progenies);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems[0].TimeLineId); // Most recent first
        }

        #endregion

        #region FamiliesYearAgo Tests

        [Fact]
        public async Task FamiliesYearAgo_ReturnsOk_WithCombinedItems()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var families = new List<int> { TestFamilyId, TestFamilyId + 1 };
            
            var family1Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 1, FamilyId = TestFamilyId, ProgenyTime = new DateTime(today.Year - 1, today.Month, today.Day, 10, 0, 0) }
            };

            var family2Items = new List<TimeLineItem>
            {
                new() { TimeLineId = 2, FamilyId = TestFamilyId + 1, ProgenyTime = new DateTime(today.Year - 2, today.Month, today.Day, 14, 0, 0) }
            };

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId, _testUser))
                .ReturnsAsync(family1Items);

            _mockTimelineService.Setup(x => x.GetTimeLineList(0, TestFamilyId + 1, _testUser))
                .ReturnsAsync(family2Items);

            _mockCalendarService.Setup(x => x.GetRecurringCalendarItemsOnThisDay(0, It.IsAny<int>(), _testUser))
                .ReturnsAsync(new List<CalendarItem>());

            // Act
            var result = await _controller.FamiliesYearAgo(families);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItems = Assert.IsAssignableFrom<List<TimeLineItem>>(okResult.Value);
            Assert.Equal(2, returnedItems.Count);
        }

        #endregion

        #region GetTimeLineItemByItemId Tests

        [Fact]
        public async Task GetTimeLineItemByItemId_ReturnsOk_WhenItemExists()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.Photo, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            var result = await _controller.GetTimeLineItemByItemId("1", (int)KinaUnaTypes.TimeLineType.Photo);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<TimeLineItem>(okResult.Value);
            Assert.Equal(_testTimeLineItem.TimeLineId, returnedItem.TimeLineId);
        }

        [Fact]
        public async Task GetTimeLineItemByItemId_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItemByItemId("999", (int)KinaUnaTypes.TimeLineType.Photo, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.GetTimeLineItemByItemId("999", (int)KinaUnaTypes.TimeLineType.Photo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region GetTimeLineItem Tests

        [Fact]
        public async Task GetTimeLineItem_ReturnsOk_WhenItemExists()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            var result = await _controller.GetTimeLineItem(TestTimeLineId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<TimeLineItem>(okResult.Value);
            Assert.Equal(TestTimeLineId, returnedItem.TimeLineId);
        }

        [Fact]
        public async Task GetTimeLineItem_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItem(999, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.GetTimeLineItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Post Tests

        [Fact]
        public async Task Post_ReturnsOk_WhenValidItemProvided()
        {
            // Arrange
            var newItem = new TimeLineItem
            {
                ProgenyId = TestProgenyId,
                ItemId = "10",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(-5)
            };

            var addedItem = new TimeLineItem
            {
                TimeLineId = 150,
                ProgenyId = TestProgenyId,
                ItemId = "10",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = newItem.ProgenyTime,
                CreatedBy = TestUserId
            };

            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync(addedItem);

            // Act
            var result = await _controller.Post(newItem);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<TimeLineItem>(okResult.Value);
            Assert.Equal(150, returnedItem.TimeLineId);
        }

        [Fact]
        public async Task Post_ReturnsUnauthorized_WhenServiceReturnsNull()
        {
            // Arrange
            var newItem = new TimeLineItem
            {
                ProgenyId = TestProgenyId,
                ItemId = "10",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(-5)
            };

            _mockTimelineService.Setup(x => x.AddTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.Post(newItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Put Tests

        [Fact]
        public async Task Put_ReturnsOk_WhenValidUpdateProvided()
        {
            // Arrange
            var updatedItem = new TimeLineItem
            {
                TimeLineId = TestTimeLineId,
                ProgenyId = TestProgenyId,
                ItemId = "1",
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                ProgenyTime = DateTime.UtcNow.AddDays(-2)
            };

            
            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, It.IsAny<UserInfo>()))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), It.IsAny<UserInfo>()))
                .ReturnsAsync(updatedItem);

            // Act
            var result = await _controller.Put(TestTimeLineId, updatedItem);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<TimeLineItem>(okResult.Value);
            Assert.Equal(updatedItem.ProgenyTime.Date, returnedItem.ProgenyTime.Date);
            Assert.Equal(updatedItem.ProgenyTime.Hour, returnedItem.ProgenyTime.Hour);
            Assert.Equal(updatedItem.ProgenyTime.Minute, returnedItem.ProgenyTime.Minute);
        }

        [Fact]
        public async Task Put_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var updatedItem = new TimeLineItem
            {
                TimeLineId = TestTimeLineId,
                ProgenyId = TestProgenyId
            };

            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.Put(TestTimeLineId, updatedItem);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsUnauthorized_WhenServiceUpdateFails()
        {
            // Arrange
            var updatedItem = new TimeLineItem
            {
                TimeLineId = TestTimeLineId,
                ProgenyId = TestProgenyId
            };

            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.UpdateTimeLineItem(It.IsAny<TimeLineItem>(), _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.Put(TestTimeLineId, updatedItem);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeletedSuccessfully()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            // Act
            var result = await _controller.Delete(TestTimeLineId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.Delete(TestTimeLineId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsUnauthorized_WhenServiceDeleteFails()
        {
            // Arrange
            _mockTimelineService.Setup(x => x.GetTimeLineItem(TestTimeLineId, _testUser))
                .ReturnsAsync(_testTimeLineItem);

            _mockTimelineService.Setup(x => x.DeleteTimeLineItem(_testTimeLineItem, _testUser))
                .ReturnsAsync((TimeLineItem)null!);

            // Act
            var result = await _controller.Delete(TestTimeLineId);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region GetOnThisDayTimeLineItems Tests

        [Fact]
        public async Task GetOnThisDayTimeLineItems_ReturnsOk_WhenValidRequestProvided()
        {
            // Arrange
            var request = new OnThisDayRequest
            {
                ProgenyId = TestProgenyId,
                Progenies = [TestProgenyId],
                Families = [],
                ThisDayDateTime = DateTime.UtcNow,
                SortOrder = 0
            };

            var expectedResponse = new OnThisDayResponse
            {
                TimeLineItems = [_testTimeLineItem]
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            _mockTimelineService.Setup(x => x.GetOnThisDayData(It.IsAny<OnThisDayRequest>(), _testUser))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetOnThisDayTimeLineItems(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponse = Assert.IsType<OnThisDayResponse>(okResult.Value);
            Assert.Single(returnedResponse.TimeLineItems);
        }

        [Fact]
        public async Task GetOnThisDayTimeLineItems_ReturnsEmptyResponse_WhenProgenyNotFound()
        {
            // Arrange
            var request = new OnThisDayRequest
            {
                ProgenyId = TestProgenyId,
                ThisDayDateTime = DateTime.UtcNow
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync((Progeny)null!);

            // Act
            var result = await _controller.GetOnThisDayTimeLineItems(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResponse = Assert.IsType<OnThisDayResponse>(okResult.Value);
            Assert.Empty(returnedResponse.TimeLineItems);
        }

        [Fact]
        public async Task GetOnThisDayTimeLineItems_AdjustsDateTime_WhenSortOrderIsDescending()
        {
            // Arrange
            var requestDate = new DateTime(2023, 5, 15, 10, 30, 0);
            var request = new OnThisDayRequest
            {
                ProgenyId = TestProgenyId,
                ThisDayDateTime = requestDate,
                SortOrder = 1
            };

            _mockProgenyService.Setup(x => x.GetProgeny(TestProgenyId, _testUser))
                .ReturnsAsync(_testProgeny);

            OnThisDayRequest? capturedRequest = null;
            _mockTimelineService.Setup(x => x.GetOnThisDayData(It.IsAny<OnThisDayRequest>(), _testUser))
                .Callback<OnThisDayRequest, UserInfo>((req, _) => capturedRequest = req)
                .ReturnsAsync(new OnThisDayResponse { TimeLineItems = [] });

            // Act
            await _controller.GetOnThisDayTimeLineItems(request);

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal(new DateTime(2023, 5, 15, 23, 59, 59), capturedRequest.ThisDayDateTime);
        }

        #endregion

        public void Dispose()
        {
            _progenyDbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}