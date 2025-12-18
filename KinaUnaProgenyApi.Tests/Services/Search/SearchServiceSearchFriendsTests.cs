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
    public class SearchServiceSearchFriendsTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchFriendsTests()
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

            // Add test Friends for progeny search
            Friend friend1 = new()
            {
                FriendId = 1,
                ProgenyId = 1,
                Name = "John Smith",
                Description = "Best friend from school",
                Context = "School",
                Notes = "Met in first grade",
                Tags = "childhood,school",
                FriendAddedDate = DateTime.UtcNow.AddDays(-30),
                FriendSince = DateTime.UtcNow.AddYears(-5),
                CreatedTime = DateTime.UtcNow.AddDays(-30),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.FriendsDb.Add(friend1);

            Friend friend2 = new()
            {
                FriendId = 2,
                ProgenyId = 1,
                Name = "Emma Johnson",
                Description = "Neighbor and playmate",
                Context = "Neighborhood",
                Notes = "Lives next door",
                Tags = "neighbor,playmate",
                FriendAddedDate = DateTime.UtcNow.AddDays(-20),
                FriendSince = DateTime.UtcNow.AddYears(-2),
                CreatedTime = DateTime.UtcNow.AddDays(-20),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.FriendsDb.Add(friend2);

            Friend friend3 = new()
            {
                FriendId = 3,
                ProgenyId = 2,
                Name = "Michael Brown",
                Description = "Friend from daycare",
                Context = "Daycare",
                Notes = "Same age group",
                Tags = "daycare,toddler",
                FriendAddedDate = DateTime.UtcNow.AddDays(-10),
                FriendSince = DateTime.UtcNow.AddYears(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-10),
                Author = "user2",
                CreatedBy = "user2"
            };
            _progenyDbContext.FriendsDb.Add(friend3);

            // Add a friend with null fields to test null handling
            Friend friend4 = new()
            {
                FriendId = 4,
                ProgenyId = 1,
                Name = null,
                Description = null,
                Context = null,
                Notes = null,
                Tags = null,
                FriendAddedDate = DateTime.UtcNow.AddDays(-5),
                CreatedTime = DateTime.UtcNow.AddDays(-5),
                Author = "user1",
                CreatedBy = "user1"
            };
            _progenyDbContext.FriendsDb.Add(friend4);

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

        #region SearchFriends Tests

        [Fact]
        public async Task SearchFriends_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "John",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchFriends_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchFriends_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchFriends_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "John",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("John Smith", result.Results[0].Name);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchFriends_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "John",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchFriends_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "John",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchFriends_SearchesNameField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "Smith",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Smith", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchFriends_SearchesDescriptionField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "school",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("school", result.Results[0].Description);
        }

        [Fact]
        public async Task SearchFriends_SearchesContextField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "neighborhood",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Neighborhood", result.Results[0].Context);
        }

        [Fact]
        public async Task SearchFriends_SearchesNotesField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "first grade",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("first grade", result.Results[0].Notes);
        }

        [Fact]
        public async Task SearchFriends_SearchesTagsField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "childhood",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("childhood", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchFriends_IsCaseInsensitive()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "JOHN",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("John Smith", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchFriends_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  John  ",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("John Smith", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchFriends_HandlesNullFieldsGracefully()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act - should not throw even with null fields in friend4
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SearchFriends_SearchesMultipleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "daycare",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 3, 2, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Michael Brown", result.Results[0].Name);
        }

        [Fact]
        public async Task SearchFriends_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "friend", // Matches multiple items via description
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.True(result.RemainingItems >= 0);
        }

        [Fact]
        public async Task SearchFriends_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Results.Count, result.TotalCount);
        }

        [Fact]
        public async Task SearchFriends_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e", // Matches multiple items
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].FriendAddedDate;
                    DateTime nextTime = result.Results[i + 1].FriendAddedDate;
                    Assert.True(currentTime >= nextTime, "Results should be sorted in descending order by FriendAddedDate");
                }
            }
        }

        [Fact]
        public async Task SearchFriends_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e", // Matches multiple items
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].FriendAddedDate;
                    DateTime nextTime = result.Results[i + 1].FriendAddedDate;
                    Assert.True(currentTime <= nextTime, "Results should be sorted in ascending order by FriendAddedDate");
                }
            }
        }

        [Fact]
        public async Task SearchFriends_SkipsItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // First get all results
            SearchRequest allRequest = new()
            {
                Query = "e",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Skip = 0
            };
            SearchResponse<Friend> allResults = await _service.SearchFriends(allRequest, _testUser);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].FriendId, result.Results[0].FriendId);
            }
        }

        [Fact]
        public async Task SearchFriends_ReturnsAllItemsWhenNumberOfItemsIsZero()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchFriends_ReturnsEmptyWhenNoMatchingItems()
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
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchFriends_SetsItemPermissionOnResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "john",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        [Fact]
        public async Task SearchFriends_DoesNotSearchFamilyItems()
        {
            // Arrange - SearchFriends only searches progenies, not families
            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [],
                FamilyIds = [1],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchFriends_ReturnsEmptyWhenProgenyIdsIsEmpty()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [],
                FamilyIds = [],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchFriends_ReturnsMultipleMatchingFriends()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "e", // Matches "John Smith", "Emma Johnson" (Name), descriptions, etc.
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Results.Count > 1, "Should match multiple friends");
        }

        [Fact]
        public async Task SearchFriends_PartialProgenyPermission_OnlyReturnsAccessibleProgenies()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "friend",
                ProgenyIds = [1, 2],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            // User has permission for progeny 1 but not progeny 2
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Friend, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Friend> result = await _service.SearchFriends(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.All(result.Results, f => Assert.Equal(1, f.ProgenyId));
        }

        #endregion
    }
}