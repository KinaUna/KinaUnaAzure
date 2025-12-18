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
    public class SearchServiceSearchSleepRecordsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchSleepRecordsTests()
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

            // Add test Sleep records for progeny search
            Sleep sleep1 = new()
            {
                SleepId = 1,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddHours(-8),
                SleepEnd = DateTime.UtcNow,
                SleepNotes = "Good night sleep with peaceful dreams",
                SleepRating = 5,
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };
            _progenyDbContext.SleepDb.Add(sleep1);

            Sleep sleep2 = new()
            {
                SleepId = 2,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-1).AddHours(-7),
                SleepEnd = DateTime.UtcNow.AddDays(-1),
                SleepNotes = "Restless night with several wake ups",
                SleepRating = 2,
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                CreatedTime = DateTime.UtcNow.AddDays(-2)
            };
            _progenyDbContext.SleepDb.Add(sleep2);

            Sleep sleep3 = new()
            {
                SleepId = 3,
                ProgenyId = 2,
                SleepStart = DateTime.UtcNow.AddDays(-2).AddHours(-6),
                SleepEnd = DateTime.UtcNow.AddDays(-2),
                SleepNotes = "Afternoon nap was short",
                SleepRating = 3,
                Author = "user2",
                CreatedBy = "user2",
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                CreatedTime = DateTime.UtcNow.AddDays(-3)
            };
            _progenyDbContext.SleepDb.Add(sleep3);

            Sleep sleep4 = new()
            {
                SleepId = 4,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-3).AddHours(-9),
                SleepEnd = DateTime.UtcNow.AddDays(-3),
                SleepNotes = "Excellent deep sleep",
                SleepRating = 5,
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                CreatedTime = DateTime.UtcNow.AddDays(-4)
            };
            _progenyDbContext.SleepDb.Add(sleep4);

            Sleep sleep5 = new()
            {
                SleepId = 5,
                ProgenyId = 1,
                SleepStart = DateTime.UtcNow.AddDays(-4).AddHours(-2),
                SleepEnd = DateTime.UtcNow.AddDays(-4),
                SleepNotes = "Quick nap in the afternoon",
                SleepRating = 4,
                Author = "user1",
                CreatedBy = "user1",
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                CreatedTime = DateTime.UtcNow.AddDays(-5)
            };
            _progenyDbContext.SleepDb.Add(sleep5);

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

        #region SearchSleepRecords Tests

        [Fact]
        public async Task SearchSleepRecords_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchSleepRecords_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchSleepRecords_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchSleepRecords_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "peaceful",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("peaceful", result.Results[0].SleepNotes);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchSleepRecords_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchSleepRecords_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "peaceful",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchSleepRecords_SearchesSleepNotesField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "restless",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Restless", result.Results[0].SleepNotes);
        }

        [Fact]
        public async Task SearchSleepRecords_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "PEACEFUL",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("peaceful", result.Results[0].SleepNotes);
        }

        [Fact]
        public async Task SearchSleepRecords_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  peaceful  ",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("peaceful", result.Results[0].SleepNotes);
        }

        [Fact]
        public async Task SearchSleepRecords_ReturnsMultipleMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count); // "Good night sleep" and "Excellent deep sleep"
        }

        [Fact]
        public async Task SearchSleepRecords_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1,2],
                NumberOfItems = 1,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.True(result.RemainingItems > 0);
        }

        [Fact]
        public async Task SearchSleepRecords_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Results.Count, result.TotalCount);
        }

        [Fact]
        public async Task SearchSleepRecords_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [1],
                NumberOfItems = 25,
                Sort = 0 // Descending (newest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    Assert.True(result.Results[i].SleepStart >= result.Results[i + 1].SleepStart,
                        "Results should be sorted in descending order by SleepStart");
                }
            }
        }

        [Fact]
        public async Task SearchSleepRecords_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [1],
                NumberOfItems = 25,
                Sort = 1 // Ascending (oldest first)
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    Assert.True(result.Results[i].SleepStart <= result.Results[i + 1].SleepStart,
                        "Results should be sorted in ascending order by SleepStart");
                }
            }
        }

        [Fact]
        public async Task SearchSleepRecords_SkipsItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 1,
                Skip = 1
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // First get all results
            SearchRequest allRequest = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 25,
                Skip = 0
            };
            SearchResponse<Sleep> allResults = await _service.SearchSleepRecords(allRequest, _testUser);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].SleepId, result.Results[0].SleepId);
            }
        }

        [Fact]
        public async Task SearchSleepRecords_ReturnsAllItemsWhenNumberOfItemsIsZero()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 0,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchSleepRecords_SearchesMultipleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count); // "Afternoon nap" from progeny 2 + "Quick nap" from progeny 1
        }

        [Fact]
        public async Task SearchSleepRecords_ReturnsEmptyWhenNoMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nonexistentquery12345",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchSleepRecords_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "peaceful",
                ProgenyIds = [1],
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        [Fact]
        public async Task SearchSleepRecords_DoesNotSearchOtherProgenyRecords()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "afternoon",
                ProgenyIds = [1], // Only searching progeny 1, but "Afternoon nap" is in progeny 2
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchSleepRecords_PartialMatchInNotes()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "deep",
                ProgenyIds = [1],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, 4, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, 4, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("deep", result.Results[0].SleepNotes);
        }

        [Fact]
        public async Task SearchSleepRecords_EmptyProgenyIdsList_ReturnsEmptyResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "sleep",
                ProgenyIds = [],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchSleepRecords_CalculatesPageNumberCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 1,
                Skip = 1
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PageNumber); // Skip 1, NumberOfItems 1 -> page 2
        }

        [Fact]
        public async Task SearchSleepRecords_CalculatesRemainingItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "nap",
                ProgenyIds = [1, 2],
                NumberOfItems = 1,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Sleep, It.IsAny<int>(), It.IsAny<int>(), 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Sleep> result = await _service.SearchSleepRecords(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal(result.TotalCount - 1, result.RemainingItems);
        }

        #endregion
    }
}