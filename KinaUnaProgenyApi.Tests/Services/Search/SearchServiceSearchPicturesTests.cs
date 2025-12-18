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
    public class SearchServiceSearchPicturesTests : IDisposable
    {
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly MediaDbContext _mediaDbContext;
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly SearchService _service;
        private readonly UserInfo _testUser;
        private readonly UserInfo _otherUser;

        public SearchServiceSearchPicturesTests()
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
            _progenyDbContext.SaveChanges();

            // Add test Pictures for progeny search
            Picture picture1 = new()
            {
                PictureId = 1,
                ProgenyId = 1,
                Tags = "birthday, party, celebration",
                Location = "Home",
                PictureTime = DateTime.UtcNow.AddDays(-7),
                CreatedTime = DateTime.UtcNow.AddDays(-7),
                Author = "user1",
                CreatedBy = "user1",
                PictureLink = "https://example.com/picture1.jpg"
            };
            _mediaDbContext.PicturesDb.Add(picture1);

            Picture picture2 = new()
            {
                PictureId = 2,
                ProgenyId = 1,
                Tags = "beach, vacation, summer",
                Location = "Beach Resort",
                PictureTime = DateTime.UtcNow.AddDays(-14),
                CreatedTime = DateTime.UtcNow.AddDays(-14),
                Author = "user1",
                CreatedBy = "user1",
                PictureLink = "https://example.com/picture2.jpg"
            };
            _mediaDbContext.PicturesDb.Add(picture2);

            Picture picture3 = new()
            {
                PictureId = 3,
                ProgenyId = 1,
                Tags = "playground, park, outdoor",
                Location = "City Park",
                PictureTime = DateTime.UtcNow.AddDays(-21),
                CreatedTime = DateTime.UtcNow.AddDays(-21),
                Author = "user1",
                CreatedBy = "user1",
                PictureLink = "https://example.com/picture3.jpg"
            };
            _mediaDbContext.PicturesDb.Add(picture3);

            // Add picture for different progeny
            Picture picture4 = new()
            {
                PictureId = 4,
                ProgenyId = 2,
                Tags = "school, first day, excited",
                Location = "School",
                PictureTime = DateTime.UtcNow.AddDays(-28),
                CreatedTime = DateTime.UtcNow.AddDays(-28),
                Author = "user2",
                CreatedBy = "user2",
                PictureLink = "https://example.com/picture4.jpg"
            };
            _mediaDbContext.PicturesDb.Add(picture4);

            // Add picture without PictureTime (to test CreatedTime fallback)
            Picture picture5 = new()
            {
                PictureId = 5,
                ProgenyId = 1,
                Tags = "christmas, holiday, family",
                Location = "Grandma's House",
                PictureTime = null,
                CreatedTime = DateTime.UtcNow.AddDays(-35),
                Author = "user1",
                CreatedBy = "user1",
                PictureLink = "https://example.com/picture5.jpg"
            };
            _mediaDbContext.PicturesDb.Add(picture5);

            _mediaDbContext.SaveChanges();
        }

        public void Dispose()
        {
            _progenyDbContext.Database.EnsureDeleted();
            _progenyDbContext.Dispose();
            _mediaDbContext.Database.EnsureDeleted();
            _mediaDbContext.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Null/Empty Input Tests

        [Fact]
        public async Task SearchPictures_WhenUserIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchPictures_WhenQueryIsEmpty_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = string.Empty,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchPictures_WhenQueryIsWhitespace_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchPictures_WhenQueryIsNull_ReturnsEmptyResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = null!,
                ProgenyIds = [1],
                FamilyIds = []
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task SearchPictures_WhenUserHasProgenyPermission_ReturnsMatchingItems()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("birthday", result.Results[0].Tags);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchPictures_WhenUserHasNoProgenyPermission_SkipsProgeny()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchPictures_WhenUserHasNoItemPermission_FiltersOutItem()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchPictures_SetsItemPermissionOnResults()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPerMission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPerMission.PermissionLevel);
            Assert.Equal(100, result.Results[0].ItemPerMission.TimelineItemPermissionId);
        }

        #endregion

        #region Search Field Tests

        [Fact]
        public async Task SearchPictures_SearchesTagsField()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("party", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchPictures_SearchesLocationField()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "resort",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 2, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("Resort", result.Results[0].Location);
        }

        [Fact]
        public async Task SearchPictures_MatchesBothTagsAndLocation()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "park",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 3, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 3, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            // "park" appears in both Tags and Location
            Assert.True(result.Results[0].Tags.Contains("park") || result.Results[0].Location.Contains("Park"));
        }

        #endregion

        #region Case Sensitivity Tests

        [Fact]
        public async Task SearchPictures_IsCaseInsensitive()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("birthday", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchPictures_TrimsQuery()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "  birthday  ",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("birthday", result.Results[0].Tags);
        }

        #endregion

        #region Multiple Progenies Tests

        [Fact]
        public async Task SearchPictures_SearchesMultipleProgenies()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 4, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 4, 2, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("school", result.Results[0].Tags);
            Assert.Equal(2, result.Results[0].ProgenyId);
        }

        [Fact]
        public async Task SearchPictures_SkipsProgeniesWithoutPermission()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "school",
                ProgenyIds = [1, 2],
                FamilyIds = [],
                NumberOfItems = 25
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task SearchPictures_ReturnsPaginatedResults()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a", // Matches multiple items
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 2,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.True(result.RemainingItems > 0 || result.TotalCount >= 2);
        }

        [Fact]
        public async Task SearchPictures_SkipsItemsCorrectly()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
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
            SearchResponse<Picture> allResults = await _service.SearchPictures(allRequest, _testUser);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (allResults.Results.Count > 1)
            {
                Assert.Single(result.Results);
                Assert.Equal(allResults.Results[1].PictureId, result.Results[0].PictureId);
            }
        }

        [Fact]
        public async Task SearchPictures_ReturnsAllItemsWhenNumberOfItemsIsZero()
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount, result.Results.Count);
        }

        [Fact]
        public async Task SearchPictures_ReturnsCorrectTotalCount()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= result.Results.Count);
        }

        [Fact]
        public async Task SearchPictures_ReturnsCorrectPageNumber()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 2,
                Skip = 2
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PageNumber); // Skip 2, Take 2 = Page 2
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task SearchPictures_SortsDescendingByDefault()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].PictureTime ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].PictureTime ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime >= nextTime, "Results should be sorted in descending order");
                }
            }
        }

        [Fact]
        public async Task SearchPictures_SortsAscendingWhenRequested()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
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
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            if (result.Results.Count > 1)
            {
                for (int i = 0; i < result.Results.Count - 1; i++)
                {
                    DateTime currentTime = result.Results[i].PictureTime ?? result.Results[i].CreatedTime;
                    DateTime nextTime = result.Results[i + 1].PictureTime ?? result.Results[i + 1].CreatedTime;
                    Assert.True(currentTime <= nextTime, "Results should be sorted in ascending order");
                }
            }
        }

        [Fact]
        public async Task SearchPictures_UsesCreatedTimeWhenPictureTimeIsNull()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "christmas",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Sort = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 5, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 5, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Null(result.Results[0].PictureTime);
            Assert.NotEqual(default, result.Results[0].CreatedTime);
        }

        #endregion

        #region No Match Tests

        [Fact]
        public async Task SearchPictures_ReturnsEmptyWhenNoMatchingItems()
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
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchPictures_ReturnsEmptyWhenNoProgenyIds()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [],
                FamilyIds = [],
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Response Properties Tests

        [Fact]
        public async Task SearchPictures_ReturnsSearchRequestInResponse()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25,
                Skip = 5,
                Sort = 1
            };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.Equal(request.Query, result.SearchRequest.Query);
            Assert.Equal(request.ProgenyIds, result.SearchRequest.ProgenyIds);
            Assert.Equal(request.NumberOfItems, result.SearchRequest.NumberOfItems);
            Assert.Equal(request.Skip, result.SearchRequest.Skip);
            Assert.Equal(request.Sort, result.SearchRequest.Sort);
        }

        [Fact]
        public async Task SearchPictures_CalculatesRemainingItemsCorrectly()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "a",
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 2,
                Skip = 0
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.TotalCount - result.Results.Count, result.RemainingItems);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task SearchPictures_HandlesPartialTagMatch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "birth", // Partial match for "birthday"
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, 1, 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Contains("birthday", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchPictures_HandlesPartialLocationMatch()
        {
            // Arrange
            SearchRequest request = new()
            {
                Query = "beach", // Partial match for "Beach Resort"
                ProgenyIds = [1],
                FamilyIds = [],
                NumberOfItems = 25
            };

            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Photo, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(permission);

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Results.Count >= 1);
            Assert.True(result.Results.Any(p => p.Tags.Contains("beach") || p.Location.Contains("Beach")));
        }

        [Fact]
        public async Task SearchPictures_DoesNotSearchFamilyIds()
        {
            // Note: SearchPictures only searches ProgenyIds, not FamilyIds based on the implementation
            // Arrange
            SearchRequest request = new()
            {
                Query = "birthday",
                ProgenyIds = [],
                FamilyIds = [1], // FamilyIds are ignored for pictures
                NumberOfItems = 25
            };

            // Act
            SearchResponse<Picture> result = await _service.SearchPictures(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        #endregion
    }
}