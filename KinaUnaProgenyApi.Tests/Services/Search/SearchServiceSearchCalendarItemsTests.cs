using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.Search;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.Search
{
    public class SearchServiceSearchCalendarItemsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchCalendarItemsTests()
        {
            // Setup test users
            _testUser = new UserInfo { UserId = "user1", UserEmail = "user1@example.com" };
            _otherUser = new UserInfo { UserId = "user2", UserEmail = "user2@example.com" };

            // Setup in-memory DbContexts (unique DB per test instance)
            DbContextOptions<ProgenyDbContext> progenyOptions = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _progenyDbContext = new ProgenyDbContext(progenyOptions);

            DbContextOptions<MediaDbContext> mediaOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _mediaDbContext = new MediaDbContext(mediaOptions);

            // Setup mocks
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            Mock<ITimelineService> mockTimelineService = new();

            // Initialize service
            _service = new SearchService(
                _progenyDbContext,
                _mediaDbContext,
                _mockAccessManagementService.Object,
                mockTimelineService.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test UserInfo records
            _progenyDbContext.UserInfoDb.Add(_testUser);
            _progenyDbContext.UserInfoDb.Add(_otherUser);

            // Add test CalendarItems for progeny search
            CalendarItem calendarItem1 = new()
            {
                EventId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Birthday Party",
                Notes = "Celebrate with cake",
                Location = "Home",
                Context = "Family",
                StartTime = DateTime.UtcNow.AddDays(7),
                CreatedTime = DateTime.UtcNow.AddDays(-1),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.CalendarDb.Add(calendarItem1);

            CalendarItem calendarItem2 = new()
            {
                EventId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                Title = "Doctor Appointment",
                Notes = "Annual checkup",
                Location = "Hospital",
                Context = "Health",
                StartTime = DateTime.UtcNow.AddDays(14),
                CreatedTime = DateTime.UtcNow.AddDays(-2),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.CalendarDb.Add(calendarItem2);

            CalendarItem calendarItem3 = new()
            {
                EventId = 3,
                ProgenyId = 2,
                FamilyId = 0,
                Title = "School Event",
                Notes = "Parent teacher meeting",
                Location = "School",
                Context = "Education",
                StartTime = DateTime.UtcNow.AddDays(21),
                CreatedTime = DateTime.UtcNow.AddDays(-3),
                Author = "user2",
                CreatedBy = "user2"
            };
            _progenyDbContext.CalendarDb.Add(calendarItem3);

            // Add test CalendarItems for family search
            CalendarItem familyCalendarItem1 = new()
            {
                EventId = 4,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Family Vacation",
                Notes = "Trip to the beach",
                Location = "Beach Resort",
                Context = "Travel",
                StartTime = DateTime.UtcNow.AddDays(30),
                CreatedTime = DateTime.UtcNow.AddDays(-4),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.CalendarDb.Add(familyCalendarItem1);

            CalendarItem familyCalendarItem2 = new()
            {
                EventId = 5,
                ProgenyId = 0,
                FamilyId = 1,
                Title = "Holiday Dinner",
                Notes = "Christmas dinner party",
                Location = "Grandma's House",
                Context = "Family",
                StartTime = DateTime.UtcNow.AddDays(45),
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.CalendarDb.Add(familyCalendarItem2);

            _progenyDbContext.SaveChanges();
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region SearchCalendarItems Tests

        [Fact]
        public async Task SearchCalendarItems_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Birthday",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Birthday Party", result.Results[0].Title);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesTitleField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "party",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Birthday Party", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesNotesField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "cake",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("cake", result.Results[0].Notes);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesLocationField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "hospital",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Hospital", result.Results[0].Location);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesContextField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "health",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Health", result.Results[0].Context);
        }

        [Fact]
        public async Task SearchCalendarItems_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "BIRTHDAY",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Birthday Party", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchCalendarItems_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  Birthday  ",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Birthday Party", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesFamilyItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "vacation",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 4, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 4, 0, 1, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Family Vacation", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchCalendarItems_WhenUserHasNoFamilyPermission_SkipsFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "vacation",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesBothProgenyAndFamily()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "family",
                ProgenyIds = [1],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count); // "Family" context from progeny item + "Family Vacation" or "Holiday Dinner" with "Family" context
        }

        [Fact]
        public async Task SearchCalendarItems_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 1,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.True(result.RemainingItems > 0);
        }

        [Fact]
        public async Task SearchCalendarItems_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Results.Count, result.TotalCount);
        }

        [Fact]
        public async Task SearchCalendarItems_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Sort = 0 // Descending (newest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].StartTime ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].StartTime ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime >= nextTime, "Results should be sorted in descending order");
                }
            }
        }

        [Fact]
        public async Task SearchCalendarItems_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Sort = 1 // Ascending (oldest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].StartTime ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].StartTime ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime <= nextTime, "Results should be sorted in ascending order");
                }
            }
        }

        [Fact]
        public async Task SearchCalendarItems_SkipsItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 1,
                Skip = 1
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // First get all results
            SearchRequest allRequest = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Skip = 0
            };
            SearchResponse<CalendarItem> allResults = await _service.SearchCalendarItems(allRequest, _testUser);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].EventId, result.Results[0].EventId);
            }
        }

        [Fact]
        public async Task SearchCalendarItems_ReturnsAllItemsWhenNumberOfItemsIsZero()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 0,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchCalendarItems_DoesNotReturnFamilyItemsForProgenySearch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "vacation", // Only matches family item
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCalendarItems_DoesNotReturnProgenyItemsForFamilySearch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday", // Only matches progeny item
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchCalendarItems_SearchesMultipleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "school",
                ProgenyIds = [1, 2],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 3, 2, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("School Event", result.Results[0].Title);
        }

        [Fact]
        public async Task SearchCalendarItems_ReturnsEmptyWhenNoMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nonexistentquery12345",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchCalendarItems_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new()
            {
                PermissionLevel = PermissionLevel.Edit,
                TimelineItemPermissionId = 100
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Calendar, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Calendar, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<CalendarItem> result = await _service.SearchCalendarItems(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        #endregion
    }
}